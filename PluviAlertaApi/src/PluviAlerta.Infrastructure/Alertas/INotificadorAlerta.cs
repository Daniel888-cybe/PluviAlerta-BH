using Microsoft.Extensions.Logging;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Risco;

namespace PluviAlerta.Infrastructure.Alertas;

/// <param name="Reducao">true = aviso de "risco baixou" (saiu de Laranja/Vermelho).</param>
public record Alerta(string NomeRegiao, ScoreRisco Score, bool Reducao = false);

/// <summary>
/// Quem entrega o alerta. Duas implementações: Telegram (com token) e só log (sem token),
/// para a API funcionar e ser testável mesmo sem o bot configurado.
/// </summary>
public interface INotificadorAlerta
{
    Task NotificarAsync(Alerta alerta, IReadOnlyList<long> chatIds, CancellationToken ct);
}

public class NotificadorSoLog(ILogger<NotificadorSoLog> log) : INotificadorAlerta
{
    public Task NotificarAsync(Alerta alerta, IReadOnlyList<long> chatIds, CancellationToken ct)
    {
        log.LogWarning("{Tipo} (sem Telegram configurado, só log) para {Inscritos} inscrito(s):\n{Mensagem}",
            alerta.Reducao ? "AVISO DE REDUÇÃO" : "ALERTA", chatIds.Count, MensagemAlerta.Montar(alerta));
        return Task.CompletedTask;
    }
}

public static class MensagemAlerta
{
    public static string Montar(Alerta a)
    {
        var s = a.Score;
        var simulacao = s.Fonte == FonteDados.Simulacao ? "[SIMULAÇÃO] " : "";

        if (a.Reducao)
            return $"""
                {simulacao}🟢 Risco reduzido na regional {a.NomeRegiao}: agora {s.Nivel} (score {s.Score}/100)

                Chuva nas últimas 6 h: {s.ChuvaAcum6hMm:0.#} mm
                Previsão para as próximas 3 h: {s.Previsao3hMm:0.#} mm

                Continue atento: ruas e córregos podem seguir cheios por algum tempo.
                Em emergência, ligue 199 (Defesa Civil).
                """;

        var emoji = s.Nivel == NivelRisco.Vermelho ? "🔴" : "🟠";

        return $"""
            {simulacao}{emoji} Risco {s.Nivel.ToString().ToUpperInvariant()} na regional {a.NomeRegiao} (score {s.Score}/100)

            Chuva nas últimas 6 h: {s.ChuvaAcum6hMm:0.#} mm
            Previsão para as próximas 3 h: {s.Previsao3hMm:0.#} mm
            Relatos de moradores: {s.RelatosAtivos}

            Estimativa de um projeto acadêmico, não é alerta oficial.
            Em emergência, ligue 199 (Defesa Civil).
            """;
    }
}
