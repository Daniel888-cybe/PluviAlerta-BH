using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PluviAlerta.Domain.Entidades;
using PluviAlerta.Domain.Relatos;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.Relatos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PluviAlerta.Infrastructure.Telegram;

/// <summary>
/// O que o bot responde:
/// /start mostra as regionais em botões (tocar inscreve); /sair apaga o cadastro;
/// /relatar pede a localização, e ao receber uma localização o bot pergunta o tipo e cria o relato.
/// LGPD: inscrição guarda só chat + regional; o relato não guarda quem enviou.
/// </summary>
public class ComandosBot(
    ITelegramBotClient bot, PluviAlertaDbContext db, ServicoDeRelatos relatos, LimiteRelatosBot limite)
{
    private const string PrefixoRegiao = "regiao:";
    private const string PrefixoRelato = "relato:"; // relato:{Tipo}:{lat}:{lon}

    private const string Aviso =
        "Estimativa de um projeto acadêmico, não é alerta oficial. Em emergência, ligue 199 (Defesa Civil).";

    private const string Ajuda =
        "Comandos:\n/start - escolher a regional para receber alertas\n" +
        "/relatar - avisar um alagamento ou deslizamento\n/sair - parar de receber alertas";

    public async Task ProcessarAsync(Update update, CancellationToken ct)
    {
        if (update.CallbackQuery is { } clique)
            await TratarCliqueAsync(clique, ct);
        else if (update.Message is { Location: { } local } comLocal)
            await TratarLocalizacaoAsync(comLocal.Chat.Id, local, ct);
        else if (update.Message is { Text: { } texto } mensagem)
            await TratarTextoAsync(mensagem.Chat.Id, texto.Trim(), ct);
    }

    private async Task TratarTextoAsync(long chatId, string texto, CancellationToken ct)
    {
        // "/start@PluviAlerta_bot" (em grupos) também conta como "/start".
        var comando = texto.Split(' ', '@')[0].ToLowerInvariant();

        switch (comando)
        {
            case "/start":
                var regioes = await db.Regioes.AsNoTracking().OrderBy(r => r.Nome).ToListAsync(ct);
                var botoes = regioes
                    .Select(r => InlineKeyboardButton.WithCallbackData(r.Nome, PrefixoRegiao + r.Id))
                    .Chunk(3); // 3 botões por linha
                await bot.SendMessage(chatId,
                    "🌧️ PluviAlerta BH\n\nEscolha a regional para receber alertas de risco de chuva:\n\n" +
                    $"Viu um alagamento? Envie /relatar.\n\n{Aviso}",
                    replyMarkup: new InlineKeyboardMarkup(botoes), cancellationToken: ct);
                break;

            case "/relatar":
                // Botão que, no celular, manda a localização com um toque.
                var pedirLocal = new ReplyKeyboardMarkup(KeyboardButton.WithRequestLocation("📍 Enviar minha localização"))
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true,
                };
                await bot.SendMessage(chatId,
                    "Toque no botão abaixo para enviar a localização do problema " +
                    "(ou use 📎 → Localização para marcar outro ponto no mapa).",
                    replyMarkup: pedirLocal, cancellationToken: ct);
                break;

            case "/sair":
                var apagadas = await db.Inscricoes.Where(i => i.TelegramChatId == chatId).ExecuteDeleteAsync(ct);
                await bot.SendMessage(chatId, apagadas > 0
                        ? "Cadastro apagado. Você não vai mais receber alertas. Para voltar, envie /start."
                        : "Você não tinha nenhuma inscrição. Para se inscrever, envie /start.",
                    cancellationToken: ct);
                break;

            default:
                await bot.SendMessage(chatId, Ajuda, cancellationToken: ct);
                break;
        }
    }

    private async Task TratarLocalizacaoAsync(long chatId, Location local, CancellationToken ct)
    {
        if (!RegrasRelato.DentroDeBH(local.Latitude, local.Longitude))
        {
            await bot.SendMessage(chatId, "Essa localização está fora de Belo Horizonte. Só aceitamos relatos dentro da cidade.",
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            return;
        }

        // A coordenada vai dentro do botão (cabe nos 64 bytes do Telegram); nada fica guardado até a pessoa escolher.
        var coord = string.Create(CultureInfo.InvariantCulture, $"{local.Latitude:F5}:{local.Longitude:F5}");
        var botoes = new[]
        {
            InlineKeyboardButton.WithCallbackData("🌊 Alagamento", $"{PrefixoRelato}{TipoRelato.Alagamento}:{coord}"),
            InlineKeyboardButton.WithCallbackData("⛰️ Deslizamento", $"{PrefixoRelato}{TipoRelato.Deslizamento}:{coord}"),
            InlineKeyboardButton.WithCallbackData("Outro", $"{PrefixoRelato}{TipoRelato.Outro}:{coord}"),
        };

        await bot.SendMessage(chatId, "Recebi a localização 📍", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        await bot.SendMessage(chatId, "O que está acontecendo aí?",
            replyMarkup: new InlineKeyboardMarkup(botoes), cancellationToken: ct);
    }

    private async Task TratarCliqueAsync(CallbackQuery clique, CancellationToken ct)
    {
        if (clique.Message is null || clique.Data is null)
        {
            await bot.AnswerCallbackQuery(clique.Id, cancellationToken: ct);
            return;
        }

        if (clique.Data.StartsWith(PrefixoRegiao))
            await InscreverAsync(clique, ct);
        else if (clique.Data.StartsWith(PrefixoRelato))
            await CriarRelatoAsync(clique, ct);
        else
            await bot.AnswerCallbackQuery(clique.Id, cancellationToken: ct);
    }

    private async Task InscreverAsync(CallbackQuery clique, CancellationToken ct)
    {
        var chatId = clique.Message!.Chat.Id;
        var regiao = int.TryParse(clique.Data![PrefixoRegiao.Length..], out var regiaoId)
            ? await db.Regioes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == regiaoId, ct)
            : null;
        if (regiao is null)
        {
            await bot.AnswerCallbackQuery(clique.Id, "Regional não encontrada.", cancellationToken: ct);
            return;
        }

        var jaInscrito = await db.Inscricoes.AnyAsync(i => i.TelegramChatId == chatId && i.RegiaoId == regiao.Id, ct);
        if (!jaInscrito)
        {
            db.Inscricoes.Add(new Inscricao { TelegramChatId = chatId, RegiaoId = regiao.Id, CriadoEm = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        await bot.AnswerCallbackQuery(clique.Id, $"Inscrito na regional {regiao.Nome}", cancellationToken: ct);
        await bot.EditMessageText(chatId, clique.Message.Id,
            $"✅ Você vai receber alertas da regional {regiao.Nome} quando o risco subir para Laranja ou Vermelho.\n\n" +
            $"Para adicionar outra regional, envie /start. Para parar, envie /sair.\n\n{Aviso}",
            cancellationToken: ct);
    }

    private async Task CriarRelatoAsync(CallbackQuery clique, CancellationToken ct)
    {
        var chatId = clique.Message!.Chat.Id;
        var partes = clique.Data![PrefixoRelato.Length..].Split(':');
        if (partes.Length != 3
            || !Enum.TryParse<TipoRelato>(partes[0], out var tipo)
            || !double.TryParse(partes[1], CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(partes[2], CultureInfo.InvariantCulture, out var lon))
        {
            await bot.AnswerCallbackQuery(clique.Id, "Não entendi esse relato. Envie /relatar de novo.", cancellationToken: ct);
            return;
        }

        if (!limite.Permitir(chatId))
        {
            await bot.AnswerCallbackQuery(clique.Id, "Muitos relatos seguidos. Tente de novo em alguns minutos.",
                showAlert: true, cancellationToken: ct);
            return;
        }

        var criado = await relatos.CriarAsync(lat, lon, tipo, null, ct);
        if (criado is null)
        {
            await bot.AnswerCallbackQuery(clique.Id, "Localização fora de Belo Horizonte.", cancellationToken: ct);
            return;
        }

        var score = criado.ScoreRecalculado is { } s ? $"\nScore atual da regional: {s.Score} ({s.Nivel})." : "";
        await bot.AnswerCallbackQuery(clique.Id, "Relato registrado. Obrigado!", cancellationToken: ct);
        await bot.EditMessageText(chatId, clique.Message.Id,
            $"✅ Relato de {tipo.ToString().ToLowerInvariant()} registrado na regional {criado.NomeRegiao}. " +
            $"Ele aparece no mapa e conta no risco por 3 h.{score}\n\nObrigado por ajudar!",
            cancellationToken: ct);
    }
}
