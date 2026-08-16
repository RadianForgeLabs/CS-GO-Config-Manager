using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class PracticeViewModel : ViewModelBase
{
    private readonly AppState _state;
    private bool _svCheats = true;
    private int _infiniteAmmo;
    private int _showImpacts;
    private bool _grenadeTrajectory;
    private double _grenadeTime = 20;
    private bool _buyAnywhere;
    private int _warmup = 60;
    private bool _friendlyFire;
    private string _lastExecCommand = string.Empty;
    private string _configStatus = string.Empty;

    public bool SvCheats { get => _svCheats; set => SetProperty(ref _svCheats, value); }
    public int InfiniteAmmo { get => _infiniteAmmo; set => SetProperty(ref _infiniteAmmo, value); }
    public int ShowImpacts { get => _showImpacts; set => SetProperty(ref _showImpacts, value); }
    public bool GrenadeTrajectory { get => _grenadeTrajectory; set => SetProperty(ref _grenadeTrajectory, value); }
    public double GrenadeTime { get => _grenadeTime; set => SetProperty(ref _grenadeTime, value); }
    public bool BuyAnywhere { get => _buyAnywhere; set => SetProperty(ref _buyAnywhere, value); }
    public int Warmup { get => _warmup; set => SetProperty(ref _warmup, value); }
    public bool FriendlyFire { get => _friendlyFire; set => SetProperty(ref _friendlyFire, value); }
    public string LastExecCommand { get => _lastExecCommand; set => SetProperty(ref _lastExecCommand, value); }
    public string ConfigStatus { get => _configStatus; set => SetProperty(ref _configStatus, value); }

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand EnableAllPracticeCommand { get; }
    public ICommand DisableAllPracticeCommand { get; }
    public ICommand GenerateConfigCommand { get; }
    public ICommand CopyExecCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }

    public PracticeViewModel(AppState state)
    {
        _state = state;
        LoadCommand = new RelayCommand(Load);
        SaveCommand = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        EnableAllPracticeCommand = new RelayCommand(() =>
        {
            SvCheats = true;
            InfiniteAmmo = 1;
            ShowImpacts = 1;
            GrenadeTrajectory = true;
            BuyAnywhere = true;
            Warmup = 9999;
        });
        DisableAllPracticeCommand = new RelayCommand(() =>
        {
            InfiniteAmmo = 0;
            ShowImpacts = 0;
            GrenadeTrajectory = false;
            BuyAnywhere = false;
        });
        GenerateConfigCommand = new RelayCommand(GenerateConfig, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        CopyExecCommand = new RelayCommand(CopyExecCommandAction, () => !string.IsNullOrWhiteSpace(_lastExecCommand));
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        Load();
    }

    public void Load()
    {
        var cfg = _state.CfgDirectory;
        if (string.IsNullOrWhiteSpace(cfg)) return;

        SvCheats = IsTruthy(_state.Services.Config.GetCurrentValue(cfg, "sv_cheats"));
        InfiniteAmmo = ParseInt(_state.Services.Config.GetCurrentValue(cfg, "sv_infinite_ammo"), 0);
        ShowImpacts = ParseInt(_state.Services.Config.GetCurrentValue(cfg, "sv_showimpacts"), 0);
        GrenadeTrajectory = IsTruthy(_state.Services.Config.GetCurrentValue(cfg, "sv_grenade_trajectory"));
        GrenadeTime = ParseDouble(_state.Services.Config.GetCurrentValue(cfg, "sv_grenade_trajectory_time"), 20);
        BuyAnywhere = IsTruthy(_state.Services.Config.GetCurrentValue(cfg, "mp_buy_anywhere"));
        Warmup = ParseInt(_state.Services.Config.GetCurrentValue(cfg, "mp_warmuptime"), 60);
        FriendlyFire = IsTruthy(_state.Services.Config.GetCurrentValue(cfg, "mp_friendlyfire"));
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
            return;

        try
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sv_cheats"] = SvCheats ? "1" : "0",
                ["sv_infinite_ammo"] = InfiniteAmmo.ToString(),
                ["sv_showimpacts"] = ShowImpacts.ToString(),
                ["sv_grenade_trajectory"] = GrenadeTrajectory ? "1" : "0",
                ["sv_grenade_trajectory_time"] = GrenadeTime.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["mp_buy_anywhere"] = BuyAnywhere ? "1" : "0",
                ["mp_warmuptime"] = Warmup.ToString(),
                ["mp_friendlyfire"] = FriendlyFire ? "1" : "0"
            };

            // Apply to all game mode config files
            var gameModeFiles = new[]
            {
                "gamemode_casual.cfg",
                "gamemode_competitive.cfg",
                "gamemode_deathmatch.cfg",
                "gamemode_armsrace.cfg",
                "gamemode_demolition.cfg",
                "gamemode_cooperative.cfg",
                "gamemode_custom.cfg",
                "practice.cfg",
                "autoexec.cfg"
            };

            var touched = _state.Services.Config.ApplyValuesToMultipleFiles(_state.CfgDirectory!, values, gameModeFiles);
            _state.SetStatus($"Practice settings saved to all game mode configs ({touched.Count} file(s)).");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Save Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void GenerateConfig()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var commands = GetPracticeCommands();
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";

            var result = _state.ConfigGeneration.GenerateAndCopy(_state.CfgDirectory!, fileName, commands);

            if (result.Success)
            {
                LastExecCommand = result.ExecCommand ?? string.Empty;
                ConfigStatus = $"✓ Config generated successfully\n✓ Saved to CS:GO cfg folder\n✓ EXEC command copied to clipboard";
                _state.SetStatus("Practice config generated. Exec command copied to clipboard.");
            }
            else
            {
                ConfigStatus = $"Error: {result.ErrorMessage}";
                _state.SetStatus($"Failed to generate config: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ConfigStatus = $"Error: {ex.Message}";
            _state.SetStatus($"Error generating config: {ex.Message}");
        }
    }

    private IEnumerable<string> GetPracticeCommands()
    {
        return new List<string>
        {
            $"sv_cheats {(SvCheats ? "1" : "0")}",
            $"sv_infinite_ammo {InfiniteAmmo}",
            $"sv_showimpacts {ShowImpacts}",
            $"sv_grenade_trajectory {(GrenadeTrajectory ? "1" : "0")}",
            $"sv_grenade_trajectory_time {GrenadeTime.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"mp_buy_anywhere {(BuyAnywhere ? "1" : "0")}",
            $"mp_warmuptime {Warmup}",
            $"mp_friendlyfire {(FriendlyFire ? "1" : "0")}"
        };
    }

    private void CopyExecCommandAction()
    {
        if (string.IsNullOrWhiteSpace(_lastExecCommand)) return;

        try
        {
            _state.ConfigGeneration.CopyExecCommand(
                _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg");
            _state.SetStatus("EXEC command copied to clipboard.");
        }
        catch (Exception ex)
        {
            _state.SetStatus($"Failed to copy exec command: {ex.Message}");
        }
    }

    private void OpenConfigFolder()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _state.CfgDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _state.SetStatus($"Failed to open config folder: {ex.Message}");
        }
    }

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out var n) ? n : fallback;

    private static double ParseDouble(string? value, double fallback) =>
        double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : fallback;

    private static bool IsTruthy(string? value) =>
        value is "1" or "true" or "True" or "yes" or "on";
}
