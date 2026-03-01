using DevARIManager.Core.Helpers;
using DevARIManager.Core.Models;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace DevARIManager.Core.Services;

public interface IServiceManager
{
    Task<List<ServiceInfo>> GetAllServicesAsync(CancellationToken ct = default);
    Task<bool> StartServiceAsync(string serviceId, CancellationToken ct = default);
    Task<bool> StopServiceAsync(string serviceId, CancellationToken ct = default);
    Task<bool> RestartServiceAsync(string serviceId, CancellationToken ct = default);
    bool IsPortInUse(int port);
}

public class ServiceManager : IServiceManager
{
    private readonly IProcessRunner _process;
    private readonly IToolManager _toolManager;
    private readonly ILogService _log;

    public ServiceManager(IProcessRunner process, IToolManager toolManager, ILogService log)
    {
        _process = process;
        _toolManager = toolManager;
        _log = log;
    }

    public async Task<List<ServiceInfo>> GetAllServicesAsync(CancellationToken ct = default)
    {
        var services = new List<ServiceInfo>();
        var tools = _toolManager.GetAllTools().Where(t => t.IsService && t.ServiceDef != null);

        foreach (var tool in tools)
        {
            var info = new ServiceInfo
            {
                Id = tool.Id,
                DisplayName = tool.DisplayName,
                IconGlyph = tool.IconGlyph,
                Port = tool.ServiceDef!.DefaultPort
            };

            try
            {
                // First try: check Windows Service
                var svcResult = await _process.RunPowerShellAsync(
                    $"Get-Service -Name '{tool.ServiceDef.ServiceName}' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Status", ct);

                if (svcResult.Success && !string.IsNullOrWhiteSpace(svcResult.Output))
                {
                    var status = svcResult.Output.Trim().ToLowerInvariant();
                    info.State = status switch
                    {
                        "running" => ServiceState.Running,
                        "stopped" => ServiceState.Stopped,
                        "startpending" => ServiceState.Starting,
                        "stoppending" => ServiceState.Stopping,
                        _ => ServiceState.Unknown
                    };
                }
                else
                {
                    // Fallback: check by process name
                    var procResult = await _process.RunPowerShellAsync(
                        $"Get-Process -Name '{tool.ServiceDef.ProcessName}' -ErrorAction SilentlyContinue | Select-Object -First 1 Id, @{{N='MemMB';E={{[math]::Round($_.WorkingSet64/1MB,1)}}}}", ct);

                    if (procResult.Success && !string.IsNullOrWhiteSpace(procResult.Output) &&
                        !procResult.Output.Contains("Cannot find"))
                    {
                        info.State = ServiceState.Running;
                        // Parse PID from output
                        var match = System.Text.RegularExpressions.Regex.Match(procResult.Output, @"^\s*(\d+)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int pid))
                            info.ProcessId = pid;
                    }
                    else
                    {
                        // Last resort: check port
                        info.State = info.Port > 0 && IsPortInUse(info.Port) ? ServiceState.Running : ServiceState.Stopped;
                    }
                }

                // If running, try to get PID by port
                if (info.State == ServiceState.Running && info.ProcessId == null && info.Port > 0)
                {
                    var pidResult = await _process.RunPowerShellAsync(
                        $"(Get-NetTCPConnection -LocalPort {info.Port} -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess", ct);
                    if (pidResult.Success && int.TryParse(pidResult.Output.Trim(), out int portPid) && portPid > 0)
                        info.ProcessId = portPid;
                }
            }
            catch
            {
                info.State = ServiceState.Unknown;
            }

            services.Add(info);
        }

        return services;
    }

    public async Task<bool> StartServiceAsync(string serviceId, CancellationToken ct = default)
    {
        var tool = _toolManager.GetAllTools().FirstOrDefault(t => t.Id == serviceId);
        if (tool?.ServiceDef == null) return false;

        _log.Info($"{tool.DisplayName} servisi başlatılıyor...", "Service");

        bool success = false;

        // Try Windows Service first
        var svcCheck = await _process.RunPowerShellAsync(
            $"Get-Service -Name '{tool.ServiceDef.ServiceName}' -ErrorAction SilentlyContinue", ct);

        if (svcCheck.Success && !string.IsNullOrWhiteSpace(svcCheck.Output))
        {
            var result = await _process.RunPowerShellAsync(
                $"Start-Service -Name '{tool.ServiceDef.ServiceName}' -ErrorAction Stop", ct);
            success = result.Success;
            if (!success)
                _log.Warn($"Windows servisi başlatılamadı, alternatif yöntem deneniyor...", "Service");
        }

        // Special case: Cassandra 5.x (no .bat on Windows, needs Java classpath setup)
        if (!success && serviceId == "cassandra")
        {
            success = await StartCassandraAsync(ct);
        }
        // Fallback: custom start command
        else if (!success && !string.IsNullOrEmpty(tool.ServiceDef.StartCommand))
        {
            _log.Info($"Özel komutla başlatılıyor: {tool.ServiceDef.StartCommand}", "Service");
            var cmdResult = await _process.RunPowerShellAsync(tool.ServiceDef.StartCommand, ct);
            success = cmdResult.Success;
            if (!success)
                _log.Error($"Başlatma hatası: {cmdResult.Error}", "Service");
        }

        // Verify by checking port or process after a short delay
        if (success)
        {
            await Task.Delay(1500, ct);
            var running = (tool.ServiceDef.DefaultPort > 0 && IsPortInUse(tool.ServiceDef.DefaultPort));
            if (!running)
            {
                // Also check by process
                var procCheck = await _process.RunPowerShellAsync(
                    $"Get-Process -Name '{tool.ServiceDef.ProcessName}' -ErrorAction SilentlyContinue", ct);
                running = procCheck.Success && !string.IsNullOrWhiteSpace(procCheck.Output);
            }
            success = running;
        }

        if (success)
            _log.Success($"{tool.DisplayName} servisi başlatıldı (port:{tool.ServiceDef.DefaultPort})", "Service");
        else
            _log.Error($"{tool.DisplayName} servisi başlatılamadı", "Service");

        return success;
    }

    public async Task<bool> StopServiceAsync(string serviceId, CancellationToken ct = default)
    {
        var tool = _toolManager.GetAllTools().FirstOrDefault(t => t.Id == serviceId);
        if (tool?.ServiceDef == null) return false;

        _log.Info($"{tool.DisplayName} servisi durduruluyor...", "Service");

        bool success = false;

        // Try Windows Service first
        var svcCheck = await _process.RunPowerShellAsync(
            $"Get-Service -Name '{tool.ServiceDef.ServiceName}' -ErrorAction SilentlyContinue", ct);

        if (svcCheck.Success && !string.IsNullOrWhiteSpace(svcCheck.Output))
        {
            var result = await _process.RunPowerShellAsync(
                $"Stop-Service -Name '{tool.ServiceDef.ServiceName}' -Force -ErrorAction Stop", ct);
            success = result.Success;
        }

        // Fallback: custom stop command
        if (!success && !string.IsNullOrEmpty(tool.ServiceDef.StopCommand))
        {
            var cmdResult = await _process.RunPowerShellAsync(tool.ServiceDef.StopCommand, ct);
            success = cmdResult.Success;
        }

        // Last resort: kill process by name
        if (!success)
        {
            var killResult = await _process.RunPowerShellAsync(
                $"Stop-Process -Name '{tool.ServiceDef.ProcessName}' -Force -ErrorAction SilentlyContinue", ct);
            success = killResult.Success;
        }

        if (success)
            _log.Success($"{tool.DisplayName} servisi durduruldu", "Service");
        else
            _log.Error($"{tool.DisplayName} servisi durdurulamadı", "Service");

        return success;
    }

    public async Task<bool> RestartServiceAsync(string serviceId, CancellationToken ct = default)
    {
        await StopServiceAsync(serviceId, ct);
        await Task.Delay(2000, ct);
        return await StartServiceAsync(serviceId, ct);
    }

    /// <summary>
    /// Starts Cassandra 5.x on Windows by constructing the Java classpath and JVM options
    /// from the config files (since Cassandra 5.x dropped Windows .bat support).
    /// </summary>
    private async Task<bool> StartCassandraAsync(CancellationToken ct)
    {
        try
        {
            // Find Cassandra home (C:\tools\apache-cassandra-X.X.X)
            var cassHome = ToolManager.FindCassandraHome();
            if (cassHome == null)
            {
                _log.Error("Cassandra dizini bulunamadı (C:\\tools\\apache-cassandra-*)", "Service");
                return false;
            }

            // Find Java 11+ (Cassandra 5.x requires JDK 11+)
            var javaExe = ToolManager.FindJava17();
            if (javaExe == null)
            {
                _log.Error("Java 11+ bulunamadı (Eclipse Adoptium veya Microsoft OpenJDK)", "Service");
                return false;
            }

            _log.Info($"Cassandra: {cassHome}, Java: {javaExe}", "Service");

            // Create required data directories
            foreach (var dir in new[] { "data", "commitlog", "saved_caches", "hints", "logs" })
                Directory.CreateDirectory(Path.Combine(cassHome, dir));

            // Read JVM options from config files
            var jvmOpts = new List<string>();
            foreach (var optFile in new[] { "jvm-server.options", "jvm17-server.options" })
            {
                var optPath = Path.Combine(cassHome, "conf", optFile);
                if (!File.Exists(optPath)) continue;

                foreach (var line in await File.ReadAllLinesAsync(optPath, ct))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                        jvmOpts.Add(trimmed);
                }
            }

            // Add Cassandra-specific system properties
            var dataDir = Path.Combine(cassHome, "data");
            var logsDir = Path.Combine(cassHome, "logs");
            var configUri = "file:///" + cassHome.Replace('\\', '/') + "/conf/cassandra.yaml";

            jvmOpts.AddRange(new[]
            {
                $"-Dcassandra.storagedir={dataDir}",
                $"-Dcassandra.logdir={logsDir}",
                "-Dlogback.configurationFile=logback.xml",
                $"-Dcassandra.config={configUri}",
                "-Xms256M",
                "-Xmx1G"
            });

            // Build classpath: conf + all jars in lib
            var classpath = $"{Path.Combine(cassHome, "conf")};{Path.Combine(cassHome, "lib", "*")}";
            jvmOpts.AddRange(new[] { "-cp", classpath, "org.apache.cassandra.service.CassandraDaemon" });

            // Build argument string (quote args that contain spaces)
            var argsString = string.Join(" ", jvmOpts.Select(o =>
                o.Contains(' ') && !o.StartsWith('"') ? $"\"{o}\"" : o));

            var psi = new ProcessStartInfo
            {
                FileName = javaExe,
                Arguments = argsString,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = cassHome
            };

            var process = Process.Start(psi);
            if (process == null)
            {
                _log.Error("Cassandra Java işlemi başlatılamadı", "Service");
                return false;
            }

            _log.Info($"Cassandra başlatıldı (PID: {process.Id})", "Service");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Cassandra başlatma hatası: {ex.Message}", "Service");
            return false;
        }
    }

    public bool IsPortInUse(int port)
    {
        if (port <= 0) return false;
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = properties.GetActiveTcpListeners();
            return listeners.Any(l => l.Port == port);
        }
        catch
        {
            return false;
        }
    }
}
