namespace PluviAlerta.Domain.Entidades;

/// <summary>De onde veio a leitura de chuva que gerou um score.</summary>
public static class FonteDados
{
    public const string OpenMeteo = "open-meteo";
    public const string Simulacao = "simulacao";
    public const string SemDados = "sem-dados";
}
