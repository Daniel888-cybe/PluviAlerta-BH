using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Alertas;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Infrastructure.Alertas;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.Risco;

namespace PluviAlerta.Infrastructure.Simulacao;

/// <summary>
/// Modo simulação para a demo: injeta uma tempestade fictícia numa regional sem depender de internet.
/// Passa pelo mesmo AtualizadorDeRisco do dado real, então score (e, depois, alerta) saem pelo caminho normal.
/// </summary>
public class SimuladorDeTempestade(PluviAlertaDbContext db, AtualizadorDeRisco atualizador, ServicoDeAlertas alertas)
{
    // Enquanto durar, o worker não sobrescreve a regional com o dado real do Open-Meteo.
    public static readonly TimeSpan Duracao = TimeSpan.FromMinutes(30);

    public async Task<ScoreRisco> SimularAsync(
        int regiaoId, double chuvaAcum6hMm, double previsao3hMm, int relatos, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var leitura = new LeituraChuva
        {
            RegiaoId = regiaoId,
            Momento = agora,
            MmUltimaHora = chuvaAcum6hMm / 6, // só ilustrativo: não entra no score
            MmAcum6h = chuvaAcum6hMm,
            MmPrevisao3h = previsao3hMm,
            Fonte = FonteDados.Simulacao,
            RelatosExtras = relatos, // entram só na conta do score, sem criar pontos no mapa
        };

        var scores = await atualizador.RegistrarAsync([leitura], agora, ct);
        return scores[0];
    }

    /// <summary>Regionais com simulação ainda valendo (o worker pula estas).</summary>
    public async Task<HashSet<int>> RegioesEmSimulacaoAsync(DateTime agoraUtc, CancellationToken ct)
    {
        var limite = agoraUtc - Duracao;
        var ids = await db.ScoresRisco
            .Where(s => s.Fonte == FonteDados.Simulacao && s.Momento > limite)
            .Select(s => s.RegiaoId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    /// <summary>
    /// Apaga todos os dados simulados, inclusive os alertas: senão a regra das 2 h
    /// bloquearia o alerta no próximo ensaio da demo. O /api/regioes volta ao último dado real,
    /// e quem recebeu o alerta da simulação recebe o aviso de "risco reduzido" (fecha o ciclo da demo).
    /// </summary>
    public async Task<int> EncerrarAsync(CancellationToken ct)
    {
        var regioesEmAlerta = await RegioesComSimulacaoEmAlertaAsync(ct);

        var scores = await db.ScoresRisco.Where(s => s.Fonte == FonteDados.Simulacao).ExecuteDeleteAsync(ct);
        await db.LeiturasChuva.Where(l => l.Fonte == FonteDados.Simulacao).ExecuteDeleteAsync(ct);
        await db.AlertasEnviados.Where(a => a.Fonte == FonteDados.Simulacao).ExecuteDeleteAsync(ct);

        foreach (var regiaoId in regioesEmAlerta)
        {
            var real = await db.ScoresRisco.AsNoTracking()
                .Where(s => s.RegiaoId == regiaoId)
                .OrderByDescending(s => s.Momento)
                .FirstOrDefaultAsync(ct);
            if (real is not null && real.Nivel < RegraAlerta.NivelMinimo)
                await alertas.AvisarReducaoAsync(real, ct);
        }

        return scores;
    }

    // Regionais cujo score atual é simulado e está em Laranja/Vermelho.
    private async Task<List<int>> RegioesComSimulacaoEmAlertaAsync(CancellationToken ct)
    {
        var ids = await db.ScoresRisco.AsNoTracking()
            .Where(s => s.Fonte == FonteDados.Simulacao)
            .Select(s => s.RegiaoId)
            .Distinct()
            .ToListAsync(ct);

        var emAlerta = new List<int>();
        foreach (var id in ids)
        {
            var atual = await db.ScoresRisco.AsNoTracking()
                .Where(s => s.RegiaoId == id)
                .OrderByDescending(s => s.Momento)
                .FirstAsync(ct);
            if (atual.Fonte == FonteDados.Simulacao && atual.Nivel >= RegraAlerta.NivelMinimo)
                emAlerta.Add(id);
        }
        return emAlerta;
    }
}
