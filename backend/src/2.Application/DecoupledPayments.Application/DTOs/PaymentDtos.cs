namespace DecoupledPayments.Application.DTOs;

public record InitiatePaymentRequestDto(
    string MerchantId,
    string UserDocumentId,
    decimal Amount,
    string Currency,
    string CallbackUrl);

public record InitiatePaymentResponseDto(
    Guid TransactionId,
    string SessionToken,
    string Status,
    int ExpiresInSeconds);

public record VerifyOtpRequestDto(
    Guid TransactionId,
    string SessionToken,
    string OtpCode);

public record VerifyOtpResponseDto(
    Guid TransactionId,
    string Status,
    string TransactionToken,
    int ExpiresInSeconds);

public record ExecutePaymentRequestDto(
    Guid TransactionId,
    string SourceAccountId);

public record ExecutePaymentResponseDto(
    Guid TransactionId,
    string BankReference,
    string Status,
    DateTime ExecutedAt);
