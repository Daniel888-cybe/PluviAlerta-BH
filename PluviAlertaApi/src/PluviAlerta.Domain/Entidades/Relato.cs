namespace PluviAlerta.Domain.Entidades;

public class Relato
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RegiaoId { get; set; }
    public string Tipo { get; set; } = "";
    public string? Descricao { get; set; }
    public DateTime CriadoEm { get; set; } // UTC
    public DateTime ExpiraEm { get; set; } // UTC
}
