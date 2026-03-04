using DevARIManager.Linux.Gui.Models;
using DevARIManager.Linux.Shared;

namespace DevARIManager.Linux.Gui.Services;

internal static class LinuxToolCatalog
{
    public static List<LinuxToolItem> Create()
    {
        return LinuxToolRegistry.All
            .Select(x => new LinuxToolItem
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Installer = x.Installer,
                CheckCommand = x.CheckCommand,
                InstallCommand = x.InstallCommand,
                UninstallCommand = x.UninstallCommand,
                IsService = !string.IsNullOrWhiteSpace(x.ServiceName),
                ServiceName = x.ServiceName
            })
            .ToList();
    }
}
