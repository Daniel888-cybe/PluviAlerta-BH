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
}
