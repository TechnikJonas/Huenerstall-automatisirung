using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>
/// Einstellungen: Funktionen, die in allen Kabellisten zur Auswahl stehen,
/// und Aktor-Vorlagen (Grundkonfigurationen), die in allen Projekten verwendet werden.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly PlannerDataService _data;
    private readonly IDialogService _dialogs;
    private readonly AutoSaver _autoSaver;

    public SettingsViewModel(PlannerDataService data, IDialogService dialogs)
    {
        _data = data;
        _dialogs = dialogs;
        _autoSaver = new AutoSaver(data, dialogs);
    }

    public ObservableCollection<CableFunction> Functions { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteFunctionCommand), nameof(MoveUpCommand), nameof(MoveDownCommand))]
    private CableFunction? _selectedFunction;

    [ObservableProperty]
    private string _newFunctionName = string.Empty;

    [ObservableProperty]
    private string _newFunctionDescription = string.Empty;

    public string DatabasePath => App.DatabasePath;

    public ObservableCollection<ActuatorTemplate> Templates { get; } = new();

    public IReadOnlyList<Option<ActuatorKind>> KindOptions => Options.ActuatorKinds;

    public IReadOnlyList<Option<ActuatorMounting>> MountingOptions => Options.ActuatorMountings;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteTemplateCommand), nameof(DuplicateTemplateCommand))]
    private ActuatorTemplate? _selectedTemplate;

    public void Load()
    {
        _autoSaver.UntrackAll<CableFunction>();
        Functions.Clear();
        foreach (var f in _data.GetFunctions())
        {
            _autoSaver.Track(f);
            Functions.Add(f);
        }

        _autoSaver.UntrackAll<ActuatorTemplate>();
        Templates.Clear();
        foreach (var t in _data.GetActuatorTemplates())
        {
            _autoSaver.Track(t);
            Templates.Add(t);
        }
    }

    [RelayCommand]
    private void AddFunction()
    {
        var name = NewFunctionName.Trim();
        if (name.Length == 0)
        {
            _dialogs.Info("Bitte einen Namen für die Funktion eingeben.");
            return;
        }

        if (Functions.Any(f => string.Equals(f.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            _dialogs.Info($"Die Funktion \"{name}\" gibt es bereits.");
            return;
        }

        var function = _data.Add(new CableFunction
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(NewFunctionDescription) ? null : NewFunctionDescription.Trim(),
            SortOrder = Functions.Count == 0 ? 1 : Functions.Max(f => f.SortOrder) + 1,
        });
        _autoSaver.Track(function);
        Functions.Add(function);
        SelectedFunction = function;
        NewFunctionName = string.Empty;
        NewFunctionDescription = string.Empty;
    }

    private bool HasSelection() => SelectedFunction != null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteFunction()
    {
        if (SelectedFunction == null)
            return;

        var function = SelectedFunction;
        var used = _data.CountCablesWithFunction(function.Id);
        var message = $"Funktion \"{function.Name}\" wirklich löschen?";
        if (used > 0)
            message += $"\n\nSie wird in {used} Kabel(n) verwendet. Die Kabel bleiben erhalten, die Funktionszuordnung wird entfernt.";

        if (!_dialogs.Confirm(message))
            return;

        _data.Delete(function);
        _autoSaver.Untrack(function);
        Functions.Remove(function);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveUp() => Move(-1);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveDown() => Move(+1);

    private void Move(int delta)
    {
        if (SelectedFunction == null)
            return;

        var selected = SelectedFunction;
        var index = Functions.IndexOf(selected);
        var target = index + delta;
        if (target < 0 || target >= Functions.Count)
            return;

        Functions.Move(index, target);
        _autoSaver.UntrackAll<CableFunction>();
        _data.SaveFunctionOrder(Functions);
        foreach (var f in Functions)
            _autoSaver.Track(f);
        SelectedFunction = selected;
    }

    // ---------- Aktor-Vorlagen ----------

    [RelayCommand]
    private void AddTemplate() => InsertTemplate(new ActuatorTemplate
    {
        Name = "Neuer Schaltaktor",
        Kind = ActuatorKind.Switch,
        Mounting = ActuatorMounting.DistributionBoard,
        ChannelCount = 8,
        MaxCurrentPerChannel = 16,
    });

    private bool HasTemplate() => SelectedTemplate != null;

    [RelayCommand(CanExecute = nameof(HasTemplate))]
    private void DuplicateTemplate()
    {
        if (SelectedTemplate is not { } t)
            return;

        InsertTemplate(new ActuatorTemplate
        {
            Name = t.Name + " (Kopie)",
            Kind = t.Kind,
            Mounting = t.Mounting,
            ChannelCount = t.ChannelCount,
            MaxCurrentPerChannel = t.MaxCurrentPerChannel,
            MaxPowerPerChannel = t.MaxPowerPerChannel,
            Manufacturer = t.Manufacturer,
            ArticleNumber = t.ArticleNumber,
            WidthTe = t.WidthTe,
        });
    }

    private void InsertTemplate(ActuatorTemplate template)
    {
        _data.Add(template);
        _autoSaver.Track(template);
        Templates.Add(template);
        SelectedTemplate = template;
    }

    [RelayCommand(CanExecute = nameof(HasTemplate))]
    private void DeleteTemplate()
    {
        if (SelectedTemplate is not { } template)
            return;

        var used = _data.CountActuatorsWithTemplate(template.Id);
        if (used > 0)
        {
            _dialogs.Info($"Die Vorlage \"{template.Name}\" wird noch von {used} Aktor(en) in Projekten verwendet " +
                          "und kann daher nicht gelöscht werden.");
            return;
        }

        if (!_dialogs.Confirm($"Aktor-Vorlage \"{template.Name}\" wirklich löschen?"))
            return;

        _data.Delete(template);
        _autoSaver.Untrack(template);
        Templates.Remove(template);
    }
}
