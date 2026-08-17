using System.Collections.ObjectModel;
using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class LaunchViewModel : ViewModelBase
{
    private readonly AppState _state;
    private string _selectedMethod;
    private string _extraArgs = string.Empty;
    private bool _offline;
    private string _lastResult = string.Empty;

    public ObservableCollection<string> Methods { get; } = new() { "exe" };

    public string SelectedMethod
    {
        get => _selectedMethod;
        set => SetProperty(ref _selectedMethod, value);
    }

    public string ExtraArgs
    {
        get => _extraArgs;
        set => SetProperty(ref _extraArgs, value);
    }

    public bool Offline
    {
        get => _offline;
        set => SetProperty(ref _offline, value);
    }

    public string LastResult
    {
        get => _lastResult;
        set => SetProperty(ref _lastResult, value);
    }

    public ICommand LaunchCommand { get; }
    public ICommand LaunchWithProfileCommand { get; }
    public ICommand SaveDefaultsCommand { get; }
    public ICommand TestPathCommand { get; }

    public ObservableCollection<string> Profiles { get; } = new();
    private string? _selectedProfile;
    public string? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public LaunchViewModel(AppState state)
    {
        _state = state;
        var settings = state.Services.Settings.Current;
        _selectedMethod = string.IsNullOrWhiteSpace(settings.DefaultLaunchMethod) ? "exe" : settings.DefaultLaunchMethod;
        _offline = settings.LaunchOffline;
        _extraArgs = settings.CustomLaunchArgs ?? string.Empty;

        LaunchCommand = new RelayCommand(Launch);
        LaunchWithProfileCommand = new RelayCommand(LaunchWithProfile);
        SaveDefaultsCommand = new RelayCommand(SaveDefaults);
        TestPathCommand = new RelayCommand(TestPath);

        ReloadProfiles();
    }

    public void ReloadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _state.Services.Profiles.ListProfiles())
            Profiles.Add(p.Name);
        if (Profiles.Count > 0 && SelectedProfile is null)
            SelectedProfile = Profiles[0];
    }

    private void Launch()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            LastResult = "CS:GO cfg folder not configured. Set the path in Settings.";
            _state.SetStatus(LastResult);
            System.Windows.MessageBox.Show("CS:GO cfg folder not configured. Set the path in Settings.", "Launch Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var extra = ExtraArgs?.Trim();
            var result = _state.Services.Launch.Launch(_selectedMethod, _state.Installation, _state.Services.Settings.Current, extra);
            LastResult = result.Message;
            _state.SetStatus(LastResult);

            if (!result.Success)
            {
                System.Windows.MessageBox.Show(result.Message, "Launch Failed",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            LastResult = $"Error: {ex.Message}";
            _state.SetStatus(LastResult);
            System.Windows.MessageBox.Show(ex.Message, "Launch Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void LaunchWithProfile()
    {
        if (string.IsNullOrWhiteSpace(_selectedProfile))
        {
            LastResult = "No profile selected.";
            _state.SetStatus(LastResult);
            return;
        }

        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            LastResult = "CS:GO cfg folder not configured. Set the path in Settings.";
            _state.SetStatus(LastResult);
            System.Windows.MessageBox.Show("CS:GO cfg folder not configured. Set the path in Settings.", "Launch Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var profile = _state.Services.Profiles.GetProfile(_selectedProfile);
            if (profile is null)
            {
                LastResult = $"Profile '{_selectedProfile}' not found.";
                _state.SetStatus(LastResult);
                System.Windows.MessageBox.Show(LastResult, "Launch Failed",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = _state.Services.Launch.Launch(_selectedMethod, _state.Installation, _state.Services.Settings.Current, _extraArgs);
            LastResult = result.Message;
            _state.SetStatus(LastResult);
            
            if (!result.Success)
            {
                System.Windows.MessageBox.Show(result.Message, "Launch Failed",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            LastResult = $"Error: {ex.Message}";
            _state.SetStatus(LastResult);
            System.Windows.MessageBox.Show(ex.Message, "Launch Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void SaveDefaults()
    {
        var settings = _state.Services.Settings.Current;
        settings.DefaultLaunchMethod = _selectedMethod;
        settings.CustomLaunchArgs = string.IsNullOrWhiteSpace(_extraArgs) ? null : _extraArgs.Trim();
        settings.LaunchOffline = _offline;
        _state.Services.Settings.Save(settings);
        _state.RefreshDetection();
        LastResult = "Defaults saved.";
        _state.SetStatus(LastResult);
    }

    private void TestPath()
    {
        if (string.IsNullOrWhiteSpace(_state.Installation.CsgoExePath))
        {
            LastResult = "CS:GO executable not found. Check game detection in Settings.";
            _state.SetStatus(LastResult);
            System.Windows.MessageBox.Show("CS:GO executable not found. Check game detection in Settings.", "Path Test Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        LastResult = $"CS:GO executable found: {_state.Installation.CsgoExePath}";
        _state.SetStatus(LastResult);
        System.Windows.MessageBox.Show(LastResult, "Path Test Success",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
