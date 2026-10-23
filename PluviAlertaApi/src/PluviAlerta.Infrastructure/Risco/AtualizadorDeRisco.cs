using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Risco;
using PluviAlerta.Infrastructure.Dados;

namespace PluviAlerta.Infrastructure.Risco;

/// <summary>
/// Caminho único de "leitura de chuva → score gravado". Usado pelo worker do Open-Meteo
/// e (depois) pela simulação, para a demo passar exatamente pelo mesmo código do dado real.
/// </summary>
public class AtualizadorDeRisco(PluviAlertaDbContext db)
{
    /// <param name="relatosExtras">Relatos fictícios somados aos reais (só a simulação usa).</param>
    public async Task<IReadOnlyList<ScoreRisco>> RegistrarAsync(
        IReadOnlyList<LeituraChuva> leituras, DateTime agoraUtc, CancellationToken ct, int relatosExtras = 0)
    {
        var vulnerabilidades = await db.Regioes.AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Vulnerabilidade, ct);

        var relatosAtivos = await db.Relatos.AsNoTracking()
            .Where(r => r.ExpiraEm > agoraUtc)
            .GroupBy(r => r.RegiaoId)
            .Select(g => new { RegiaoId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.RegiaoId, x => x.Total, ct);

        var scores = new List<ScoreRisco>();
        foreach (var leitura in leituras)
        {
            var vulnerabilidade = vulnerabilidades[leitura.RegiaoId];
            var relatos = relatosAtivos.GetValueOrDefault(leitura.RegiaoId) + relatosExtras;
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
        return scores;
    }
}
