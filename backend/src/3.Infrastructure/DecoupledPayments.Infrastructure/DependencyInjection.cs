namespace DecoupledPayments.Infrastructure;

using DecoupledPayments.Application.Interfaces;
using DecoupledPayments.Infrastructure.Persistence;
using DecoupledPayments.Infrastructure.Services;
using DecoupledPayments.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<DatabaseSettings>(configuration.GetSection("Database"));

        var db = configuration.GetSection("Database").Get<DatabaseSettings>() ?? new DatabaseSettings();
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Default' (variable ConnectionStrings__Default).");

        // Un único punto de decisión: el motor se elige por configuración.
        services.AddDbContext<PaymentsDbContext>(options =>
        {
            switch (db.Provider)
            {
                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure());
                    break;
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());
                    break;
                default:
                    throw new NotSupportedException($"Proveedor de base de datos no soportado: {db.Provider}");
            }
        });

        services.AddScoped<ITransactionRepository, EfTransactionRepository>();
        services.AddScoped<IReplayProtectionService, DbReplayProtectionService>();

        services.AddSingleton<JwtKeyProvider>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddHostedService<ExpiredTokenCleanupService>();

        return services;
    }
}
