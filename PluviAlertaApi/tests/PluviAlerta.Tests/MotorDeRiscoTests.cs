using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Tests;

public class MotorDeRiscoTests
{
    // Casos obrigatórios, conferidos à mão pelo grupo.
    [Theory]
    [InlineData(45, 30, 70, 2, 72, NivelRisco.Laranja)]  // 71,5 antes de arredondar
    [InlineData(60, 40, 80, 4, 96, NivelRisco.Vermelho)]
    [InlineData(5, 2, 40, 0, 13, NivelRisco.Verde)]
    public void Calcular_CasosObrigatorios(
        double chuva, double previsao, int vuln, int relatos, int scoreEsperado, NivelRisco nivelEsperado)
    {
        var (score, nivel) = MotorDeRisco.Calcular(new EntradaRisco(chuva, previsao, vuln, relatos));

        Assert.Equal(scoreEsperado, score);
        Assert.Equal(nivelEsperado, nivel);
    }

    // Bordas entre níveis, passando pelo cálculo completo.
    [Theory]
    [InlineData(13.5, 0, 100, 0, 29, NivelRisco.Verde)]
    [InlineData(15, 0, 100, 0, 30, NivelRisco.Amarelo)]
    [InlineData(51, 0, 100, 0, 54, NivelRisco.Amarelo)]
    [InlineData(52.5, 0, 100, 0, 55, NivelRisco.Laranja)]
    [InlineData(60, 40, 20, 0, 74, NivelRisco.Laranja)]
    [InlineData(60, 40, 25, 0, 75, NivelRisco.Vermelho)]
    public void Calcular_BordasEntreNiveis(
        double chuva, double previsao, int vuln, int relatos, int scoreEsperado, NivelRisco nivelEsperado)
    {
        var (score, nivel) = MotorDeRisco.Calcular(new EntradaRisco(chuva, previsao, vuln, relatos));

        Assert.Equal(scoreEsperado, score);
        Assert.Equal(nivelEsperado, nivel);
    }

    [Theory]
    [InlineData(0, NivelRisco.Verde)]
    [InlineData(29, NivelRisco.Verde)]
    [InlineData(30, NivelRisco.Amarelo)]
    [InlineData(54, NivelRisco.Amarelo)]
    [InlineData(55, NivelRisco.Laranja)]
    [InlineData(74, NivelRisco.Laranja)]
    [InlineData(75, NivelRisco.Vermelho)]
    [InlineData(100, NivelRisco.Vermelho)]
    public void ClassificarNivel_RespeitaOsCortes(int score, NivelRisco esperado)
    {
        Assert.Equal(esperado, MotorDeRisco.ClassificarNivel(score));
    }

    [Fact]
    public void Calcular_MeioPontoSobe_54e5ViraLaranja()
    {
        // 32 (chuva) + 0 + 20 (vuln) + 2,5 (1 relato) = 54,5
        var (score, nivel) = MotorDeRisco.Calcular(new EntradaRisco(48, 0, 100, 1));

        Assert.Equal(55, score);
        Assert.Equal(NivelRisco.Laranja, nivel);
    }

    [Theory]
    [InlineData(200, 0, 0, 0, 40)]  // chuva limitada a 100 no fator: 0,40 × 100
    [InlineData(0, 500, 0, 0, 30)]  // previsão limitada: 0,30 × 100
    [InlineData(0, 0, 0, 10, 10)]   // relatos limitados: 0,10 × 100
    [InlineData(200, 500, 100, 10, 100)]
    public void Calcular_ValoresAcimaDoLimite_NaoPassamDe100NoFator(
        double chuva, double previsao, int vuln, int relatos, int scoreEsperado)
    {
        var (score, _) = MotorDeRisco.Calcular(new EntradaRisco(chuva, previsao, vuln, relatos));

        Assert.Equal(scoreEsperado, score);
    }

    [Fact]
    public void Calcular_ValoresNegativos_ViramZero()
    {
        var (score, nivel) = MotorDeRisco.Calcular(new EntradaRisco(-10, -5, -20, -3));

        Assert.Equal(0, score);
        Assert.Equal(NivelRisco.Verde, nivel);
    }
}
