namespace CSGOConfigManager.Core.Models;

/// <summary>
/// User preferences persisted to Config/Settings.json.
/// </summary>
public sealed class AppSettings
{
    public string? SteamPath { get; set; }
    public string? CsgoPath { get; set; }
    public string? SevenLauncherPath { get; set; }
    public string? RevLoaderPath { get; set; }
    public string? CustomExePath { get; set; }
    public string DefaultLaunchMethod { get; set; } = "exe";
    public string? CustomLaunchArgs { get; set; }
    public bool AutoBackupOnChange { get; set; } = true;
    public bool LaunchOffline { get; set; }
    public string Theme { get; set; } = "Dark";
    public string? ActiveProfile { get; set; }
    public int MaxBackupCount { get; set; } = 50;
    public bool MinimizeToTrayOnLaunch { get; set; }
    public string ConfigFileName { get; set; } = "rfl_config.cfg";
}
