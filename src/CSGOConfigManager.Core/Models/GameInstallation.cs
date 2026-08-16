namespace CSGOConfigManager.Core.Models;

/// <summary>
/// Detected CS:GO Legacy installation paths and status.
/// </summary>
public sealed class GameInstallation
{
    public bool SteamFound { get; init; }
    public string? SteamPath { get; init; }
    public bool SevenLauncherFound { get; init; }
    public string? SevenLauncherPath { get; init; }
    public bool CsgoFound { get; init; }
    public string? CsgoRootPath { get; init; }
    public string? CsgoCfgPath { get; init; }
    public string? CsgoExePath { get; init; }
    public string? GameVersion { get; init; }
    public string DetectionSource { get; init; } = "None";
    public List<string> Messages { get; init; } = new();

    public bool IsReady => CsgoFound && !string.IsNullOrWhiteSpace(CsgoCfgPath);
}
