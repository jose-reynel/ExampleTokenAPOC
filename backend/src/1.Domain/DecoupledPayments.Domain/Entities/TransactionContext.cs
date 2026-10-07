namespace DecoupledPayments.Domain.Entities;

public enum TransactionStatus
{
    PendingOtpVerification = 1,
    Authorized = 2,
    Executed = 3,
    Failed = 4
}

public class TransactionContext
{
    public Guid TransactionId { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public string UserDocumentId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "COP";
    public TransactionStatus Status { get; private set; }
    public string ExpectedOtp { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Requerido por el ORM (materialización). No usar desde código de negocio.
    private TransactionContext() { }

    public TransactionContext(string merchantId, string userDocumentId, decimal amount, string currency, string generatedOtp, int ttlSeconds = 300)
    {
        TransactionId = Guid.NewGuid();
        MerchantId = merchantId;
        UserDocumentId = userDocumentId;
        Amount = amount;
        Currency = currency;
        ExpectedOtp = generatedOtp;
        Status = TransactionStatus.PendingOtpVerification;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.AddSeconds(ttlSeconds);
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public void Authorize(string inputOtp)
    {
        if (IsExpired())
            throw new InvalidOperationException("El contexto de la transacción ha expirado.");

        if (Status != TransactionStatus.PendingOtpVerification)
            throw new InvalidOperationException("La transacción no está pendiente de verificación.");

        if (ExpectedOtp != inputOtp)
            throw new ArgumentException("El código OTP es incorrecto.");

        Status = TransactionStatus.Authorized;
    }

    public void MarkAsExecuted()
    {
        if (Status != TransactionStatus.Authorized)
            throw new InvalidOperationException("Solo se pueden ejecutar transacciones autorizadas.");

        Status = TransactionStatus.Executed;
    }
}
