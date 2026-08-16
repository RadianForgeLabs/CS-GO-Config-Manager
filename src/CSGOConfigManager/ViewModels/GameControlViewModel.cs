using System.Windows.Input;
using CSGOConfigManager.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class GameControlViewModel : ViewModelBase
{
    private readonly AppState _state;
    private readonly GameCommandSender _commands;
    private bool _godMode;
    private bool _noClip;
    private bool _infiniteAmmo;
    private bool _bunnyHop;
    private string _currentRoundTime = "1.92";
    private string _freezeTime = "15";
    private string _maxRounds = "30";
    private string _warmupTime = "60";
    private string _lastExecCommand = string.Empty;
    private string _configStatus = string.Empty;

    public bool GodMode { get => _godMode; set => SetProperty(ref _godMode, value); }
    public bool NoClip { get => _noClip; set => SetProperty(ref _noClip, value); }
    public bool InfiniteAmmo { get => _infiniteAmmo; set => SetProperty(ref _infiniteAmmo, value); }
    public bool BunnyHop { get => _bunnyHop; set => SetProperty(ref _bunnyHop, value); }
    public string CurrentRoundTime { get => _currentRoundTime; set => SetProperty(ref _currentRoundTime, value); }
    public string FreezeTime { get => _freezeTime; set => SetProperty(ref _freezeTime, value); }
    public string MaxRounds { get => _maxRounds; set => SetProperty(ref _maxRounds, value); }
    public string WarmupTime { get => _warmupTime; set => SetProperty(ref _warmupTime, value); }
    public string LastExecCommand { get => _lastExecCommand; set => SetProperty(ref _lastExecCommand, value); }
    public string ConfigStatus { get => _configStatus; set => SetProperty(ref _configStatus, value); }

    public ICommand EndRoundCommand { get; }
    public ICommand RestartGameCommand { get; }
    public ICommand EndWarmupCommand { get; }
    public ICommand SetRoundTimeCommand { get; }
    public ICommand SetFreezeTimeCommand { get; }
    public ICommand SetMaxRoundsCommand { get; }
    public ICommand SetWarmupTimeCommand { get; }
    public ICommand ApplyGameSettingsCommand { get; }
    public ICommand GiveWeaponCommand { get; }
    public ICommand RespawnCommand { get; }
    public ICommand KillBotsCommand { get; }
    public ICommand GenerateConfigCommand { get; }
    public ICommand CopyExecCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }

    public GameControlViewModel(AppState state, GameCommandSender commands)
    {
        _state = state;
        _commands = commands;

        EndRoundCommand = new RelayCommand(EndRound);
        RestartGameCommand = new RelayCommand(RestartGame);
        EndWarmupCommand = new RelayCommand(EndWarmup);
        SetRoundTimeCommand = new RelayCommand(SetRoundTime);
        SetFreezeTimeCommand = new RelayCommand(SetFreezeTime);
        SetMaxRoundsCommand = new RelayCommand(SetMaxRounds);
        SetWarmupTimeCommand = new RelayCommand(SetWarmupTime);
        ApplyGameSettingsCommand = new RelayCommand(ApplyGameSettings);
        GiveWeaponCommand = new RelayCommand(GiveWeapon);
        RespawnCommand = new RelayCommand(Respawn);
        KillBotsCommand = new RelayCommand(KillBots);
        GenerateConfigCommand = new RelayCommand(GenerateConfig, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));
        CopyExecCommand = new RelayCommand(CopyExecCommandAction, () => !string.IsNullOrWhiteSpace(_lastExecCommand));
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder, () => !string.IsNullOrWhiteSpace(_state.CfgDirectory));

        LoadFromConfigs();
    }

    public void LoadFromConfigs()
    {
        var cfg = _state.CfgDirectory;
        if (string.IsNullOrWhiteSpace(cfg)) return;

        CurrentRoundTime = _state.Services.Config.GetCurrentValue(cfg, "mp_roundtime_defuse") ?? "1.92";
        FreezeTime = _state.Services.Config.GetCurrentValue(cfg, "mp_freezetime") ?? "15";
        MaxRounds = _state.Services.Config.GetCurrentValue(cfg, "mp_maxrounds") ?? "30";
        WarmupTime = _state.Services.Config.GetCurrentValue(cfg, "mp_warmuptime") ?? "60";
    }

    private async void EndRound()
    {
        await SendCommandsAsync(new[] { "endround" });
    }

    private async void RestartGame()
    {
        await SendCommandsAsync(new[] { "mp_restartgame 1" });
    }

    private async void EndWarmup()
    {
        await SendCommandsAsync(new[] { "mp_warmup_end" });
    }

    private async void SetRoundTime()
    {
        if (double.TryParse(CurrentRoundTime, out var time))
        {
            await SendCommandsAsync(new[] { $"mp_roundtime_defuse {time}" });
        }
    }

    private async void SetFreezeTime()
    {
        if (int.TryParse(FreezeTime, out var time))
        {
            await SendCommandsAsync(new[] { $"mp_freezetime {time}" });
        }
    }

    private async void SetMaxRounds()
    {
        if (int.TryParse(MaxRounds, out var rounds))
        {
            await SendCommandsAsync(new[] { $"mp_maxrounds {rounds}" });
        }
    }

    private async void SetWarmupTime()
    {
        if (int.TryParse(WarmupTime, out var time))
        {
            await SendCommandsAsync(new[] { $"mp_warmuptime {time}" });
        }
    }

    private async void ApplyGameSettings()
    {
        ApplyGameSettingsToAllGameModes();
        
        var commands = new List<string>
        {
            $"sv_cheats {(GodMode || NoClip || InfiniteAmmo || BunnyHop ? "1" : "0")}",
            $"god {(GodMode ? "1" : "0")}",
            $"noclip {(NoClip ? "1" : "0")}",
            $"sv_infinite_ammo {(InfiniteAmmo ? "1" : "0")}",
            $"sv_autobunnyhopping {(BunnyHop ? "1" : "0")}"
        };

        if (double.TryParse(CurrentRoundTime, out var roundTime))
            commands.Add($"mp_roundtime_defuse {roundTime}");

        if (int.TryParse(FreezeTime, out var freezeTime))
            commands.Add($"mp_freezetime {freezeTime}");

        if (int.TryParse(MaxRounds, out var maxRounds))
            commands.Add($"mp_maxrounds {maxRounds}");

        if (int.TryParse(WarmupTime, out var warmupTime))
            commands.Add($"mp_warmuptime {warmupTime}");

        await SendCommandsAsync(commands);
    }

    private async void GiveWeapon()
    {
        await SendCommandsAsync(new[] { "give weapon_ak47" });
    }

    private async void Respawn()
    {
        await SendCommandsAsync(new[] { "respawn" });
    }

    private async void KillBots()
    {
        await SendCommandsAsync(new[] { "bot_kick" });
    }

    private Action<string>? _statusCallback;

    public void SetStatusCallback(Action<string> callback)
    {
        _statusCallback = callback;
    }

    private async Task SendCommandsAsync(IEnumerable<string> commands)
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            _statusCallback?.Invoke("CS:GO cfg folder not configured.");
            return;
        }

        try
        {
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";
            var result = _state.ConfigGeneration.GenerateAndCopy(_state.CfgDirectory!, fileName, commands);

            if (result.Success)
            {
                LastExecCommand = result.ExecCommand ?? string.Empty;
                ConfigStatus = $"✓ Config generated successfully\n✓ Saved to CS:GO cfg folder\n✓ EXEC command copied to clipboard";
                _statusCallback?.Invoke("Game control applied via config file. Exec command copied to clipboard.");
            }
            else
            {
                ConfigStatus = $"Error: {result.ErrorMessage}";
                _statusCallback?.Invoke($"Failed to apply game control: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ConfigStatus = $"Error: {ex.Message}";
            _statusCallback?.Invoke($"Error: {ex.Message}");
        }
    }

    private void ApplyGameSettingsToAllGameModes()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var gameControlValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sv_cheats"] = (GodMode || NoClip || InfiniteAmmo || BunnyHop) ? "1" : "0",
                ["god"] = GodMode ? "1" : "0",
                ["noclip"] = NoClip ? "1" : "0",
                ["sv_infinite_ammo"] = InfiniteAmmo ? "1" : "0",
                ["sv_autobunnyhopping"] = BunnyHop ? "1" : "0"
            };

            if (double.TryParse(CurrentRoundTime, out var roundTime))
                gameControlValues["mp_roundtime_defuse"] = roundTime.ToString();

            if (int.TryParse(FreezeTime, out var freezeTime))
                gameControlValues["mp_freezetime"] = freezeTime.ToString();

            if (int.TryParse(MaxRounds, out var maxRounds))
                gameControlValues["mp_maxrounds"] = maxRounds.ToString();

            if (int.TryParse(WarmupTime, out var warmupTime))
                gameControlValues["mp_warmuptime"] = warmupTime.ToString();

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

            _state.Services.Config.ApplyValuesToMultipleFiles(_state.CfgDirectory!, gameControlValues, gameModeFiles);
            _statusCallback?.Invoke("Game settings applied to all game mode configs.");
        }
        catch (Exception ex)
        {
            _statusCallback?.Invoke($"Error applying game settings: {ex.Message}");
        }
    }

    private void GenerateConfig()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var commands = GetGameControlCommands();
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";

            var result = _state.ConfigGeneration.GenerateAndCopy(_state.CfgDirectory!, fileName, commands);

            if (result.Success)
            {
                LastExecCommand = result.ExecCommand ?? string.Empty;
                ConfigStatus = $"✓ Config generated successfully\n✓ Saved to CS:GO cfg folder\n✓ EXEC command copied to clipboard";
                _statusCallback?.Invoke("Game control config generated. Exec command copied to clipboard.");
            }
            else
            {
                ConfigStatus = $"Error: {result.ErrorMessage}";
                _statusCallback?.Invoke($"Failed to generate config: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ConfigStatus = $"Error: {ex.Message}";
            _statusCallback?.Invoke($"Error generating config: {ex.Message}");
        }
    }

    private IEnumerable<string> GetGameControlCommands()
    {
        var commands = new List<string>
        {
            $"sv_cheats {(GodMode || NoClip || InfiniteAmmo || BunnyHop ? "1" : "0")}",
            $"god {(GodMode ? "1" : "0")}",
            $"noclip {(NoClip ? "1" : "0")}",
            $"sv_infinite_ammo {(InfiniteAmmo ? "1" : "0")}",
            $"sv_autobunnyhopping {(BunnyHop ? "1" : "0")}"
        };

        if (double.TryParse(CurrentRoundTime, out var roundTime))
            commands.Add($"mp_roundtime_defuse {roundTime}");

        if (int.TryParse(FreezeTime, out var freezeTime))
            commands.Add($"mp_freezetime {freezeTime}");

        if (int.TryParse(MaxRounds, out var maxRounds))
            commands.Add($"mp_maxrounds {maxRounds}");

        if (int.TryParse(WarmupTime, out var warmupTime))
            commands.Add($"mp_warmuptime {warmupTime}");

        return commands;
    }

    private void CopyExecCommandAction()
    {
        if (string.IsNullOrWhiteSpace(_lastExecCommand)) return;

        try
        {
            _state.ConfigGeneration.CopyExecCommand(
                _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg");
            _statusCallback?.Invoke("EXEC command copied to clipboard.");
        }
        catch (Exception ex)
        {
            _statusCallback?.Invoke($"Failed to copy exec command: {ex.Message}");
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
            _statusCallback?.Invoke($"Failed to open config folder: {ex.Message}");
        }
    }
}