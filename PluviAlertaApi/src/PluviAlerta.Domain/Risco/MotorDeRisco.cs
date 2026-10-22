namespace PluviAlerta.Domain.Risco;

/// <summary>
/// Calcula o score de risco (0-100) de uma regional.
/// Pesos e cortes são PROVISÓRIOS: serão ajustados na validação com um dia real de chuva.
/// </summary>
public static class MotorDeRisco
{
    public const decimal PesoChuva = 0.40m;
    public const decimal PesoPrevisao = 0.30m;
    public const decimal PesoVulnerabilidade = 0.20m;
    public const decimal PesoRelatos = 0.10m;

    // Valor que leva cada fator a 100.
    public const decimal ChuvaAcum6hMaximaMm = 60m;
    public const decimal Previsao3hMaximaMm = 40m;
    public const decimal PontosPorRelato = 25m;

    // Score mínimo de cada nível.
    public const int InicioAmarelo = 30;
    public const int InicioLaranja = 55;
    public const int InicioVermelho = 75;

    public static (int Score, NivelRisco Nivel) Calcular(EntradaRisco e)
    {
        // decimal em vez de double: evita que 71,5 vire 71,4999... e arredonde para baixo.
        var chuva = Limitar((decimal)e.ChuvaAcum6hMm / ChuvaAcum6hMaximaMm * 100);
        var previsao = Limitar((decimal)e.Previsao3hMm / Previsao3hMaximaMm * 100);
        var vulnerabilidade = Limitar(e.Vulnerabilidade);
        var relatos = Limitar(e.RelatosAtivos * PontosPorRelato);

        var bruto = PesoChuva * chuva
                  + PesoPrevisao * previsao
                  + PesoVulnerabilidade * vulnerabilidade
                  + PesoRelatos * relatos;

        // AwayFromZero: x,5 sempre sobe (o padrão do .NET arredondaria 54,5 para 54).
        var score = (int)Math.Round(bruto, MidpointRounding.AwayFromZero);
        return (score, ClassificarNivel(score));
    }

    public static NivelRisco ClassificarNivel(int score) => score switch
    {
        >= InicioVermelho => NivelRisco.Vermelho,
        >= InicioLaranja => NivelRisco.Laranja,
        >= InicioAmarelo => NivelRisco.Amarelo,
        _ => NivelRisco.Verde
    };

    private static decimal Limitar(decimal fator) => Math.Clamp(fator, 0m, 100m);
}
