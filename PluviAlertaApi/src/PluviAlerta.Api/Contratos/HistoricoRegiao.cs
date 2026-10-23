using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Api.Contratos;

public record HistoricoRegiao(
    int RegiaoId,
    string Nome,
    string Fonte,
    IReadOnlyList<PontoHistorico> Pontos);

public record PontoHistorico(
    DateTime Momento,
    int Score,
    NivelRisco Nivel,
    double ChuvaAcum6hMm,
    double Previsao3hMm);
