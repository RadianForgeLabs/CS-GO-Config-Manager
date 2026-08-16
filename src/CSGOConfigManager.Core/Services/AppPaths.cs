namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Resolves portable application folder layout next to the executable.
/// </summary>
public sealed class AppPaths
{
    public string Root { get; }
    public string Data { get; }
    public string Config { get; }
    public string Profiles { get; }
    public string Backups { get; }
    public string Logs { get; }
    public string Themes { get; }
    public string SettingsFile { get; }
    public string CommandsFile { get; }
    public string GameModesFile { get; }
    public string LaunchersFile { get; }
    public string Presets { get; }

    public AppPaths(string? rootDirectory = null)
    {
        Root = rootDirectory ?? AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Data = Path.Combine(Root, "Data");
        Config = Path.Combine(Root, "Config");
        Profiles = Path.Combine(Config, "Profiles");
        Backups = Path.Combine(Root, "Backups");
        Logs = Path.Combine(Root, "Logs");
        Themes = Path.Combine(Root, "Themes");
        SettingsFile = Path.Combine(Config, "Settings.json");
        CommandsFile = Path.Combine(Data, "Commands.json");
        GameModesFile = Path.Combine(Data, "GameModes.json");
        LaunchersFile = Path.Combine(Data, "Launchers.json");
        Presets = Path.Combine(Data, "Presets");
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(Config);
        Directory.CreateDirectory(Profiles);
        Directory.CreateDirectory(Backups);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Themes);
        Directory.CreateDirectory(Presets);
    }
}
