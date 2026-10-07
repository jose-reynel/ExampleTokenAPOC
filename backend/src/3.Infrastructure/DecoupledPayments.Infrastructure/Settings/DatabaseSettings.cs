namespace DecoupledPayments.Infrastructure.Settings;

public enum DatabaseProvider
{
    PostgreSql = 1,
    SqlServer = 2
}

public enum DatabaseInitializationMode
{
    /// <summary>Crea el esquema/tablas al arrancar si no existen (APOC, pruebas).</summary>
    EnsureCreated = 1,

    /// <summary>No toca la base de datos: el esquema se aplica con los scripts de /database o migraciones.</summary>
    None = 2
}

public class DatabaseSettings
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSql;
    public DatabaseInitializationMode Initialization { get; set; } = DatabaseInitializationMode.EnsureCreated;

    /// <summary>Reintentos al inicializar (la base puede tardar en estar disponible).</summary>
    public int MaxRetries { get; set; } = 10;
    public int RetryDelaySeconds { get; set; } = 3;
}
