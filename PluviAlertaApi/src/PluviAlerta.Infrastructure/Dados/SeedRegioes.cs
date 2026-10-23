using PluviAlerta.Domain.Entidades;

namespace PluviAlerta.Infrastructure.Dados;

/// <summary>
/// As 9 regionais administrativas de BH.
/// ATENÇÃO: as coordenadas são APROXIMADAS (ponto central estimado, herdado do protótipo)
/// e a Vulnerabilidade é PROVISÓRIA: 50 para todas até o grupo definir os valores.
/// Nenhum desses números é dado oficial.
/// </summary>
public static class SeedRegioes
{
    public const int VulnerabilidadeProvisoria = 50;

    public static readonly Regiao[] Todas =
    [
        new() { Id = 1, Nome = "Barreiro",   Latitude = -19.975, Longitude = -44.025, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 2, Nome = "Centro-Sul", Latitude = -19.930, Longitude = -43.940, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 3, Nome = "Leste",      Latitude = -19.910, Longitude = -43.900, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 4, Nome = "Nordeste",   Latitude = -19.860, Longitude = -43.910, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 5, Nome = "Noroeste",   Latitude = -19.900, Longitude = -43.970, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 6, Nome = "Norte",      Latitude = -19.820, Longitude = -43.950, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 7, Nome = "Oeste",      Latitude = -19.930, Longitude = -44.000, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 8, Nome = "Pampulha",   Latitude = -19.850, Longitude = -43.970, Vulnerabilidade = VulnerabilidadeProvisoria },
        new() { Id = 9, Nome = "Venda Nova", Latitude = -19.810, Longitude = -43.960, Vulnerabilidade = VulnerabilidadeProvisoria },
    ];
}
