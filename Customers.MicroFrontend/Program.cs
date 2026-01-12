using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Customers.MicroFrontend;
using Shared.Contracts.Communication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

if (!builder.HostEnvironment.BaseAddress.Contains("iframe"))
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

await builder.Build().RunAsync();
