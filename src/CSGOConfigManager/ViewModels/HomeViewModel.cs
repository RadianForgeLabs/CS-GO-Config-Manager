using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private readonly AppState _state;

    public ObservableCollection<string> DetectionMessages { get; } = new();

    public string SteamStatus => _state.Installation.SteamFound ? $"Found · {_state.Installation.SteamPath}" : "Not found";
    public string SevenStatus => _state.Installation.SevenLauncherFound ? $"Found · {_state.Installation.SevenLauncherPath}" : "Not found";
    public string CsgoStatus => _state.Installation.CsgoFound ? $"Found · {_state.Installation.CsgoRootPath}" : "Not found";
    public string CfgStatus => string.IsNullOrWhiteSpace(_state.Installation.CsgoCfgPath) ? "—" : _state.Installation.CsgoCfgPath!;
    public string VersionStatus => string.IsNullOrWhiteSpace(_state.Installation.GameVersion) ? "Unknown" : _state.Installation.GameVersion!;
    public string DetectionSource => _state.Installation.DetectionSource;
    public bool CanLaunch => _state.IsGameReady || _state.Installation.SteamFound;

    public ICommand RefreshCommand { get; }
    public ICommand LaunchExeCommand { get; }
    public ICommand OpenGameFolderCommand { get; }
    public ICommand OpenCfgFolderCommand { get; }
    public ICommand OpenUserdataCommand { get; }

    public HomeViewModel(AppState state)
    {
        _state = state;
        _state.InstallationChanged += (_, _) => RefreshDisplay();

        RefreshCommand = new RelayCommand(Refresh);
        LaunchExeCommand = new RelayCommand(() => Launch("exe"), () => !string.IsNullOrWhiteSpace(_state.Installation.CsgoExePath));
        OpenGameFolderCommand = new RelayCommand(OpenGameFolder, () => _state.Installation.CsgoFound);
        OpenCfgFolderCommand = new RelayCommand(OpenCfgFolder, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        OpenUserdataCommand = new RelayCommand(OpenUserdata, () => _state.Installation.SteamFound);

        RefreshDisplay();
    }

    public void Refresh()
    {
        _state.RefreshDetection();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        DetectionMessages.Clear();
        foreach (var msg in _state.Installation.Messages)
            DetectionMessages.Add(msg);

        OnPropertyChanged(nameof(SteamStatus));
        OnPropertyChanged(nameof(SevenStatus));
        OnPropertyChanged(nameof(CsgoStatus));
        OnPropertyChanged(nameof(CfgStatus));
        OnPropertyChanged(nameof(VersionStatus));
        OnPropertyChanged(nameof(DetectionSource));
        OnPropertyChanged(nameof(CanLaunch));
    }

    private void Launch(string method)
    {
        var result = _state.Services.Launch.Launch(method, _state.Installation, _state.Services.Settings.Current);
        _state.SetStatus(result.Message);
        if (!result.Success)
            System.Windows.MessageBox.Show(result.Message, "Launch Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }

    private void OpenGameFolder()
    {
        if (_state.Installation.CsgoRootPath is null) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = _state.Installation.CsgoRootPath,
            UseShellExecute = true
        });
    }

    private void OpenCfgFolder()
    {
        if (_state.CfgDirectory is null) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = _state.CfgDirectory,
            UseShellExecute = true
        });
    }

    private void OpenUserdata()
    {
        if (_state.Installation.SteamPath is null) return;
        var userdata = Path.Combine(_state.Installation.SteamPath, "userdata");
        if (!Directory.Exists(userdata)) return;
        Process.Start(new ProcessStartInfo { FileName = userdata, UseShellExecute = true });
    }
}
