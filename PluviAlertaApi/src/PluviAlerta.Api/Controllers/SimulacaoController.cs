using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Risco;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.Simulacao;

namespace PluviAlerta.Api.Controllers;

/// <summary>Padrão: a tempestade da demo (60 mm em 6 h, 40 mm previstos, 4 relatos).</summary>
public record SimulacaoRequest(
    [Required] int? RegiaoId,
    [Range(0, 500)] double ChuvaAcum6hMm = 60,
    [Range(0, 500)] double Previsao3hMm = 40,
    [Range(0, 20)] int Relatos = 4);

public record SimulacaoResposta(
    int RegiaoId, string Nome, int Score, NivelRisco Nivel, DateTime SimuladoEm, DateTime ValidaAte);

[ApiController]
[Route("api/simulacao")]
[Authorize(Roles = "admin")]
public class SimulacaoController(PluviAlertaDbContext db, SimuladorDeTempestade simulador) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Injeta uma tempestade fictícia numa regional (demo)")]
    [EndpointDescription("Funciona sem internet. Por 30 min o worker não sobrescreve essa regional com o dado real.")]
    [ProducesResponseType<SimulacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Simular(SimulacaoRequest request, CancellationToken ct)
    {
        var regiao = await db.Regioes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RegiaoId, ct);
        if (regiao is null)
            return NotFound();

        var s = await simulador.SimularAsync(
            regiao.Id, request.ChuvaAcum6hMm, request.Previsao3hMm, request.Relatos, ct);

        return Ok(new SimulacaoResposta(
            regiao.Id, regiao.Nome, s.Score, s.Nivel, s.Momento, s.Momento + SimuladorDeTempestade.Duracao));
    }

    [HttpDelete]
    [EndpointSummary("Encerra todas as simulações e volta ao dado real")]
    [EndpointDescription("Apaga leituras e scores simulados. Útil para ensaiar a demo de novo.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Encerrar(CancellationToken ct)
    {
        var apagados = await simulador.EncerrarAsync(ct);
        return Ok(new { scoresSimuladosApagados = apagados });
    }
}
