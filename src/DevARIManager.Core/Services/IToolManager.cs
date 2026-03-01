using DevARIManager.Core.Models;

namespace DevARIManager.Core.Services;

public interface IToolManager
{
    IReadOnlyList<ToolDefinition> GetAllTools();
    IReadOnlyList<ToolDefinition> GetToolsByCategory(ToolCategory category);
    Task<ToolStatus> CheckToolStatusAsync(string toolId, CancellationToken ct = default);
    Task<List<ToolStatus>> CheckAllToolsAsync(CancellationToken ct = default);
    Task<bool> InstallToolAsync(string toolId, IProgress<string>? progress = null, CancellationToken ct = default);
    Task<bool> UninstallToolAsync(string toolId, IProgress<string>? progress = null, CancellationToken ct = default);
}
