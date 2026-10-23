namespace PluviAlerta.Domain.Entidades;

public class LeituraChuva
{
    public int Id { get; set; }
    public int RegiaoId { get; set; }
    public DateTime Momento { get; set; } // UTC
    public double MmUltimaHora { get; set; }
    public double MmAcum6h { get; set; }
    public double MmPrevisao3h { get; set; }
    public string Fonte { get; set; } = FonteDados.OpenMeteo;

    // Relatos fictícios da simulação (0 no dado real). Ficam na leitura para um recálculo não perdê-los.
    public int RelatosExtras { get; set; }
}
