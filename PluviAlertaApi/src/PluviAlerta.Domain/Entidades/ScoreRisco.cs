using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Domain.Entidades;

/// <summary>
/// Score calculado num momento. Guarda também as entradas usadas no cálculo
/// (o "recibo"), para poder explicar qualquer score sem recalcular.
/// </summary>
public class ScoreRisco
{
    public int Id { get; set; }
    public int RegiaoId { get; set; }
    public DateTime Momento { get; set; } // UTC
    public int Score { get; set; }
    public NivelRisco Nivel { get; set; }

    public double ChuvaAcum6hMm { get; set; }
    public double Previsao3hMm { get; set; }
    public int Vulnerabilidade { get; set; }
    public int RelatosAtivos { get; set; }
    public string Fonte { get; set; } = FonteDados.OpenMeteo;
}
