using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.Risco;

namespace PluviAlerta.Infrastructure.Simulacao;

/// <summary>
/// Modo simulação para a demo: injeta uma tempestade fictícia numa regional sem depender de internet.
/// Passa pelo mesmo AtualizadorDeRisco do dado real, então score (e, depois, alerta) saem pelo caminho normal.
/// </summary>
public class SimuladorDeTempestade(PluviAlertaDbContext db, AtualizadorDeRisco atualizador)
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
        };

        var scores = await atualizador.RegistrarAsync([leitura], agora, ct, relatosExtras: relatos);
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

    /// <summary>Apaga todos os dados simulados. O /api/regioes volta a mostrar o último dado real.</summary>
    public async Task<int> EncerrarAsync(CancellationToken ct)
    {
        var scores = await db.ScoresRisco.Where(s => s.Fonte == FonteDados.Simulacao).ExecuteDeleteAsync(ct);
        await db.LeiturasChuva.Where(l => l.Fonte == FonteDados.Simulacao).ExecuteDeleteAsync(ct);
        return scores;
    }
}
