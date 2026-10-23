using System.Threading.RateLimiting;

namespace PluviAlerta.Infrastructure.Telegram;

/// <summary>
/// Mesmo limite do site (5 relatos a cada 10 min), mas por chat do Telegram em vez de IP.
/// Fica só em memória: o relato gravado no banco não guarda quem enviou (LGPD).
/// </summary>
public sealed class LimiteRelatosBot : IDisposable
{
    private readonly PartitionedRateLimiter<long> limite = PartitionedRateLimiter.Create<long, long>(
        chatId => RateLimitPartition.GetFixedWindowLimiter(chatId,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));

    public bool Permitir(long chatId)
    {
        using var lease = limite.AttemptAcquire(chatId);
        return lease.IsAcquired;
    }

    public void Dispose() => limite.Dispose();
}
