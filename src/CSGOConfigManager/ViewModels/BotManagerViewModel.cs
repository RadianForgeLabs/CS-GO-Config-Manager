using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class BotManagerViewModel : ViewModelBase
{
    private readonly AppState _state;
    private readonly GameWindowService _gameWindows;
    private readonly GameCommandSender _commandSender;
    private int _quota = 10;
    private string _quotaMode = "fill";
    private int _difficulty = 1;
    private string _joinTeam = "any";
    private bool _botStop;
    private bool _dontShoot;
    private bool _isApplyingLive;
    private string _lastExecCommand = string.Empty;
    private string _configStatus = string.Empty;

    public int Quota { get => _quota; set => SetProperty(ref _quota, value); }
    public string QuotaMode { get => _quotaMode; set => SetProperty(ref _quotaMode, value); }
    public int Difficulty { get => _difficulty; set => SetProperty(ref _difficulty, value); }
    public string JoinTeam { get => _joinTeam; set => SetProperty(ref _joinTeam, value); }
    public bool BotStop { get => _botStop; set => SetProperty(ref _botStop, value); }
    public bool DontShoot { get => _dontShoot; set => SetProperty(ref _dontShoot, value); }
    public bool IsApplyingLive { get => _isApplyingLive; set => SetProperty(ref _isApplyingLive, value); }
    public string LastExecCommand { get => _lastExecCommand; set => SetProperty(ref _lastExecCommand, value); }
    public string ConfigStatus { get => _configStatus; set => SetProperty(ref _configStatus, value); }

    public string[] QuotaModes { get; } = { "normal", "fill", "match" };
    public string[] JoinTeams { get; } = { "any", "t", "ct" };
    public string[] Difficulties { get; } = { "0 - Easy", "1 - Normal", "2 - Hard", "3 - Expert" };

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ApplyLiveCommand { get; }
    public ICommand KickAllLiveCommand { get; }
    public ICommand PresetTOnlyCommand { get; }
    public ICommand PresetCtOnlyCommand { get; }
    public ICommand Preset5Command { get; }
    public ICommand Preset10Command { get; }
    public ICommand KickAllCommand { get; }
    public ICommand GenerateConfigCommand { get; }
    public ICommand CopyExecCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }

    public BotManagerViewModel(AppState state)
    {
        _state = state;
        _gameWindows = new GameWindowService();
        _commandSender = new GameCommandSender(_gameWindows);
        LoadCommand = new RelayCommand(Load);
        SaveCommand = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        ApplyLiveCommand = new RelayCommand(ApplyLive, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory) && !_isApplyingLive);
        KickAllLiveCommand = new RelayCommand(KickAllLive, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory) && !_isApplyingLive);
        PresetTOnlyCommand = new RelayCommand(() => { JoinTeam = "t"; Quota = 10; QuotaMode = "fill"; });
        PresetCtOnlyCommand = new RelayCommand(() => { JoinTeam = "ct"; Quota = 10; QuotaMode = "fill"; });
        Preset5Command = new RelayCommand(() => { Quota = 5; QuotaMode = "normal"; });
        Preset10Command = new RelayCommand(() => { Quota = 10; QuotaMode = "normal"; });
        KickAllCommand = new RelayCommand(() =>
        {
            Quota = 0;
            Save();
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

        Quota = ParseInt(_state.Services.Config.GetCurrentValue(cfg, "bot_quota"), 10);
        QuotaMode = _state.Services.Config.GetCurrentValue(cfg, "bot_quota_mode") ?? "fill";
        Difficulty = ParseInt(_state.Services.Config.GetCurrentValue(cfg, "bot_difficulty"), 1);
        JoinTeam = _state.Services.Config.GetCurrentValue(cfg, "bot_join_team") ?? "any";
        BotStop = IsTruthy(_state.Services.Config.GetCurrentValue(cfg, "bot_stop"));
        DontShoot = IsTruthy(_state.Services.Config.GetCurrentValue(cfg, "bot_dont_shoot"));
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            System.Windows.MessageBox.Show("CS:GO cfg folder not configured.", "Bots",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bot_quota"] = Quota.ToString(),
                ["bot_quota_mode"] = QuotaMode,
                ["bot_difficulty"] = Difficulty.ToString(),
                ["bot_join_team"] = JoinTeam,
                ["bot_stop"] = BotStop ? "1" : "0",
                ["bot_dont_shoot"] = DontShoot ? "1" : "0"
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
            _state.SetStatus($"Bot settings saved to all game mode configs ({touched.Count} file(s)).");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Save Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public async void ApplyLive()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory) || _isApplyingLive) return;

        IsApplyingLive = true;
        try
        {
            var commands = GetBotCommands();
            var cfgDirectory = _state.CfgDirectory!;
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";

            var result = _state.ConfigGeneration.GenerateAndCopy(cfgDirectory, fileName, commands);

            if (result.Success)
            {
                LastExecCommand = result.ExecCommand ?? string.Empty;
                ConfigStatus = $"✓ Config generated successfully\n✓ Saved to CS:GO cfg folder\n✓ EXEC command copied to clipboard";
                _state.SetStatus("Bot settings applied via config file. Exec command copied to clipboard.");
            }
            else
            {
                ConfigStatus = $"Error: {result.ErrorMessage}";
                _state.SetStatus($"Failed to apply bot settings: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ConfigStatus = $"Error: {ex.Message}";
            _state.SetStatus($"Error applying bot settings: {ex.Message}");
        }
        finally
        {
            IsApplyingLive = false;
        }
    }

    private IEnumerable<string> GetBotCommands()
    {
        yield return $"bot_quota {Quota}";
        yield return $"bot_quota_mode {QuotaMode}";
        yield return $"bot_difficulty {Difficulty}";
        yield return $"bot_join_team {JoinTeam}";
        yield return $"bot_stop {(BotStop ? "1" : "0")}";
        yield return $"bot_dont_shoot {(DontShoot ? "1" : "0")}";
    }

    public async void KickAllLive()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory) || _isApplyingLive) return;

        IsApplyingLive = true;
        try
        {
            var commands = new[] { "bot_quota 0", "bot_kick" };
            var cfgDirectory = _state.CfgDirectory!;
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";

            var result = _state.ConfigGeneration.GenerateAndCopy(cfgDirectory, fileName, commands);

            if (result.Success)
            {
                Quota = 0;
                LastExecCommand = result.ExecCommand ?? string.Empty;
                ConfigStatus = $"✓ Config generated successfully\n✓ Saved to CS:GO cfg folder\n✓ EXEC command copied to clipboard";
                _state.SetStatus("All bots kicked via config file. Exec command copied to clipboard.");
            }
            else
            {
                ConfigStatus = $"Error: {result.ErrorMessage}";
                _state.SetStatus($"Failed to kick bots: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ConfigStatus = $"Error: {ex.Message}";
            _state.SetStatus($"Error kicking bots: {ex.Message}");
        }
        finally
        {
            IsApplyingLive = false;
        }
    }

    private void GenerateConfig()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var commands = GetBotCommands();
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";

            var result = _state.ConfigGeneration.GenerateAndCopy(_state.CfgDirectory!, fileName, commands);

            if (result.Success)
            {
                LastExecCommand = result.ExecCommand ?? string.Empty;
                ConfigStatus = $"✓ Config generated successfully\n✓ Saved to CS:GO cfg folder\n✓ EXEC command copied to clipboard";
                _state.SetStatus("Bot config generated. Exec command copied to clipboard.");
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

    private static bool IsTruthy(string? value) =>
        value is "1" or "true" or "True" or "yes" or "on";
}
