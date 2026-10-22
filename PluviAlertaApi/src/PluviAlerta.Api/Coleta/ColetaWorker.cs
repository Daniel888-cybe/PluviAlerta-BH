using Microsoft.EntityFrameworkCore;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.OpenMeteo;
using PluviAlerta.Infrastructure.Risco;
using PluviAlerta.Infrastructure.Simulacao;

namespace PluviAlerta.Api.Coleta;

/// <summary>
/// Ao subir e depois a cada 15 min: busca chuva no Open-Meteo e grava leitura + score de cada regional.
/// Se o Open-Meteo falhar, só registra no log: o último score continua valendo e nada novo é gerado
/// (portanto nenhum alerta sai com dado velho).
/// </summary>
public class ColetaWorker(
    IServiceScopeFactory scopes,
    IConfiguration config,
    ILogger<ColetaWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!config.GetValue("Coleta:Habilitada", true))
        {
            log.LogWarning("Coleta do Open-Meteo desabilitada (Coleta:Habilitada = false).");
            return;
        }

        var intervalo = TimeSpan.FromMinutes(config.GetValue("Coleta:IntervaloMinutos", 15));
        using var timer = new PeriodicTimer(intervalo);
        do
        {
            await ColetarAsync(ct);
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task ColetarAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PluviAlertaDbContext>();
            var openMeteo = scope.ServiceProvider.GetRequiredService<OpenMeteoClient>();
            var atualizador = scope.ServiceProvider.GetRequiredService<AtualizadorDeRisco>();
            var simulador = scope.ServiceProvider.GetRequiredService<SimuladorDeTempestade>();

            var agora = DateTime.UtcNow;
            var regioes = await db.Regioes.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
            var leituras = await openMeteo.BuscarAsync(regioes, agora, ct);

            // Não sobrescreve regional com simulação em andamento (senão a demo some no meio).
            var emSimulacao = await simulador.RegioesEmSimulacaoAsync(agora, ct);
            if (emSimulacao.Count > 0)
                log.LogInformation("Pulando regionais em simulação: {Ids}", string.Join(", ", emSimulacao));

            var scores = await atualizador.RegistrarAsync(
                leituras.Where(l => !emSimulacao.Contains(l.RegiaoId)).ToList(), agora, ct);

            log.LogInformation("Coleta ok: {Resumo}",
                string.Join(", ", scores.Select(s => $"{s.RegiaoId}={s.Score}/{s.Nivel}")));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // API desligando: sai sem logar erro.
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Falha na coleta. Mantendo o último dado; nova tentativa no próximo ciclo.");
        }
    }
}
