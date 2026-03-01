using DevARIManager.Core.Helpers;
using DevARIManager.Core.Models;

namespace DevARIManager.Core.Services;

public interface IEnvironmentManager
{
    List<EnvironmentVariableInfo> GetEnvironmentVariables();
    List<PathEntryInfo> GetPathEntries();
    Task<bool> AddToPathAsync(string path, bool systemLevel = true);
    Task<bool> SetEnvironmentVariableAsync(string name, string value, bool systemLevel = true);
    Task<bool> RemoveFromPathAsync(string path, bool systemLevel = true);
    Task RefreshEnvironmentAsync();
}

public class EnvironmentManager : IEnvironmentManager
{
    private readonly IProcessRunner _process;
    private readonly IToolManager _toolManager;

    public EnvironmentManager(IProcessRunner process, IToolManager toolManager)
    {
        _process = process;
        _toolManager = toolManager;
    }

    public List<EnvironmentVariableInfo> GetEnvironmentVariables()
    {
        var vars = new List<EnvironmentVariableInfo>();
        var tools = _toolManager.GetAllTools();

        var envVarNames = tools.SelectMany(t => t.EnvironmentVariables ?? [])
            .Distinct()
            .ToList();

        foreach (var varName in envVarNames)
        {
            var value = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine)
                     ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);

            var relatedTool = tools.FirstOrDefault(t => t.EnvironmentVariables?.Contains(varName) == true);

            vars.Add(new EnvironmentVariableInfo
            {
                Name = varName,
                CurrentValue = value,
                Status = string.IsNullOrEmpty(value) ? EnvVarStatus.Missing : EnvVarStatus.Correct,
                IsSystemLevel = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine) != null,
                RelatedTool = relatedTool?.DisplayName
            });
        }

        return vars;
    }

    public List<PathEntryInfo> GetPathEntries()
    {
        var entries = new List<PathEntryInfo>();
        var systemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
        var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
        var allPaths = $"{systemPath};{userPath}".Split(';', StringSplitOptions.RemoveEmptyEntries);

        var tools = _toolManager.GetAllTools();
        var requiredPaths = tools.SelectMany(t => (t.RequiredPaths ?? []).Select(p => new { Path = p, Tool = t.DisplayName })).ToList();

        foreach (var req in requiredPaths)
        {
            var exists = allPaths.Any(p => p.Trim().Equals(req.Path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
                                        || p.Trim().Equals(req.Path, StringComparison.OrdinalIgnoreCase));
            entries.Add(new PathEntryInfo
            {
                Path = req.Path,
                Exists = exists,
                IsRequired = true,
                RelatedTool = req.Tool
            });
        }

        return entries;
    }

    public async Task<bool> AddToPathAsync(string path, bool systemLevel = true)
    {
        var target = systemLevel ? "Machine" : "User";
        var script = $@"
            $current = [Environment]::GetEnvironmentVariable('PATH', '{target}')
            if ($current -notlike '*{path}*') {{
                [Environment]::SetEnvironmentVariable('PATH', ""$current;{path}"", '{target}')
            }}";
        var result = await _process.RunPowerShellAsync(script);
        if (result.Success) await RefreshEnvironmentAsync();
        return result.Success;
    }

    public async Task<bool> SetEnvironmentVariableAsync(string name, string value, bool systemLevel = true)
    {
        var target = systemLevel ? "Machine" : "User";
        var script = $"[Environment]::SetEnvironmentVariable('{name}', '{value}', '{target}')";
        var result = await _process.RunPowerShellAsync(script);
        if (result.Success) await RefreshEnvironmentAsync();
        return result.Success;
    }

    public async Task<bool> RemoveFromPathAsync(string path, bool systemLevel = true)
    {
        var target = systemLevel ? "Machine" : "User";
        var script = $@"
            $current = [Environment]::GetEnvironmentVariable('PATH', '{target}')
            $new = ($current.Split(';') | Where-Object {{ $_ -ne '{path}' }}) -join ';'
            [Environment]::SetEnvironmentVariable('PATH', $new, '{target}')";
        var result = await _process.RunPowerShellAsync(script);
        return result.Success;
    }

    public async Task RefreshEnvironmentAsync()
    {
        await _process.RunPowerShellAsync(
            @"Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class NativeMethods { [DllImport(""user32.dll"", SetLastError = true, CharSet = CharSet.Auto)] public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult); }'; $HWND_BROADCAST = [IntPtr]0xffff; $WM_SETTINGCHANGE = 0x1a; $result = [UIntPtr]::Zero; [NativeMethods]::SendMessageTimeout($HWND_BROADCAST, $WM_SETTINGCHANGE, [UIntPtr]::Zero, 'Environment', 2, 5000, [ref]$result)");
    }
}
