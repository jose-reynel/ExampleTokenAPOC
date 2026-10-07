using System.Text.Json;
using DecoupledPayments.Application.Services;
using DecoupledPayments.Infrastructure;
using DecoupledPayments.Infrastructure.Persistence;
using DecoupledPayments.Infrastructure.Services;
using DecoupledPayments.Infrastructure.Settings;
using DecoupledPayments.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- JSON en snake_case (contrato OpenAPI) ----
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ---- Capas ----
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<PaymentApplicationService>();

// ---- Autenticación JWT (RS256) para el transaction_token ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtKeyProvider, IOptions<JwtSettings>>((options, keys, jwt) =>
    {
        options.MapInboundClaims = false; // conservar nombres originales: jti, sub, scope...
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Value.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = keys.SecurityKey,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ExecutePayment", p => p
        .RequireAuthenticatedUser()
        .RequireClaim("scope", JwtTokenService.ExecutePaymentScope));

// ---- CORS para el frontend Blazor ----
string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                   ?? new[] { "https://localhost:7200", "http://localhost:5200" };
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SingleUseTokenMiddleware>(); // anti-replay: 1 solo uso por jti

app.MapControllers();

// Liveness: el proceso responde (no consulta la base de datos)
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// Readiness: además verifica la conexión a la base de datos
app.MapGet("/healthz/ready", async (PaymentsDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

// Crea el esquema si Database:Initialization = EnsureCreated (con reintentos)
await app.Services.InitializeDatabaseAsync();

app.Run();
