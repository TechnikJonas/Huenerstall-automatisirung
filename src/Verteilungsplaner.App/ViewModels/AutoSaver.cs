using System.ComponentModel;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>
/// Speichert Objekte automatisch, sobald in der Oberfläche ein Wert geändert wurde.
/// </summary>
public class AutoSaver
{
    private readonly PlannerDataService _data;
    private readonly IDialogService _dialogs;
    private readonly HashSet<ObservableEntity> _tracked = new();

    public AutoSaver(PlannerDataService data, IDialogService dialogs)
    {
        _data = data;
        _dialogs = dialogs;
    }

    /// <summary>Wird nach jedem erfolgreichen Speichern ausgelöst.</summary>
    public event EventHandler<ObservableEntity>? Saved;

    public void Track(ObservableEntity entity)
    {
        if (_tracked.Add(entity))
            entity.PropertyChanged += OnPropertyChanged;
    }

    public void Untrack(ObservableEntity entity)
    {
        if (_tracked.Remove(entity))
            entity.PropertyChanged -= OnPropertyChanged;
    }

    public void UntrackAll<T>() where T : ObservableEntity
    {
        foreach (var entity in _tracked.OfType<T>().ToList())
            Untrack(entity);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ObservableEntity entity)
            return;

        try
        {
            switch (entity)
            {
                case Project p: _data.Update(p); break;
                case DistributionBoard b: _data.Update(b); break;
                case Room r: _data.Update(r); break;
                case CableFunction f: _data.Update(f); break;
                case CableList l: _data.Update(l); break;
                case Cable c: _data.Update(c); break;
            }
            Saved?.Invoke(this, entity);
        }
        catch (Exception ex)
        {
            _dialogs.Error($"Die Änderung konnte nicht gespeichert werden:\n{ex.Message}");
        }
    }
}
