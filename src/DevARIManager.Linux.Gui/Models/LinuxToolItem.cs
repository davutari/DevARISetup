using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevARIManager.Linux.Gui.Models;

public partial class LinuxToolItem : ObservableObject
{
    [ObservableProperty]
    private string status = "unknown";

    [ObservableProperty]
    private IBrush statusColor = Brushes.Gray;

    [ObservableProperty]
    private string serviceState = "unknown";

    [ObservableProperty]
    private IBrush serviceStateColor = Brushes.Gray;

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Installer { get; init; }
    public required string CheckCommand { get; init; }
    public required string InstallCommand { get; init; }
    public required string UninstallCommand { get; init; }
    public bool IsService { get; init; }
    public string? ServiceName { get; init; }

    public string StatusLabel => Status switch
    {
        "installed" => "Kurulu",
        "not-installed" => "Eksik",
        _ => "Bilinmiyor"
    };

    public string ServiceStateLabel => ServiceState switch
    {
        "active" => "Aktif",
        "inactive" => "Pasif",
        _ => "Bilinmiyor"
    };

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusLabel));
    }

    partial void OnServiceStateChanged(string value)
    {
        OnPropertyChanged(nameof(ServiceStateLabel));
    }
}
