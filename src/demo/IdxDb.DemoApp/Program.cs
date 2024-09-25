using IdxDb;
using IdxDb.DemoApp.Services;
using IdxDb.DemoApp.Components;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
services.AddRazorComponents()
    .AddInteractiveServerComponents(config =>
    {
        config.DetailedErrors = builder.Environment.IsDevelopment();
    });

services.AddIndexedDb();

services.AddScoped<IndexedDbInterop>();
services.AddScoped<PersonRepository>();
services.AddScoped<StorageManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();