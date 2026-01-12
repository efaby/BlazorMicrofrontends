using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Orders.MicroFrontend;
using Shared.Contracts.Communication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

if (!builder.HostEnvironment.BaseAddress.Contains("iframe"))
{
    Console.WriteLine("Cargando Micro Frontend como aplicación independiente.");
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

await builder.Build().RunAsync();