using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Products.MicroFrontend;
using Products.MicroFrontend.Services;
using Shared.Contracts.Communication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Servicios singleton para mantener estado entre navegaciones
builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

builder.Services.AddScoped<IProductService, ProductService>();

await builder.Build().RunAsync();
