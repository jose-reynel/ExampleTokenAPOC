namespace DecoupledPayments.Client.Models;

public class InitiatePaymentModel
{
    public string MerchantId { get; set; } = "MERCHANT_PSE_8892";
    public string UserDocumentId { get; set; } = "1018234567";
    public decimal Amount { get; set; } = 250000.00m;
    public string Currency { get; set; } = "COP";
    public string CallbackUrl { get; set; } = "https://comercio.com/pago/callback";
}

public class InitiatePaymentResult
{
    public Guid TransactionId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public class VerifyOtpModel
{
    public Guid TransactionId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class VerifyOtpResult
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransactionToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public class ExecutePaymentModel
{
    public Guid TransactionId { get; set; }
    public string SourceAccountId { get; set; } = "ACC_SAVINGS_9812";
}

public class ExecutePaymentResult
{
    public Guid TransactionId { get; set; }
    public string BankReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
}
