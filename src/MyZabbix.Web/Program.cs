using MudBlazor.Services;
using Radzen;
using ApexCharts;
using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.SessionStorage;
using MyZabbix.Core.Services;
using MyZabbix.Web.Components;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using SharedServices;
using SharedServices.Services;
using MyZabbix.Web.Achievements;

var builder = WebApplication.CreateBuilder(args);

Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
        tableName: "Logs",
        columnOptions: new Dictionary<string, ColumnWriterBase>
        {
            { "message",    new RenderedMessageColumnWriter() },
            { "level",      new LevelColumnWriter() },
            { "raise_date", new TimestampColumnWriter() },
            { "exception",  new ExceptionColumnWriter() },
            { "properties", new PropertiesColumnWriter() },
            { "machine_name", new SinglePropertyColumnWriter("MachineName") }
        },
        needAutoCreateTable: true,
        restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Shared UI services
builder.Services.AddMudServices();
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<UiLibraryService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AchievementService>(sp =>
    new AchievementService(
        sp.GetRequiredService<ToastService>(),
        sp.GetRequiredService<IWebHostEnvironment>())
    {
        Definitions = MyZabbixAchievements.All
    });
builder.Services.AddScoped<AlertService>();
builder.Services.AddSingleton<SharedServices.Services.ThemeService>(_ => new SharedServices.Services.ThemeService(builder.Configuration));
builder.Services.AddBlazoredModal();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddApexCharts();

// Zabbix API — pojmenovaný HttpClient + Scoped service (stav = auth token přežívá v rámci Blazor circuit)
builder.Services.AddHttpClient("Zabbix");
builder.Services.AddScoped(sp => new ZabbixApiService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Zabbix"),
    sp.GetRequiredService<ILogger<ZabbixApiService>>()));

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
    Log.Fatal(e.ExceptionObject as Exception, "UNHANDLED AppDomain exception");

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Log.Fatal(e.Exception, "UNOBSERVED task exception");
    e.SetObserved();
};

builder.Services.AddHealthChecks();

var app = builder.Build();

var zabbixUrl = builder.Configuration["Zabbix:Url"] ?? "";
if (string.IsNullOrWhiteSpace(zabbixUrl) || zabbixUrl.Contains("your-zabbix-server"))
    Log.Warning("Zabbix:Url is not configured — configure via Settings page or appsettings");

var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStopping.Register(() =>
    Log.Warning("Application stopping — flushing logs..."));

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
