namespace PluviAlerta.Domain.Entidades;

public class Regiao
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // 0-100. Valor PROVISÓRIO definido pelo grupo, não é dado oficial.
    public int Vulnerabilidade { get; set; }

    public string? GeoJson { get; set; }
}
