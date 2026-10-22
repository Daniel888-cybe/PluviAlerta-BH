namespace PluviAlerta.Domain.Risco;

public record EntradaRisco(
    double ChuvaAcum6hMm,
    double Previsao3hMm,
    int Vulnerabilidade,
    int RelatosAtivos);
