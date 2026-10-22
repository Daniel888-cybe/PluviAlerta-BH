using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Api.Contratos;

/// <summary>
/// Situação atual de uma regional. Traz as entradas do cálculo junto do score,
/// para o front conseguir explicar de onde o número veio.
/// Sem nenhuma leitura ainda: Fonte = "sem-dados" e AtualizadoEm = null.
/// </summary>
public record RegiaoAtual(
    int Id,
    string Nome,
    double Latitude,
    double Longitude,
    int Vulnerabilidade,
    int Score,
    NivelRisco Nivel,
    double ChuvaAcum6hMm,
    double Previsao3hMm,
    int RelatosAtivos,
    DateTime? AtualizadoEm,
    string Fonte);
