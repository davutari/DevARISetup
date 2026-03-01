namespace DevARIManager.Core.Models;

public enum EnvVarStatus
{
    Correct,
    Missing,
    Incorrect,
    Optional
}

public class EnvironmentVariableInfo
{
    public string Name { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
    public string? ExpectedValue { get; set; }
    public EnvVarStatus Status { get; set; }
    public bool IsSystemLevel { get; set; }
    public string? RelatedTool { get; set; }
}

public class PathEntryInfo
{
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public bool IsRequired { get; set; }
    public string? RelatedTool { get; set; }
}
