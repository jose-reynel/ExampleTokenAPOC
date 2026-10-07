namespace DecoupledPayments.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Emite el transaction_token (JWT de un solo uso, scope execute_payment, validez ultracorta).
    /// </summary>
    string GenerateTransactionToken(Guid transactionId, string userDocumentId, decimal amount, string currency, out string jti);
}
