namespace DecoupledPayments.Infrastructure.Persistence;

using DecoupledPayments.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public static class DatabaseInitializer
{
    /// <summary>
    /// Crea el esquema si Database:Initialization = EnsureCreated, con reintentos
    /// (el motor puede estar arrancando, p. ej. en Docker/OpenShift).
    /// Con "None" no hace nada: use los scripts de /database o migraciones.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (settings.Initialization == DatabaseInitializationMode.None)
        {
            logger.LogInformation("Initialization=None: no se modifica el esquema ({Provider}).", settings.Provider);
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(ct);
                logger.LogInformation("Base de datos lista ({Provider}).", settings.Provider);
                return;
            }
            catch (Exception ex) when (attempt < settings.MaxRetries && ex is not OperationCanceledException)
            {
                logger.LogWarning("Base de datos no disponible (intento {Attempt}/{Max}): {Message}",
                    attempt, settings.MaxRetries, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(settings.RetryDelaySeconds), ct);
            }
        }
    }
}
