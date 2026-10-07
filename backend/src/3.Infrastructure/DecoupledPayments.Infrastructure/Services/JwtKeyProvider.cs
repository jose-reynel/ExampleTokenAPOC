namespace DecoupledPayments.Infrastructure.Services;

using System.Security.Cryptography;
using DecoupledPayments.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Proveedor de la clave RSA (RS256). Si Jwt:PrivateKeyPem está configurada, todas las réplicas
/// comparten la misma clave; si no, se genera una clave efímera (solo válida con una réplica).
/// En producción la clave debe vivir en un KMS/HSM/Vault.
/// </summary>
public sealed class JwtKeyProvider : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    public RsaSecurityKey SecurityKey { get; }

    public JwtKeyProvider(IOptions<JwtSettings> options, ILogger<JwtKeyProvider> logger)
    {
        var settings = options.Value;

        if (!string.IsNullOrWhiteSpace(settings.PrivateKeyPem))
        {
            _rsa.ImportFromPem(settings.PrivateKeyPem);
            logger.LogInformation("Clave de firma JWT cargada desde configuración (kid={KeyId}).", settings.KeyId);
        }
        else
        {
            logger.LogWarning("Jwt:PrivateKeyPem no configurada: se usa una clave RSA efímera. " +
                              "Los tokens dejan de ser válidos al reiniciar y no se puede escalar a varias réplicas.");
        }

        SecurityKey = new RsaSecurityKey(_rsa) { KeyId = settings.KeyId };
    }

    public void Dispose() => _rsa.Dispose();
}
