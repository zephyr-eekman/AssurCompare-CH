using AssurCompare.Web;
using AssurCompare.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<PremiumDataService>();
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<AppModeState>();
builder.Services.AddScoped<ClientProfileState>();
builder.Services.AddScoped<AnalysisStore>();
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddScoped<UiState>();
builder.Services.AddMudServices();

var host = builder.Build();

// L'état sauvegardé dans le navigateur est chargé avant le premier affichage : pas de saut à l'écran.
await host.Services.GetRequiredService<AppModeState>().LoadAsync();
await host.Services.GetRequiredService<ClientProfileState>().LoadAsync();
await host.Services.GetRequiredService<AnalysisStore>().LoadAsync();

await host.RunAsync();
