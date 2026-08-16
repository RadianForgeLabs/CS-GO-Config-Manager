using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CSGOConfigManager.Services;
using CSGOConfigManager.ViewModels;

namespace CSGOConfigManager.Views;

/// <summary>
/// Overlay display style options for different appearance and behavior
/// </summary>
public enum OverlayStyle
{
    /// <summary>
    /// Normal bordered window with solid background (most compatible with anti-cheat)
    /// </summary>
    Normal,
    
    /// <summary>
    /// Transparent window with rounded corners (may trigger anti-cheat)
    /// </summary>
    Transparent,
    
    /// <summary>
    /// Minimal window with reduced UI elements
    /// </summary>
    Minimal
}

/// <summary>
/// Transparent always-on-top overlay that provides real-time CS:GO config management.
/// All settings apply to all game mode config files.
/// </summary>
public partial class OverlayWindow : Window, INotifyPropertyChanged
{
    private readonly AppState _state;
    private readonly BotManagerViewModel _botManager;
    private readonly OverlayStyle _style;
    private bool _svCheats = true;
    private bool _infiniteAmmo = true;
    private bool _grenadeTrajectory = true;
    private bool _buyAnywhere = true;
    private bool _showImpacts = true;
    private bool _godMode = false;
    private string _status = "Config Manager Ready";
    private bool _applying;

    public BotManagerViewModel BotManager => _botManager;
    public bool SvCheats { get => _svCheats; set { _svCheats = value; OnPropertyChanged(); } }
    public bool InfiniteAmmo { get => _infiniteAmmo; set { _infiniteAmmo = value; OnPropertyChanged(); } }
    public bool GrenadeTrajectory { get => _grenadeTrajectory; set { _grenadeTrajectory = value; OnPropertyChanged(); } }
    public bool BuyAnywhere { get => _buyAnywhere; set { _buyAnywhere = value; OnPropertyChanged(); } }
    public bool ShowImpacts { get => _showImpacts; set { _showImpacts = value; OnPropertyChanged(); } }
    public bool GodMode { get => _godMode; set { _godMode = value; OnPropertyChanged(); } }
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;

