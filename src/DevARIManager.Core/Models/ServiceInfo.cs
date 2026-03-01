namespace DevARIManager.Core.Models;

public enum ServiceState
{
    Stopped,
    Running,
    Starting,
    Stopping,
    Error,
    Unknown
}

public class ServiceInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
    public ServiceState State { get; set; } = ServiceState.Unknown;
    public int Port { get; set; }
    public int? ProcessId { get; set; }
    public string? Version { get; set; }
    public long MemoryUsageMb { get; set; }
    public DateTime? StartTime { get; set; }
}
