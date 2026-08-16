namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Simple composition root for core services (no DI container required).
/// </summary>
public sealed class AppServices
{
    public AppPaths Paths { get; }
    public DataService Data { get; }
    public SettingsService Settings { get; }
    public GameDetectionService Detection { get; }
    public BackupService Backups { get; }
    public ConfigService Config { get; }
    public ConflictService Conflicts { get; }
    public ProfileService Profiles { get; }
    public LaunchService Launch { get; }
    public LogService Log { get; }
    public ConfigFileService ConfigFile { get; }

    public AppServices(string? rootDirectory = null)
    {
        Paths = new AppPaths(rootDirectory);
        Paths.EnsureDirectories();

        Data = new DataService(Paths);
        Settings = new SettingsService(Paths);
        Detection = new GameDetectionService();
        Backups = new BackupService(Paths, Settings);
        Config = new ConfigService(Backups, Settings, Data);
        Conflicts = new ConflictService(Config);
        Profiles = new ProfileService(Paths, Config, Data);
        Launch = new LaunchService(Data);
        Log = new LogService(Paths);
        ConfigFile = new ConfigFileService();
    }
}
