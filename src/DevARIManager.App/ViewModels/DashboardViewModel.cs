using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Models;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IToolManager _toolManager;
    private readonly IServiceManager _serviceManager;
    private readonly IHealthChecker _healthChecker;
    private readonly IProfileManager _profileManager;
    private readonly ILogService _logService;

    [ObservableProperty] private int _installedCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _runningServices;
    [ObservableProperty] private int _pathIssues;
    [ObservableProperty] private int _updatesAvailable;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _statusMessage = "Sistem taranıyor...";
    [ObservableProperty] private string _systemInfo = "";

    public ObservableCollection<ToolStatusItem> RecentTools { get; } = [];
    public ObservableCollection<ServiceStatusItem> QuickServices { get; } = [];
    public ObservableCollection<QuickProfileItem> QuickProfiles { get; } = [];
    public ObservableCollection<RecentLogItem> RecentLogs { get; } = [];

    public DashboardViewModel(
        IToolManager toolManager,
        IServiceManager serviceManager,
        IHealthChecker healthChecker,
        IProfileManager profileManager,
        ILogService logService)
    {
        _toolManager = toolManager;
        _serviceManager = serviceManager;
        _healthChecker = healthChecker;
        _profileManager = profileManager;
        _logService = logService;

        LoadQuickProfiles();
        LoadRecentLogs();
        _logService.OnLog += _ => LoadRecentLogs();
        _ = LoadDataAsync();
    }

    private void LoadQuickProfiles()
    {
        QuickProfiles.Clear();
        foreach (var profile in _profileManager.GetAllProfiles().Take(4))
        {
            QuickProfiles.Add(new QuickProfileItem
            {
                Id = profile.Id,
                Name = profile.Name,
                IconKind = profile.IconGlyph,
                ToolCount = profile.ToolIds.Length
            });
        }
    }

    private void LoadRecentLogs()
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            RecentLogs.Clear();
            foreach (var entry in _logService.Entries.TakeLast(5).Reverse())
            {
                RecentLogs.Add(new RecentLogItem
                {
                    Time = entry.Timestamp.ToString("HH:mm:ss"),
                    Message = entry.Message.Length > 60 ? entry.Message[..60] + "..." : entry.Message,
                    Color = entry.Level switch
                    {
                        LogLevel.Success => "#FF22C55E",
                        LogLevel.Warning => "#FFEAB308",
                        LogLevel.Error => "#FFEF4444",
                        _ => "#FF9CA3AF"
                    }
                });
            }
        });
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Araçlar kontrol ediliyor...";

            SystemInfo = $"{Environment.OSVersion.VersionString} | .NET {Environment.Version}";

            var tools = _toolManager.GetAllTools();
            TotalCount = tools.Count;

            var statuses = await _toolManager.CheckAllToolsAsync();
            InstalledCount = statuses.Count(s => s.State == ToolState.Installed);

            RecentTools.Clear();
            foreach (var tool in tools.Take(8))
            {
                var status = statuses.FirstOrDefault(s => s.ToolId == tool.Id);
                RecentTools.Add(new ToolStatusItem
                {
                    Name = tool.DisplayName,
                    Category = tool.Category.ToString(),
                    Version = status?.InstalledVersion ?? "-",
                    IsInstalled = status?.State == ToolState.Installed,
                    IconGlyph = tool.IconGlyph
                });
            }

            StatusMessage = "Servisler kontrol ediliyor...";
            var services = await _serviceManager.GetAllServicesAsync();
            RunningServices = services.Count(s => s.State == ServiceState.Running);

            QuickServices.Clear();
            foreach (var svc in services)
            {
                QuickServices.Add(new ServiceStatusItem
                {
                    Name = svc.DisplayName,
                    IsRunning = svc.State == ServiceState.Running,
                    Port = svc.Port,
                    ServiceId = svc.Id
                });
            }

            StatusMessage = $"{InstalledCount}/{TotalCount} arac kurulu, {RunningServices} servis aktif";
            IsLoading = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ToggleServiceAsync(string? serviceId)
    {
        if (string.IsNullOrEmpty(serviceId)) return;
        var svc = QuickServices.FirstOrDefault(s => s.ServiceId == serviceId);
        if (svc == null) return;

        if (svc.IsRunning)
            await _serviceManager.StopServiceAsync(serviceId);
        else
            await _serviceManager.StartServiceAsync(serviceId);

        await LoadDataAsync();
    }
}

public partial class ToolStatusItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private string _version = "";
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private string _iconGlyph = "";
}

public partial class ServiceStatusItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int _port;
    [ObservableProperty] private string _serviceId = "";
}

public partial class QuickProfileItem : ObservableObject
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _iconKind = "";
    [ObservableProperty] private int _toolCount;
}

public partial class RecentLogItem : ObservableObject
{
    [ObservableProperty] private string _time = "";
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _color = "#FF9CA3AF";
}
