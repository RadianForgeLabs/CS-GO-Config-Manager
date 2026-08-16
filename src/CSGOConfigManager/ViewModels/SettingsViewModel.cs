using System.Diagnostics;
using System.Windows.Input;
using CSGOConfigManager.Services;
using Microsoft.Win32;

namespace CSGOConfigManager.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string _steamPath = string.Empty;
    private string _csgoPath = string.Empty;
    private string _sevenPath = string.Empty;
    private string _customExe = string.Empty;
    private string _customArgs = string.Empty;
    private string _defaultMethod = "steam";
    private bool _autoBackup = true;
    private bool _offline;
    private int _maxBackups = 50;
    private string _configFileName = "rfl_config.cfg";

    public string[] LaunchMethods { get; } = { "steam", "7launcher", "exe", "custom" };

    public string SteamPath { get => _steamPath; set => SetProperty(ref _steamPath, value); }
    public string CsgoPath { get => _csgoPath; set => SetProperty(ref _csgoPath, value); }
    public string SevenPath { get => _sevenPath; set => SetProperty(ref _sevenPath, value); }
    public string CustomExe { get => _customExe; set => SetProperty(ref _customExe, value); }
    public string CustomArgs { get => _customArgs; set => SetProperty(ref _customArgs, value); }
    public string DefaultMethod { get => _defaultMethod; set => SetProperty(ref _defaultMethod, value); }
    public bool AutoBackup { get => _autoBackup; set => SetProperty(ref _autoBackup, value); }
    public bool Offline { get => _offline; set => SetProperty(ref _offline, value); }
    public int MaxBackups { get => _maxBackups; set => SetProperty(ref _maxBackups, value); }
    public string ConfigFileName { get => _configFileName; set => SetProperty(ref _configFileName, value); }

    public ICommand SaveCommand { get; }
    public ICommand BrowseSteamCommand { get; }
    public ICommand BrowseCsgoCommand { get; }
    public ICommand BrowseSevenCommand { get; }
    public ICommand BrowseCustomCommand { get; }
    public ICommand DetectCommand { get; }
    public ICommand VisitWebsiteCommand { get; }

    public SettingsViewModel(AppState state)
    {
        _state = state;
        LoadFromSettings();

        SaveCommand = new RelayCommand(Save);
        BrowseSteamCommand = new RelayCommand(() => BrowseFolder(p => SteamPath = p));
        BrowseCsgoCommand = new RelayCommand(() => BrowseFolder(p => CsgoPath = p));
        BrowseSevenCommand = new RelayCommand(() => BrowseFile(p => SevenPath = p, "7Launcher|7launcher.exe|Executables|*.exe"));
        BrowseCustomCommand = new RelayCommand(() => BrowseFile(p => CustomExe = p, "Executables|*.exe"));
        DetectCommand = new RelayCommand(() =>
        {
            Save();
            _state.RefreshDetection();
            LoadFromSettings();
            // Also fill detected values if empty
            if (string.IsNullOrWhiteSpace(SteamPath) && _state.Installation.SteamPath is not null)
                SteamPath = _state.Installation.SteamPath;
            if (string.IsNullOrWhiteSpace(CsgoPath) && _state.Installation.CsgoRootPath is not null)
                CsgoPath = _state.Installation.CsgoRootPath;
            if (string.IsNullOrWhiteSpace(SevenPath) && _state.Installation.SevenLauncherPath is not null)
                SevenPath = _state.Installation.SevenLauncherPath;
        });
        VisitWebsiteCommand = new RelayCommand(VisitWebsite);
    }

    private void LoadFromSettings()
    {
        var s = _state.Services.Settings.Load();
        SteamPath = s.SteamPath ?? string.Empty;
        CsgoPath = s.CsgoPath ?? string.Empty;
        SevenPath = s.SevenLauncherPath ?? string.Empty;
        CustomExe = s.CustomExePath ?? string.Empty;
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
        s.SteamPath = string.IsNullOrWhiteSpace(SteamPath) ? null : SteamPath.Trim();
        s.CsgoPath = string.IsNullOrWhiteSpace(CsgoPath) ? null : CsgoPath.Trim();
        s.SevenLauncherPath = string.IsNullOrWhiteSpace(SevenPath) ? null : SevenPath.Trim();
        s.CustomExePath = string.IsNullOrWhiteSpace(CustomExe) ? null : CustomExe.Trim();
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
