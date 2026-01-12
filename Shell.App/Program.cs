using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Shell.App;
using Shell.App.Services;
using Shared.Contracts.Communication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient como Scoped (default en Blazor WASM)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Communication services
builder.Services.AddSingleton<IEventAggregator, EventAggregator>();


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

// Get the module manager and initialize it
var moduleFederation = host.Services.GetRequiredService<ModuleFederationManager>();
var routeManager = host.Services.GetRequiredService<DynamicRouteManager>();

// Initialize the route manager
routeManager.Initialize();

// Suscribirse a eventos
moduleFederation.OnModuleLoaded += (evt) =>
{
    Console.WriteLine($"✅ Module loaded: {evt.ModuleName} at {evt.LoadedAt:HH:mm:ss}");
};

moduleFederation.OnModuleError += (evt) =>
{
    Console.Error.WriteLine($"❌ Error in module {evt.ModuleName}: {evt.Error}");
};

Console.WriteLine("✅ Shell Application ready");
Console.WriteLine($"📍 URL: {builder.HostEnvironment.BaseAddress}");

await host.RunAsync();