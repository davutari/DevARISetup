using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Core.Services;

namespace DevARIManager.App.ViewModels;

public partial class TerminalViewModel : ObservableObject
{
    private readonly ILogService _logService;
    private readonly Dispatcher _dispatcher;
    private readonly List<TerminalEntry> _allEntries = [];

    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private bool _autoScroll = true;
    [ObservableProperty] private int _totalEntries;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private int _successCount;
    [ObservableProperty] private string _selectedFilter = "All";

    // Raised when terminal needs to scroll to bottom
    public event Action? ScrollToBottomRequested;

    public ObservableCollection<TerminalEntry> Entries { get; } = [];

    public TerminalViewModel(ILogService logService)
    {
        _logService = logService;
        _dispatcher = Application.Current.Dispatcher;

        // Load existing entries
        foreach (var entry in _logService.Entries)
        {
            var te = ToTerminalEntry(entry);
            _allEntries.Add(te);
            Entries.Add(te);
        }
        UpdateCounts();

        // Subscribe to new logs
        _logService.OnLog += OnNewLog;
    }

    private void OnNewLog(LogEntry entry)
    {
        _dispatcher.BeginInvoke(() =>
        {
            var te = ToTerminalEntry(entry);
            _allEntries.Add(te);

            if (MatchesFilter(te))
                Entries.Add(te);

            UpdateCounts();

            if (AutoScroll)
                ScrollToBottomRequested?.Invoke();
        });
    }

    private bool MatchesFilter(TerminalEntry entry)
    {
        // Level filter
        if (SelectedFilter != "All")
        {
            var requiredLevel = SelectedFilter switch
            {
                "Error" => LogLevel.Error,
                "Warning" => LogLevel.Warning,
                "Success" => LogLevel.Success,
                _ => LogLevel.Info
            };
            if (entry.Level != requiredLevel) return false;
        }

        // Text filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            return entry.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                   entry.Source.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();
    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Entries.Clear();
        foreach (var entry in _allEntries)
        {
            if (MatchesFilter(entry))
                Entries.Add(entry);
        }
    }

    private void UpdateCounts()
    {
        TotalEntries = _allEntries.Count;
        ErrorCount = _allEntries.Count(e => e.Level == LogLevel.Error);
        WarningCount = _allEntries.Count(e => e.Level == LogLevel.Warning);
        SuccessCount = _allEntries.Count(e => e.Level == LogLevel.Success);
    }

    private static TerminalEntry ToTerminalEntry(LogEntry entry) => new()
    {
        Timestamp = entry.Timestamp.ToString("HH:mm:ss"),
        Level = entry.Level,
        Message = entry.Message,
        Source = entry.Source,
        Color = entry.Level switch
        {
            LogLevel.Success => "#FF22C55E",
            LogLevel.Warning => "#FFEAB308",
            LogLevel.Error => "#FFEF4444",
            _ => "#FF9CA3AF"
        },
        LevelTag = entry.Level switch
        {
            LogLevel.Success => "OK",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERR",
            _ => "INFO"
        }
    };

    [RelayCommand]
    private void Clear()
    {
        _logService.Clear();
        _allEntries.Clear();
        Entries.Clear();
        UpdateCounts();
    }

    [RelayCommand]
    private void FilterByLevel(string level)
    {
        SelectedFilter = level;
    }
}

public partial class TerminalEntry : ObservableObject
{
    [ObservableProperty] private string _timestamp = "";
    [ObservableProperty] private LogLevel _level;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _source = "";
    [ObservableProperty] private string _color = "#FF9CA3AF";
    [ObservableProperty] private string _levelTag = "INFO";
}
