using PluviAlerta.Domain.Entidades;

namespace PluviAlerta.Domain.Relatos;

public static class RegrasRelato
{
    // Quanto tempo um relato conta no risco. Os textos do bot e da API leem daqui.
    // (No front, a mesma constante está em src/api.ts: VALIDADE_RELATO_HORAS.)
    public const int ValidadeHoras = 1;
    public static readonly TimeSpan Validade = TimeSpan.FromHours(ValidadeHoras);

    // Retângulo APROXIMADO em volta do município de BH (com folga). Não é o limite oficial:
    // serve só para barrar coordenadas claramente fora da cidade.
    public const double LatitudeMin = -20.07;
    public const double LatitudeMax = -19.76;
    public const double LongitudeMin = -44.07;
    public const double LongitudeMax = -43.85;

    public static bool DentroDeBH(double latitude, double longitude) =>
        latitude is >= LatitudeMin and <= LatitudeMax &&
        longitude is >= LongitudeMin and <= LongitudeMax;

    /// <summary>
    /// Regional cujo ponto central está mais perto do relato.
    /// Aproximação até termos os polígonos (GeoJson) das regionais.
    /// </summary>
    public static Regiao RegiaoMaisProxima(double latitude, double longitude, IEnumerable<Regiao> regioes) =>
        regioes.MinBy(r => Math.Pow(r.Latitude - latitude, 2) + Math.Pow(r.Longitude - longitude, 2))
        ?? throw new InvalidOperationException("Nenhuma regional cadastrada.");
}
