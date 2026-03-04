using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevARIManager.Linux.Gui.Models;
using System.Collections.ObjectModel;

namespace DevARIManager.Linux.Gui.ViewModels;

public partial class ServicesViewModel : ObservableObject
{
    private readonly Func<LinuxToolItem?, Task> _startServiceAction;
    private readonly Func<LinuxToolItem?, Task> _stopServiceAction;
    private readonly Func<LinuxToolItem?, Task> _checkServiceAction;

    public ObservableCollection<LinuxToolItem> ServiceTools { get; } = [];

    [ObservableProperty]
    private int activeServiceCount;

    [ObservableProperty]
    private int inactiveServiceCount;

    public IAsyncRelayCommand<LinuxToolItem> StartServiceCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> StopServiceCommand { get; }
    public IAsyncRelayCommand<LinuxToolItem> CheckServiceCommand { get; }

    public ServicesViewModel(
        Func<LinuxToolItem?, Task> startServiceAction,
        Func<LinuxToolItem?, Task> stopServiceAction,
        Func<LinuxToolItem?, Task> checkServiceAction)
    {
        _startServiceAction = startServiceAction;
        _stopServiceAction = stopServiceAction;
        _checkServiceAction = checkServiceAction;

        StartServiceCommand = new AsyncRelayCommand<LinuxToolItem>(tool => _startServiceAction(tool));
        StopServiceCommand = new AsyncRelayCommand<LinuxToolItem>(tool => _stopServiceAction(tool));
        CheckServiceCommand = new AsyncRelayCommand<LinuxToolItem>(tool => _checkServiceAction(tool));
    }

    public void SetServiceTools(IEnumerable<LinuxToolItem> tools)
    {
        ServiceTools.Clear();
        foreach (var tool in tools)
        {
            ServiceTools.Add(tool);
        }
    }

    public void UpdateStats(IEnumerable<LinuxToolItem> tools)
    {
        var serviceTools = tools.Where(t => t.IsService).ToList();
        ActiveServiceCount = serviceTools.Count(t => t.ServiceState == "active");
        InactiveServiceCount = serviceTools.Count - ActiveServiceCount;
    }
}
