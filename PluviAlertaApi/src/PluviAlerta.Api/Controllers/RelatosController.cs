using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PluviAlerta.Api.Contratos;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Relatos;
using PluviAlerta.Infrastructure.Dados;

namespace PluviAlerta.Api.Controllers;

[ApiController]
[Route("api/relatos")]
public class RelatosController(PluviAlertaDbContext db) : ControllerBase
{
    public const string PoliticaLimite = "relatos";

    [HttpGet]
    [EndpointSummary("Lista relatos da comunidade")]
    [EndpointDescription("ativos=true (padrão): só os que ainda não expiraram. ativos=false: todos das últimas 24 h.")]
    public async Task<ActionResult<IReadOnlyList<RelatoResposta>>> Listar(
        [FromQuery] bool ativos = true, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var consulta = db.Relatos.AsNoTracking();
        consulta = ativos
            ? consulta.Where(r => r.ExpiraEm > agora)
            : consulta.Where(r => r.CriadoEm > agora.AddHours(-24));

        var relatos = await consulta
            .OrderByDescending(r => r.CriadoEm)
            .Take(200)
            .Select(r => ParaResposta(r))
            .ToListAsync(ct);

        return Ok(relatos);
    }

    [HttpPost]
    [EnableRateLimiting(PoliticaLimite)]
    [EndpointSummary("Cria um relato de alagamento ou outro problema")]
    [EndpointDescription("A regional é a do centro mais próximo. O relato expira em 3 h. Limite de envios por IP.")]
    [ProducesResponseType<RelatoResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Criar(CriarRelatoRequest request, CancellationToken ct)
    {
        var (lat, lon) = (request.Latitude!.Value, request.Longitude!.Value);
        if (!RegrasRelato.DentroDeBH(lat, lon))
        {
            ModelState.AddModelError(nameof(request.Latitude), "A coordenada precisa estar dentro de Belo Horizonte.");
            return ValidationProblem(ModelState);
        }

        var regioes = await db.Regioes.AsNoTracking().ToListAsync(ct);
        var agora = DateTime.UtcNow;
        var relato = new Relato
        {
            Latitude = lat,
            Longitude = lon,
            RegiaoId = RegrasRelato.RegiaoMaisProxima(lat, lon, regioes).Id,
            Tipo = request.Tipo!.Value.ToString(),
            Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim(),
            CriadoEm = agora,
            ExpiraEm = agora + RegrasRelato.Validade,
        };

        db.Relatos.Add(relato);
        await db.SaveChangesAsync(ct);

        return Created($"/api/relatos/{relato.Id}", ParaResposta(relato));
    }

    private static RelatoResposta ParaResposta(Relato r) =>
        new(r.Id, r.Latitude, r.Longitude, r.RegiaoId, r.Tipo, r.Descricao, r.CriadoEm, r.ExpiraEm);
}
