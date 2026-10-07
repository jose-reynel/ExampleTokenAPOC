using DecoupledPayments.Client;
using DecoupledPayments.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL del backend (wwwroot/appsettings.json -> ApiBaseUrl). Debe terminar en "/".
string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7100/";

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<PaymentApiClient>();

await builder.Build().RunAsync();
