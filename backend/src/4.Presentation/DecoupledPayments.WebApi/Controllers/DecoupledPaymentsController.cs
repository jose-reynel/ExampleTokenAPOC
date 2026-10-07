namespace DecoupledPayments.WebApi.Controllers;

using DecoupledPayments.Application.DTOs;
using DecoupledPayments.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/decoupled-payments")]
public class DecoupledPaymentsController : ControllerBase
{
    private readonly PaymentApplicationService _service;

    public DecoupledPaymentsController(PaymentApplicationService service) => _service = service;

    [HttpPost("initiate")]
    [ProducesResponseType(typeof(InitiatePaymentResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequestDto dto)
    {
        var result = await _service.InitiateAsync(dto);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(VerifyOtpResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
    {
        var result = await _service.VerifyOtpAsync(dto);
        return Ok(result);
    }

    [HttpPost("execute")]
    [Authorize(Policy = "ExecutePayment")]
    [ProducesResponseType(typeof(ExecutePaymentResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Execute(
        [FromHeader(Name = "X-Idempotency-Key")] Guid idempotencyKey,
        [FromBody] ExecutePaymentRequestDto dto)
    {
        if (idempotencyKey == Guid.Empty)
            return BadRequest(new { error = "MISSING_IDEMPOTENCY_KEY", message = "El header X-Idempotency-Key (UUID) es obligatorio." });

        // El token solo puede ejecutar la transacción para la que fue emitido.
        var tokenTxId = User.FindFirst("transaction_id")?.Value;
        if (!Guid.TryParse(tokenTxId, out var parsed) || parsed != dto.TransactionId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "TOKEN_TRANSACTION_MISMATCH", message = "El token no corresponde a esta transacción." });

        var result = await _service.ExecuteAsync(dto);
        return Ok(result);
    }
}
