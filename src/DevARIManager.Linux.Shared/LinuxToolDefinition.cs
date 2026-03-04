namespace DevARIManager.Linux.Shared;

public sealed record LinuxToolDefinition(
    string Id,
    string DisplayName,
    string Installer,
    string CheckCommand,
    string InstallCommand,
    string UninstallCommand,
    string? ServiceName = null);
