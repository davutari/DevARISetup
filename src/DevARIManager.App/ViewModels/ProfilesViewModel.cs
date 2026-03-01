using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Models;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private readonly IProfileManager _profileManager;
    private readonly IToolManager _toolManager;
    private readonly ILogService _logService;

    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private string _installStatus = "";
    [ObservableProperty] private int _installProgress;
    [ObservableProperty] private int _totalSteps;
    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private ProfileCardViewModel? _selectedProfile;

    public ObservableCollection<ProfileCardViewModel> Profiles { get; } = [];

    public ProfilesViewModel(IProfileManager profileManager, IToolManager toolManager, ILogService logService)
    {
        _profileManager = profileManager;
        _toolManager = toolManager;
        _logService = logService;
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        var allTools = _toolManager.GetAllTools();

        foreach (var profile in _profileManager.GetAllProfiles())
        {
            var toolNames = profile.ToolIds
                .Select(id => allTools.FirstOrDefault(t => t.Id == id)?.DisplayName ?? id)
                .ToList();

            Profiles.Add(new ProfileCardViewModel
            {
                Id = profile.Id,
                Name = profile.Name,
                Description = profile.Description,
                IconKind = profile.IconGlyph,
                ToolCount = profile.ToolIds.Length,
                ToolNames = string.Join(", ", toolNames),
                ToolIds = profile.ToolIds
            });
        }
    }

    [RelayCommand]
    private async Task InstallProfileAsync(ProfileCardViewModel? profile)
    {
        if (profile == null || IsInstalling) return;

        SelectedProfile = profile;
        IsInstalling = true;
        TotalSteps = profile.ToolIds.Length;
        CurrentStep = 0;
        InstallProgress = 0;

        _logService.Info($"=== Profil kurulumu başlıyor: {profile.Name} ===", "Profil");

        foreach (var toolId in profile.ToolIds)
        {
            var tool = _toolManager.GetAllTools().FirstOrDefault(t => t.Id == toolId);
            var toolName = tool?.DisplayName ?? toolId;

            // Check if already installed
            var status = await _toolManager.CheckToolStatusAsync(toolId);
            if (status.State == ToolState.Installed)
            {
                _logService.Success($"[ATLA] {toolName} v{status.InstalledVersion} zaten kurulu", "Profil");
                InstallStatus = $"{toolName} zaten kurulu";
            }
            else
            {
                InstallStatus = $"{toolName} yükleniyor...";
                _logService.Info($"[YUKLE] {toolName} yükleniyor...", "Profil");

                var progress = new Progress<string>(msg =>
                {
                    _logService.Info(msg, toolName);
                    InstallStatus = msg;
                });

                var success = await _toolManager.InstallToolAsync(toolId, progress);

                if (success)
                    _logService.Success($"[OK] {toolName} başarıyla yüklendi", "Profil");
                else
                    _logService.Error($"[HATA] {toolName} yüklenemedi", "Profil");
            }

            CurrentStep++;
            InstallProgress = (int)((double)CurrentStep / TotalSteps * 100);
        }

        _logService.Success($"=== Profil kurulumu tamamlandı: {profile.Name} ===", "Profil");
        InstallStatus = $"{profile.Name} profili kurulumu tamamlandı!";
        IsInstalling = false;
    }
}

public partial class ProfileCardViewModel : ObservableObject
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _iconKind = "";
    [ObservableProperty] private int _toolCount;
    [ObservableProperty] private string _toolNames = "";

    public string[] ToolIds { get; set; } = [];
}
