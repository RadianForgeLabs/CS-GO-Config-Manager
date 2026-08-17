using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class CommandBrowserViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string _search = string.Empty;
    private string? _category = "(All)";
    private CommandItemViewModel? _selected;
    private string _editValue = string.Empty;
    private string _conflictText = string.Empty;

    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<CommandItemViewModel> Commands { get; } = new();

    public string Search
    {
        get => _search;
        set
        {
            SetProperty(ref _search, value);
            Reload();
        }
    }

    public string? Category
    {
        get => _category;
        set
        {
            SetProperty(ref _category, value);
            Reload();
        }
    }

    public CommandItemViewModel? Selected
    {
        get => _selected;
        set
        {
            SetProperty(ref _selected, value);
            if (value is not null)
            {
                EditValue = value.CurrentValue;
                LoadConflict(value.Name);
            }
            else
            {
                ConflictText = string.Empty;
            }
        }
    }

    public string EditValue
    {
        get => _editValue;
        set => SetProperty(ref _editValue, value);
    }

    public string ConflictText
    {
        get => _conflictText;
        set => SetProperty(ref _conflictText, value);
    }

    public ICommand ApplyCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ResetCommand { get; }

    public CommandBrowserViewModel(AppState state)
    {
        _state = state;
        Categories.Add("(All)");
        foreach (var cat in state.Services.Data.GetCategories())
            Categories.Add(cat);

        ApplyCommand = new RelayCommand(Apply, () => Selected is not null && !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        ReloadCommand = new RelayCommand(Reload);
        ResetCommand = new RelayCommand(() =>
        {
            if (Selected is null) return;
            EditValue = Selected.DefaultValue;
        }, () => Selected is not null);

        Reload();
    }

    public void Reload()
    {
        try
        {
            var selectedName = Selected?.Name;
            Commands.Clear();
            var cfg = _state.CfgDirectory;

            if (_state.Services.Data == null)
            {
                System.Windows.MessageBox.Show("Data service is not available.", "Reload Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var commands = _state.Services.Data.GetCommands();
            if (commands == null)
            {
                System.Windows.MessageBox.Show("Unable to load commands data.", "Reload Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            foreach (var def in commands.Where(c => !c.Hidden))
            {
                if (Category is not null && Category != "(All)" &&
                    !string.Equals(def.Category, Category, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(Search) &&
                    !def.Name.Contains(Search, StringComparison.OrdinalIgnoreCase) &&
                    !def.Description.Contains(Search, StringComparison.OrdinalIgnoreCase) &&
                    !def.Category.Contains(Search, StringComparison.OrdinalIgnoreCase))
                    continue;

                string? current = null;
                if (!string.IsNullOrWhiteSpace(cfg) && _state.Services.Config != null)
                    current = _state.Services.Config.GetCurrentValue(cfg, def.Name, def.File);

                Commands.Add(new CommandItemViewModel(def, current));
            }

            if (selectedName is not null)
                Selected = Commands.FirstOrDefault(c => c.Name == selectedName);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error reloading commands: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                "Reload Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void Apply()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        try
        {
            Selected.CurrentValue = EditValue;
            if (Selected.HasValidationError)
            {
                System.Windows.MessageBox.Show(Selected.ValidationError, "Validation",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [Selected.Name] = Selected.CurrentValue
            };
            var touched = _state.Services.Config.ApplyValues(_state.CfgDirectory!, values);
            Selected.MarkSaved();
            LoadConflict(Selected.Name);
            _state.SetStatus($"Applied {Selected.Name} = {Selected.CurrentValue} → {Path.GetFileName(touched.FirstOrDefault() ?? Selected.File)}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Apply Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void LoadConflict(string commandName)
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            ConflictText = "Configure CS:GO path to detect conflicts.";
            return;
        }

        var conflict = _state.Services.Conflicts.GetConflict(_state.CfgDirectory!, commandName);
        if (conflict is null)
        {
            ConflictText = "Not present in any config file (using default/metadata).";
            return;
        }

        var lines = conflict.Sources.Select(s =>
            $"{(s.IsEffective ? "★ " : "  ")}{s.SourceFile}: {s.Value}{(s.IsEffective ? "  (effective)" : "")}");
        ConflictText = string.Join(Environment.NewLine, lines);
    }
}
