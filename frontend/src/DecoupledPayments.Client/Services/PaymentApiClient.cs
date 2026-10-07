namespace DecoupledPayments.Client.Services;

using System.Net.Http.Json;
using System.Text.Json;
using DecoupledPayments.Client.Models;

public class PaymentApiClient
{
    // El contrato OpenAPI usa snake_case (transaction_id, otp_code, ...).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public PaymentApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<InitiatePaymentResult?> InitiateAsync(InitiatePaymentModel model)
    {
        var response = await _http.PostAsJsonAsync("api/v1/decoupled-payments/initiate", model, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InitiatePaymentResult>(JsonOptions);
    }

    public async Task<VerifyOtpResult?> VerifyOtpAsync(VerifyOtpModel model)
    {
        var response = await _http.PostAsJsonAsync("api/v1/decoupled-payments/verify-otp", model, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VerifyOtpResult>(JsonOptions);
    }

    public async Task<(bool Success, ExecutePaymentResult? Result, string? ErrorMessage)> ExecutePaymentAsync(
        ExecutePaymentModel model, string transactionToken, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/decoupled-payments/execute")
        {
            Content = JsonContent.Create(model, options: JsonOptions)
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", transactionToken);
        request.Headers.Add("X-Idempotency-Key", idempotencyKey);

        var response = await _http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ExecutePaymentResult>(JsonOptions);
            return (true, result, null);
        }

        string errorJson = await response.Content.ReadAsStringAsync();
        string error = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
        if (!string.IsNullOrWhiteSpace(errorJson))
            error += $" — {errorJson}";
        return (false, null, error);
    }
}
