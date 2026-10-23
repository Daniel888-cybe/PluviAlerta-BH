using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Domain.Alertas;

/// <summary>
/// Quando avisar os inscritos: o nível SUBIU e chegou a Laranja ou Vermelho,
/// e a mesma regional não recebeu alerta desse mesmo nível nas últimas 2 h.
/// </summary>
public static class RegraAlerta
{
    public const NivelRisco NivelMinimo = NivelRisco.Laranja;
    public static readonly TimeSpan IntervaloMinimoMesmoNivel = TimeSpan.FromHours(2);

    /// <param name="anterior">Nível do score anterior da regional (null = primeira leitura).</param>
    /// <param name="ultimoAlertaMesmoNivelUtc">Último alerta desta regional com o MESMO nível de <paramref name="atual"/>.</param>
    public static bool DeveAlertar(
        NivelRisco? anterior, NivelRisco atual, DateTime? ultimoAlertaMesmoNivelUtc, DateTime agoraUtc)
    {
        if (atual < NivelMinimo)
            return false;

        var subiu = anterior is null || atual > anterior;
        if (!subiu)
            return false;

        var alertouHaPouco = ultimoAlertaMesmoNivelUtc is not null
                             && agoraUtc - ultimoAlertaMesmoNivelUtc < IntervaloMinimoMesmoNivel;
        return !alertouHaPouco;
    }

    /// <summary>
    /// "Risco baixou": a regional estava em alerta (Laranja/Vermelho) e voltou para Verde/Amarelo.
    /// Vermelho → Laranja não conta: continua em alerta.
    /// </summary>
    public static bool DeveAvisarReducao(NivelRisco? anterior, NivelRisco atual) =>
        anterior >= NivelMinimo && atual < NivelMinimo;
}
