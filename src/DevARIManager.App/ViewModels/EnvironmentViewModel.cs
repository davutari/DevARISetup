using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Models;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class EnvironmentViewModel : ObservableObject
{
    private readonly IEnvironmentManager _envManager;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _missingPathCount;
    [ObservableProperty] private int _missingEnvCount;

    public ObservableCollection<PathEntryViewModel> PathEntries { get; } = [];
    public ObservableCollection<EnvVarViewModel> EnvironmentVariables { get; } = [];

    public EnvironmentViewModel(IEnvironmentManager envManager)
    {
        _envManager = envManager;
        LoadData();
    }

    private void LoadData()
    {
        IsLoading = true;

        var paths = _envManager.GetPathEntries();
        PathEntries.Clear();
        foreach (var p in paths)
        {
            PathEntries.Add(new PathEntryViewModel(_envManager)
            {
                Path = p.Path,
                Exists = p.Exists,
                RelatedTool = p.RelatedTool ?? ""
            });
        }
        MissingPathCount = paths.Count(p => !p.Exists && p.IsRequired);

        var vars = _envManager.GetEnvironmentVariables();
        EnvironmentVariables.Clear();
        foreach (var v in vars)
        {
            EnvironmentVariables.Add(new EnvVarViewModel
            {
                Name = v.Name,
                Value = v.CurrentValue ?? "(tanimlanmamis)",
                Status = v.Status,
                RelatedTool = v.RelatedTool ?? ""
            });
        }
        MissingEnvCount = vars.Count(v => v.Status == EnvVarStatus.Missing);

        IsLoading = false;
    }

    [RelayCommand]
    private async Task FixAllPathsAsync()
    {
        foreach (var entry in PathEntries.Where(p => !p.Exists))
        {
            await _envManager.AddToPathAsync(entry.Path);
            entry.Exists = true;
        }
        MissingPathCount = 0;
    }

    [RelayCommand]
    private void Refresh() => LoadData();
}

public partial class PathEntryViewModel : ObservableObject
{
    private readonly IEnvironmentManager _envManager;

    public PathEntryViewModel(IEnvironmentManager envManager) => _envManager = envManager;

    [ObservableProperty] private string _path = "";
    [ObservableProperty] private bool _exists;
    [ObservableProperty] private string _relatedTool = "";

    [RelayCommand]
    private async Task AddToPathAsync()
    {
        await _envManager.AddToPathAsync(Path);
        Exists = true;
    }
}

public partial class EnvVarViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _value = "";
    [ObservableProperty] private EnvVarStatus _status;
    [ObservableProperty] private string _relatedTool = "";
}
