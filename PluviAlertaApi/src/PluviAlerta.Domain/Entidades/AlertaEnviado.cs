using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Domain.Entidades;

public class AlertaEnviado
{
    public int Id { get; set; }
    public int RegiaoId { get; set; }
    public NivelRisco Nivel { get; set; }
    public DateTime EnviadoEm { get; set; } // UTC

    // Para o DELETE /api/simulacao conseguir apagar só os alertas da demo.
    public string Fonte { get; set; } = FonteDados.OpenMeteo;
}
