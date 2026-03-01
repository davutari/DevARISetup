namespace DevARIManager.Core.Models;

public enum ToolCategory
{
    Runtime,
    Database,
    WebFramework,
    Mobile,
    DevOps,
    PackageManager,
    IDE
}

public enum InstallMethod
{
    Winget,
    Chocolatey,
    DirectDownload,
    CustomScript,
    Npm
}

public enum ToolState
{
    NotInstalled,
    Installed,
    UpdateAvailable,
    Installing,
    Uninstalling,
    Error
}

public class ToolDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ToolCategory Category { get; set; }
    public string IconGlyph { get; set; } = string.Empty;
    public string VersionCheckCommand { get; set; } = string.Empty;
    public string[] RequiredPaths { get; set; } = [];
    public string[] EnvironmentVariables { get; set; } = [];
    public string[] Dependencies { get; set; } = [];
    public InstallMethod PreferredInstallMethod { get; set; }
    public string? WingetId { get; set; }
    public string? ChocoId { get; set; }
    public string? DirectDownloadUrl { get; set; }
    public string? SilentInstallArgs { get; set; }
    public bool IsService { get; set; }
    public ServiceDefinition? ServiceDef { get; set; }
}

public class ServiceDefinition
{
    public string ServiceName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string StartCommand { get; set; } = string.Empty;
    public string StopCommand { get; set; } = string.Empty;
    public int DefaultPort { get; set; }
}
