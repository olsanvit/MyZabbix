using ApexCharts;
using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.SessionStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyZabbix.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using MyZabbix.Mobile.Services;
using SharedServices;
using SharedServices.Services;

namespace MyZabbix.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Konfigurace z embedded appsettings.json
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("MyZabbix.Mobile.appsettings.json");
        if (stream is not null)
        {
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);
        }

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

        // Nové sdílené služby
        builder.Services.AddScoped<LoadingService>();
        builder.Services.AddScoped<ConfirmService>();
        builder.Services.AddScoped<UserPreferencesService>();
        builder.Services.AddScoped<ClipboardService>();
        builder.Services.AddTransient<Debouncer>();
        builder.Services.AddSingleton<ConnectionStateService>();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<SecureStorageService>();

        return builder.Build();
    }
}
