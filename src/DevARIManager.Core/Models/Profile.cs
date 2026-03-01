namespace DevARIManager.Core.Models;

public class Profile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
    public string[] ToolIds { get; set; } = [];
    public bool IsBuiltIn { get; set; }
}
