using PluviAlerta.Infrastructure.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PluviAlerta.Api.Telegram;

/// <summary>
/// Long polling: a API pergunta ao Telegram "chegou mensagem?" e espera até 30 s pela resposta.
/// Não precisa de endereço público (webhook), então funciona na máquina de qualquer um e no Docker.
/// </summary>
public class BotTelegramWorker(
    ITelegramBotClient bot,
    IServiceScopeFactory scopes,
    ILogger<BotTelegramWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var eu = await bot.GetMe(ct);
            log.LogInformation("Bot do Telegram ativo: @{Usuario}", eu.Username);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Sem internet na demo: a API segue no ar e o laço abaixo continua tentando.
            log.LogWarning(ex, "Não foi possível falar com o Telegram agora.");
        }

        int? offset = null;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var updates = await bot.GetUpdates(offset, timeout: 30,
                    allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery], cancellationToken: ct);

                foreach (var update in updates)
                {
                    offset = update.Id + 1; // confirma para o Telegram não mandar de novo
                    using var scope = scopes.CreateScope();
                    try
                    {
                        await scope.ServiceProvider.GetRequiredService<ComandosBot>().ProcessarAsync(update, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Uma mensagem com problema não pode travar as próximas.
                        log.LogError(ex, "Erro ao tratar mensagem do Telegram (update {Id}).", update.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Sem internet ou Telegram fora do ar: espera e tenta de novo, sem derrubar a API.
                log.LogWarning(ex, "Falha ao consultar o Telegram. Nova tentativa em 10 s.");
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }
    }
}
