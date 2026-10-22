using PluviAlerta.Infrastructure.OpenMeteo;

namespace PluviAlerta.Tests;

public class SomarChuvaTests
{
    // Série como a do Open-Meteo com past_hours=6 e forecast_hours=4, consultada às 02:14 UTC:
    // horas de 20:00 até 05:00. O valor da hora T é a chuva entre T-1h e T.
    private static readonly DateTime Agora = new(2026, 10, 22, 2, 14, 0, DateTimeKind.Utc);

    private static List<DateTime> Horas() =>
        Enumerable.Range(0, 10).Select(i => new DateTime(2026, 10, 21, 20, 0, 0, DateTimeKind.Utc).AddHours(i)).ToList();

    [Fact]
    public void SomaJanelasPeloHorario()
    {
        //                         20   21   22   23   00   01   02   03   04   05
        double?[] mm =           [100, 1,   2,   3,   4,   5,   6,   10,  20,  30];

        var (ultimaHora, acum6h, previsao3h) = OpenMeteoClient.SomarChuva(Horas(), mm, Agora);

        Assert.Equal(6, ultimaHora);   // 02:00 = chuva de 01:00 a 02:00
        Assert.Equal(21, acum6h);      // 21:00..02:00 (o 100 das 20:00 fica de fora)
        Assert.Equal(60, previsao3h);  // 03:00..05:00
    }

    [Fact]
    public void ValorAusenteContaComoZero()
    {
        double?[] mm = [null, null, 1.5, null, null, null, 0.5, null, 2, null];

        var (ultimaHora, acum6h, previsao3h) = OpenMeteoClient.SomarChuva(Horas(), mm, Agora);

        Assert.Equal(0.5, ultimaHora);
        Assert.Equal(2, acum6h);
        Assert.Equal(2, previsao3h);
    }

    [Fact]
    public void ArredondaParaUmaCasa()
    {
        double?[] mm = [0, 0.11, 0.11, 0.11, 0, 0, 0, 0.33, 0.33, 0.33];

        var (_, acum6h, previsao3h) = OpenMeteoClient.SomarChuva(Horas(), mm, Agora);

        Assert.Equal(0.3, acum6h);
        Assert.Equal(1.0, previsao3h);
    }
}
