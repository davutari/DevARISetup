using DevARIManager.Core.Helpers;
using DevARIManager.Core.Models;

namespace DevARIManager.Core.Services;

public interface IHealthChecker
{
    Task<HealthReport> RunFullCheckAsync(IProgress<int>? progress = null, CancellationToken ct = default);
}

public class HealthChecker : IHealthChecker
{
    private readonly IToolManager _toolManager;
    private readonly IEnvironmentManager _envManager;
    private readonly IProcessRunner _process;
    private readonly ILogService _log;

    public HealthChecker(IToolManager toolManager, IEnvironmentManager envManager, IProcessRunner process, ILogService log)
    {
        _toolManager = toolManager;
        _envManager = envManager;
        _process = process;
        _log = log;
    }

    public async Task<HealthReport> RunFullCheckAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        _log.Info("Sağlık taraması başlatılıyor...", "HealthCheck");
        var report = new HealthReport();
        var tools = _toolManager.GetAllTools();
        var totalSteps = tools.Count + 3;
        var currentStep = 0;

        // 1. Check all tools
        foreach (var tool in tools)
        {
            ct.ThrowIfCancellationRequested();
            var status = await _toolManager.CheckToolStatusAsync(tool.Id, ct);

            if (status.State == ToolState.Installed)
            {
                report.Results.Add(new HealthCheckResult
                {
                    ToolId = tool.Id,
                    ToolName = tool.DisplayName,
                    Status = HealthStatus.Healthy,
                    Message = $"{tool.DisplayName} v{status.InstalledVersion} kurulu"
                });
            }
            else if (status.State == ToolState.NotInstalled)
            {
                report.Results.Add(new HealthCheckResult
                {
                    ToolId = tool.Id,
                    ToolName = tool.DisplayName,
                    Status = HealthStatus.Warning,
                    Message = $"{tool.DisplayName} kurulu değil",
                    CanAutoFix = true,
                    FixDescription = $"{tool.DisplayName} yükle"
                });
            }

            currentStep++;
            progress?.Report((int)((double)currentStep / totalSteps * 100));
        }

        // 2. PATH checks
        ct.ThrowIfCancellationRequested();
        var pathEntries = _envManager.GetPathEntries();
        foreach (var entry in pathEntries.Where(e => !e.Exists && e.IsRequired))
        {
            report.Results.Add(new HealthCheckResult
            {
                ToolId = entry.RelatedTool ?? "path",
                ToolName = entry.RelatedTool ?? "PATH",
                Status = HealthStatus.Error,
                Message = $"PATH eksik: {entry.Path}",
                Detail = $"{entry.RelatedTool} için {entry.Path} PATH'te bulunamadı",
                CanAutoFix = true,
                FixDescription = $"PATH'e {entry.Path} ekle"
            });
        }
        currentStep++;
        progress?.Report((int)((double)currentStep / totalSteps * 100));

        // 3. Environment variable checks
        ct.ThrowIfCancellationRequested();
        var envVars = _envManager.GetEnvironmentVariables();
        foreach (var env in envVars.Where(e => e.Status == EnvVarStatus.Missing))
        {
            report.Results.Add(new HealthCheckResult
            {
                ToolId = env.RelatedTool ?? "env",
                ToolName = env.RelatedTool ?? "Ortam",
                Status = HealthStatus.Error,
                Message = $"{env.Name} ortam değişkeni tanımlı değil",
                Detail = $"{env.RelatedTool} için {env.Name} tanımlanmalı",
                CanAutoFix = false,
                FixDescription = $"{env.Name} değerini tanımlayın"
            });
        }
        currentStep++;
        progress?.Report((int)((double)currentStep / totalSteps * 100));

        // 4. Flutter doctor
        ct.ThrowIfCancellationRequested();
        await RunFlutterDoctorCheckAsync(report, ct);

        // 5. Android SDK check
        await RunAndroidSdkCheckAsync(report);

        var errors = report.Results.Count(r => r.Status == HealthStatus.Error);
        var warnings = report.Results.Count(r => r.Status == HealthStatus.Warning);
        var healthy = report.Results.Count(r => r.Status == HealthStatus.Healthy);
        _log.Success($"Sağlık taraması tamamlandı: {healthy} sağlıklı, {warnings} uyarı, {errors} hata", "HealthCheck");

        progress?.Report(100);
        return report;
    }

    private async Task RunFlutterDoctorCheckAsync(HealthReport report, CancellationToken ct)
    {
        var exists = await _process.CommandExistsAsync("flutter");
        if (!exists) return;

        try
        {
            var result = await _process.RunAsync("flutter", "doctor --no-color", ct);
            var output = !string.IsNullOrWhiteSpace(result.Output) ? result.Output : result.Error;
            if (string.IsNullOrWhiteSpace(output)) return;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[✗]") || trimmed.StartsWith("[X]"))
                {
                    report.Results.Add(new HealthCheckResult
                    {
                        ToolId = "flutter", ToolName = "Flutter Doctor",
                        Status = HealthStatus.Error,
                        Message = trimmed.TrimStart('[', '✗', 'X', ']', ' '),
                        Detail = "flutter doctor"
                    });
                }
                else if (trimmed.StartsWith("[!]"))
                {
                    report.Results.Add(new HealthCheckResult
                    {
                        ToolId = "flutter", ToolName = "Flutter Doctor",
                        Status = HealthStatus.Warning,
                        Message = trimmed.TrimStart('[', '!', ']', ' '),
                        Detail = "flutter doctor"
                    });
                }
            }
        }
        catch { /* skip if flutter doctor fails */ }
    }

    private Task RunAndroidSdkCheckAsync(HealthReport report)
    {
        var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME")
                       ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(androidHome)) return Task.CompletedTask;

        var dirs = new[] { "platform-tools", "build-tools", "emulator" };
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(Path.Combine(androidHome, dir)))
            {
                report.Results.Add(new HealthCheckResult
                {
                    ToolId = "android-studio", ToolName = "Android SDK",
                    Status = HealthStatus.Warning,
                    Message = $"Android SDK '{dir}' bulunamadı",
                    Detail = "Android Studio SDK Manager ile yükleyin"
                });
            }
        }

        if (!Directory.Exists(Path.Combine(androidHome, "licenses")))
        {
            report.Results.Add(new HealthCheckResult
            {
                ToolId = "android-studio", ToolName = "Android SDK",
                Status = HealthStatus.Warning,
                Message = "Android SDK lisansları kabul edilmemiş",
                Detail = "flutter doctor --android-licenses komutu ile kabul edin"
            });
        }

        return Task.CompletedTask;
    }
}