    public OverlayWindow(AppState state, BotManagerViewModel botManager, OverlayStyle style = OverlayStyle.Normal)
    {
        InitializeComponent();
        _state = state;
        _botManager = botManager;
        _style = style;
        DataContext = this;
        
        // Force simple reliable window properties
        WindowStyle = WindowStyle.SingleBorderWindow;
        AllowsTransparency = false;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 46));
        ShowActivated = true;
        ShowInTaskbar = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        LoadFromConfigs();

        Topmost = true;

        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
        Closed += OnClosed;
    }

    private void ApplyOverlayStyle()
    {
        // Simplified - always use normal window for reliability
        WindowStyle = WindowStyle.SingleBorderWindow;
        AllowsTransparency = false;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 46));
        ShowInTaskbar = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTopmostChrome();
        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
        
        // Position overlay to right side of screen
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Left = screenWidth - Width - 20;
        Top = (screenHeight - Height) / 2;
        
        // Force window to be topmost
        Topmost = true;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            ApplyTopmostChrome();
            CompositionTarget.Rendering -= OnRendering;
            CompositionTarget.Rendering += OnRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e) => ApplyTopmostChrome();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyTopmostChrome();
    }

    private void ApplyTopmostChrome()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            hwnd = new WindowInteropHelper(this).EnsureHandle();
        
        var useAggressive = _style == OverlayStyle.Transparent;
        GameWindowService.ForceTopmost(hwnd, useAggressive);
        Topmost = true;
    }

    private void LoadFromConfigs()
    {
        var cfg = _state.CfgDirectory;
        if (string.IsNullOrWhiteSpace(cfg)) return;

        _botManager.Load();

        SvCheats = IsOn(_state.Services.Config.GetCurrentValue(cfg, "sv_cheats"));
        InfiniteAmmo = (_state.Services.Config.GetCurrentValue(cfg, "sv_infinite_ammo") ?? "0") is not "0";
        GrenadeTrajectory = IsOn(_state.Services.Config.GetCurrentValue(cfg, "sv_grenade_trajectory"));
        BuyAnywhere = IsOn(_state.Services.Config.GetCurrentValue(cfg, "mp_buy_anywhere"));
        ShowImpacts = (_state.Services.Config.GetCurrentValue(cfg, "sv_showimpacts") ?? "0") is not "0";
        GodMode = IsOn(_state.Services.Config.GetCurrentValue(cfg, "god"));
    }

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove throws if the mouse button is released mid-call.
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Hide();

    private void OnBotSettingChanged(object sender, RoutedEventArgs e)
    {
        // Sync with main bot manager and apply to all game mode configs
        _botManager.Save();
        ApplyBotSettingsToAllGameModes();
    }

    private void OnPracticeSettingChanged(object sender, RoutedEventArgs e)
    {
        // Sync with main practice settings and apply to all game mode configs
        ApplyPracticeSettingsToAllGameModes();
    }

    private void ApplyBotSettingsToAllGameModes()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var botValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bot_quota"] = _botManager.Quota.ToString(),
                ["bot_quota_mode"] = _botManager.QuotaMode,
                ["bot_difficulty"] = _botManager.Difficulty.ToString(),
                ["bot_join_team"] = _botManager.JoinTeam,
                ["bot_stop"] = _botManager.BotStop ? "1" : "0",
                ["bot_dont_shoot"] = _botManager.DontShoot ? "1" : "0"
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

            _state.Services.Config.ApplyValuesToMultipleFiles(_state.CfgDirectory!, botValues, gameModeFiles);
            Status = "Bot settings applied to all game mode configs.";
        }
        catch (Exception ex)
        {
            Status = $"Error applying bot settings: {ex.Message}";
        }
    }

    private void ApplyPracticeSettingsToAllGameModes()
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var practiceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sv_cheats"] = SvCheats ? "1" : "0",
                ["sv_infinite_ammo"] = InfiniteAmmo ? "1" : "0",
                ["sv_grenade_trajectory"] = GrenadeTrajectory ? "1" : "0",
                ["mp_buy_anywhere"] = BuyAnywhere ? "1" : "0",
                ["sv_showimpacts"] = ShowImpacts ? "1" : "0",
                ["god"] = GodMode ? "1" : "0"
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

            _state.Services.Config.ApplyValuesToMultipleFiles(_state.CfgDirectory!, practiceValues, gameModeFiles);
            Status = "Practice settings applied to all game mode configs.";
        }
        catch (Exception ex)
        {
            Status = $"Error applying practice settings: {ex.Message}";
        }
    }

    private async void OnSaveBots(object sender, RoutedEventArgs e)
    {
        _botManager.Save();
        ApplyBotSettingsToAllGameModes();
        await ApplyConfigAsync(BotCommands().ToArray(), "Bot config generated. Exec command copied to clipboard.");
    }

    private async void OnKickBots(object sender, RoutedEventArgs e)
    {
        _botManager.Quota = 0;
        _botManager.Save();
        ApplyBotSettingsToAllGameModes();
        await ApplyConfigAsync(new[]
        {
            "bot_quota 0",
            "bot_kick"
        }, "Bot kick config generated. Exec command copied to clipboard.");
    }

    private async void OnExecuteCommand(object sender, RoutedEventArgs e)
    {
        var commandBox = FindName("ConsoleCommandBox") as System.Windows.Controls.TextBox;
        if (commandBox != null && !string.IsNullOrWhiteSpace(commandBox.Text))
        {
            var command = commandBox.Text.Trim();
            await ApplyConfigAsync(new[] { command! }, $"Command config generated. Exec command copied to clipboard.");
            commandBox.Text = string.Empty;
        }
    }

    private async void OnSavePractice(object sender, RoutedEventArgs e)
    {
        Apply(new Dictionary<string, string>
        {
            ["sv_cheats"] = SvCheats ? "1" : "0",
            ["sv_infinite_ammo"] = InfiniteAmmo ? "1" : "0",
            ["sv_grenade_trajectory"] = GrenadeTrajectory ? "1" : "0",
            ["mp_buy_anywhere"] = BuyAnywhere ? "1" : "0",
            ["sv_showimpacts"] = ShowImpacts ? "1" : "0",
            ["god"] = GodMode ? "1" : "0"
        }, "Practice settings written to cfg.");

        ApplyPracticeSettingsToAllGameModes();
        await ApplyConfigAsync(PracticeCommands().ToArray(), "Practice config generated. Exec command copied to clipboard.");
    }

    private async void OnQuickCommand(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && !string.IsNullOrWhiteSpace(button.Tag?.ToString()))
        {
            var command = button.Tag.ToString()!;
            await ApplyConfigAsync(new[] { command }, $"Command '{command}' config generated. Exec command copied to clipboard.");
        }
    }

    private async void OnSetRoundValue(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && !string.IsNullOrWhiteSpace(button.Tag?.ToString()))
        {
            var commandName = button.Tag.ToString();
            string? value = null;

            switch (commandName)
            {
                case "mp_roundtime_defuse":
                    var roundTimeBox = FindName("RoundTimeBox") as System.Windows.Controls.TextBox;
                    value = roundTimeBox?.Text;
                    break;
                case "mp_freezetime":
                    var freezeTimeBox = FindName("FreezeTimeBox") as System.Windows.Controls.TextBox;
                    value = freezeTimeBox?.Text;
                    break;
                case "mp_maxrounds":
                    var maxRoundsBox = FindName("MaxRoundsBox") as System.Windows.Controls.TextBox;
                    value = maxRoundsBox?.Text;
                    break;
                case "mp_warmuptime":
                    var warmupTimeBox = FindName("WarmupTimeBox") as System.Windows.Controls.TextBox;
                    value = warmupTimeBox?.Text;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                var command = $"{commandName} {value}";
                await ApplyConfigAsync(new[] { command! }, $"Setting '{commandName}' config generated. Exec command copied to clipboard.");
                
                // Also apply to all game mode configs
                ApplyRoundSettingToAllGameModes(commandName!, value!);
            }
        }
    }

    private void ApplyRoundSettingToAllGameModes(string commandName, string value)
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory)) return;

        try
        {
            var roundValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [commandName] = value
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

            _state.Services.Config.ApplyValuesToMultipleFiles(_state.CfgDirectory!, roundValues, gameModeFiles);
            Status = $"Round setting '{commandName}' applied to all game mode configs.";
        }
        catch (Exception ex)
        {
            Status = $"Error applying round setting: {ex.Message}";
        }
    }

    private void OnApplyAllSettings(object sender, RoutedEventArgs e)
    {
        // Apply all current settings to all game mode configs
        ApplyBotSettingsToAllGameModes();
        ApplyPracticeSettingsToAllGameModes();
        
        // Apply round settings from UI text boxes
        var roundTimeBox = FindName("RoundTimeBox") as System.Windows.Controls.TextBox;
        var freezeTimeBox = FindName("FreezeTimeBox") as System.Windows.Controls.TextBox;
        var maxRoundsBox = FindName("MaxRoundsBox") as System.Windows.Controls.TextBox;
        var warmupTimeBox = FindName("WarmupTimeBox") as System.Windows.Controls.TextBox;

        if (roundTimeBox != null && !string.IsNullOrWhiteSpace(roundTimeBox.Text))
            ApplyRoundSettingToAllGameModes("mp_roundtime_defuse", roundTimeBox.Text);
        if (freezeTimeBox != null && !string.IsNullOrWhiteSpace(freezeTimeBox.Text))
            ApplyRoundSettingToAllGameModes("mp_freezetime", freezeTimeBox.Text);
        if (maxRoundsBox != null && !string.IsNullOrWhiteSpace(maxRoundsBox.Text))
            ApplyRoundSettingToAllGameModes("mp_maxrounds", maxRoundsBox.Text);
        if (warmupTimeBox != null && !string.IsNullOrWhiteSpace(warmupTimeBox.Text))
            ApplyRoundSettingToAllGameModes("mp_warmuptime", warmupTimeBox.Text);

        Status = "All settings applied to all game mode configs.";
    }

    private async void OnCopyExecCommand(object sender, RoutedEventArgs e)
    {
        // Generate config with all current settings and copy exec command
        // This works without requiring any UI changes
        var allCommands = new List<string>();
        
        // Add bot commands
        allCommands.AddRange(BotCommands());
        
        // Add practice commands
        allCommands.AddRange(PracticeCommands());
        
        // Add round settings from UI text boxes
        var roundTimeBox = FindName("RoundTimeBox") as System.Windows.Controls.TextBox;
        var freezeTimeBox = FindName("FreezeTimeBox") as System.Windows.Controls.TextBox;
        var maxRoundsBox = FindName("MaxRoundsBox") as System.Windows.Controls.TextBox;
        var warmupTimeBox = FindName("WarmupTimeBox") as System.Windows.Controls.TextBox;

        if (roundTimeBox != null && !string.IsNullOrWhiteSpace(roundTimeBox.Text))
            allCommands.Add($"mp_roundtime_defuse {roundTimeBox.Text}");
        if (freezeTimeBox != null && !string.IsNullOrWhiteSpace(freezeTimeBox.Text))
            allCommands.Add($"mp_freezetime {freezeTimeBox.Text}");
        if (maxRoundsBox != null && !string.IsNullOrWhiteSpace(maxRoundsBox.Text))
            allCommands.Add($"mp_maxrounds {maxRoundsBox.Text}");
        if (warmupTimeBox != null && !string.IsNullOrWhiteSpace(warmupTimeBox.Text))
            allCommands.Add($"mp_warmuptime {warmupTimeBox.Text}");

        // Apply to all game modes
        ApplyBotSettingsToAllGameModes();
        ApplyPracticeSettingsToAllGameModes();
        
        if (roundTimeBox != null && !string.IsNullOrWhiteSpace(roundTimeBox.Text))
            ApplyRoundSettingToAllGameModes("mp_roundtime_defuse", roundTimeBox.Text);
        if (freezeTimeBox != null && !string.IsNullOrWhiteSpace(freezeTimeBox.Text))
            ApplyRoundSettingToAllGameModes("mp_freezetime", freezeTimeBox.Text);
        if (maxRoundsBox != null && !string.IsNullOrWhiteSpace(maxRoundsBox.Text))
            ApplyRoundSettingToAllGameModes("mp_maxrounds", maxRoundsBox.Text);
        if (warmupTimeBox != null && !string.IsNullOrWhiteSpace(warmupTimeBox.Text))
            ApplyRoundSettingToAllGameModes("mp_warmuptime", warmupTimeBox.Text);

        // Generate config and copy exec command
        await ApplyConfigAsync(allCommands, "Config generated. Exec command copied to clipboard.");
    }

    private IEnumerable<string> BotCommands()
    {
        yield return $"bot_quota {_botManager.Quota}";
        yield return $"bot_quota_mode {_botManager.QuotaMode}";
        yield return $"bot_difficulty {_botManager.Difficulty}";
        yield return $"bot_join_team {_botManager.JoinTeam}";
        yield return $"bot_stop {(_botManager.BotStop ? "1" : "0")}";
        yield return $"bot_dont_shoot {(_botManager.DontShoot ? "1" : "0")}";
    }

    private IEnumerable<string> PracticeCommands()
    {
        yield return $"sv_cheats {(SvCheats ? "1" : "0")}";
        yield return $"sv_infinite_ammo {(InfiniteAmmo ? "1" : "0")}";
        yield return $"sv_grenade_trajectory {(GrenadeTrajectory ? "1" : "0")}";
        yield return $"mp_buy_anywhere {(BuyAnywhere ? "1" : "0")}";
        yield return $"sv_showimpacts {(ShowImpacts ? "1" : "0")}";
        yield return $"god {(GodMode ? "1" : "0")}";
    }

    private async Task ApplyConfigAsync(IEnumerable<string> commands, string okMessage)
    {
        if (_applying) return;
        var cfg = _state.CfgDirectory;
        if (string.IsNullOrWhiteSpace(cfg))
        {
            Status = "CS:GO cfg path not configured.";
            return;
        }

        _applying = true;
        try
        {
            var list = commands.ToList();
            var fileName = _state.Services.Settings.Current.ConfigFileName ?? "rfl_config.cfg";
            var result = _state.ConfigGeneration.GenerateAndCopy(cfg, fileName, list);

            if (result.Success)
            {
                Status = okMessage;
                _state.SetStatus(Status);
            }
            else
            {
                Status = $"Failed to generate config: {result.ErrorMessage}";
                _state.SetStatus(Status);
            }
        }
        catch (Exception ex)
        {
            Status = $"Failed to generate config: {ex.Message}";
            _state.SetStatus(Status);
        }
        finally
        {
            _applying = false;
        }
    }

    private void Apply(Dictionary<string, string> values, string okMessage)
    {
        if (string.IsNullOrWhiteSpace(_state.CfgDirectory))
        {
            Status = "CS:GO cfg path not configured.";
            return;
        }

        try
        {
            _state.Services.Config.ApplyValues(_state.CfgDirectory!, values);
            Status = okMessage;
            _state.SetStatus(okMessage);
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private static bool IsOn(string? value) => value is "1" or "true" or "True" or "yes" or "on";

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
