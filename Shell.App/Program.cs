using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Shell.App;
using Shell.App.Services;
using Shared.Contracts.Http;
using Shared.Contracts.Services;
using Shared.Contracts.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient como Scoped (default en Blazor WASM)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSharedServiceBridge();


// Module Federation - ModuleFederationManager as Singleton
builder.Services.AddSingleton<ModuleFederationManager>();

// DynamicRouteManager as Singleton (without injecting NavigationManager directly)
builder.Services.AddSingleton<DynamicRouteManager>();

var host = builder.Build();

// Initialize Module Federation
Console.WriteLine("╔════════════════════════════════════════════╗");
Console.WriteLine("║   🚀 Shell Application with Module         ║");
Console.WriteLine("║      Federation Starting...                ║");
Console.WriteLine("╚════════════════════════════════════════════╝");


// Inicializar Module Federation
var moduleFederation = host.Services.GetRequiredService<ModuleFederationManager>();
var routeManager = host.Services.GetRequiredService<DynamicRouteManager>();

routeManager.Initialize();

// Eventos
moduleFederation.OnModuleLoaded += (evt) =>
{
    Console.WriteLine($"✅ Module loaded: {evt.ModuleName}");
};

moduleFederation.OnModuleError += (evt) =>
{
    Console.Error.WriteLine($"❌ Error in module {evt.ModuleName}: {evt.Error}");
};

Console.WriteLine("✅ Shell ready");
Console.WriteLine($"📍 URL: {builder.HostEnvironment.BaseAddress}");
Console.WriteLine($"🌐 API: https://tu-api.com/api/");
Console.WriteLine("🔐 Auth synced via localStorage with all modules");
Console.WriteLine("💡 Login here will sync automatically with Products");
Console.WriteLine("");

await host.RunAsync();