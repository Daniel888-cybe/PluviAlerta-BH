using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Relatos;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.Risco;

namespace PluviAlerta.Infrastructure.Relatos;

public record RelatoCriado(Relato Relato, string NomeRegiao, ScoreRisco? ScoreRecalculado);

/// <summary>
/// Cria relato (site ou Telegram): valida, acha a regional, grava com validade de 3 h
/// e já recalcula o score da regional, para o número subir na hora.
/// </summary>
public class ServicoDeRelatos(PluviAlertaDbContext db, AtualizadorDeRisco atualizador)
{
    /// <returns>null se a coordenada estiver fora de BH.</returns>
    public async Task<RelatoCriado?> CriarAsync(
        double latitude, double longitude, TipoRelato tipo, string? descricao, CancellationToken ct)
    {
        if (!RegrasRelato.DentroDeBH(latitude, longitude))
            return null;

        var regioes = await db.Regioes.AsNoTracking().ToListAsync(ct);
        var regiao = RegrasRelato.RegiaoMaisProxima(latitude, longitude, regioes);
        var agora = DateTime.UtcNow;

        var relato = new Relato
        {
            Latitude = latitude,
            Longitude = longitude,
            RegiaoId = regiao.Id,
            Tipo = tipo.ToString(),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            CriadoEm = agora,
            ExpiraEm = agora + RegrasRelato.Validade,
        };
        db.Relatos.Add(relato);
        await db.SaveChangesAsync(ct);

        var score = await atualizador.RecalcularAsync(regiao.Id, ct);
        return new RelatoCriado(relato, regiao.Nome, score);
    }
}
