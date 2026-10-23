using Microsoft.Extensions.Logging;
using PluviAlerta.Infrastructure.Alertas;
using Telegram.Bot;

namespace PluviAlerta.Infrastructure.Telegram;

/// <summary>Envia o alerta para cada inscrito da regional. Falha em um chat não impede os outros.</summary>
public class NotificadorTelegram(ITelegramBotClient bot, ILogger<NotificadorTelegram> log) : INotificadorAlerta
{
    public async Task NotificarAsync(Alerta alerta, IReadOnlyList<long> chatIds, CancellationToken ct)
    {
        var texto = MensagemAlerta.Montar(alerta);
        var enviados = 0;

        foreach (var chatId in chatIds)
        {
            try
            {
                await bot.SendMessage(chatId, texto, cancellationToken: ct);
                enviados++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ex.: a pessoa bloqueou o bot. Segue para os próximos.
                log.LogWarning(ex, "Não foi possível enviar alerta para o chat {ChatId}.", chatId);
            }
        }

        log.LogInformation("Alerta {Nivel} da regional {Regiao} enviado para {Enviados}/{Total} inscrito(s).",
            alerta.Score.Nivel, alerta.NomeRegiao, enviados, chatIds.Count);
    }
}
