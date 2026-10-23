namespace PluviAlerta.Domain.Entidades;

// LGPD: guardamos só o chat do Telegram e a regional, nada de nome ou telefone.
public class Inscricao
{
    public int Id { get; set; }
    public long TelegramChatId { get; set; }
    public int RegiaoId { get; set; }
    public DateTime CriadoEm { get; set; } // UTC
}
