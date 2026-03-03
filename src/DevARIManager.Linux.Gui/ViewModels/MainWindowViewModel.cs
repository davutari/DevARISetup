using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Linux.Gui.Models;
using DevARIManager.Linux.Gui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DevARIManager.Linux.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<LinuxToolItem> Tools { get; } = new(LinuxToolCatalog.Create().OrderBy(x => x.DisplayName));
    public ObservableCollection<LinuxToolItem> VisibleTools { get; } = [];
    public ObservableCollection<LinuxToolItem> ServiceTools { get; } = [];
    public IReadOnlyList<string> FilterOptions { get; } = ["all", "installed", "missing", "apt", "snap", "rustup"];
    public IReadOnlyList<string> SortOptions { get; } = ["name-asc", "name-desc", "status", "installer"];
    public IReadOnlyList<string> ThemeOptions { get; } = ["dark", "light", "system"];
    public IReadOnlyList<string> InstallerOptions { get; } = ["all", "apt", "snap", "rustup"];

    [ObservableProperty]
    private string activityLog = "Hazir.";

    [ObservableProperty]
    private int installedCount;

    [ObservableProperty]
    private int notInstalledCount;

    [ObservableProperty]
    private int unknownCount;

    [ObservableProperty]
    private int activeServiceCount;

    [ObservableProperty]
    private int inactiveServiceCount;

    [ObservableProperty]
    private string selectedFilter = "all";

    [ObservableProperty]
    private string selectedMenu = "dashboard";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedSort = "name-asc";

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

    public IRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand CheckAllCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> CheckCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> InstallCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> UninstallCommand { get; }
    public IRelayCommand<string> SetFilterCommand { get; }
    public IAsyncRelayCommand<string> MenuCommand { get; }
    public IRelayCommand<string> SetSortCommand { get; }
    public IRelayCommand<string> SetInstallerPreferenceCommand { get; }
    public IRelayCommand<string> SetThemeCommand { get; }
    public IAsyncRelayCommand SaveSettingsCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> StartServiceCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> StopServiceCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> CheckServiceCommand { get; }

    public bool IsDashboardVisible => SelectedMenu is "dashboard" or "tools" or "health";
    public bool IsServicesVisible => SelectedMenu == "services";
    public bool IsSettingsVisible => SelectedMenu == "settings";
    public int TotalTools => Tools.Count;
    public double HealthPercent => TotalTools == 0 ? 0 : InstalledCount * 100.0 / TotalTools;

    public MainWindowViewModel()
    {
        foreach (var tool in Tools)
        {
            tool.PropertyChanged += OnToolPropertyChanged;
        }

        RefreshCommand = new RelayCommand(() =>
        {
            foreach (var tool in Tools)
            {
                tool.Status = "unknown";
                tool.StatusColor = Brushes.Gray;
                if (tool.IsService)
                {
                    tool.ServiceState = "unknown";
                    tool.ServiceStateColor = Brushes.Gray;
                }
            }

            RefreshVisibleTools();
            RefreshStats();
            ActivityLog = "Arac durumu sifirlandi.";
        });

        CheckAllCommand = new AsyncRelayCommand(CheckAllAsync);
        CheckCommand = new AsyncRelayCommand<LinuxToolItem>(CheckAsync);
        InstallCommand = new AsyncRelayCommand<LinuxToolItem>(InstallAsync);
        UninstallCommand = new AsyncRelayCommand<LinuxToolItem>(UninstallAsync);
        SetFilterCommand = new RelayCommand<string>(SetFilter);
        MenuCommand = new AsyncRelayCommand<string>(HandleMenuAsync);
        SetSortCommand = new RelayCommand<string>(SetSort);
        SetInstallerPreferenceCommand = new RelayCommand<string>(SetInstallerPreference);
        SetThemeCommand = new RelayCommand<string>(SetTheme);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        StartServiceCommand = new AsyncRelayCommand<LinuxToolItem>(StartServiceAsync);
        StopServiceCommand = new AsyncRelayCommand<LinuxToolItem>(StopServiceAsync);
        CheckServiceCommand = new AsyncRelayCommand<LinuxToolItem>(CheckServiceStateAsync);

        LoadSettings();
        RefreshVisibleTools();
        RefreshStats();
    }

    private async Task CheckAllAsync()
    {
        ActivityLog = "Tum araclar kontrol ediliyor...";
        foreach (var tool in Tools)
        {
            await CheckAsync(tool);
            if (tool.IsService)
            {
                await CheckServiceStateAsync(tool);
            }
        }

        ActivityLog = "Tum kontrol islemi bitti.";
        RefreshStats();
    }

    private async Task CheckAsync(LinuxToolItem? tool)
    {
        if (tool is null)
        {
            return;
        }

        var result = await RunShellAsync(tool.CheckCommand);
        if (result.ExitCode == 0)
        {
            tool.Status = "installed";
            tool.StatusColor = Brushes.LimeGreen;
        }
        else
        {
            tool.Status = "not-installed";
            tool.StatusColor = Brushes.OrangeRed;
        }

        if (tool.IsService)
        {
            await CheckServiceStateAsync(tool);
        }

        RefreshStats();
    }

    private async Task InstallAsync(LinuxToolItem? tool)
    {
        if (tool is null)
        {
            return;
        }

        ActivityLog = $"{tool.DisplayName} kuruluyor...";
        var result = await RunPrivilegedAsync(tool.InstallCommand);
        ActivityLog = result.ExitCode == 0
            ? $"{tool.DisplayName} kurulum tamamlandi."
            : $"{tool.DisplayName} kurulum hatasi: {FirstLine(result.Error)}";

        if (AutoCheckAfterAction)
        {
            await CheckAsync(tool);
        }
    }

    private async Task UninstallAsync(LinuxToolItem? tool)
    {
        if (tool is null)
        {
            return;
        }

        ActivityLog = $"{tool.DisplayName} kaldiriliyor...";
        var result = await RunPrivilegedAsync(tool.UninstallCommand);
        ActivityLog = result.ExitCode == 0
            ? $"{tool.DisplayName} kaldirma tamamlandi."
            : $"{tool.DisplayName} kaldirma hatasi: {FirstLine(result.Error)}";

        if (AutoCheckAfterAction)
        {
            await CheckAsync(tool);
        }
    }

    private void SetFilter(string? filter)
    {
        SelectedFilter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.ToLowerInvariant();
        RefreshVisibleTools();
    }

    private void SetSort(string? sort)
    {
        SelectedSort = string.IsNullOrWhiteSpace(sort) ? "name-asc" : sort.ToLowerInvariant();
        RefreshVisibleTools();
    }

    private void SetTheme(string? mode)
    {
        ThemeMode = string.IsNullOrWhiteSpace(mode) ? "dark" : mode.ToLowerInvariant();
        ApplyTheme();
    }

    private void SetInstallerPreference(string? preference)
    {
        InstallerPreference = string.IsNullOrWhiteSpace(preference) ? "all" : preference.ToLowerInvariant();
        RefreshVisibleTools();
    }

    private void RefreshVisibleTools()
    {
        var normalizedSearch = SearchText.Trim().ToLowerInvariant();

        var toolList = Tools
            .Where(t =>
                MatchesFilter(t, SelectedFilter) &&
                MatchesInstallerPreference(t, InstallerPreference) &&
                MatchesSearch(t, normalizedSearch))
            .ToList();
        toolList = ApplySort(toolList, SelectedSort);

        VisibleTools.Clear();
        foreach (var tool in toolList)
        {
            VisibleTools.Add(tool);
        }

        var serviceList = Tools
            .Where(t => t.IsService && MatchesSearch(t, normalizedSearch))
            .ToList();
        serviceList = ApplySort(serviceList, SelectedSort);

        ServiceTools.Clear();
        foreach (var tool in serviceList)
        {
            ServiceTools.Add(tool);
        }
    }

    private static bool MatchesFilter(LinuxToolItem tool, string filter)
    {
        return filter switch
        {
            "apt" => tool.Installer.Equals("apt", StringComparison.OrdinalIgnoreCase),
            "snap" => tool.Installer.Equals("snap", StringComparison.OrdinalIgnoreCase),
            "rustup" => tool.Installer.Equals("rustup", StringComparison.OrdinalIgnoreCase),
            "installed" => tool.Status == "installed",
            "missing" => tool.Status == "not-installed",
            _ => true
        };
    }

    private static bool MatchesInstallerPreference(LinuxToolItem tool, string preference)
    {
        return preference switch
        {
            "apt" => tool.Installer.Equals("apt", StringComparison.OrdinalIgnoreCase),
            "snap" => tool.Installer.Equals("snap", StringComparison.OrdinalIgnoreCase),
            "rustup" => tool.Installer.Equals("rustup", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static bool MatchesSearch(LinuxToolItem tool, string normalizedSearch)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return true;
        }

        return tool.DisplayName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
            || tool.Id.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
            || tool.Installer.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase);
    }

    private static List<LinuxToolItem> ApplySort(List<LinuxToolItem> list, string sort)
    {
        return sort switch
        {
            "name-desc" => list.OrderByDescending(t => t.DisplayName).ToList(),
            "status" => list.OrderBy(t => StatusRank(t.Status)).ThenBy(t => t.DisplayName).ToList(),
            "installer" => list.OrderBy(t => t.Installer).ThenBy(t => t.DisplayName).ToList(),
            _ => list.OrderBy(t => t.DisplayName).ToList()
        };
    }

    private static int StatusRank(string status) => status switch
    {
        "installed" => 0,
        "unknown" => 1,
        "not-installed" => 2,
        _ => 3
    };

    private async Task HandleMenuAsync(string? menu)
    {
        var target = string.IsNullOrWhiteSpace(menu) ? "dashboard" : menu.ToLowerInvariant();
        SelectedMenu = target;

        switch (target)
        {
            case "dashboard":
            case "tools":
                SetFilter("all");
                ActivityLog = "Tum araclar listeleniyor.";
                break;
            case "services":
                ActivityLog = "Servis paneli acildi.";
                break;
            case "health":
                await CheckAllAsync();
                break;
            case "settings":
                ActivityLog = "Ayarlar paneli acildi.";
                break;
            default:
                SetFilter("all");
                break;
        }
    }

    private async Task StartServiceAsync(LinuxToolItem? tool)
    {
        if (tool is null || !tool.IsService || string.IsNullOrWhiteSpace(tool.ServiceName))
        {
            return;
        }

        ActivityLog = $"{tool.DisplayName} servisi baslatiliyor...";
        var result = await RunPrivilegedAsync($"systemctl start {tool.ServiceName}");
        ActivityLog = result.ExitCode == 0
            ? $"{tool.DisplayName} servisi baslatildi."
            : $"{tool.DisplayName} servis baslatma hatasi: {FirstLine(result.Error)}";
        await CheckServiceStateAsync(tool);
    }

    private async Task StopServiceAsync(LinuxToolItem? tool)
    {
        if (tool is null || !tool.IsService || string.IsNullOrWhiteSpace(tool.ServiceName))
        {
            return;
        }

        ActivityLog = $"{tool.DisplayName} servisi durduruluyor...";
        var result = await RunPrivilegedAsync($"systemctl stop {tool.ServiceName}");
        ActivityLog = result.ExitCode == 0
            ? $"{tool.DisplayName} servisi durduruldu."
            : $"{tool.DisplayName} servis durdurma hatasi: {FirstLine(result.Error)}";
        await CheckServiceStateAsync(tool);
    }

    private async Task CheckServiceStateAsync(LinuxToolItem? tool)
    {
        if (tool is null || !tool.IsService || string.IsNullOrWhiteSpace(tool.ServiceName))
        {
            return;
        }

        var result = await RunShellAsync($"systemctl is-active {tool.ServiceName} >/dev/null 2>&1");
        if (result.ExitCode == 0)
        {
            tool.ServiceState = "active";
            tool.ServiceStateColor = Brushes.MediumSpringGreen;
        }
        else
        {
            tool.ServiceState = "inactive";
            tool.ServiceStateColor = Brushes.OrangeRed;
        }
    }

    private void RefreshStats()
    {
        InstalledCount = Tools.Count(t => t.Status == "installed");
        NotInstalledCount = Tools.Count(t => t.Status == "not-installed");
        UnknownCount = Tools.Count(t => t.Status != "installed" && t.Status != "not-installed");
        ActiveServiceCount = Tools.Count(t => t.IsService && t.ServiceState == "active");
        InactiveServiceCount = Tools.Count(t => t.IsService && t.ServiceState != "active");
        OnPropertyChanged(nameof(HealthPercent));
        OnPropertyChanged(nameof(TotalTools));
    }

    private void OnToolPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LinuxToolItem.Status))
        {
            RefreshStats();
            if (SelectedFilter is "installed" or "missing")
            {
                RefreshVisibleTools();
            }
        }
        else if (e.PropertyName == nameof(LinuxToolItem.ServiceState))
        {
            RefreshStats();
        }
    }

    partial void OnSelectedMenuChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardVisible));
        OnPropertyChanged(nameof(IsServicesVisible));
        OnPropertyChanged(nameof(IsSettingsVisible));
    }

    partial void OnSearchTextChanged(string value) => RefreshVisibleTools();
    partial void OnSelectedFilterChanged(string value) => RefreshVisibleTools();
    partial void OnSelectedSortChanged(string value) => RefreshVisibleTools();
    partial void OnInstallerPreferenceChanged(string value) => RefreshVisibleTools();

    private static async Task<(int ExitCode, string Output, string Error)> RunShellAsync(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-lc \"{EscapeForDoubleQuotedBash(command)}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await outputTask, await errorTask);
    }

    private async Task<(int ExitCode, string Output, string Error)> RunPrivilegedAsync(string command)
    {
        if (!UsePkexec)
        {
            return await RunShellAsync(command);
        }

        var commandWithoutSudo = command.Replace("sudo ", "", StringComparison.OrdinalIgnoreCase);
        return await RunShellAsync($"pkexec bash -lc \"{EscapeForDoubleQuotedBash(commandWithoutSudo)}\" || bash -lc \"{EscapeForDoubleQuotedBash(command)}\"");
    }

    private static string EscapeForDoubleQuotedBash(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is '\\' or '"' or '$' or '`')
            {
                builder.Append('\\');
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static string FirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "bilinmeyen hata";
        }

        var line = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(line) ? "bilinmeyen hata" : line.Trim();
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            if (File.Exists(settingsPath))
            {
                var raw = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<LinuxUiSettings>(raw);
                if (data is not null)
                {
                    ThemeMode = string.IsNullOrWhiteSpace(data.ThemeMode) ? "dark" : data.ThemeMode;
                    InstallerPreference = string.IsNullOrWhiteSpace(data.InstallerPreference) ? "all" : data.InstallerPreference;
                    UsePkexec = data.UsePkexec;
                    AutoCheckAfterAction = data.AutoCheckAfterAction;
                }
            }
        }
        catch
        {
            // Defaults are already set.
        }

        AutoStartEnabled = File.Exists(GetAutostartPath());
        ApplyTheme();
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            Directory.CreateDirectory(GetConfigDirectory());
            var settings = new LinuxUiSettings
            {
                ThemeMode = ThemeMode,
                InstallerPreference = InstallerPreference,
                UsePkexec = UsePkexec,
                AutoCheckAfterAction = AutoCheckAfterAction
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(GetSettingsPath(), json);
            await ApplyAutostartAsync(AutoStartEnabled);
            ApplyTheme();
            ActivityLog = "Ayarlar kaydedildi.";
        }
        catch (Exception ex)
        {
            ActivityLog = $"Ayar kaydetme hatasi: {FirstLine(ex.Message)}";
        }
    }

    private async Task ApplyAutostartAsync(bool enabled)
    {
        var autostartPath = GetAutostartPath();
        var autostartDir = Path.GetDirectoryName(autostartPath);
        if (string.IsNullOrWhiteSpace(autostartDir))
        {
            return;
        }

        if (!enabled)
        {
            if (File.Exists(autostartPath))
            {
                File.Delete(autostartPath);
            }

            return;
        }

        Directory.CreateDirectory(autostartDir);
        var desktopContent = $$"""
[Desktop Entry]
Type=Application
Version=1.0
Name=DevARI Manager
Comment=DevARI Manager auto start
Exec={{ResolveAutostartExec()}}
Terminal=false
X-GNOME-Autostart-enabled=true
""";
        await File.WriteAllTextAsync(autostartPath, desktopContent);
    }

    private static string ResolveAutostartExec()
    {
        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
        if (!string.IsNullOrWhiteSpace(appImagePath))
        {
            return appImagePath;
        }

        const string knownPath = "/home/ara/Applications/DevARIManager/linux/devari-manager.AppImage";
        if (File.Exists(knownPath))
        {
            return knownPath;
        }

        return "devari-manager";
    }

    private void ApplyTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = ThemeMode switch
        {
            "light" => ThemeVariant.Light,
            "system" => ThemeVariant.Default,
            _ => ThemeVariant.Dark
        };
    }

    private static string GetConfigDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "devari-manager");
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(GetConfigDirectory(), "settings.json");
    }

    private static string GetAutostartPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart", "devari-manager.desktop");
    }

    private sealed class LinuxUiSettings
    {
        public string ThemeMode { get; init; } = "dark";
        public string InstallerPreference { get; init; } = "all";
        public bool UsePkexec { get; init; } = true;
        public bool AutoCheckAfterAction { get; init; } = true;
    }
}
