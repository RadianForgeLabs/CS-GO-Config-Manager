using System.Collections.ObjectModel;
using System.Windows.Input;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class ConflictRow
{
    public string Command { get; init; } = string.Empty;
    public string EffectiveValue { get; init; } = string.Empty;
    public string EffectiveSource { get; init; } = string.Empty;
    public string AllSources { get; init; } = string.Empty;
    public bool HasConflict { get; init; }
}

public sealed class ConflictsViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string _filter = string.Empty;
    private bool _onlyConflicts = true;

    public ObservableCollection<ConflictRow> Rows { get; } = new();

    public string Filter
    {
        get => _filter;
        set
        {
            SetProperty(ref _filter, value);
            Reload();
        }
    }

    public bool OnlyConflicts
    {
        get => _onlyConflicts;
        set
        {
            SetProperty(ref _onlyConflicts, value);
            Reload();
        }
    }

    public ICommand ReloadCommand { get; }

    public ConflictsViewModel(AppState state)
    {
        _state = state;
        ReloadCommand = new RelayCommand(Reload);
        Reload();
    }

    public void Reload()
    {
        Rows.Clear();
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        var conflicts = _state.Services.Conflicts.DetectConflicts(_state.CfgDirectory!);
        foreach (var c in conflicts)
        {
            if (OnlyConflicts && !c.HasConflict)
                continue;

            if (!string.IsNullOrWhiteSpace(Filter) &&
                !c.CommandName.Contains(Filter, StringComparison.OrdinalIgnoreCase) &&
                !(c.EffectiveSource?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            Rows.Add(new ConflictRow
            {
                Command = c.CommandName,
                EffectiveValue = c.EffectiveValue ?? string.Empty,
                EffectiveSource = c.EffectiveSource ?? string.Empty,
                HasConflict = c.HasConflict,
                AllSources = string.Join(" | ", c.Sources.Select(s =>
                    $"{s.SourceFile}={s.Value}{(s.IsEffective ? "*" : "")}"))
            });
        }
    }
}
