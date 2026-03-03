using DevARIManager.Core.Helpers;
using System.Runtime.InteropServices;
using System.Text;

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    Console.Error.WriteLine("Bu port su an sadece Linux icin tasarlanmistir.");
    return 1;
}

var app = new LinuxPortApp(new ProcessRunner());
return await app.RunAsync(args);

internal sealed class LinuxPortApp
{
    private readonly IProcessRunner _process;
    private readonly List<LinuxTool> _tools;

    public LinuxPortApp(IProcessRunner process)
    {
        _process = process;
        _tools = BuildToolCatalog();
    }

    public async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        return command switch
        {
            "list" => ListTools(),
            "check" => await CheckToolsAsync(args.Skip(1).ToArray()),
            "install" => await InstallToolAsync(args.Skip(1).ToArray()),
            "uninstall" => await UninstallToolAsync(args.Skip(1).ToArray()),
            _ => UnknownCommand(command)
        };
    }

    private int ListTools()
    {
        Console.WriteLine("ID              Name            Installer   Check");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var tool in _tools.OrderBy(x => x.Id))
        {
            Console.WriteLine($"{tool.Id,-15} {tool.DisplayName,-15} {tool.Installer,-10} {tool.CheckCommand}");
        }

        return 0;
    }

    private async Task<int> CheckToolsAsync(string[] args)
    {
        var ids = args.Length == 0 || args[0] == "all"
            ? _tools.Select(t => t.Id).ToArray()
            : args;

        var exitCode = 0;
        foreach (var id in ids)
        {
            var tool = FindTool(id);
            if (tool == null)
            {
                Console.Error.WriteLine($"Bilinmeyen arac: {id}");
                exitCode = 1;
                continue;
            }

            var isInstalled = await IsInstalledAsync(tool);
            Console.WriteLine($"{tool.Id}: {(isInstalled ? "installed" : "not-installed")}");
        }

        return exitCode;
    }

    private async Task<int> InstallToolAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Kullanim: devari-manager install <tool-id>");
            return 1;
        }

        var tool = FindTool(args[0]);
        if (tool == null)
        {
            Console.Error.WriteLine($"Bilinmeyen arac: {args[0]}");
            return 1;
        }

        if (await IsInstalledAsync(tool))
        {
            Console.WriteLine($"{tool.DisplayName} zaten kurulu.");
            return 0;
        }

        Console.WriteLine($"{tool.DisplayName} kuruluyor ({tool.Installer})...");
        var result = await RunShellWithProgressAsync(tool.InstallCommand);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Kurulum basarisiz: {result.Error}");
            return 1;
        }

        Console.WriteLine($"{tool.DisplayName} kuruldu.");
        return 0;
    }

    private async Task<int> UninstallToolAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Kullanim: devari-manager uninstall <tool-id>");
            return 1;
        }

        var tool = FindTool(args[0]);
        if (tool == null)
        {
            Console.Error.WriteLine($"Bilinmeyen arac: {args[0]}");
            return 1;
        }

        Console.WriteLine($"{tool.DisplayName} kaldiriliyor...");
        var result = await RunShellWithProgressAsync(tool.UninstallCommand);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Kaldirma basarisiz: {result.Error}");
            return 1;
        }

        Console.WriteLine($"{tool.DisplayName} kaldirildi.");
        return 0;
    }

    private LinuxTool? FindTool(string id)
        => _tools.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> IsInstalledAsync(LinuxTool tool)
    {
        var result = await RunShellAsync(tool.CheckCommand);
        return result.Success;
    }

    private async Task<ProcessResult> RunShellAsync(string command)
    {
        var escaped = EscapeForDoubleQuotedBash(command);
        return await _process.RunAsync("bash", $"-lc \"{escaped}\"");
    }

    private async Task<ProcessResult> RunShellWithProgressAsync(string command)
    {
        var escaped = EscapeForDoubleQuotedBash(command);
        var progress = new Progress<string>(line => Console.WriteLine(line));
        return await _process.RunWithLiveOutputAsync("bash", $"-lc \"{escaped}\"", progress);
    }

    private static string EscapeForDoubleQuotedBash(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is '\\' or '"' or '$' or '`')
                builder.Append('\\');
            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Bilinmeyen komut: {command}");
        Console.Error.WriteLine("Yardim icin: devari-manager help");
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("DevARI Linux Port");
        Console.WriteLine();
        Console.WriteLine("Komutlar:");
        Console.WriteLine("  devari-manager list");
        Console.WriteLine("  devari-manager check [all|tool-id]");
        Console.WriteLine("  devari-manager install <tool-id>");
        Console.WriteLine("  devari-manager uninstall <tool-id>");
    }

    private static List<LinuxTool> BuildToolCatalog()
    {
        return
        [
            new LinuxTool("git", "Git", "apt", "command -v git >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y git", "sudo apt-get remove -y git"),
            new LinuxTool("nodejs", "Node.js", "apt", "command -v node >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y nodejs npm", "sudo apt-get remove -y nodejs npm"),
            new LinuxTool("python3", "Python 3", "apt", "command -v python3 >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y python3 python3-pip python3-venv", "sudo apt-get remove -y python3 python3-pip python3-venv"),
            new LinuxTool("docker", "Docker", "apt", "command -v docker >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y docker.io docker-compose-v2", "sudo apt-get remove -y docker.io docker-compose-v2"),
            new LinuxTool("dotnet8", ".NET SDK 8", "apt", "dotnet --list-sdks 2>/dev/null | grep -q '^8\\.'", "sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0", "sudo apt-get remove -y dotnet-sdk-8.0"),
            new LinuxTool("openjdk17", "OpenJDK 17", "apt", "java -version 2>&1 | grep -q '17\\.'", "sudo apt-get update && sudo apt-get install -y openjdk-17-jdk", "sudo apt-get remove -y openjdk-17-jdk"),
            new LinuxTool("go", "Go", "apt", "command -v go >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y golang", "sudo apt-get remove -y golang"),
            new LinuxTool("rust", "Rust", "rustup", "command -v rustc >/dev/null 2>&1", "curl https://sh.rustup.rs -sSf | sh -s -- -y", "rustup self uninstall -y"),
            new LinuxTool("vscode", "VS Code", "snap", "command -v code >/dev/null 2>&1", "sudo snap install code --classic", "sudo snap remove code"),
            new LinuxTool("postgresql", "PostgreSQL", "apt", "command -v psql >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y postgresql postgresql-contrib", "sudo apt-get remove -y postgresql postgresql-contrib"),
            new LinuxTool("mysql", "MySQL", "apt", "command -v mysql >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y mysql-server", "sudo apt-get remove -y mysql-server"),
            new LinuxTool("mongodb", "MongoDB", "apt", "command -v mongod >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y mongodb", "sudo apt-get remove -y mongodb"),
            new LinuxTool("redis", "Redis", "apt", "command -v redis-server >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y redis-server", "sudo apt-get remove -y redis-server"),
            new LinuxTool("nginx", "Nginx", "apt", "command -v nginx >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y nginx", "sudo apt-get remove -y nginx"),
            new LinuxTool("flutter", "Flutter", "snap", "command -v flutter >/dev/null 2>&1", "sudo snap install flutter --classic", "sudo snap remove flutter")
        ];
    }
}

internal sealed record LinuxTool(
    string Id,
    string DisplayName,
    string Installer,
    string CheckCommand,
    string InstallCommand,
    string UninstallCommand);
