using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using SchemaBot.SettingService.Client;
using SchemaBot.Ui;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddRadzenComponents();
builder.Services.AddSettingServiceClient("https://localhost:7235");//(builder.HostEnvironment.BaseAddress);
await builder.Build().RunAsync();
