using System.Diagnostics;
using System.Windows.Input;
using CSGOConfigManager.Services;
using Microsoft.Win32;

namespace CSGOConfigManager.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string _csgoPath = string.Empty;
    private string _sevenLauncherPath = string.Empty;
    private string _revLoaderPath = string.Empty;
    private string _customArgs = string.Empty;
    private string _defaultMethod = "exe";
    private bool _autoBackup = true;
    private int _maxBackups = 50;
    private string _configFileName = "rfl_config.cfg";
    private string _overlayDisplayMethod = "NormalWindow";

    public string CsgoPath { get => _csgoPath; set => SetProperty(ref _csgoPath, value); }
    public string SevenLauncherPath { get => _sevenLauncherPath; set => SetProperty(ref _sevenLauncherPath, value); }
    public string RevLoaderPath { get => _revLoaderPath; set => SetProperty(ref _revLoaderPath, value); }
    public string CustomArgs { get => _customArgs; set => SetProperty(ref _customArgs, value); }
    public string DefaultMethod { get => _defaultMethod; set => SetProperty(ref _defaultMethod, value); }
    public bool AutoBackup { get => _autoBackup; set => SetProperty(ref _autoBackup, value); }
    public int MaxBackups { get => _maxBackups; set => SetProperty(ref _maxBackups, value); }
    public string ConfigFileName { get => _configFileName; set => SetProperty(ref _configFileName, value); }
    public string OverlayDisplayMethod { get => _overlayDisplayMethod; set => SetProperty(ref _overlayDisplayMethod, value); }

    public string[] LaunchMethods { get; } = { "exe", "7launcher", "revloader" };
    public string[] OverlayDisplayMethods { get; } = { "NormalWindow", "TransparentWindow", "MinimalWindow" };

    public ICommand SaveCommand { get; }
    public ICommand BrowseCsgoCommand { get; }
    public ICommand BrowseSevenLauncherCommand { get; }
    public ICommand BrowseRevLoaderCommand { get; }
    public ICommand DetectCommand { get; }
    public ICommand VisitWebsiteCommand { get; }

    public SettingsViewModel(AppState state)
    {
        _state = state;
        LoadFromSettings();

        SaveCommand = new RelayCommand(Save);
        BrowseCsgoCommand = new RelayCommand(() => BrowseFolder(p => CsgoPath = p));
        BrowseSevenLauncherCommand = new RelayCommand(() => BrowseFile(p => SevenLauncherPath = p, "Run_CS2|Run_CS2.exe|Executables|*.exe"));
        BrowseRevLoaderCommand = new RelayCommand(() => BrowseFile(p => RevLoaderPath = p, "RevLoader|revLoader.exe|Executables|*.exe"));
        DetectCommand = new RelayCommand(() =>
        {
            // First save current settings
            var s = _state.Services.Settings.Current;
            s.CsgoPath = string.IsNullOrWhiteSpace(CsgoPath) ? null : CsgoPath.Trim();
            s.SevenLauncherPath = string.IsNullOrWhiteSpace(SevenLauncherPath) ? null : SevenLauncherPath.Trim();
            s.RevLoaderPath = string.IsNullOrWhiteSpace(RevLoaderPath) ? null : RevLoaderPath.Trim();
            s.CustomLaunchArgs = string.IsNullOrWhiteSpace(CustomArgs) ? null : CustomArgs.Trim();
            s.DefaultLaunchMethod = DefaultMethod;
            s.AutoBackupOnChange = AutoBackup;
            s.MaxBackupCount = Math.Clamp(MaxBackups, 5, 500);
            s.ConfigFileName = string.IsNullOrWhiteSpace(ConfigFileName) ? "rfl_config.cfg" : ConfigFileName.Trim();
            s.OverlayDisplayMethod = OverlayDisplayMethod;
            _state.Services.Settings.Save(s);
            
            // Then refresh detection
            _state.RefreshDetection();
            LoadFromSettings();
            
            // Fill detected values if empty
            if (string.IsNullOrWhiteSpace(CsgoPath) && _state.Installation.CsgoRootPath is not null)
                CsgoPath = _state.Installation.CsgoRootPath;
            if (string.IsNullOrWhiteSpace(SevenLauncherPath) && _state.Installation.SevenLauncherPath is not null)
                SevenLauncherPath = _state.Installation.SevenLauncherPath;
            
            _state.SetStatus("Game detection refreshed. Paths updated.");
        });
        VisitWebsiteCommand = new RelayCommand(VisitWebsite);
    }

    private void LoadFromSettings()
    {
        var s = _state.Services.Settings.Load();
        CsgoPath = s.CsgoPath ?? string.Empty;
        SevenLauncherPath = s.SevenLauncherPath ?? string.Empty;
        RevLoaderPath = s.RevLoaderPath ?? string.Empty;
        CustomArgs = s.CustomLaunchArgs ?? string.Empty;
        DefaultMethod = s.DefaultLaunchMethod;
        AutoBackup = s.AutoBackupOnChange;
        MaxBackups = s.MaxBackupCount;
        ConfigFileName = s.ConfigFileName ?? "rfl_config.cfg";
        OverlayDisplayMethod = s.OverlayDisplayMethod ?? "NormalWindow";
    }

    private void Save()
    {
        var s = _state.Services.Settings.Current;
        s.CsgoPath = string.IsNullOrWhiteSpace(CsgoPath) ? null : CsgoPath.Trim();
        s.SevenLauncherPath = string.IsNullOrWhiteSpace(SevenLauncherPath) ? null : SevenLauncherPath.Trim();
        s.RevLoaderPath = string.IsNullOrWhiteSpace(RevLoaderPath) ? null : RevLoaderPath.Trim();
        s.CustomLaunchArgs = string.IsNullOrWhiteSpace(CustomArgs) ? null : CustomArgs.Trim();
        s.DefaultLaunchMethod = DefaultMethod;
        s.AutoBackupOnChange = AutoBackup;
        s.MaxBackupCount = Math.Clamp(MaxBackups, 5, 500);
        s.ConfigFileName = string.IsNullOrWhiteSpace(ConfigFileName) ? "rfl_config.cfg" : ConfigFileName.Trim();
        s.OverlayDisplayMethod = OverlayDisplayMethod;
        _state.Services.Settings.Save(s);
        _state.RefreshDetection();
        _state.SetStatus("Settings saved.");
        System.Windows.MessageBox.Show("Settings saved.", "Settings",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private static void BrowseFolder(Action<string> assign)
    {
        var dialog = new OpenFolderDialog { Title = "Select folder" };
        if (dialog.ShowDialog() == true)
            assign(dialog.FolderName);
    }

    private static void BrowseFile(Action<string> assign, string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter };
        if (dialog.ShowDialog() == true)
            assign(dialog.FileName);
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
