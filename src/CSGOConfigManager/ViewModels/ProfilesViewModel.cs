using System.Collections.ObjectModel;
using System.Windows.Input;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Services;
using Microsoft.Win32;

namespace CSGOConfigManager.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly AppState _state;
    private ProfileDefinition? _selected;
    private string _newName = string.Empty;
    private string _newDescription = string.Empty;
    private string _valuesPreview = string.Empty;

    public ObservableCollection<ProfileDefinition> Profiles { get; } = new();

    public ProfileDefinition? Selected
    {
        get => _selected;
        set
        {
            SetProperty(ref _selected, value);
            if (value is not null)
            {
                NewName = value.Name;
                NewDescription = value.Description;
                ValuesPreview = string.Join(Environment.NewLine,
                    value.Values.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} {kv.Value}"));
            }
        }
    }

    public string NewName
    {
        get => _newName;
        set => SetProperty(ref _newName, value);
    }

    public string NewDescription
    {
        get => _newDescription;
        set => SetProperty(ref _newDescription, value);
    }

    public string ValuesPreview
    {
        get => _valuesPreview;
        set => SetProperty(ref _valuesPreview, value);
    }

    public ICommand ReloadCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CreateFromCurrentCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public ProfilesViewModel(AppState state)
    {
        _state = state;
        ReloadCommand = new RelayCommand(Reload);
        ApplyCommand = new RelayCommand(Apply, () => Selected is not null && !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        SaveCommand = new RelayCommand(Save);
        DeleteCommand = new RelayCommand(Delete, () => Selected is not null);
        CreateFromCurrentCommand = new RelayCommand(CreateFromCurrent, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        ExportCommand = new RelayCommand(Export, () => Selected is not null);
        ImportCommand = new RelayCommand(Import);
        Reload();
    }

    public void Reload()
    {
        var name = Selected?.Name;
        Profiles.Clear();
        foreach (var p in _state.Services.Profiles.ListProfiles())
            Profiles.Add(p);
        if (name is not null)
            Selected = Profiles.FirstOrDefault(p => p.Name == name);
    }

    private void Apply()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        try
        {
            var touched = _state.Services.Profiles.ApplyProfile(Selected, _state.CfgDirectory!);
            _state.Services.Settings.Current.ActiveProfile = Selected.Name;
            _state.Services.Settings.Save(_state.Services.Settings.Current);
            _state.SetStatus($"Applied profile '{Selected.Name}' ({touched.Count} file(s)).");
            System.Windows.MessageBox.Show($"Profile '{Selected.Name}' applied.", "Profiles",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Apply Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            System.Windows.MessageBox.Show("Enter a profile name.", "Profiles",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in ValuesPreview.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("//")) continue;
            var parts = trimmed.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            values[parts[0]] = parts.Length > 1 ? parts[1].Trim().Trim('"') : string.Empty;
        }

        var profile = new ProfileDefinition
        {
            Name = NewName.Trim(),
            Description = NewDescription.Trim(),
            Values = values
        };

        _state.Services.Profiles.SaveProfile(profile);
        Reload();
        Selected = Profiles.FirstOrDefault(p => p.Name == profile.Name);
        _state.SetStatus($"Saved profile '{profile.Name}'.");
    }

    private void Delete()
    {
        if (Selected is null) return;
        var name = Selected.Name;
        // Only delete user profiles (in Config/Profiles)
        var userPath = Path.Combine(_state.Services.Paths.Profiles, $"{name}.json");
        if (!File.Exists(userPath))
        {
            System.Windows.MessageBox.Show("Built-in presets cannot be deleted.", "Profiles",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        if (System.Windows.MessageBox.Show($"Delete profile '{name}'?", "Confirm",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        _state.Services.Profiles.DeleteProfile(name);
        Selected = null;
        Reload();
        _state.SetStatus($"Deleted profile '{name}'.");
    }

    private void CreateFromCurrent()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        var name = string.IsNullOrWhiteSpace(NewName) ? $"Profile_{DateTime.Now:yyyyMMdd_HHmm}" : NewName.Trim();
        var profile = _state.Services.Profiles.CreateFromCurrent(name, NewDescription, _state.CfgDirectory!);
        Reload();
        Selected = Profiles.FirstOrDefault(p => p.Name == profile.Name);
        _state.SetStatus($"Created profile '{profile.Name}' from current configs.");
    }

    private void Export()
    {
        if (Selected is null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Profile JSON|*.json",
            FileName = $"{Selected.Name}.json"
        };
        if (dialog.ShowDialog() == true)
        {
            _state.Services.Profiles.Export(Selected, dialog.FileName);
            _state.SetStatus($"Exported profile to {dialog.FileName}");
        }
    }

    private void Import()
    {
        var dialog = new OpenFileDialog { Filter = "Profile JSON|*.json" };
        if (dialog.ShowDialog() != true) return;
        var profile = _state.Services.Profiles.Import(dialog.FileName);
        Reload();
        Selected = Profiles.FirstOrDefault(p => p.Name == profile.Name);
        _state.SetStatus($"Imported profile '{profile.Name}'.");
    }
}
