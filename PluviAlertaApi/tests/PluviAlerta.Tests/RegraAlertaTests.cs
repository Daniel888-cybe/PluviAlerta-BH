using PluviAlerta.Domain.Alertas;
using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Tests;

public class RegraAlertaTests
{
    private static readonly DateTime Agora = new(2026, 10, 22, 18, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(NivelRisco.Verde, NivelRisco.Laranja, true)]
    [InlineData(NivelRisco.Amarelo, NivelRisco.Laranja, true)]
    [InlineData(NivelRisco.Verde, NivelRisco.Vermelho, true)]
    [InlineData(NivelRisco.Laranja, NivelRisco.Vermelho, true)]
    [InlineData(NivelRisco.Verde, NivelRisco.Amarelo, false)]   // subiu, mas não chegou a Laranja
    [InlineData(NivelRisco.Laranja, NivelRisco.Laranja, false)] // continuou igual
    [InlineData(NivelRisco.Vermelho, NivelRisco.Vermelho, false)]
    [InlineData(NivelRisco.Vermelho, NivelRisco.Laranja, false)] // desceu
    [InlineData(NivelRisco.Laranja, NivelRisco.Verde, false)]
    public void SoAlertaQuandoSobeParaLaranjaOuVermelho(NivelRisco anterior, NivelRisco atual, bool esperado)
    {
        Assert.Equal(esperado, RegraAlerta.DeveAlertar(anterior, atual, null, Agora));
    }

    [Theory]
    [InlineData(NivelRisco.Laranja, true)]
    [InlineData(NivelRisco.Vermelho, true)]
    [InlineData(NivelRisco.Amarelo, false)]
    public void PrimeiraLeitura_ContaComoSubida(NivelRisco atual, bool esperado)
    {
        Assert.Equal(esperado, RegraAlerta.DeveAlertar(null, atual, null, Agora));
    }

    [Theory]
    [InlineData(10, false)]   // alertou há 10 min
    [InlineData(119, false)]  // há 1h59
    [InlineData(120, true)]   // há exatamente 2 h: pode alertar de novo
    [InlineData(300, true)]
    public void NaoRepeteMesmoNivelEmMenosDe2h(int minutosDesdeUltimoAlerta, bool esperado)
    {
        var ultimo = Agora.AddMinutes(-minutosDesdeUltimoAlerta);

        Assert.Equal(esperado, RegraAlerta.DeveAlertar(NivelRisco.Laranja, NivelRisco.Vermelho, ultimo, Agora));
    }
}
