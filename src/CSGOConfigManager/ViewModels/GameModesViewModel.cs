using System.Collections.ObjectModel;
using System.Windows.Input;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class GameModesViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string? _selectedMode;
    private string _filter = string.Empty;
    private string? _selectedCategory;

    public ObservableCollection<string> Modes { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<CommandItemViewModel> Commands { get; } = new();

    public string? SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetAndLoad(ref _selectedMode, value))
                LoadCommands();
        }
    }

    public string Filter
    {
        get => _filter;
        set
        {
            SetProperty(ref _filter, value);
            LoadCommands();
        }
    }

    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            SetProperty(ref _selectedCategory, value);
            LoadCommands();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ResetSelectedCommand { get; }

    public GameModesViewModel(AppState state)
    {
        _state = state;
        foreach (var mode in state.Services.Data.GetGameModes().Keys)
            Modes.Add(mode);

        if (Modes.Count > 0)
            _selectedMode = Modes[0];

        SaveCommand = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        ReloadCommand = new RelayCommand(LoadCommands);
        ResetSelectedCommand = new RelayCommand(ResetModified);

        LoadCommands();
    }

    public void LoadCommands()
    {
        Commands.Clear();
        Categories.Clear();
        Categories.Add("(All)");

        if (string.IsNullOrWhiteSpace(SelectedMode))
            return;

        var modeCommands = _state.Services.Data.GetCommandsForMode(SelectedMode);
        foreach (var cat in modeCommands.Select(c => c.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c))
            Categories.Add(cat);

        if (SelectedCategory is null)
            _selectedCategory = "(All)";

        var cfg = _state.CfgDirectory;
        foreach (var def in modeCommands)
        {
            if (!string.IsNullOrWhiteSpace(Filter) &&
                def.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase) == false &&
                def.Description.Contains(Filter, StringComparison.OrdinalIgnoreCase) == false &&
                def.Category.Contains(Filter, StringComparison.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            if (SelectedCategory is not null &&
                SelectedCategory != "(All)" &&
                !string.Equals(def.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? current = null;
            if (!string.IsNullOrWhiteSpace(cfg))
                current = _state.Services.Config.GetCurrentValue(cfg, def.Name, def.File);

            Commands.Add(new CommandItemViewModel(def, current));
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            System.Windows.MessageBox.Show("CS:GO cfg folder not configured.", "Save",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var modified = Commands.Where(c => c.IsModified || c.IsDifferentFromDefault).ToList();
            // Save only explicitly modified items to avoid rewriting everything on first load defaults
            modified = Commands.Where(c => c.IsModified).ToList();
            if (modified.Count == 0)
            {
                _state.SetStatus("No changes to save.");
                return;
            }

            var values = modified.ToDictionary(c => c.Name, c => c.CurrentValue, StringComparer.OrdinalIgnoreCase);

            // Prefer mode-specific file when available
            string? forceFile = null;
            if (SelectedMode is not null &&
                _state.Services.Data.GetGameModes().TryGetValue(SelectedMode, out var modeFile))
            {
                // Only force for mode-owned settings that target gamemode/practice files
                // Mixed: let each command use its own file metadata
                forceFile = null;
                _ = modeFile;
            }

            var touched = _state.Services.Config.ApplyValues(_state.CfgDirectory!, values, forceFile);
            foreach (var item in modified)
                item.MarkSaved();

            _state.SetStatus($"Saved {modified.Count} setting(s) across {touched.Count} file(s).");
            System.Windows.MessageBox.Show(
                $"Saved {modified.Count} setting(s).\nFiles:\n{string.Join("\n", touched.Select(Path.GetFileName))}",
                "Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _state.Services.Log.Error("Save game mode settings failed", ex);
            System.Windows.MessageBox.Show(ex.Message, "Save Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ResetModified()
    {
        foreach (var cmd in Commands.Where(c => c.IsModified))
            cmd.ResetToDefault();
    }

    private bool SetAndLoad(ref string? field, string? value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<string?>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
