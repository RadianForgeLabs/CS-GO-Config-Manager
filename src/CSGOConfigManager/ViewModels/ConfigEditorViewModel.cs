using System.Collections.ObjectModel;
using System.Windows.Input;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class ConfigEditorViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string? _selectedFile;
    private string _editorText = string.Empty;
    private bool _isDirty;
    private ConfigDocument? _document;

    public ObservableCollection<string> ConfigFiles { get; } = new();

    public string? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (!SetAndNotify(ref _selectedFile, value))
                return;
            LoadSelected();
        }
    }

    public string EditorText
    {
        get => _editorText;
        set
        {
            SetProperty(ref _editorText, value);
            IsDirty = true;
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    public ICommand ReloadListCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ReloadFileCommand { get; }
    public ICommand BackupCommand { get; }

    public ConfigEditorViewModel(AppState state)
    {
        _state = state;
        ReloadListCommand = new RelayCommand(ReloadList);
        SaveCommand = new RelayCommand(Save, () => SelectedFile is not null);
        ReloadFileCommand = new RelayCommand(LoadSelected, () => SelectedFile is not null);
        BackupCommand = new RelayCommand(BackupSelected, () => SelectedFile is not null);
        ReloadList();
    }

    public void ReloadList()
    {
        var previous = SelectedFile;
        ConfigFiles.Clear();
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        foreach (var path in _state.Services.Config.ListConfigFiles(_state.CfgDirectory))
            ConfigFiles.Add(path);

        // Also offer standard files even if missing
        foreach (var name in new[] { "autoexec.cfg", "config.cfg", "practice.cfg" })
        {
            var path = Path.Combine(_state.CfgDirectory!, name);
            if (!ConfigFiles.Contains(path, StringComparer.OrdinalIgnoreCase))
                ConfigFiles.Add(path);
        }

        if (previous is not null && ConfigFiles.Contains(previous))
            SelectedFile = previous;
        else if (ConfigFiles.Count > 0)
            SelectedFile = ConfigFiles.FirstOrDefault(f => f.EndsWith("autoexec.cfg", StringComparison.OrdinalIgnoreCase))
                           ?? ConfigFiles[0];
    }

    private void LoadSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedFile))
        {
            EditorText = string.Empty;
            IsDirty = false;
            return;
        }

        _document = _state.Services.Config.Load(SelectedFile);
        _editorText = CSGOConfigManager.Core.Parsing.CfgParser.Serialize(_document);
        OnPropertyChanged(nameof(EditorText));
        IsDirty = false;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(SelectedFile))
            return;

        try
        {
            // Re-parse editor text so free-form edits are preserved
            var doc = CSGOConfigManager.Core.Parsing.CfgParser.Parse(SelectedFile, EditorText);
            _state.Services.Config.Save(doc);
            _document = doc;
            IsDirty = false;
            _state.SetStatus($"Saved {Path.GetFileName(SelectedFile)}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Save Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BackupSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedFile) || !File.Exists(SelectedFile))
        {
            System.Windows.MessageBox.Show("File does not exist yet.", "Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var backup = _state.Services.Backups.CreateManualBackup(
            Path.GetFileNameWithoutExtension(SelectedFile) + "_manual",
            new[] { SelectedFile });
        _state.SetStatus($"Backup created: {backup.DisplayName}");
    }

    private bool SetAndNotify(ref string? field, string? value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<string?>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
