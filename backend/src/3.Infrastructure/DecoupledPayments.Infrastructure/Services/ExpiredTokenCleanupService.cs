namespace DecoupledPayments.Infrastructure.Services;

using DecoupledPayments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Purga periódicamente los jti vencidos (un token vencido ya no pasa la validación JWT).</summary>
public sealed class ExpiredTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ExpiredTokenCleanupService> _logger;

    public ExpiredTokenCleanupService(IServiceScopeFactory scopes, ILogger<ExpiredTokenCleanupService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                    var now = DateTime.UtcNow;

                    int removed = await db.UsedTokens
                        .Where(t => t.ExpiresAt < now)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (removed > 0)
                        _logger.LogInformation("Purgados {Count} jti vencidos.", removed);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "No se pudo purgar la tabla used_tokens.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // apagado normal
        }
    }
}
