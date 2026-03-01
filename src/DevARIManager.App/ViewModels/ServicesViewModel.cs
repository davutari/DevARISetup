using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Models;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class ServicesViewModel : ObservableObject
{
    private readonly IServiceManager _serviceManager;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _runningCount;
    [ObservableProperty] private int _stoppedCount;

    public ObservableCollection<ServiceItemViewModel> Services { get; } = [];

    public ServicesViewModel(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
        _ = LoadServicesAsync();
    }

    private async Task LoadServicesAsync()
    {
        IsLoading = true;
        var services = await _serviceManager.GetAllServicesAsync();

        Services.Clear();
        foreach (var svc in services)
        {
            Services.Add(new ServiceItemViewModel(_serviceManager, this)
            {
                Id = svc.Id,
                Name = svc.DisplayName,
                IsRunning = svc.State == ServiceState.Running,
                Port = svc.Port,
                ProcessId = svc.ProcessId
            });
        }

        RunningCount = Services.Count(s => s.IsRunning);
        StoppedCount = Services.Count(s => !s.IsRunning);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task StartAllAsync()
    {
        foreach (var svc in Services.Where(s => !s.IsRunning))
            await svc.StartAsync();
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        foreach (var svc in Services.Where(s => s.IsRunning))
            await svc.StopAsync();
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync() => await LoadServicesAsync();
}

public partial class ServiceItemViewModel : ObservableObject
{
    private readonly IServiceManager _serviceManager;
    private readonly ServicesViewModel _parent;

    public ServiceItemViewModel(IServiceManager serviceManager, ServicesViewModel parent)
    {
        _serviceManager = serviceManager;
        _parent = parent;
    }

    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int _port;
    [ObservableProperty] private int? _processId;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    public async Task StartAsync()
    {
        IsBusy = true;
        var success = await _serviceManager.StartServiceAsync(Id);
        IsRunning = success;
        IsBusy = false;
    }

    [RelayCommand]
    public async Task StopAsync()
    {
        IsBusy = true;
        var success = await _serviceManager.StopServiceAsync(Id);
        if (success) IsRunning = false;
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RestartAsync()
    {
        IsBusy = true;
        var success = await _serviceManager.RestartServiceAsync(Id);
        IsRunning = success;
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsRunning) await StopAsync();
        else await StartAsync();
        await _parent.RefreshAsync();
    }
}
