using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevARIManager.Linux.Gui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Func<Task> _saveAction;

    public IReadOnlyList<string> ThemeOptions { get; } = ["dark", "light", "system"];
    public IReadOnlyList<string> InstallerOptions { get; } = ["all", "apt", "snap", "rustup"];

    [ObservableProperty]
    private string themeMode = "dark";

    [ObservableProperty]
    private string installerPreference = "all";

    [ObservableProperty]
    private bool autoStartEnabled;

    [ObservableProperty]
    private bool usePkexec = true;

    [ObservableProperty]
    private bool autoCheckAfterAction = true;

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public SettingsViewModel(Func<Task> saveAction)
    {
        _saveAction = saveAction;
        SaveSettingsCommand = new AsyncRelayCommand(() => _saveAction());
    }
}
