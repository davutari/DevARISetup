using System.Diagnostics;
using System.Text;

namespace DevARIManager.Core.Helpers;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool NeedsRestart => ExitCode == 3010;
    // winget exit code 3010 = reboot required (but install succeeded)
    public bool Success => ExitCode == 0 || ExitCode == 3010;
}

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string command, string? arguments = null, CancellationToken ct = default);
    Task<ProcessResult> RunWithLiveOutputAsync(string command, string? arguments, IProgress<string>? progress, CancellationToken ct = default);
    Task<ProcessResult> RunElevatedAsync(string command, string arguments, IProgress<string>? progress, CancellationToken ct = default);
    Task<ProcessResult> RunPowerShellAsync(string script, CancellationToken ct = default);
    Task<ProcessResult> RunPowerShellWithProgressAsync(string script, IProgress<string>? progress, CancellationToken ct = default);
    Task<string> GetCommandOutputAsync(string command, string? arguments = null);
    Task<bool> CommandExistsAsync(string command);
}

public class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string command, string? arguments = null, CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output.ToString().Trim(),
                Error = error.ToString().Trim()
            };
        }
        catch (Exception ex)
        {
            return new ProcessResult
            {
                ExitCode = -1,
                Output = string.Empty,
                Error = ex.Message
            };
        }
    }

    public async Task<ProcessResult> RunWithLiveOutputAsync(string command, string? arguments, IProgress<string>? progress, CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                output.AppendLine(e.Data);
                progress?.Report(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                error.AppendLine(e.Data);
                progress?.Report($"[STDERR] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output.ToString().Trim(),
                Error = error.ToString().Trim()
            };
        }
        catch (Exception ex)
        {
            return new ProcessResult { ExitCode = -1, Error = ex.Message };
        }
    }

    public async Task<ProcessResult> RunElevatedAsync(string command, string arguments, IProgress<string>? progress, CancellationToken ct = default)
    {
        var logFile = Path.Combine(Path.GetTempPath(), $"devari_elevated_{Guid.NewGuid():N}.log");
        try
        {
            // Build inner script: run command, tee output to log file, exit with correct code
            var innerCmd = $"& '{command}' {arguments} *>&1 | Tee-Object -FilePath '{logFile}'; exit $LASTEXITCODE";
            var psArgs = $"-NoProfile -ExecutionPolicy Bypass -Command \"{innerCmd}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = psArgs,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            progress?.Report("[elevated] Yönetici izni isteniyor...");

            using var process = Process.Start(psi);
            if (process == null)
                return new ProcessResult { ExitCode = -1, Error = "Yönetici izni alınamadı veya işlem başlatılamadı." };

            // Monitor log file for live output while process runs
            var lastPos = 0L;
            while (!process.HasExited)
            {
                if (File.Exists(logFile))
                {
                    try
                    {
                        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        if (fs.Length > lastPos)
                        {
                            fs.Position = lastPos;
                            using var reader = new StreamReader(fs, Encoding.UTF8);
                            var newContent = await reader.ReadToEndAsync(ct);
                            if (!string.IsNullOrWhiteSpace(newContent))
                            {
                                foreach (var line in newContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                                    progress?.Report(line.TrimEnd());
                            }
                            lastPos = fs.Length;
                        }
                    }
                    catch { /* file may be locked momentarily */ }
                }
                await Task.Delay(500, ct);
            }

            // Read final output
            var output = File.Exists(logFile) ? await File.ReadAllTextAsync(logFile, ct) : "";

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output.Trim(),
                Error = process.ExitCode != 0 ? output.Trim() : string.Empty
            };
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ProcessResult { ExitCode = -1, Error = "Yönetici izni reddedildi." };
        }
        catch (Exception ex)
        {
            return new ProcessResult { ExitCode = -1, Error = ex.Message };
        }
        finally
        {
            try { if (File.Exists(logFile)) File.Delete(logFile); } catch { }
        }
    }

    public async Task<ProcessResult> RunPowerShellAsync(string script, CancellationToken ct = default)
    {
        var escapedScript = script.Replace("\"", "\\\"");
        return await RunAsync("powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{escapedScript}\"", ct);
    }

    public async Task<ProcessResult> RunPowerShellWithProgressAsync(string script, IProgress<string>? progress, CancellationToken ct = default)
    {
        var escapedScript = script.Replace("\"", "\\\"");
        return await RunWithLiveOutputAsync("powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{escapedScript}\"", progress, ct);
    }

    public async Task<string> GetCommandOutputAsync(string command, string? arguments = null)
    {
        var result = await RunAsync(command, arguments);
        return result.Success ? result.Output : string.Empty;
    }

    public async Task<bool> CommandExistsAsync(string command)
    {
        var result = await RunAsync("where", command);
        return result.Success && !string.IsNullOrWhiteSpace(result.Output);
    }
}
