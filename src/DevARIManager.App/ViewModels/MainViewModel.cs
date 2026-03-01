using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DevARIManager.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentPage = "Dashboard";

    [ObservableProperty]
    private string _windowTitle = "DevARI Manager";

    public MainViewModel()
    {
        NavigateTo("Dashboard");
    }

    [RelayCommand]
    public void NavigateTo(string page)
    {
        CurrentPage = page;
        CurrentView = page switch
        {
            "Dashboard" => App.Services.GetRequiredService<DashboardViewModel>(),
            "Tools" => App.Services.GetRequiredService<ToolsViewModel>(),
            "Services" => App.Services.GetRequiredService<ServicesViewModel>(),
            "Environment" => App.Services.GetRequiredService<EnvironmentViewModel>(),
            "HealthCheck" => App.Services.GetRequiredService<HealthCheckViewModel>(),
            "Profiles" => App.Services.GetRequiredService<ProfilesViewModel>(),
            "Terminal" => App.Services.GetRequiredService<TerminalViewModel>(),
            "Settings" => App.Services.GetRequiredService<SettingsViewModel>(),
            _ => App.Services.GetRequiredService<DashboardViewModel>()
        };
    }
}
