
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

// IDs das nove regionais
int[] ID = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

// Nomes das regionais
string[] Regioes =
{
    "Barreiro",
    "Centro-Sul",
    "Leste",
    "Nordeste",
    "Noroeste",
    "Norte",
    "Oeste",
    "Pampulha",
    "Venda Nova"
};

// Coordenadas aproximadas de cada regional
double[] Latitudes =
{
    -19.975, -19.930, -19.910,
    -19.860, -19.900, -19.820,
    -19.930, -19.850, -19.810
};

double[] Longitudes =
{
    -44.025, -43.940, -43.900,
    -43.910, -43.970, -43.950,
    -44.000, -43.970, -43.960
};

// Rota 1: listar todas as regionais
app.MapGet("/api/regioes", () =>
{
    var resultado = ID.Zip(Regioes, (id, nome) => new
    {
        id,
        nome
    });

    return Results.Ok(resultado);
});

// Rota 2: buscar uma regional pelo ID
app.MapGet("/api/regioes/{id:int}", (int id) =>
{
    int indice = Array.IndexOf(ID, id);

    if (indice == -1)
    {
        return Results.NotFound(new
        {
            mensagem = "Regional não encontrada."
        });
    }

    return Results.Ok(new
    {
        id = ID[indice],
        nome = Regioes[indice]
    });
});

// Rota 3: consultar dados meteorológicos
app.MapGet("/api/chuva/{id:int}", async (
    int id,
    IHttpClientFactory httpClientFactory) =>
{
    int indice = Array.IndexOf(ID, id);

    if (indice == -1)
    {
        return Results.NotFound(new
        {
            mensagem = "Regional não encontrada."
        });
    }

    double latitude = Latitudes[indice];
    double longitude = Longitudes[indice];

    string url =
        $"https://api.open-meteo.com/v1/forecast" +
        $"?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
        $"&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
        "&current=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m" +
        "&timezone=America%2FSao_Paulo";

    try
    {
        var http = httpClientFactory.CreateClient();

        var dados = await http.GetFromJsonAsync<RespostaMeteorologica>(url);

        if (dados?.Current == null)
        {
            return Results.Problem(
                "A API meteorológica não retornou os dados esperados.",
                statusCode: 502);
        }

        double chuva = dados.Current.Precipitation;

        // Classificação ilustrativa, não oficial
        string risco = chuva switch
        {
            < 1 => "Verde",
            < 5 => "Amarelo",
            < 10 => "Laranja",
            _ => "Vermelho"
        };

        return Results.Ok(new
        {
            id = ID[indice],
            regional = Regioes[indice],
            fonte = "Open-Meteo",
            horario = dados.Current.Time,
            temperaturaC = dados.Current.Temperature,
            umidadePercentual = dados.Current.Humidity,
            precipitacaoMm = chuva,
            ventoKmH = dados.Current.WindSpeed,
            classificacaoIlustrativa = risco,
            aviso = "Dados meteorológicos de modelo, não medição oficial. " +
                    "A classificação não representa um alerta oficial."
        });
    }
    catch (HttpRequestException)
    {
        return Results.Problem(
            "Não foi possível acessar a API meteorológica.",
            statusCode: 502);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem(
            "A consulta meteorológica demorou demais.",
            statusCode: 504);
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.Problem(
            "Não foi possível interpretar a resposta meteorológica.",
            statusCode: 502);
    }
});

app.Run();

// Estrutura da resposta da Open-Meteo
public class RespostaMeteorologica
{
    [JsonPropertyName("current")]
    public CondicoesAtuais? Current { get; set; }
}

public class CondicoesAtuais
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = "";

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public double Humidity { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }
}
