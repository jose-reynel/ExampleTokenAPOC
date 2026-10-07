namespace DecoupledPayments.WebApi.Middleware;

using DecoupledPayments.Application.Interfaces;

/// <summary>
/// Control anti-replay: consume de forma atómica el jti del transaction_token.
/// Se ejecuta DESPUÉS de UseAuthentication/UseAuthorization, por lo que el token
/// ya tiene firma, iss, aud, exp y scope validados.
/// </summary>
public class SingleUseTokenMiddleware
{
    private const string ExecutePath = "/api/v1/decoupled-payments/execute";

    private readonly RequestDelegate _next;

    public SingleUseTokenMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IReplayProtectionService replayService)
    {
        bool isExecute = context.Request.Path.StartsWithSegments(ExecutePath)
                         && HttpMethods.IsPost(context.Request.Method);

        if (!isExecute)
        {
            await _next(context);
            return;
        }

        string? authHeader = context.Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await Reject(context, StatusCodes.Status401Unauthorized, "MISSING_BEARER_TOKEN", "Se requiere un transaction_token Bearer.");
            return;
        }

        string? jti = context.User.FindFirst("jti")?.Value;
        string? exp = context.User.FindFirst("exp")?.Value;

        if (string.IsNullOrEmpty(jti) || !long.TryParse(exp, out long expUnix))
        {
            await Reject(context, StatusCodes.Status401Unauthorized, "INVALID_TOKEN", "El token no contiene jti/exp válidos.");
            return;
        }

        TimeSpan ttl = DateTimeOffset.FromUnixTimeSeconds(expUnix) - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            await Reject(context, StatusCodes.Status401Unauthorized, "EXPIRED_TOKEN", "El transaction_token ha expirado.");
            return;
        }

        bool isFirstUse = await replayService.TryRegisterJtiAsync(jti, ttl, context.RequestAborted);
        if (!isFirstUse)
        {
            await Reject(context, StatusCodes.Status409Conflict, "TOKEN_ALREADY_USED",
                "Este transaction_token ya fue ejecutado previamente y no se puede reutilizar.");
            return;
        }

        await _next(context);
    }

    private static Task Reject(HttpContext context, int status, string error, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { error, message });
    }
}
