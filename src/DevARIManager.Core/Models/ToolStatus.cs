namespace DevARIManager.Core.Models;

public class ToolStatus
{
    public string ToolId { get; set; } = string.Empty;
    public ToolState State { get; set; } = ToolState.NotInstalled;
    public string? InstalledVersion { get; set; }
    public string? LatestVersion { get; set; }
    public string? InstallPath { get; set; }
    public bool IsPathConfigured { get; set; }
    public bool IsEnvConfigured { get; set; }
    public DateTime? LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}
