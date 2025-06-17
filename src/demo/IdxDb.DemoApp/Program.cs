using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using IdxDb;
using IdxDb.DemoApp;
using IdxDb.DemoApp.Data;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddIndexedDb();
builder.Services.AddMudServices();

builder.Services.AddScoped<IndexedDbInterop>();
builder.Services.AddScoped<StorageManager>();

// Register the application's DbContext
builder.Services.AddScoped<AppDbContext>();

await builder.Build().RunAsync();