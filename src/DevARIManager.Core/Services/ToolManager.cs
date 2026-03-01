using DevARIManager.Core.Helpers;
using DevARIManager.Core.Models;
using System.Text.RegularExpressions;

namespace DevARIManager.Core.Services;

public class ToolManager : IToolManager
{
    private readonly IProcessRunner _process;
    private readonly ILogService _log;
    private readonly List<ToolDefinition> _tools;

    public ToolManager(IProcessRunner process, ILogService log)
    {
        _process = process;
        _log = log;
        _tools = InitializeTools();
    }

    public IReadOnlyList<ToolDefinition> GetAllTools() => _tools.AsReadOnly();

    public IReadOnlyList<ToolDefinition> GetToolsByCategory(ToolCategory category)
        => _tools.Where(t => t.Category == category).ToList().AsReadOnly();

    public async Task<ToolStatus> CheckToolStatusAsync(string toolId, CancellationToken ct = default)
    {
        var tool = _tools.FirstOrDefault(t => t.Id == toolId);
        if (tool == null) return new ToolStatus { ToolId = toolId, State = ToolState.Error, ErrorMessage = "Araç bulunamadı" };

        var status = new ToolStatus { ToolId = toolId, LastChecked = DateTime.Now };

        try
        {
            if (string.IsNullOrEmpty(tool.VersionCheckCommand))
            {
                // GUI apps with WingetId: detect via winget list
                if (!string.IsNullOrEmpty(tool.WingetId))
                {
                    var wingetResult = await _process.RunAsync("winget",
                        $"list --id {tool.WingetId} --source winget --accept-source-agreements", ct);
                    if (wingetResult.Success && wingetResult.Output.Contains(tool.WingetId, StringComparison.OrdinalIgnoreCase))
                    {
                        status.State = ToolState.Installed;
                        status.InstalledVersion = ParseVersion(wingetResult.Output);
                        return status;
                    }
                }

                // Choco-installed tools without CLI: detect via directory or choco list
                if (!string.IsNullOrEmpty(tool.ChocoId))
                {
                    // For Cassandra: check C:\tools\apache-cassandra-* directory
                    if (tool.Id == "cassandra")
                    {
                        var cassHome = FindCassandraHome();
                        if (cassHome != null)
                        {
                            status.State = ToolState.Installed;
                            var dirName = Path.GetFileName(cassHome);
                            status.InstalledVersion = dirName.Replace("apache-cassandra-", "");
                            return status;
                        }
                    }

                    // General choco detection
                    var chocoResult = await _process.RunAsync("choco", "list --local-only --id-only", ct);
                    if (chocoResult.Success && chocoResult.Output.Contains(tool.ChocoId, StringComparison.OrdinalIgnoreCase))
                    {
                        status.State = ToolState.Installed;
                        status.InstalledVersion = ParseVersion(chocoResult.Output);
                        return status;
                    }
                }

                // Npm-based tools or tools that come with parent
                if (tool.Dependencies?.Length > 0)
                {
                    var parentInstalled = await _process.CommandExistsAsync(tool.Dependencies[0]);
                    status.State = parentInstalled ? ToolState.Installed : ToolState.NotInstalled;
                }
                else
                {
                    status.State = ToolState.NotInstalled;
                }
                return status;
            }

            var parts = tool.VersionCheckCommand.Split(' ', 2);
            var cmd = parts[0];
            var args = parts.Length > 1 ? parts[1] : null;

            // First check if command exists on PATH
            var exists = await _process.CommandExistsAsync(cmd);

            // If not on PATH, try to find in RequiredPaths or known install directories
            var resolvedCmd = cmd;
            if (!exists && tool.RequiredPaths?.Length > 0)
            {
                foreach (var rp in tool.RequiredPaths)
                {
                    var expandedPath = Environment.ExpandEnvironmentVariables(rp.Replace("%USERNAME%", Environment.UserName));
                    var candidatePath = Path.Combine(expandedPath, cmd + ".exe");
                    if (File.Exists(candidatePath))
                    {
                        resolvedCmd = candidatePath;
                        exists = true;
                        break;
                    }
                }
            }

            // For services, also check if the Windows service is registered and running
            if (!exists && tool.IsService && tool.ServiceDef != null)
            {
                var scResult = await _process.RunAsync("sc.exe", $"query {tool.ServiceDef.ServiceName}", ct);
                if (scResult.Success && scResult.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
                {
                    // Service is running - find the executable from service config
                    var scQc = await _process.RunAsync("sc.exe", $"qc {tool.ServiceDef.ServiceName}", ct);
                    if (scQc.Success)
                    {
                        var binPathMatch = Regex.Match(scQc.Output, @"BINARY_PATH_NAME\s*:\s*""?([^""]+\.exe)");
                        if (binPathMatch.Success)
                        {
                            resolvedCmd = binPathMatch.Groups[1].Value.Trim();
                            exists = true;
                        }
                    }
                }
            }

            if (!exists)
            {
                status.State = ToolState.NotInstalled;
                return status;
            }

            var result = await _process.RunAsync(resolvedCmd, args, ct);

            // Some tools output version to stderr (e.g., java --version)
            var output = !string.IsNullOrWhiteSpace(result.Output) ? result.Output : result.Error;

            if (!string.IsNullOrWhiteSpace(output))
            {
                status.State = ToolState.Installed;
                status.InstalledVersion = ParseVersion(output);
            }
            else
            {
                status.State = ToolState.NotInstalled;
            }
        }
        catch
        {
            status.State = ToolState.NotInstalled;
        }

        return status;
    }

    public async Task<List<ToolStatus>> CheckAllToolsAsync(CancellationToken ct = default)
    {
        // Run checks in parallel with limited concurrency
        var semaphore = new SemaphoreSlim(8);
        var tasks = _tools.Select(async t =>
        {
            await semaphore.WaitAsync(ct);
            try { return await CheckToolStatusAsync(t.Id, ct); }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<bool> InstallToolAsync(string toolId, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var tool = _tools.FirstOrDefault(t => t.Id == toolId);
        if (tool == null)
        {
            progress?.Report("Hata: Araç tanımı bulunamadı.");
            return false;
        }

        // Check dependencies first
        if (tool.Dependencies?.Length > 0)
        {
            foreach (var depId in tool.Dependencies)
            {
                var depStatus = await CheckToolStatusAsync(depId, ct);
                if (depStatus.State != ToolState.Installed)
                {
                    var depTool = _tools.FirstOrDefault(t => t.Id == depId);
                    progress?.Report($"Bağımlılık yükleniyor: {depTool?.DisplayName ?? depId}...");
                    var depResult = await InstallToolAsync(depId, progress, ct);
                    if (!depResult)
                    {
                        progress?.Report($"Hata: {depTool?.DisplayName ?? depId} bağımlılığı yüklenemedi.");
                        return false;
                    }
                }
            }
        }

        _log.Info($"{tool.DisplayName} yükleniyor...", "ToolManager");
        progress?.Report($"{tool.DisplayName} yükleniyor...");

        ProcessResult result;

        switch (tool.PreferredInstallMethod)
        {
            case InstallMethod.Winget when !string.IsNullOrEmpty(tool.WingetId):
                progress?.Report($"[winget] {tool.WingetId} yükleniyor...");
                result = await _process.RunWithLiveOutputAsync("winget",
                    $"install --id {tool.WingetId} --source winget --accept-source-agreements --accept-package-agreements -h",
                    progress, ct);
                break;

            case InstallMethod.Chocolatey when !string.IsNullOrEmpty(tool.ChocoId):
                progress?.Report($"[choco] {tool.ChocoId} yükleniyor (yönetici izni gerekebilir)...");
                result = await _process.RunElevatedAsync("choco",
                    $"install {tool.ChocoId} -y --no-progress",
                    progress, ct);
                break;

            case InstallMethod.Npm:
                progress?.Report($"[npm] {tool.Id} global olarak yükleniyor...");
                var npmPackage = tool.Id switch
                {
                    "yarn" => "yarn",
                    "pnpm" => "pnpm",
                    "nestjs-cli" => "@nestjs/cli",
                    "angular-cli" => "@angular/cli",
                    "vue-cli" => "@vue/cli",
                    "nextjs" => "create-next-app",
                    "expo-cli" => "expo-cli",
                    _ => tool.Id
                };
                result = await _process.RunWithLiveOutputAsync("npm",
                    $"install -g {npmPackage}", progress, ct);
                break;

            case InstallMethod.DirectDownload when !string.IsNullOrEmpty(tool.DirectDownloadUrl):
                result = await DownloadAndInstallMsiAsync(tool, progress, ct);
                break;

            case InstallMethod.CustomScript:
                // Custom install commands per tool
                var customCmd = tool.Id switch
                {
                    "laravel-cli" => ("composer", "global require laravel/installer"),
                    "dotnet-ef" => ("dotnet", "tool install --global dotnet-ef"),
                    _ => ((string?)null, (string?)null)
                };
                if (customCmd.Item1 != null)
                {
                    progress?.Report($"[custom] {tool.DisplayName} yükleniyor...");
                    result = await _process.RunWithLiveOutputAsync(customCmd.Item1, customCmd.Item2!, progress, ct);
                }
                else
                {
                    progress?.Report("Hata: Özel kurulum komutu tanımlı değil.");
                    return false;
                }
                break;

            default:
                // Fallback: try winget first, then choco
                if (!string.IsNullOrEmpty(tool.WingetId))
                {
                    progress?.Report($"[winget fallback] {tool.WingetId} deneniyor...");
                    result = await _process.RunWithLiveOutputAsync("winget",
                        $"install --id {tool.WingetId} --source winget --accept-source-agreements --accept-package-agreements -h",
                        progress, ct);
                }
                else if (!string.IsNullOrEmpty(tool.ChocoId))
                {
                    progress?.Report($"[choco fallback] {tool.ChocoId} deneniyor...");
                    result = await _process.RunWithLiveOutputAsync("choco",
                        $"install {tool.ChocoId} -y --no-progress", progress, ct);
                }
                else
                {
                    progress?.Report("Hata: Kurulum yöntemi bulunamadı.");
                    return false;
                }
                break;
        }

        if (result.Success)
        {
            // Post-install hooks
            await RunPostInstallAsync(tool, progress, ct);

            if (result.NeedsRestart)
            {
                _log.Success($"{tool.DisplayName} başarıyla yüklendi! (Yeniden başlatma gerekebilir)", "ToolManager");
                progress?.Report($"[OK] {tool.DisplayName} başarıyla yüklendi! Sistem yeniden başlatılmalı.");
            }
            else
            {
                _log.Success($"{tool.DisplayName} başarıyla yüklendi!", "ToolManager");
                progress?.Report($"[OK] {tool.DisplayName} başarıyla yüklendi!");
            }
        }
        else
        {
            _log.Error($"{tool.DisplayName} yüklenemedi (exit: {result.ExitCode}): {result.Error}", "ToolManager");
            progress?.Report($"[HATA] {tool.DisplayName} yüklenemedi: {result.Error}");
        }

        return result.Success;
    }

    public async Task<bool> UninstallToolAsync(string toolId, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var tool = _tools.FirstOrDefault(t => t.Id == toolId);
        if (tool == null) return false;

        progress?.Report($"{tool.DisplayName} kaldırılıyor...");

        ProcessResult result;

        // MSI-installed tools: stop service first, then uninstall via msiexec product name
        if (tool.PreferredInstallMethod == InstallMethod.DirectDownload && tool.IsService && tool.ServiceDef != null)
        {
            progress?.Report($"[service] {tool.ServiceDef.ServiceName} durduruluyor...");
            await _process.RunWithLiveOutputAsync("net", $"stop {tool.ServiceDef.ServiceName}", progress, ct);

            // Remove via sc.exe and msiexec (find product by name)
            progress?.Report($"[msiexec] {tool.DisplayName} kaldırılıyor...");
            var uninstallScript =
                $"$app = Get-WmiObject -Class Win32_Product | Where-Object {{ $_.Name -like '*MongoDB*Server*' }}; " +
                $"if ($app) {{ $app.Uninstall() | Out-Null; Write-Output 'Kaldırıldı' }} " +
                $"else {{ Write-Output 'Ürün bulunamadı, sc ile deneniyor...'; sc.exe delete {tool.ServiceDef.ServiceName} }}";
            result = await _process.RunPowerShellWithProgressAsync(uninstallScript, progress, ct);
        }
        else if (!string.IsNullOrEmpty(tool.WingetId))
        {
            progress?.Report($"[winget] {tool.WingetId} kaldırılıyor...");
            result = await _process.RunWithLiveOutputAsync("winget",
                $"uninstall --id {tool.WingetId} -h", progress, ct);
        }
        else if (!string.IsNullOrEmpty(tool.ChocoId))
        {
            progress?.Report($"[choco] {tool.ChocoId} kaldırılıyor...");
            result = await _process.RunWithLiveOutputAsync("choco",
                $"uninstall {tool.ChocoId} -y", progress, ct);
        }
        else if (tool.PreferredInstallMethod == InstallMethod.Npm)
        {
            var npmPackage = tool.Id switch
            {
                "nestjs-cli" => "@nestjs/cli",
                "angular-cli" => "@angular/cli",
                "vue-cli" => "@vue/cli",
                _ => tool.Id
            };
            progress?.Report($"[npm] {npmPackage} kaldırılıyor...");
            result = await _process.RunWithLiveOutputAsync("npm",
                $"uninstall -g {npmPackage}", progress, ct);
        }
        else
        {
            progress?.Report("Kaldırma yöntemi bulunamadı.");
            return false;
        }

        if (result.Success)
            _log.Success($"{tool.DisplayName} kaldırıldı.", "ToolManager");
        else
            _log.Error($"{tool.DisplayName} kaldırılamadı: {result.Error}", "ToolManager");

        progress?.Report(result.Success ? $"[OK] {tool.DisplayName} kaldırıldı." : $"[HATA] {result.Error}");
        return result.Success;
    }

    private async Task<ProcessResult> DownloadAndInstallMsiAsync(ToolDefinition tool, IProgress<string>? progress, CancellationToken ct)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DevARIManager");
        Directory.CreateDirectory(tempDir);
        var fileName = Path.GetFileName(new Uri(tool.DirectDownloadUrl!).LocalPath);
        var filePath = Path.Combine(tempDir, fileName);

        try
        {
            // Download MSI
            progress?.Report($"[download] {fileName} indiriliyor...");
            _log.Info($"İndiriliyor: {tool.DirectDownloadUrl}", "ToolManager");

            var dlScript = "$ProgressPreference = 'SilentlyContinue'; " +
                $"[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                $"Invoke-WebRequest -Uri '{tool.DirectDownloadUrl}' -OutFile '{filePath}' -UseBasicParsing";
            var dlResult = await _process.RunPowerShellWithProgressAsync(dlScript, progress, ct);

            if (!dlResult.Success)
            {
                progress?.Report($"[HATA] İndirme başarısız: {dlResult.Error}");
                return dlResult;
            }

            if (!File.Exists(filePath))
            {
                return new ProcessResult { ExitCode = -1, Error = "İndirilen dosya bulunamadı." };
            }

            progress?.Report($"[OK] İndirme tamamlandı ({fileName})");

            // Install MSI using msiexec
            if (filePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            {
                var args = tool.SilentInstallArgs ?? "/qn /norestart";
                var msiArgs = $"/i \"{filePath}\" {args}";
                progress?.Report($"[msiexec] Kurulum başlatılıyor...");
                _log.Info($"msiexec.exe {msiArgs}", "ToolManager");
                return await _process.RunWithLiveOutputAsync("msiexec.exe", msiArgs, progress, ct);
            }
            else if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var args = tool.SilentInstallArgs ?? "/S";
                progress?.Report($"Kurulum başlatılıyor...");
                return await _process.RunWithLiveOutputAsync(filePath, args, progress, ct);
            }

            return new ProcessResult { ExitCode = -1, Error = "Desteklenmeyen dosya formatı." };
        }
        finally
        {
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { /* cleanup */ }
        }
    }

    private async Task RunPostInstallAsync(ToolDefinition tool, IProgress<string>? progress, CancellationToken ct)
    {
        try
        {
            switch (tool.Id)
            {
                case "docker":
                    // Docker requires WSL 2 - update WSL after Docker install
                    progress?.Report("[post-install] WSL güncelleniyor...");
                    _log.Info("WSL güncelleniyor (Docker için gerekli)...", "PostInstall");
                    var wslResult = await _process.RunWithLiveOutputAsync("wsl", "--update", progress, ct);
                    if (wslResult.Success)
                    {
                        _log.Success("WSL başarıyla güncellendi.", "PostInstall");
                        progress?.Report("[OK] WSL güncellendi. Docker Desktop'ı yeniden başlatın.");
                    }
                    else
                    {
                        _log.Warn("WSL güncellenemedi. Manuel olarak 'wsl --update' çalıştırın.", "PostInstall");
                        progress?.Report("[UYARI] WSL güncellenemedi. Manuel: wsl --update");
                    }
                    break;

                case "go":
                    // Go needs GOPATH set
                    progress?.Report("[post-install] GOPATH ortam değişkeni kontrol ediliyor...");
                    var gopath = Environment.GetEnvironmentVariable("GOPATH", EnvironmentVariableTarget.User);
                    if (string.IsNullOrEmpty(gopath))
                    {
                        var defaultGopath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "go");
                        Environment.SetEnvironmentVariable("GOPATH", defaultGopath, EnvironmentVariableTarget.User);
                        _log.Success($"GOPATH ayarlandı: {defaultGopath}", "PostInstall");
                        progress?.Report($"[OK] GOPATH = {defaultGopath}");
                    }
                    break;

                case "mongodb":
                    // Create default data directory
                    progress?.Report("[post-install] MongoDB veri dizini oluşturuluyor...");
                    var dataDir = @"C:\data\db";
                    if (!Directory.Exists(dataDir))
                    {
                        Directory.CreateDirectory(dataDir);
                        _log.Success($"MongoDB veri dizini oluşturuldu: {dataDir}", "PostInstall");
                        progress?.Report($"[OK] Veri dizini oluşturuldu: {dataDir}");
                    }

                    // Add MongoDB bin to system PATH (find actual installed version)
                    var mongoBaseDir = @"C:\Program Files\MongoDB\Server";
                    var mongoBinDir = "";
                    if (Directory.Exists(mongoBaseDir))
                    {
                        // Find latest version directory
                        var versionDir = Directory.GetDirectories(mongoBaseDir)
                            .OrderByDescending(d => d)
                            .FirstOrDefault(d => Directory.Exists(Path.Combine(d, "bin")));
                        if (versionDir != null)
                            mongoBinDir = Path.Combine(versionDir, "bin");
                    }
                    if (!string.IsNullOrEmpty(mongoBinDir) && Directory.Exists(mongoBinDir))
                    {
                        var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
                        if (!currentPath.Contains(mongoBinDir, StringComparison.OrdinalIgnoreCase))
                        {
                            progress?.Report("[post-install] MongoDB PATH'e ekleniyor...");
                            await _process.RunPowerShellAsync(
                                $"$p = [System.Environment]::GetEnvironmentVariable('PATH','Machine'); " +
                                $"[System.Environment]::SetEnvironmentVariable('PATH', $p + ';{mongoBinDir}', 'Machine')");
                            _log.Success($"MongoDB PATH'e eklendi: {mongoBinDir}", "PostInstall");
                            progress?.Report($"[OK] PATH'e eklendi: {mongoBinDir}");
                        }
                    }
                    break;

                case "postgresql":
                    // Initialize data directory if needed
                    progress?.Report("[post-install] PostgreSQL data dizini kontrol ediliyor...");
                    var pgDir = @"C:\Program Files\PostgreSQL\17";
                    var pgDataDir = Path.Combine(pgDir, "data");
                    var initdbPath = Path.Combine(pgDir, "bin", "initdb.exe");

                    if (!File.Exists(Path.Combine(pgDataDir, "postgresql.conf")) && File.Exists(initdbPath))
                    {
                        progress?.Report("[post-install] initdb ile data dizini oluşturuluyor...");
                        var initResult = await _process.RunWithLiveOutputAsync(initdbPath,
                            $"-D \"{pgDataDir}\" -U postgres -E UTF8 --locale=C", progress, ct);

                        if (initResult.Success)
                        {
                            _log.Success("PostgreSQL data dizini başarıyla oluşturuldu.", "PostInstall");
                            progress?.Report("[OK] Data dizini oluşturuldu.");

                            // Create log directory
                            var pgLogDir = Path.Combine(pgDataDir, "log");
                            if (!Directory.Exists(pgLogDir))
                                Directory.CreateDirectory(pgLogDir);
                        }
                        else
                        {
                            _log.Error($"initdb hatası: {initResult.Error}", "PostInstall");
                            progress?.Report($"[HATA] initdb başarısız: {initResult.Error}");
                        }
                    }
                    else if (File.Exists(Path.Combine(pgDataDir, "postgresql.conf")))
                    {
                        _log.Info("PostgreSQL data dizini zaten mevcut.", "PostInstall");
                        progress?.Report("[OK] Data dizini zaten mevcut.");
                    }
                    break;

                case "mysql":
                    // Initialize data directory in ProgramData (Program Files needs admin)
                    progress?.Report("[post-install] MySQL data dizini kontrol ediliyor...");
                    var mysqlBase = (string?)null;
                    foreach (var ver in new[] { "8.4", "8.0", "8.2", "9.0" })
                    {
                        var candidate = $@"C:\Program Files\MySQL\MySQL Server {ver}";
                        if (File.Exists(Path.Combine(candidate, "bin", "mysqld.exe")))
                        {
                            mysqlBase = candidate;
                            break;
                        }
                    }
                    if (mysqlBase != null)
                    {
                        var mysqldPath = Path.Combine(mysqlBase, "bin", "mysqld.exe");
                        var mysqlDataDir = @"C:\ProgramData\MySQL\data";

                        if (!Directory.Exists(Path.Combine(mysqlDataDir, "mysql")))
                        {
                            Directory.CreateDirectory(mysqlDataDir);
                            progress?.Report("[post-install] mysqld --initialize-insecure ile data dizini oluşturuluyor...");
                            var initResult = await _process.RunWithLiveOutputAsync(mysqldPath,
                                $"--initialize-insecure --datadir=\"{mysqlDataDir}\" --console", progress, ct);

                            if (initResult.Success)
                            {
                                _log.Success("MySQL data dizini başarıyla oluşturuldu.", "PostInstall");
                                progress?.Report("[OK] MySQL data dizini oluşturuldu.");
                            }
                            else
                            {
                                _log.Error($"MySQL initialize hatası: {initResult.Error}", "PostInstall");
                                progress?.Report($"[HATA] MySQL initialize başarısız: {initResult.Error}");
                            }
                        }
                        else
                        {
                            progress?.Report("[OK] MySQL data dizini zaten mevcut.");
                        }
                    }
                    break;

                case "cassandra":
                    // Create Cassandra data directories after choco install
                    var cassHome = FindCassandraHome();
                    if (cassHome != null)
                    {
                        progress?.Report("[post-install] Cassandra veri dizinleri oluşturuluyor...");
                        foreach (var dir in new[] { "data", "commitlog", "saved_caches", "hints", "logs" })
                            Directory.CreateDirectory(Path.Combine(cassHome, dir));
                        _log.Success($"Cassandra dizinleri oluşturuldu: {cassHome}", "PostInstall");
                        progress?.Report($"[OK] Cassandra dizinleri oluşturuldu: {cassHome}");
                    }
                    break;

                case "java-jdk":
                    // Set JAVA_HOME after JDK install
                    progress?.Report("[post-install] JAVA_HOME kontrol ediliyor...");
                    var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine);
                    if (string.IsNullOrEmpty(javaHome))
                    {
                        // Search known JDK locations
                        var jdkCandidates = new[]
                        {
                            @"C:\Program Files\Eclipse Adoptium\jdk-17.0.13.11-hotspot",
                            @"C:\Program Files\Microsoft\jdk-17",
                            @"C:\Program Files\AdoptOpenJDK\jdk-11.0.5.10-hotspot"
                        };
                        var defaultJavaHome = jdkCandidates.FirstOrDefault(Directory.Exists) ?? @"C:\Program Files\Microsoft\jdk-17";
                        if (Directory.Exists(defaultJavaHome))
                        {
                            await _process.RunPowerShellAsync(
                                $"[System.Environment]::SetEnvironmentVariable('JAVA_HOME', '{defaultJavaHome}', 'Machine')");
                            _log.Success($"JAVA_HOME ayarlandı: {defaultJavaHome}", "PostInstall");
                            progress?.Report($"[OK] JAVA_HOME = {defaultJavaHome}");
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"Post-install hook hatası: {ex.Message}", "PostInstall");
        }
    }

    /// <summary>Finds the Cassandra home directory under C:\tools</summary>
    internal static string? FindCassandraHome()
    {
        try
        {
            var toolsDir = @"C:\tools";
            if (!Directory.Exists(toolsDir)) return null;
            return Directory.GetDirectories(toolsDir, "apache-cassandra-*")
                .OrderByDescending(d => d)
                .FirstOrDefault();
        }
        catch { return null; }
    }

    /// <summary>Finds a JDK 11+ java.exe (Adoptium or Microsoft OpenJDK)</summary>
    internal static string? FindJava17()
    {
        var jdkRoots = new[]
        {
            @"C:\Program Files\Eclipse Adoptium",
            @"C:\Program Files\Microsoft"
        };
        foreach (var root in jdkRoots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (var jdkDir in Directory.GetDirectories(root, "jdk-*").OrderByDescending(d => d))
            {
                var javaExe = Path.Combine(jdkDir, "bin", "java.exe");
                if (File.Exists(javaExe)) return javaExe;
            }
        }
        return null;
    }

    private static string ParseVersion(string output)
    {
        // Take first meaningful line
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Match version patterns: v20.11.0, 3.12.1, 8.0.418, etc.
            var match = Regex.Match(trimmed, @"v?(\d+\.\d+[\.\d]*)");
            if (match.Success)
                return match.Groups[1].Value;
        }
        return lines.FirstOrDefault()?.Trim() ?? "unknown";
    }

    // =============================================
    // TOOL DEFINITIONS - Verified against official winget/choco repos (2026-02-25)
    // =============================================
    private static List<ToolDefinition> InitializeTools() =>
    [
        // ── RUNTIME & LANGUAGES ──
        new()
        {
            Id = "nodejs", DisplayName = "Node.js (LTS)", Description = "JavaScript runtime - LTS sürümü",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "node --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "OpenJS.NodeJS.LTS",       // Verified: winget search
            ChocoId = "nodejs-lts",                 // Verified: choco search
            RequiredPaths = [@"C:\Program Files\nodejs\"]
        },
        new()
        {
            Id = "python", DisplayName = "Python 3.13", Description = "Python runtime - en güncel sürüm",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "python --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Python.Python.3.13",        // Verified: Python.Python.3.13 -> 3.13.12
            ChocoId = "python",
            RequiredPaths = [@"C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python313\", @"C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python313\Scripts\", @"C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\", @"C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\Scripts\"]
        },
        new()
        {
            Id = "java-jdk", DisplayName = "Java JDK 17", Description = "Microsoft OpenJDK 17 (LTS)",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "java --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Microsoft.OpenJDK.17",      // Verified: 17.0.18.8
            ChocoId = "openjdk",
            EnvironmentVariables = ["JAVA_HOME"],
            RequiredPaths = [@"C:\Program Files\Eclipse Adoptium\jdk-17.0.13.11-hotspot\bin\", @"C:\Program Files\Microsoft\jdk-17\bin\", @"C:\Program Files\AdoptOpenJDK\jdk-11.0.5.10-hotspot\bin\"]
        },
        new()
        {
            Id = "dotnet-sdk", DisplayName = ".NET 8 SDK", Description = ".NET 8 SDK (LTS)",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "dotnet --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Microsoft.DotNet.SDK.8",    // Verified: just installed
            RequiredPaths = [@"C:\Program Files\dotnet\"]
        },
        new()
        {
            Id = "go", DisplayName = "Go", Description = "Go programlama dili",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "go version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "GoLang.Go",                 // Verified: 1.26.0
            ChocoId = "golang",
            EnvironmentVariables = ["GOPATH"],
            RequiredPaths = [@"C:\Program Files\Go\bin\"]
        },
        new()
        {
            Id = "rust", DisplayName = "Rust (rustup)", Description = "Rust toolchain installer",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "rustc --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Rustlang.Rustup",           // Verified: 1.28.2
            ChocoId = "rustup.install"
        },

        // ── DATABASES ──
        new()
        {
            Id = "mongodb", DisplayName = "MongoDB", Description = "NoSQL document veritabanı",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            VersionCheckCommand = "mongod --version",
            PreferredInstallMethod = InstallMethod.DirectDownload,
            DirectDownloadUrl = "https://fastdl.mongodb.org/windows/mongodb-windows-x86_64-8.0.17-signed.msi",
            SilentInstallArgs = "/l*v \"%TEMP%\\mongodb-install.log\" /qb ADDLOCAL=\"ServerService\" SHOULD_INSTALL_COMPASS=\"0\"",
            WingetId = "MongoDB.Server",             // Fallback
            ChocoId = "mongodb.install",             // Fallback
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "MongoDB", ProcessName = "mongod",
                StartCommand = "net start MongoDB",
                StopCommand = "net stop MongoDB",
                DefaultPort = 27017
            },
            RequiredPaths = [@"C:\Program Files\MongoDB\Server\8.2\bin\", @"C:\Program Files\MongoDB\Server\8.0\bin\", @"C:\Program Files\MongoDB\Server\7.0\bin\"]
        },
        new()
        {
            Id = "redis", DisplayName = "Redis", Description = "In-memory cache ve veri deposu",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            VersionCheckCommand = "redis-server --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Redis.Redis",                // Verified: 3.0.504 (Windows)
            ChocoId = "redis-64",                    // Verified: redis-64 3.1.0
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "Redis", ProcessName = "redis-server",
                StartCommand = "net start Redis",
                StopCommand = "net stop Redis",
                DefaultPort = 6379
            }
        },
        new()
        {
            Id = "postgresql", DisplayName = "PostgreSQL 17", Description = "İlişkisel veritabanı",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            VersionCheckCommand = "psql --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "PostgreSQL.PostgreSQL.17",   // Verified: 17.8-1
            ChocoId = "postgresql17",
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "postgresql-x64-17", ProcessName = "postgres",
                StartCommand = "$d='C:\\Program Files\\PostgreSQL\\17';$dd=$d+'\\data';$bb=$d+'\\bin';if(!(Test-Path ($dd+'\\postgresql.conf'))){if(Test-Path ($bb+'\\initdb.exe')){& ($bb+'\\initdb.exe') -D $dd -U postgres -E UTF8 --locale=C;$ld=$dd+'\\log';if(!(Test-Path $ld)){New-Item $ld -ItemType Directory -Force|Out-Null}}};if(Test-Path ($dd+'\\postgresql.conf')){$s=Get-Service postgresql-x64-17 -EA 0;if($s){Start-Service postgresql-x64-17 -EA Stop}else{& ($bb+'\\pg_ctl.exe') start -D $dd -l ($dd+'\\log\\postgresql.log')}}",
                StopCommand = "$d='C:\\Program Files\\PostgreSQL\\17';$dd=$d+'\\data';$bb=$d+'\\bin';$s=Get-Service postgresql-x64-17 -EA 0;if($s -and $s.Status -eq 'Running'){Stop-Service postgresql-x64-17 -Force}elseif(Test-Path ($bb+'\\pg_ctl.exe')){& ($bb+'\\pg_ctl.exe') stop -D $dd}",
                DefaultPort = 5432
            },
            RequiredPaths = [@"C:\Program Files\PostgreSQL\17\bin\", @"C:\Program Files\PostgreSQL\14\bin\"]
        },
        new()
        {
            Id = "mysql", DisplayName = "MySQL", Description = "İlişkisel veritabanı",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            VersionCheckCommand = "mysql --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Oracle.MySQL",               // Verified: 8.4.8
            ChocoId = "mysql",
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "", ProcessName = "mysqld",
                StartCommand = "$b=$null;foreach($v in '8.4','8.0','8.2','9.0'){$t='C:\\Program Files\\MySQL\\MySQL Server '+$v;if(Test-Path ($t+'\\bin\\mysqld.exe')){$b=$t;break}};if($b){$m=$b+'\\bin\\mysqld.exe';$d='C:\\ProgramData\\MySQL\\data';if(!(Test-Path ($d+'\\mysql'))){New-Item $d -ItemType Directory -Force|Out-Null;Write-Host 'MySQL data dizini olusturuluyor...';& $m --initialize-insecure --datadir=$d --console 2>&1|Out-Null};Start-Process $m -ArgumentList ('--datadir='+$d),'--port=3306','--console' -WindowStyle Hidden}",
                StopCommand = "Stop-Process -Name mysqld -Force -EA 0",
                DefaultPort = 3306
            },
            RequiredPaths = [@"C:\Program Files\MySQL\MySQL Server 8.4\bin\", @"C:\Program Files\MySQL\MySQL Server 8.0\bin\"]
        },
        new()
        {
            Id = "cassandra", DisplayName = "Apache Cassandra", Description = "Dağıtık NoSQL veritabanı",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            // VersionCheckCommand left empty; detected via FindCassandraHome() in CheckToolStatusAsync
            PreferredInstallMethod = InstallMethod.Chocolatey,
            ChocoId = "apache-cassandra",            // Verified: 5.0.6
            Dependencies = ["java-jdk"],
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "", ProcessName = "CassandraDaemon", // Virtual name → falls to port-based detection
                StartCommand = "",  // Handled by StartCassandraAsync() in ServiceManager
                StopCommand = "$pids=Get-WmiObject Win32_Process -Filter \"CommandLine LIKE '%CassandraDaemon%' AND Name='java.exe'\"|Select-Object -Expand ProcessId;foreach($pid in $pids){Stop-Process -Id $pid -Force -EA 0}",
                DefaultPort = 9042
            }
        },

        // ── MOBILE DEVELOPMENT ──
        new()
        {
            Id = "flutter", DisplayName = "Flutter SDK", Description = "Cross-platform mobil UI framework",
            Category = ToolCategory.Mobile, IconGlyph = "\uE8D4",
            VersionCheckCommand = "flutter --version",
            PreferredInstallMethod = InstallMethod.Chocolatey,
            ChocoId = "flutter",                     // Verified: flutter 3.38.5
            Dependencies = ["git", "java-jdk"],
            EnvironmentVariables = ["FLUTTER_HOME", "ANDROID_HOME"],
            RequiredPaths = [@"C:\flutter\bin\", @"C:\tools\flutter\bin\"]
        },
        new()
        {
            Id = "dart-sdk", DisplayName = "Dart SDK", Description = "Dart programlama dili (Flutter ile gelir)",
            Category = ToolCategory.Mobile, IconGlyph = "\uE8D4",
            VersionCheckCommand = "dart --version",
            PreferredInstallMethod = InstallMethod.Chocolatey,
            ChocoId = "dart-sdk",                    // Verified: 3.11.0
            RequiredPaths = [@"C:\flutter\bin\", @"C:\tools\dart-sdk\bin\"]
        },
        new()
        {
            Id = "android-studio", DisplayName = "Android Studio", Description = "Android IDE + SDK Manager",
            Category = ToolCategory.Mobile, IconGlyph = "\uE8D4",
            VersionCheckCommand = "adb --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Google.AndroidStudio",       // Verified: 2025.3.1.8
            ChocoId = "androidstudio",
            EnvironmentVariables = ["ANDROID_HOME", "ANDROID_SDK_ROOT"],
            RequiredPaths = [@"C:\Users\%USERNAME%\AppData\Local\Android\Sdk\platform-tools\"]
        },

        // ── DEVOPS & VCS ──
        new()
        {
            Id = "git", DisplayName = "Git", Description = "Dağıtık versiyon kontrol sistemi",
            Category = ToolCategory.DevOps, IconGlyph = "\uE8D7",
            VersionCheckCommand = "git --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Git.Git",                    // Verified: 2.53.0
            ChocoId = "git",
            RequiredPaths = [@"C:\Program Files\Git\cmd\"]
        },
        new()
        {
            Id = "github-cli", DisplayName = "GitHub CLI", Description = "GitHub komut satırı aracı (gh)",
            Category = ToolCategory.DevOps, IconGlyph = "\uE8D7",
            VersionCheckCommand = "gh --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "GitHub.cli",                 // Verified: 2.87.3
            ChocoId = "gh"
        },
        new()
        {
            Id = "docker", DisplayName = "Docker Desktop", Description = "Konteyner çalışma ortamı",
            Category = ToolCategory.DevOps, IconGlyph = "\uE838",
            VersionCheckCommand = "docker --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Docker.DockerDesktop",       // Verified: 4.60.1
            ChocoId = "docker-desktop",
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "", ProcessName = "Docker Desktop",
                StartCommand = "$dp=$null;foreach($p in 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe',$env:LOCALAPPDATA+'\\Docker\\Docker Desktop.exe'){if(Test-Path $p){$dp=$p;break}};if($dp){Start-Process $dp}else{Start-Process 'docker' -EA 0}",
                StopCommand = "Stop-Process -Name 'Docker Desktop' -Force -EA 0;Stop-Process -Name 'com.docker.backend' -Force -EA 0",
                DefaultPort = 0
            }
        },
        new()
        {
            Id = "docker-compose", DisplayName = "Docker Compose", Description = "Multi-container orkestrasyon",
            Category = ToolCategory.DevOps, IconGlyph = "\uE838",
            VersionCheckCommand = "docker compose version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Docker.DockerCompose",       // Verified: 5.0.2
            Dependencies = ["docker"]
        },

        // ── IDE & EDITORS ──
        new()
        {
            Id = "vscode", DisplayName = "VS Code", Description = "Lightweight kod editörü",
            Category = ToolCategory.IDE, IconGlyph = "\uE943",
            VersionCheckCommand = "code --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Microsoft.VisualStudioCode", // Verified: 1.109.5
            ChocoId = "vscode"
        },
        new()
        {
            Id = "postman", DisplayName = "Postman", Description = "API geliştirme ve test aracı",
            Category = ToolCategory.IDE, IconGlyph = "\uE943",
            VersionCheckCommand = "postman --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Postman.Postman",            // Verified: 11.85.1
            ChocoId = "postman"
        },

        // ── PACKAGE MANAGERS ──
        new()
        {
            Id = "npm", DisplayName = "npm", Description = "Node.js paket yöneticisi (Node.js ile gelir)",
            Category = ToolCategory.PackageManager, IconGlyph = "\uE71B",
            VersionCheckCommand = "npm --version",
            Dependencies = ["nodejs"]
        },
        new()
        {
            Id = "yarn", DisplayName = "Yarn", Description = "Hızlı ve güvenli Node paket yöneticisi",
            Category = ToolCategory.PackageManager, IconGlyph = "\uE71B",
            VersionCheckCommand = "yarn --version",
            PreferredInstallMethod = InstallMethod.Npm,
            Dependencies = ["nodejs"]
        },
        new()
        {
            Id = "pnpm", DisplayName = "pnpm", Description = "Verimli disk kullanımlı Node paket yöneticisi",
            Category = ToolCategory.PackageManager, IconGlyph = "\uE71B",
            VersionCheckCommand = "pnpm --version",
            PreferredInstallMethod = InstallMethod.Npm,
            Dependencies = ["nodejs"]
        },

        // ── WEB FRAMEWORKS (npm global CLI) ──
        new()
        {
            Id = "nestjs-cli", DisplayName = "NestJS CLI", Description = "NestJS backend framework CLI",
            Category = ToolCategory.WebFramework, IconGlyph = "\uE774",
            VersionCheckCommand = "nest --version",
            PreferredInstallMethod = InstallMethod.Npm,
            Dependencies = ["nodejs"]
        },
        new()
        {
            Id = "angular-cli", DisplayName = "Angular CLI", Description = "Angular frontend framework CLI",
            Category = ToolCategory.WebFramework, IconGlyph = "\uE774",
            VersionCheckCommand = "ng version",
            PreferredInstallMethod = InstallMethod.Npm,
            Dependencies = ["nodejs"]
        },
        new()
        {
            Id = "vue-cli", DisplayName = "Vue CLI", Description = "Vue.js frontend framework CLI",
            Category = ToolCategory.WebFramework, IconGlyph = "\uE774",
            VersionCheckCommand = "vue --version",
            PreferredInstallMethod = InstallMethod.Npm,
            Dependencies = ["nodejs"]
        },
        new()
        {
            Id = "expo-cli", DisplayName = "Expo CLI", Description = "React Native Expo geliştirme aracı",
            Category = ToolCategory.Mobile, IconGlyph = "\uE8D4",
            VersionCheckCommand = "expo --version",
            PreferredInstallMethod = InstallMethod.Npm,
            Dependencies = ["nodejs"]
        },

        // ── PHP ECOSYSTEM ──
        new()
        {
            Id = "php", DisplayName = "PHP 8.4", Description = "PHP runtime - en güncel kararlı sürüm",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "php --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "PHP.PHP.8.4",                  // Verified: 8.4.18
            ChocoId = "php",                            // Verified: 8.5.3
            RequiredPaths = [@"C:\php\", @"C:\Users\%USERNAME%\AppData\Local\Microsoft\WinGet\Packages\PHP.PHP.8.4_Microsoft.Winget.Source_8wekyb3d8bbwe\"]
        },
        new()
        {
            Id = "composer", DisplayName = "Composer", Description = "PHP paket yöneticisi (dependency manager)",
            Category = ToolCategory.PackageManager, IconGlyph = "\uE71B",
            VersionCheckCommand = "composer --version",
            PreferredInstallMethod = InstallMethod.Chocolatey,
            ChocoId = "composer",                       // Verified: 6.3.0
            Dependencies = ["php"],
            RequiredPaths = [@"C:\ProgramData\ComposerSetup\bin\"]
        },
        new()
        {
            Id = "laravel-cli", DisplayName = "Laravel Installer", Description = "Laravel framework CLI aracı",
            Category = ToolCategory.WebFramework, IconGlyph = "\uE774",
            VersionCheckCommand = "laravel --version",
            PreferredInstallMethod = InstallMethod.CustomScript,
            Dependencies = ["php", "composer"]
        },
        new()
        {
            Id = "laragon", DisplayName = "Laragon", Description = "Taşınabilir PHP geliştirme ortamı (Apache, MySQL, PHP)",
            Category = ToolCategory.IDE, IconGlyph = "\uE943",
            VersionCheckCommand = "laragon --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "LeNgocKhoa.Laragon",           // Verified: 8.6.0
            ChocoId = "laragon",                        // Verified: 8.6.0
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "Laragon", ProcessName = "laragon",
                StartCommand = "$lp=$null;foreach($p in 'C:\\laragon\\laragon.exe','C:\\Program Files\\Laragon\\laragon.exe','D:\\laragon\\laragon.exe'){if(Test-Path $p){$lp=$p;break}};if(!$lp){$lp=(Get-Command laragon -EA 0).Source};if($lp){Start-Process $lp}",
                StopCommand = "Stop-Process -Name 'laragon' -Force -EA 0",
                DefaultPort = 80
            }
        },

        // ── C/C++ DEVELOPMENT ──
        new()
        {
            Id = "mingw-gcc", DisplayName = "MinGW (GCC)", Description = "C/C++ derleyici (GCC for Windows)",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "gcc --version",
            PreferredInstallMethod = InstallMethod.Chocolatey,
            ChocoId = "mingw",                            // Verified: 15.2.0
            RequiredPaths = [@"C:\ProgramData\mingw64\mingw64\bin\"]
        },
        new()
        {
            Id = "cmake", DisplayName = "CMake", Description = "Cross-platform build sistemi (C/C++)",
            Category = ToolCategory.DevOps, IconGlyph = "\uE8D7",
            VersionCheckCommand = "cmake --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Kitware.CMake",                   // Verified: 4.2.3
            ChocoId = "cmake",                            // Verified: 4.2.3
            RequiredPaths = [@"C:\Program Files\CMake\bin\"]
        },

        // ── KOTLIN ──
        new()
        {
            Id = "kotlin", DisplayName = "Kotlin Compiler", Description = "Kotlin programlama dili (Android + Backend)",
            Category = ToolCategory.Runtime, IconGlyph = "\uE756",
            VersionCheckCommand = "kotlin -version",
            PreferredInstallMethod = InstallMethod.Chocolatey,
            ChocoId = "kotlinc",                          // Verified: choco kotlinc
            Dependencies = ["java-jdk"],
            RequiredPaths = [@"C:\ProgramData\chocolatey\lib\kotlinc\tools\kotlinc\bin\"]
        },

        // ── SQLITE ──
        new()
        {
            Id = "sqlite", DisplayName = "SQLite", Description = "Hafif dosya tabanlı veritabanı",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            VersionCheckCommand = "sqlite3 --version",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "SQLite.SQLite",                   // Verified: 3.51.2
            ChocoId = "sqlite",                           // Verified: 3.51.2
            RequiredPaths = [@"C:\Program Files\SQLite\", @"C:\Users\%USERNAME%\AppData\Local\Microsoft\WinGet\Packages\SQLite.SQLite_Microsoft.Winget.Source_8wekyb3d8bbwe\"]
        },

        // ── ASP.NET / ENTITY FRAMEWORK ──
        new()
        {
            Id = "dotnet-ef", DisplayName = "EF Core Tools", Description = "Entity Framework Core CLI (dotnet-ef)",
            Category = ToolCategory.WebFramework, IconGlyph = "\uE774",
            VersionCheckCommand = "dotnet ef --version",
            PreferredInstallMethod = InstallMethod.CustomScript,
            Dependencies = ["dotnet-sdk"]
        },

        // ── WEB SERVER ──
        new()
        {
            Id = "nginx", DisplayName = "Nginx", Description = "Yüksek performanslı web server / reverse proxy",
            Category = ToolCategory.DevOps, IconGlyph = "\uE8D7",
            VersionCheckCommand = "nginx -v",
            PreferredInstallMethod = InstallMethod.Chocolatey,
            WingetId = "nginxinc.nginx",                  // Verified: 1.29.4
            ChocoId = "nginx",                            // Verified: 1.29.5
            IsService = true,
            ServiceDef = new()
            {
                ServiceName = "nginx", ProcessName = "nginx",
                StartCommand = "$np=$null;foreach($p in 'C:\\tools\\nginx\\nginx.exe','C:\\nginx\\nginx.exe','C:\\ProgramData\\chocolatey\\lib\\nginx\\tools\\nginx.exe'){if(Test-Path $p){$np=$p;break}};if(!$np){$np=(Get-Command nginx -EA 0).Source};if($np){Start-Process $np -WorkingDirectory (Split-Path $np)}",
                StopCommand = "$np=$null;foreach($p in 'C:\\tools\\nginx\\nginx.exe','C:\\nginx\\nginx.exe','C:\\ProgramData\\chocolatey\\lib\\nginx\\tools\\nginx.exe'){if(Test-Path $p){$np=$p;break}};if(!$np){$np=(Get-Command nginx -EA 0).Source};if($np){& $np -s stop}else{Stop-Process -Name nginx -Force -EA 0}",
                DefaultPort = 80
            }
        },

        // ── GUI / DATABASE TOOLS ──
        new()
        {
            Id = "mongodb-compass", DisplayName = "MongoDB Compass", Description = "MongoDB GUI istemcisi",
            Category = ToolCategory.Database, IconGlyph = "\uE968",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "MongoDB.Compass.Full",            // Verified: 1.49.2.0
        },
        new()
        {
            Id = "antigravity", DisplayName = "Antigravity", Description = "Google Antigravity geliştirici aracı",
            Category = ToolCategory.DevOps, IconGlyph = "\uE8D7",
            PreferredInstallMethod = InstallMethod.Winget,
            WingetId = "Google.Antigravity",              // Verified: 1.19.5
        },
    ];
}
