namespace DecoupledPayments.Infrastructure.Persistence.Entities;

/// <summary>jti de un transaction_token ya consumido (control anti-replay).</summary>
public class UsedToken
{
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
