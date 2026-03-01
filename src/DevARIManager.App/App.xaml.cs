using System.Windows;
using DevARIManager.App.ViewModels;
using DevARIManager.Core.Helpers;
using DevARIManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevARIManager.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // Core
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IToolManager, ToolManager>();
        services.AddSingleton<IServiceManager, ServiceManager>();
        services.AddSingleton<IEnvironmentManager, EnvironmentManager>();
        services.AddSingleton<IHealthChecker, HealthChecker>();
        services.AddSingleton<IProfileManager, ProfileManager>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddSingleton<ToolsViewModel>();       // Singleton: kurulum arka planda devam etsin
        services.AddSingleton<ServicesViewModel>();     // Singleton: servis durumu korunsun
        services.AddTransient<EnvironmentViewModel>();
        services.AddTransient<HealthCheckViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<ProfilesViewModel>();    // Singleton: profil kurulumu devam etsin
        services.AddSingleton<TerminalViewModel>();

        Services = services.BuildServiceProvider();

        base.OnStartup(e);
    }
}
