namespace DecoupledPayments.Infrastructure.Services;

using System.Globalization;
using System.Security.Claims;
using DecoupledPayments.Application.Interfaces;
using DecoupledPayments.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    public const string ExecutePaymentScope = "execute_payment";

    private readonly JwtSettings _settings;
    private readonly JwtKeyProvider _keys;

    public JwtTokenService(IOptions<JwtSettings> settings, JwtKeyProvider keys)
    {
        _settings = settings.Value;
        _keys = keys;
    }

    public string GenerateTransactionToken(Guid transactionId, string userDocumentId, decimal amount, string currency, out string jti)
    {
        jti = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            NotBefore = now,
            IssuedAt = now,
            Expires = now.AddSeconds(_settings.TransactionTokenTtlSeconds),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userDocumentId),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim("scope", ExecutePaymentScope),
                new Claim("transaction_id", transactionId.ToString()),
                new Claim("amount", amount.ToString(CultureInfo.InvariantCulture)),
                new Claim("currency", currency)
            }),
            SigningCredentials = new SigningCredentials(_keys.SecurityKey, SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
