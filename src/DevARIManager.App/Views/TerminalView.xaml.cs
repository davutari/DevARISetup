using System.Windows.Controls;
using DevARIManager.App.ViewModels;

namespace DevARIManager.App.Views;

public partial class TerminalView : UserControl
{
    public TerminalView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TerminalViewModel oldVm)
            oldVm.ScrollToBottomRequested -= ScrollToBottom;

        if (e.NewValue is TerminalViewModel newVm)
            newVm.ScrollToBottomRequested += ScrollToBottom;
    }

    private void ScrollToBottom()
    {
        if (TerminalListBox.Items.Count > 0)
        {
            TerminalListBox.ScrollIntoView(TerminalListBox.Items[^1]);
        }
    }
}
