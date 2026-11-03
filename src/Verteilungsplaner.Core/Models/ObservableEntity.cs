using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Verteilungsplaner.Core.Models;

/// <summary>
/// Basisklasse für alle gespeicherten Objekte. Meldet Änderungen an die Oberfläche,
/// damit Bearbeitungen automatisch gespeichert werden können.
/// </summary>
public abstract class ObservableEntity : INotifyPropertyChanged
{
    public int Id { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
