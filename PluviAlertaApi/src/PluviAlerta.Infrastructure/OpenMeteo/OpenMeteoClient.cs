using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PluviAlerta.Domain.Entidades;

namespace PluviAlerta.Infrastructure.OpenMeteo;

/// <summary>
/// Busca chuva das últimas 6 h e previsão das próximas 3 h no Open-Meteo (dado de modelo, não pluviômetro).
/// Uma requisição só para todas as regionais: a API aceita várias coordenadas separadas por vírgula.
/// </summary>
public class OpenMeteoClient(HttpClient http)
{
    public async Task<IReadOnlyList<LeituraChuva>> BuscarAsync(
        IReadOnlyList<Regiao> regioes, DateTime agoraUtc, CancellationToken ct)
    {
        var lat = string.Join(",", regioes.Select(r => r.Latitude.ToString(CultureInfo.InvariantCulture)));
        var lon = string.Join(",", regioes.Select(r => r.Longitude.ToString(CultureInfo.InvariantCulture)));

        // past_hours=6 e forecast_hours=4 cobrem com folga a janela usada em SomarChuva.
        var url = $"v1/forecast?latitude={lat}&longitude={lon}" +
                  "&hourly=precipitation&past_hours=6&forecast_hours=4&timeformat=unixtime";

        // Com mais de uma coordenada a resposta é uma lista, na mesma ordem do pedido.
        var respostas = await http.GetFromJsonAsync<List<RespostaOpenMeteo>>(url, ct)
            ?? throw new InvalidOperationException("Open-Meteo respondeu vazio.");
        if (respostas.Count != regioes.Count)
            throw new InvalidOperationException(
                $"Open-Meteo devolveu {respostas.Count} locais, esperado {regioes.Count}.");

        return regioes.Zip(respostas, (regiao, resposta) =>
        {
            var horas = resposta.Hourly.Time.Select(t => DateTimeOffset.FromUnixTimeSeconds(t).UtcDateTime).ToList();
            var (ultimaHora, acum6h, previsao3h) = SomarChuva(horas, resposta.Hourly.Precipitation, agoraUtc);
            return new LeituraChuva
            {
                RegiaoId = regiao.Id,
                Momento = agoraUtc,
                MmUltimaHora = ultimaHora,
                MmAcum6h = acum6h,
                MmPrevisao3h = previsao3h,
                Fonte = FonteDados.OpenMeteo,
            };
        }).ToList();
    }

    /// <summary>
    /// No Open-Meteo, o valor da hora T é a chuva entre T-1h e T.
    /// Com H = hora cheia atual: última hora = valor de H; acumulado 6 h = valores de H-5h até H;
    /// previsão 3 h = valores de H+1h até H+3h. Valor ausente (null) conta como 0.
    /// </summary>
    public static (double UltimaHora, double Acum6h, double Previsao3h) SomarChuva(
        IReadOnlyList<DateTime> horasUtc, IReadOnlyList<double?> mm, DateTime agoraUtc)
    {
        var h = new DateTime(agoraUtc.Year, agoraUtc.Month, agoraUtc.Day, agoraUtc.Hour, 0, 0, DateTimeKind.Utc);

        double Somar(DateTime de, DateTime ate) => horasUtc
            .Select((hora, i) => (hora, valor: mm[i] ?? 0))
            .Where(x => x.hora >= de && x.hora <= ate)
            .Sum(x => x.valor);

        return (
            Math.Round(Somar(h, h), 1),
            Math.Round(Somar(h.AddHours(-5), h), 1),
            Math.Round(Somar(h.AddHours(1), h.AddHours(3)), 1));
    }

    private record RespostaOpenMeteo([property: JsonPropertyName("hourly")] SerieHoraria Hourly);

    private record SerieHoraria(
        [property: JsonPropertyName("time")] List<long> Time,
        [property: JsonPropertyName("precipitation")] List<double?> Precipitation);
}
