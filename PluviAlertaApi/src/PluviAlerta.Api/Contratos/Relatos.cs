using System.ComponentModel.DataAnnotations;
using PluviAlerta.Domain.Relatos;

namespace PluviAlerta.Api.Contratos;

public record CriarRelatoRequest(
    [Required] double? Latitude,
    [Required] double? Longitude,
    [Required] TipoRelato? Tipo,
    [MaxLength(280)] string? Descricao);

public record RelatoResposta(
    int Id,
    double Latitude,
    double Longitude,
    int RegiaoId,
    string Tipo,
    string? Descricao,
    DateTime CriadoEm,
    DateTime ExpiraEm);
