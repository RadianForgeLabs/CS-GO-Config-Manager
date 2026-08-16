using System.Text.Json;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

public sealed class SettingsService
{
    private readonly AppPaths _paths;
    private AppSettings? _settings;

    public SettingsService(AppPaths paths)
    {
        _paths = paths;
    }

    public AppSettings Load()
    {
        if (_settings is not null)
            return _settings;

        if (!File.Exists(_paths.SettingsFile))
        {
            _settings = new AppSettings();
            return _settings;
        }

        try
        {
            var json = File.ReadAllText(_paths.SettingsFile);
            _settings = JsonSerializer.Deserialize<AppSettings>(json, DataService.SharedJsonOptions) ?? new AppSettings();
        }
        catch
        {
            _settings = new AppSettings();
        }

        return _settings;
    }

    public void Save(AppSettings settings)
    {
        _paths.EnsureDirectories();
        var json = JsonSerializer.Serialize(settings, DataService.SharedJsonOptions);
        File.WriteAllText(_paths.SettingsFile, json);
        _settings = settings;
    }

    public AppSettings Current => Load();
}
