using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Verteilungsplaner.App.ViewModels;

namespace Verteilungsplaner.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };
        Closing += (_, _) => CommitPendingEdits(this);
    }

    /// <summary>Hält die Navigation links synchron, wenn die Seite per Code gewechselt wird.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.CurrentPage) || sender is not MainViewModel vm)
            return;

        var nav = vm.CurrentPage switch
        {
            var p when p == vm.CableLists => NavCableLists,
            var p when p == vm.RoomFunctions => NavRoomFunctions,
            var p when p == vm.Actuators => NavActuators,
            var p when p == vm.Settings => NavSettings,
            _ => NavStart,
        };
        nav.IsChecked = true;
    }

    /// <summary>Übernimmt noch offene Eingaben (Tabellenzellen, Textfelder), damit beim Schließen nichts verloren geht.</summary>
    private static void CommitPendingEdits(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is DataGrid grid)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            CommitPendingEdits(child);
        }

        if (root is Window)
        {
            if (Keyboard.FocusedElement is TextBox box)
                box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }
}
