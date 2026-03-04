using DevARIManager.Core.Helpers;
using DevARIManager.Linux.Shared;
using System.Runtime.InteropServices;

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
    private readonly List<LinuxToolDefinition> _tools;

    public LinuxPortApp(IProcessRunner process)
    {
        _process = process;
        _tools = LinuxToolRegistry.All.OrderBy(x => x.DisplayName).ToList();
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

    private LinuxToolDefinition? FindTool(string id)
        => _tools.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> IsInstalledAsync(LinuxToolDefinition tool)
    {
        var result = await RunShellAsync(tool.CheckCommand);
        return result.Success;
    }

    private async Task<ProcessResult> RunShellAsync(string command)
    {
        var escaped = LinuxToolRegistry.EscapeForDoubleQuotedBash(command);
        return await _process.RunAsync("bash", $"-lc \"{escaped}\"");
    }

    private async Task<ProcessResult> RunShellWithProgressAsync(string command)
    {
        var escaped = LinuxToolRegistry.EscapeForDoubleQuotedBash(command);
        var progress = new Progress<string>(line => Console.WriteLine(line));
        return await _process.RunWithLiveOutputAsync("bash", $"-lc \"{escaped}\"", progress);
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
}
