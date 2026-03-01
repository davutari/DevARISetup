using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Models;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    private readonly IToolManager _toolManager;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedCategory = "Tumu";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _progressMessage = "";
    [ObservableProperty] private bool _isOperating;

    public ObservableCollection<ToolItemViewModel> Tools { get; } = [];
    public ObservableCollection<string> Categories { get; } = ["Tumu", "Runtime", "Database", "WebFramework", "Mobile", "DevOps", "PackageManager", "IDE"];

    public ToolsViewModel(IToolManager toolManager)
    {
        _toolManager = toolManager;
        _ = LoadToolsAsync();
    }

    partial void OnSearchTextChanged(string value) => FilterTools();
    partial void OnSelectedCategoryChanged(string value) => FilterTools();

    private async Task LoadToolsAsync()
    {
        IsLoading = true;
        var tools = _toolManager.GetAllTools();
        var statuses = await _toolManager.CheckAllToolsAsync();

        Tools.Clear();
        foreach (var tool in tools)
        {
            var status = statuses.FirstOrDefault(s => s.ToolId == tool.Id);
            Tools.Add(new ToolItemViewModel(_toolManager)
            {
                Id = tool.Id,
                Name = tool.DisplayName,
                Description = tool.Description,
                Category = tool.Category.ToString(),
                IconGlyph = tool.IconGlyph,
                IsInstalled = status?.State == ToolState.Installed,
                InstalledVersion = status?.InstalledVersion ?? "",
                State = status?.State ?? ToolState.NotInstalled
            });
        }
        IsLoading = false;
    }

    private void FilterTools()
    {
        // Filtering is handled in View via CollectionViewSource
    }
}

public partial class ToolItemViewModel : ObservableObject
{
    private readonly IToolManager _toolManager;

    public ToolItemViewModel(IToolManager toolManager)
    {
        _toolManager = toolManager;
    }

    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private string _iconGlyph = "";
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private string _installedVersion = "";
    [ObservableProperty] private ToolState _state;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "";

    [RelayCommand]
    private async Task InstallAsync()
    {
        IsBusy = true;
        StatusText = "Yükleniyor...";
        var progress = new Progress<string>(msg => StatusText = msg);
        var success = await _toolManager.InstallToolAsync(Id, progress);
        IsInstalled = success;
        State = success ? ToolState.Installed : ToolState.Error;
        StatusText = success ? "Kuruldu" : "Hata oluştu";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        IsBusy = true;
        StatusText = "Kaldırılıyor...";
        var progress = new Progress<string>(msg => StatusText = msg);
        var success = await _toolManager.UninstallToolAsync(Id, progress);
        if (success)
        {
            IsInstalled = false;
            State = ToolState.NotInstalled;
        }
        StatusText = success ? "Kaldırıldı" : "Hata oluştu";
        IsBusy = false;
    }
}
