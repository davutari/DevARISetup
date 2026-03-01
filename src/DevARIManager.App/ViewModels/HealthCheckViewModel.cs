using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Models;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class HealthCheckViewModel : ObservableObject
{
    private readonly IHealthChecker _healthChecker;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private int _healthyCount;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private bool _hasResults;

    public ObservableCollection<HealthItemViewModel> Results { get; } = [];

    public HealthCheckViewModel(IHealthChecker healthChecker)
    {
        _healthChecker = healthChecker;
    }

    [RelayCommand]
    private async Task RunScanAsync()
    {
        IsScanning = true;
        HasResults = false;
        Progress = 0;
        Results.Clear();

        var progressReporter = new Progress<int>(p => Progress = p);
        var report = await _healthChecker.RunFullCheckAsync(progressReporter);

        foreach (var result in report.Results)
        {
            Results.Add(new HealthItemViewModel
            {
                ToolName = result.ToolName,
                Status = result.Status,
                Message = result.Message,
                Detail = result.Detail ?? "",
                CanAutoFix = result.CanAutoFix,
                FixDescription = result.FixDescription ?? ""
            });
        }

        HealthyCount = report.HealthyCount;
        WarningCount = report.WarningCount;
        ErrorCount = report.ErrorCount;
        IsScanning = false;
        HasResults = true;
    }
}

public partial class HealthItemViewModel : ObservableObject
{
    [ObservableProperty] private string _toolName = "";
    [ObservableProperty] private HealthStatus _status;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _detail = "";
    [ObservableProperty] private bool _canAutoFix;
    [ObservableProperty] private string _fixDescription = "";
}
