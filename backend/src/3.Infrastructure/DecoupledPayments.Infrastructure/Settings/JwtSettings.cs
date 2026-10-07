namespace DecoupledPayments.Infrastructure.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = "decoupled-payments-auth";
    public string Audience { get; set; } = "decoupled-payments-api";
    public int TransactionTokenTtlSeconds { get; set; } = 60;

    /// <summary>Identificador de la clave (cabecera "kid").</summary>
    public string KeyId { get; set; } = "apoc-key-1";

    /// <summary>
    /// Clave privada RSA en PEM (PKCS#8). Si se define, todas las réplicas firman/validan con la misma clave.
    /// Si se omite, se genera una clave efímera por proceso (solo válida con una réplica).
    /// </summary>
    public string? PrivateKeyPem { get; set; }
}
