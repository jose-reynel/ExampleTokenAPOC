namespace DecoupledPayments.Application.Services;

using DecoupledPayments.Application.DTOs;
using DecoupledPayments.Application.Interfaces;
using DecoupledPayments.Domain.Entities;

public class PaymentApplicationService
{
    private readonly ITransactionRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;

    public PaymentApplicationService(ITransactionRepository repository, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<InitiatePaymentResponseDto> InitiateAsync(InitiatePaymentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MerchantId) || dto.MerchantId.Length > 64)
            throw new ArgumentException("merchant_id es obligatorio (máximo 64 caracteres).");

        if (string.IsNullOrWhiteSpace(dto.UserDocumentId) || dto.UserDocumentId.Length > 32)
            throw new ArgumentException("user_document_id es obligatorio (máximo 32 caracteres).");

        if (string.IsNullOrWhiteSpace(dto.Currency) || dto.Currency.Length != 3)
            throw new ArgumentException("currency debe ser un código de 3 letras (p. ej. COP).");

        if (dto.Amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");

        if (decimal.Round(dto.Amount, 2) != dto.Amount || dto.Amount > 9_999_999_999_999_999m)
            throw new ArgumentException("El monto admite máximo 2 decimales y 16 dígitos enteros.");

        string simulatedOtp = "123456"; // OTP simulado para APOC
        var tx = new TransactionContext(dto.MerchantId, dto.UserDocumentId, dto.Amount, dto.Currency, simulatedOtp);
        await _repository.SaveAsync(tx);

        string sessionToken = $"st_live_{Guid.NewGuid():N}";
        return new InitiatePaymentResponseDto(tx.TransactionId, sessionToken, tx.Status.ToString(), 300);
    }

    public async Task<VerifyOtpResponseDto> VerifyOtpAsync(VerifyOtpRequestDto dto)
    {
        var tx = await _repository.GetByIdAsync(dto.TransactionId)
            ?? throw new KeyNotFoundException("Transacción no encontrada.");

        tx.Authorize(dto.OtpCode);
        await _repository.UpdateAsync(tx);

        string txToken = _jwtTokenService.GenerateTransactionToken(tx.TransactionId, tx.UserDocumentId, tx.Amount, tx.Currency, out _);
        return new VerifyOtpResponseDto(tx.TransactionId, tx.Status.ToString(), txToken, 60);
    }

    public async Task<ExecutePaymentResponseDto> ExecuteAsync(ExecutePaymentRequestDto dto)
    {
        var tx = await _repository.GetByIdAsync(dto.TransactionId)
            ?? throw new KeyNotFoundException("Transacción no encontrada.");

        tx.MarkAsExecuted();
        await _repository.UpdateAsync(tx);

        string bankRef = $"CUS_{DateTime.UtcNow:yyyyMMdd}_{Random.Shared.Next(100000, 999999)}";
        return new ExecutePaymentResponseDto(tx.TransactionId, bankRef, tx.Status.ToString(), DateTime.UtcNow);
    }
}
