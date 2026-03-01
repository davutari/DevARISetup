namespace DevARIManager.Core.Models;

public enum HealthStatus
{
    Healthy,
    Warning,
    Error
}

public class HealthCheckResult
{
    public string ToolId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public bool CanAutoFix { get; set; }
    public string? FixDescription { get; set; }
}

public class HealthReport
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<HealthCheckResult> Results { get; set; } = [];
    public int HealthyCount => Results.Count(r => r.Status == HealthStatus.Healthy);
    public int WarningCount => Results.Count(r => r.Status == HealthStatus.Warning);
    public int ErrorCount => Results.Count(r => r.Status == HealthStatus.Error);
}
