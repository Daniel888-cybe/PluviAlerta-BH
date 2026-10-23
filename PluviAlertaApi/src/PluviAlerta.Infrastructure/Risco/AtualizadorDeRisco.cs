using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Risco;
using PluviAlerta.Infrastructure.Alertas;
using PluviAlerta.Infrastructure.Dados;

namespace PluviAlerta.Infrastructure.Risco;

/// <summary>
/// Caminho único de "leitura de chuva → score gravado → alerta". Usado pelo worker do Open-Meteo,
/// pela simulação e pelo recálculo após um relato, para tudo passar pelo mesmo código.
/// </summary>
public class AtualizadorDeRisco(PluviAlertaDbContext db, ServicoDeAlertas alertas)
{
    // Recalcular com leitura mais velha que isso seria "alertar com dado velho".
    public static readonly TimeSpan IdadeMaximaParaRecalcular = TimeSpan.FromMinutes(30);

    public async Task<IReadOnlyList<ScoreRisco>> RegistrarAsync(
        IReadOnlyList<LeituraChuva> leituras, DateTime agoraUtc, CancellationToken ct)
    {
        var vulnerabilidades = await db.Regioes.AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Vulnerabilidade, ct);

        var relatosAtivos = await db.Relatos.AsNoTracking()
            .Where(r => r.ExpiraEm > agoraUtc)
            .GroupBy(r => r.RegiaoId)
            .Select(g => new { RegiaoId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.RegiaoId, x => x.Total, ct);

        // Nível antes desta leitura, para saber se "subiu" (regra do alerta).
        var niveisAnteriores = new Dictionary<int, NivelRisco>();
        foreach (var leitura in leituras)
        {
            var anterior = await db.ScoresRisco.AsNoTracking()
                .Where(s => s.RegiaoId == leitura.RegiaoId)
                .OrderByDescending(s => s.Momento)
                .Select(s => (NivelRisco?)s.Nivel)
                .FirstOrDefaultAsync(ct);
            if (anterior is not null)
                niveisAnteriores[leitura.RegiaoId] = anterior.Value;
        }

        var scores = new List<ScoreRisco>();
        foreach (var leitura in leituras)
        {
            var vulnerabilidade = vulnerabilidades[leitura.RegiaoId];
            var relatos = relatosAtivos.GetValueOrDefault(leitura.RegiaoId) + leitura.RelatosExtras;
            var (score, nivel) = MotorDeRisco.Calcular(
                new EntradaRisco(leitura.MmAcum6h, leitura.MmPrevisao3h, vulnerabilidade, relatos));

            db.LeiturasChuva.Add(leitura);
            scores.Add(new ScoreRisco
            {
                RegiaoId = leitura.RegiaoId,
                Momento = agoraUtc,
                Score = score,
                Nivel = nivel,
                ChuvaAcum6hMm = leitura.MmAcum6h,
                Previsao3hMm = leitura.MmPrevisao3h,
                Vulnerabilidade = vulnerabilidade,
                RelatosAtivos = relatos,
                Fonte = leitura.Fonte,
            });
        }

        db.ScoresRisco.AddRange(scores);
        await db.SaveChangesAsync(ct);

        await alertas.AvaliarAsync(scores, niveisAnteriores, agoraUtc, ct);
        return scores;
    }

    /// <summary>
    /// Recalcula uma regional agora, com a mesma chuva da última leitura (ex.: chegou um relato).
    /// Não faz nada se a última leitura tiver mais de 30 min: o relato entra na próxima coleta.
    /// </summary>
    public async Task<ScoreRisco?> RecalcularAsync(int regiaoId, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var ultima = await db.LeiturasChuva.AsNoTracking()
            .Where(l => l.RegiaoId == regiaoId)
            .OrderByDescending(l => l.Momento)
            .FirstOrDefaultAsync(ct);

        if (ultima is null || agora - ultima.Momento > IdadeMaximaParaRecalcular)
            return null;

        var copia = new LeituraChuva
        {
            RegiaoId = regiaoId,
            Momento = agora,
            MmUltimaHora = ultima.MmUltimaHora,
            MmAcum6h = ultima.MmAcum6h,
            MmPrevisao3h = ultima.MmPrevisao3h,
            Fonte = ultima.Fonte,
            RelatosExtras = ultima.RelatosExtras,
        };
        var scores = await RegistrarAsync([copia], agora, ct);
        return scores[0];
    }
}
