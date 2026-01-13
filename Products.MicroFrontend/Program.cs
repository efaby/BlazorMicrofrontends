using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Products.MicroFrontend;
using Products.MicroFrontend.Services;
using Shared.Contracts.Communication;
using Shared.Contracts.Authentication;
using Shared.Contracts.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

const string MODULE_NAME = "Products";

Console.WriteLine("╔════════════════════════════════════════════╗");
Console.WriteLine($"║   📦 {MODULE_NAME} Module Starting...           ║");
Console.WriteLine("╚════════════════════════════════════════════╝");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Servicios singleton para mantener estado entre navegaciones
builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

builder.Services.AddScoped<IProductService, ProductService>();

Console.WriteLine("📦 Creating local services (synced via localStorage)...");

// EventAggregator local (solo para eventos internos del módulo)
builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

// SharedAuthenticationStateProvider (sincronizado via localStorage)
builder.Services.AddScoped<SharedAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<SharedAuthenticationStateProvider>());

// AuthenticatedHttpClient
builder.Services.AddScoped<AuthenticatedHttpClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();

    Func<Task<SharedAuthenticationStateProvider>> authProviderFactory = async () =>
    {
        return sp.GetRequiredService<SharedAuthenticationStateProvider>();
    };

    return new AuthenticatedHttpClient(httpClient, authProviderFactory);
});

// Servicios de autorización
builder.Services.AddAuthorizationCore();

Console.WriteLine("✅ Local services configured");

var host = builder.Build();


Console.WriteLine($"🔌 Connecting module '{MODULE_NAME}' to Shell...");

try
{
    var eventAggregator = host.Services.GetRequiredService<IEventAggregator>();
    await eventAggregator.PublishAsync(new ModuleConnectedEvent(MODULE_NAME));

    Console.WriteLine($"✅ Module '{MODULE_NAME}' connected to Shell");
    Console.WriteLine($"📡 EventAggregator available");
}
catch (Exception ex)
{
    Console.Error.WriteLine("═══════════════════════════════════════════");
    Console.Error.WriteLine($"❌ ERROR: Cannot connect to Shell");
    Console.Error.WriteLine("═══════════════════════════════════════════");
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine("");
    Console.Error.WriteLine("REQUIRED: Start Shell first!");
    Console.Error.WriteLine("  1. Stop this module (Ctrl+C)");
    Console.Error.WriteLine("  2. cd Shell.App && dotnet run");
    Console.Error.WriteLine("  3. Wait for 'Shell ready'");
    Console.Error.WriteLine($"  4. Then start this module again");
    Console.Error.WriteLine("═══════════════════════════════════════════");
}


Console.WriteLine($"✅ Module '{MODULE_NAME}' ready");
Console.WriteLine($"📍 Base URL: {builder.HostEnvironment.BaseAddress}");
Console.WriteLine("");

await host.RunAsync();
