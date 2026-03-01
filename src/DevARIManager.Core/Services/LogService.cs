namespace DevARIManager.Core.Services;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public interface ILogService
{
    event Action<LogEntry>? OnLog;
    IReadOnlyList<LogEntry> Entries { get; }
    void Log(string message, LogLevel level = LogLevel.Info, string source = "");
    void Info(string message, string source = "");
    void Success(string message, string source = "");
    void Warn(string message, string source = "");
    void Error(string message, string source = "");
    void Clear();
}

public class LogService : ILogService
{
    private readonly List<LogEntry> _entries = [];
    private readonly object _lock = new();

    public event Action<LogEntry>? OnLog;

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) { return _entries.ToList().AsReadOnly(); } }
    }

    public void Log(string message, LogLevel level = LogLevel.Info, string source = "")
    {
        var entry = new LogEntry { Message = message, Level = level, Source = source };
        lock (_lock) { _entries.Add(entry); }
        OnLog?.Invoke(entry);
    }

    public void Info(string message, string source = "") => Log(message, LogLevel.Info, source);
    public void Success(string message, string source = "") => Log(message, LogLevel.Success, source);
    public void Warn(string message, string source = "") => Log(message, LogLevel.Warning, source);
    public void Error(string message, string source = "") => Log(message, LogLevel.Error, source);

    public void Clear()
    {
        lock (_lock) { _entries.Clear(); }
    }
}
