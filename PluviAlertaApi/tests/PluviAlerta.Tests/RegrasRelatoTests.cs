using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Relatos;

namespace PluviAlerta.Tests;

public class RegrasRelatoTests
{
    [Theory]
    [InlineData(-19.93, -43.94, true)]   // perto do centro de BH
    [InlineData(-23.55, -46.63, false)]  // São Paulo
    [InlineData(-19.93, -43.80, false)]  // a leste do retângulo
    [InlineData(0, 0, false)]
    public void DentroDeBH(double lat, double lon, bool esperado)
    {
        Assert.Equal(esperado, RegrasRelato.DentroDeBH(lat, lon));
    }

    [Fact]
    public void RegiaoMaisProxima_EscolheOCentroMaisPerto()
    {
        var regioes = new[]
        {
            new Regiao { Id = 1, Nome = "A", Latitude = -19.90, Longitude = -43.90 },
            new Regiao { Id = 2, Nome = "B", Latitude = -19.80, Longitude = -43.95 },
        };

        Assert.Equal(2, RegrasRelato.RegiaoMaisProxima(-19.81, -43.96, regioes).Id);
        Assert.Equal(1, RegrasRelato.RegiaoMaisProxima(-19.91, -43.89, regioes).Id);
    }

    [Fact]
    public void RegiaoMaisProxima_SemRegioes_Falha()
    {
        Assert.Throws<InvalidOperationException>(() => RegrasRelato.RegiaoMaisProxima(-19.9, -43.9, []));
    }
}
