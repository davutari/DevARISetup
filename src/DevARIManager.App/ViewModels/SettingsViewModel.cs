using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILogService _log;

    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private string _language = "";
    [ObservableProperty] private bool _autoStartServices;
    [ObservableProperty] private bool _checkUpdatesOnStartup;
    [ObservableProperty] private string _preferredInstaller = "";
    [ObservableProperty] private string _appVersion = "1.0.0";
    [ObservableProperty] private string _saveStatus = "";

    public SettingsViewModel(ISettingsService settings, ILogService log)
    {
        _settings = settings;
        _log = log;

        // Load from persisted settings
        IsDarkTheme = _settings.Current.IsDarkTheme;
        Language = _settings.Current.Language;
        AutoStartServices = _settings.Current.AutoStartServices;
        CheckUpdatesOnStartup = _settings.Current.CheckUpdatesOnStartup;
        PreferredInstaller = _settings.Current.PreferredInstaller;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Current.IsDarkTheme = IsDarkTheme;
        _settings.Current.Language = Language;
        _settings.Current.AutoStartServices = AutoStartServices;
        _settings.Current.CheckUpdatesOnStartup = CheckUpdatesOnStartup;
        _settings.Current.PreferredInstaller = PreferredInstaller;

        _settings.Save();
        _log.Success("Ayarlar kaydedildi", "Ayarlar");
        SaveStatus = "Ayarlar başarıyla kaydedildi!";
    }
}
