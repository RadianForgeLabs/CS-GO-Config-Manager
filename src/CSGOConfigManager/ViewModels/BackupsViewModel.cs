using System.Collections.ObjectModel;
using System.Windows.Input;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Services;
using Microsoft.Win32;

namespace CSGOConfigManager.ViewModels;

public sealed class BackupsViewModel : ViewModelBase
{
    private readonly AppState _state;
    private BackupInfo? _selected;
    private string _diffText = string.Empty;
    private string _manualName = "snapshot";

    public ObservableCollection<BackupInfo> Backups { get; } = new();

    public BackupInfo? Selected
    {
        get => _selected;
        set
        {
            SetProperty(ref _selected, value);
            DiffText = string.Empty;
        }
    }

    public string DiffText
    {
        get => _diffText;
        set => SetProperty(ref _diffText, value);
    }

    public string ManualName
    {
        get => _manualName;
        set => SetProperty(ref _manualName, value);
    }

    public ICommand ReloadCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand DiffCommand { get; }

    public BackupsViewModel(AppState state)
    {
        _state = state;
        ReloadCommand = new RelayCommand(Reload);
        CreateCommand = new RelayCommand(Create, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        RestoreCommand = new RelayCommand(Restore, () => Selected is not null && !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        DeleteCommand = new RelayCommand(Delete, () => Selected is not null);
        ExportCommand = new RelayCommand(Export, () => Selected is not null);
        DiffCommand = new RelayCommand(ShowDiff, () => Selected is not null && !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        Reload();
    }

    public void Reload()
    {
        Backups.Clear();
        foreach (var b in _state.Services.Backups.ListBackups())
            Backups.Add(b);
    }

    private void Create()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        try
        {
            var backup = _state.Services.Backups.CreateFullCfgBackup(ManualName, _state.CfgDirectory!);
            Reload();
            Selected = Backups.FirstOrDefault(b => b.Id == backup.Id);
            _state.SetStatus($"Backup created: {backup.DisplayName}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Backup Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void Restore()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        if (System.Windows.MessageBox.Show(
                $"Restore backup '{Selected.DisplayName}' into cfg folder?\nCurrent files will be overwritten.",
                "Confirm Restore",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            // Safety backup of current state first
            _state.Services.Backups.CreateFullCfgBackup("pre_restore", _state.CfgDirectory!);
            _state.Services.Backups.Restore(Selected, _state.CfgDirectory!);
            Reload();
            _state.SetStatus($"Restored backup '{Selected.DisplayName}'.");
            System.Windows.MessageBox.Show("Backup restored successfully.", "Backups",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Restore Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void Delete()
    {
        if (Selected is null) return;
        if (System.Windows.MessageBox.Show($"Delete backup '{Selected.DisplayName}'?", "Confirm",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        _state.Services.Backups.Delete(Selected);
        Selected = null;
        Reload();
        _state.SetStatus("Backup deleted.");
    }

    private void Export()
    {
        if (Selected is null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Zip archive|*.zip",
            FileName = $"{Selected.Id}.zip"
        };
        if (dialog.ShowDialog() == true)
        {
            _state.Services.Backups.ExportZip(Selected, dialog.FileName);
            _state.SetStatus($"Exported backup to {dialog.FileName}");
        }
    }

    private void ShowDiff()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        var autoexec = Path.Combine(_state.CfgDirectory!, "autoexec.cfg");
        DiffText = _state.Services.Backups.Diff(autoexec, Selected);
    }
}
