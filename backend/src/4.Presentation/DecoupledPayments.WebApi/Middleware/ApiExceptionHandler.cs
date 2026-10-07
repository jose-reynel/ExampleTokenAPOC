namespace DecoupledPayments.WebApi.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string code, string message) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "CONCURRENT_UPDATE", "La transacción fue modificada por otra operación. Reintente."),
            KeyNotFoundException e => (StatusCodes.Status404NotFound, "TRANSACTION_NOT_FOUND", e.Message),
            ArgumentException e => (StatusCodes.Status400BadRequest, "INVALID_REQUEST", e.Message),
            InvalidOperationException e => (StatusCodes.Status409Conflict, "INVALID_TRANSACTION_STATE", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Error interno del servidor.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = code, message }, cancellationToken);
        return true;
    }
}
