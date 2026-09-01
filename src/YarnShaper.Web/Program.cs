using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using YarnShaper.Web;
using YarnShaper.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ProjectStore>();
builder.Services.AddScoped(sp => new YarnColorwayService(
    sp.GetRequiredService<HttpClient>(),
    builder.Configuration["YarnColorwayProxyUrl"]));

await builder.Build().RunAsync();
