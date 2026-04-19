using ApexCharts;
using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.SessionStorage;
using MyZabbix.Core.Services;
using MyZabbix.Web.Components;
using Serilog;
using SharedServices;
using SharedServices.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Shared UI services
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddSingleton<ThemeService>(_ => new ThemeService(builder.Configuration));
builder.Services.AddBlazoredModal();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddApexCharts();

// Zabbix API
builder.Services.AddHttpClient<ZabbixApiService>();
builder.Services.AddScoped<ZabbixApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
