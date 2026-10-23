using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PluviAlerta.Domain.Alertas;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Risco;
using PluviAlerta.Infrastructure.Dados;

namespace PluviAlerta.Infrastructure.Alertas;

/// <summary>
/// Depois de cada novo score: aplica a RegraAlerta, registra o AlertaEnviado e avisa os inscritos da regional.
/// </summary>
public class ServicoDeAlertas(PluviAlertaDbContext db, INotificadorAlerta notificador, ILogger<ServicoDeAlertas> log)
{
    public async Task AvaliarAsync(
        IReadOnlyList<ScoreRisco> novos, IReadOnlyDictionary<int, NivelRisco> niveisAnteriores,
        DateTime agoraUtc, CancellationToken ct)
    {
        foreach (var s in novos)
        {
            var ultimoMesmoNivel = await db.AlertasEnviados
                .Where(a => a.RegiaoId == s.RegiaoId && a.Nivel == s.Nivel)
                .MaxAsync(a => (DateTime?)a.EnviadoEm, ct);

            var anterior = niveisAnteriores.TryGetValue(s.RegiaoId, out var n) ? n : (NivelRisco?)null;

            // Saiu do alerta: avisa que o risco baixou (não entra em AlertasEnviados, que controla só as subidas).
            if (RegraAlerta.DeveAvisarReducao(anterior, s.Nivel))
            {
                await NotificarAsync(s, reducao: true, ct);
                continue;
            }

            if (!RegraAlerta.DeveAlertar(anterior, s.Nivel, ultimoMesmoNivel, agoraUtc))
                continue;

            // Registra antes de enviar: se o Telegram falhar, não fica tentando de novo a cada 15 min.
            db.AlertasEnviados.Add(new AlertaEnviado
            {
                RegiaoId = s.RegiaoId, Nivel = s.Nivel, EnviadoEm = agoraUtc, Fonte = s.Fonte,
            });
            await db.SaveChangesAsync(ct);

            await NotificarAsync(s, reducao: false, ct);
        }
    }

    /// <summary>Avisa "risco reduzido" aos inscritos (usado também ao encerrar uma simulação).</summary>
    public Task AvisarReducaoAsync(ScoreRisco atual, CancellationToken ct) => NotificarAsync(atual, reducao: true, ct);

    private async Task NotificarAsync(ScoreRisco s, bool reducao, CancellationToken ct)
    {
        var nome = await db.Regioes.Where(r => r.Id == s.RegiaoId).Select(r => r.Nome).FirstAsync(ct);
        var chatIds = await db.Inscricoes
            .Where(i => i.RegiaoId == s.RegiaoId)
            .Select(i => i.TelegramChatId)
            .ToListAsync(ct);

        try
        {
            await notificador.NotificarAsync(new Alerta(nome, s, reducao), chatIds, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Falha ao enviar alerta da regional {Regiao}.", nome);
        }
    }
}
