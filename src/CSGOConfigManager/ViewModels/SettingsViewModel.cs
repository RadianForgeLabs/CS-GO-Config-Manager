using System.Diagnostics;
using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string _csgoPath = string.Empty;
    private string _customArgs = string.Empty;
    private string _defaultMethod = "exe";
    private bool _autoBackup = true;
    private bool _offline;
    private int _maxBackups = 50;
    private string _configFileName = "rfl_config.cfg";

    public string CsgoPath { get => _csgoPath; set => SetProperty(ref _csgoPath, value); }
    public string CustomArgs { get => _customArgs; set => SetProperty(ref _customArgs, value); }
    public string DefaultMethod { get => _defaultMethod; set => SetProperty(ref _defaultMethod, value); }
    public bool AutoBackup { get => _autoBackup; set => SetProperty(ref _autoBackup, value); }
    public bool Offline { get => _offline; set => SetProperty(ref _offline, value); }
    public int MaxBackups { get => _maxBackups; set => SetProperty(ref _maxBackups, value); }
    public string ConfigFileName { get => _configFileName; set => SetProperty(ref _configFileName, value); }

    public ICommand SaveCommand { get; }
    public ICommand BrowseCsgoCommand { get; }
    public ICommand DetectCommand { get; }
    public ICommand VisitWebsiteCommand { get; }

    public SettingsViewModel(AppState state)
    {
        _state = state;
        LoadFromSettings();

        SaveCommand = new RelayCommand(Save);
        BrowseCsgoCommand = new RelayCommand(() => BrowseFolder(p => CsgoPath = p));
        DetectCommand = new RelayCommand(() =>
        {
            // First save current settings
            var s = _state.Services.Settings.Current;
            s.CsgoPath = string.IsNullOrWhiteSpace(CsgoPath) ? null : CsgoPath.Trim();
            s.CustomLaunchArgs = string.IsNullOrWhiteSpace(CustomArgs) ? null : CustomArgs.Trim();
            s.DefaultLaunchMethod = DefaultMethod;
            s.AutoBackupOnChange = AutoBackup;
            s.LaunchOffline = Offline;
            s.MaxBackupCount = Math.Clamp(MaxBackups, 5, 500);
            s.ConfigFileName = string.IsNullOrWhiteSpace(ConfigFileName) ? "rfl_config.cfg" : ConfigFileName.Trim();
            _state.Services.Settings.Save(s);
            
            // Then refresh detection
            _state.RefreshDetection();
            LoadFromSettings();
            
            // Fill detected values if empty
            if (string.IsNullOrWhiteSpace(CsgoPath) && _state.Installation.CsgoRootPath is not null)
                CsgoPath = _state.Installation.CsgoRootPath;
            
            _state.SetStatus("Game detection refreshed. Paths updated.");
        });
        VisitWebsiteCommand = new RelayCommand(VisitWebsite);
    }

    private void LoadFromSettings()
    {
        var s = _state.Services.Settings.Load();
        CsgoPath = s.CsgoPath ?? string.Empty;
        CustomArgs = s.CustomLaunchArgs ?? string.Empty;
        DefaultMethod = s.DefaultLaunchMethod;
        AutoBackup = s.AutoBackupOnChange;
        Offline = s.LaunchOffline;
        MaxBackups = s.MaxBackupCount;
        ConfigFileName = s.ConfigFileName ?? "rfl_config.cfg";
    }

    private void Save()
    {
        var s = _state.Services.Settings.Current;
        s.CsgoPath = string.IsNullOrWhiteSpace(CsgoPath) ? null : CsgoPath.Trim();
        s.CustomLaunchArgs = string.IsNullOrWhiteSpace(CustomArgs) ? null : CustomArgs.Trim();
        s.DefaultLaunchMethod = DefaultMethod;
        s.AutoBackupOnChange = AutoBackup;
        s.LaunchOffline = Offline;
        s.MaxBackupCount = Math.Clamp(MaxBackups, 5, 500);
        s.ConfigFileName = string.IsNullOrWhiteSpace(ConfigFileName) ? "rfl_config.cfg" : ConfigFileName.Trim();
        _state.Services.Settings.Save(s);
        _state.RefreshDetection();
        _state.SetStatus("Settings saved.");
        System.Windows.MessageBox.Show("Settings saved.", "Settings",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private static void BrowseFolder(Action<string> assign)
    {
        System.Windows.MessageBox.Show("Please manually enter the path in the text box above.", "Browse Folder",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void VisitWebsite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://radianforgelabs.pages.dev/",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Unable to open website: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
