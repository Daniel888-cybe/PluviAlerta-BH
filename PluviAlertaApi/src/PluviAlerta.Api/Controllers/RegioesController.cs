using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PluviAlerta.Api.Contratos;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Risco;
using PluviAlerta.Infrastructure.Dados;

namespace PluviAlerta.Api.Controllers;

[ApiController]
[Route("api/regioes")]
public class RegioesController(PluviAlertaDbContext db) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lista as 9 regionais com score e nível atuais")]
    [EndpointDescription("Último score calculado de cada regional, com as entradas usadas no cálculo.")]
    public async Task<ActionResult<IReadOnlyList<RegiaoAtual>>> Listar(CancellationToken ct)
    {
        var regioes = await db.Regioes.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);

        // Uma consulta por regional (são só 9), usando o índice (RegiaoId, Momento).
        var resultado = new List<RegiaoAtual>();
        foreach (var r in regioes)
        {
            var s = await db.ScoresRisco.AsNoTracking()
                .Where(x => x.RegiaoId == r.Id)
                .OrderByDescending(x => x.Momento)
                .FirstOrDefaultAsync(ct);

            resultado.Add(s is null
                ? new RegiaoAtual(r.Id, r.Nome, r.Latitude, r.Longitude, r.Vulnerabilidade,
                    0, NivelRisco.Verde, 0, 0, 0, null, FonteDados.SemDados)
                : new RegiaoAtual(r.Id, r.Nome, r.Latitude, r.Longitude, s.Vulnerabilidade,
                    s.Score, s.Nivel, s.ChuvaAcum6hMm, s.Previsao3hMm, s.RelatosAtivos, s.Momento, s.Fonte));
        }

        return Ok(resultado);
    }

    [HttpGet("{id:int}/historico")]
    [EndpointSummary("Score e chuva das últimas 24 h de uma regional")]
    [EndpointDescription("Um ponto por coleta (a cada 15 min), do mais antigo para o mais recente.")]
    [ProducesResponseType<HistoricoRegiao>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricoRegiao>> Historico(int id, CancellationToken ct)
    {
        var regiao = await db.Regioes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (regiao is null)
            return NotFound();

        var desde = DateTime.UtcNow.AddHours(-24);
        var pontos = await db.ScoresRisco.AsNoTracking()
            .Where(s => s.RegiaoId == id && s.Momento >= desde)
            .OrderBy(s => s.Momento)
            .Select(s => new PontoHistorico(s.Momento, s.Score, s.Nivel, s.ChuvaAcum6hMm, s.Previsao3hMm))
            .ToListAsync(ct);

        var fonte = await db.ScoresRisco.AsNoTracking()
            .Where(s => s.RegiaoId == id)
            .OrderByDescending(s => s.Momento)
            .Select(s => s.Fonte)
            .FirstOrDefaultAsync(ct) ?? FonteDados.SemDados;

        return Ok(new HistoricoRegiao(regiao.Id, regiao.Nome, fonte, pontos));
    }
}
