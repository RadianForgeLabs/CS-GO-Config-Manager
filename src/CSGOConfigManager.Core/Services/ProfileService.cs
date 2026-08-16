using System.Text.Json;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

public sealed class ProfileService
{
    private readonly AppPaths _paths;
    private readonly ConfigService _configService;
    private readonly DataService _dataService;

    public ProfileService(AppPaths paths, ConfigService configService, DataService dataService)
    {
        _paths = paths;
        _configService = configService;
        _dataService = dataService;
    }

    public IReadOnlyList<ProfileDefinition> ListProfiles()
    {
        _paths.EnsureDirectories();
        var profiles = new List<ProfileDefinition>();

        // User profiles
        foreach (var file in Directory.GetFiles(_paths.Profiles, "*.json"))
        {
            var profile = LoadProfileFile(file);
            if (profile is not null)
                profiles.Add(profile);
        }

        // Built-in presets (read-only style, still applyable)
        foreach (var presetName in _dataService.GetPresetNames())
        {
            if (profiles.Any(p => string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var values = _dataService.LoadPreset(presetName);
            profiles.Add(new ProfileDefinition
            {
                Name = presetName,
                Description = $"Built-in preset: {presetName}",
                Values = values.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                FilePath = Path.Combine(_paths.Presets, $"{presetName}.json")
            });
        }

        return profiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public ProfileDefinition? GetProfile(string name) =>
        ListProfiles().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public ProfileDefinition SaveProfile(ProfileDefinition profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new ArgumentException("Profile name is required.", nameof(profile));

        _paths.EnsureDirectories();
        var safeName = string.Join("_", profile.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        var path = Path.Combine(_paths.Profiles, $"{safeName}.json");
        profile.FilePath = path;

        var json = JsonSerializer.Serialize(profile, DataService.SharedJsonOptions);
        File.WriteAllText(path, json);
        return profile;
    }

    public void DeleteProfile(string name)
    {
        var path = Path.Combine(_paths.Profiles, $"{name}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    public IReadOnlyList<string> ApplyProfile(ProfileDefinition profile, string cfgDirectory)
    {
        return _configService.ApplyValues(cfgDirectory, profile.Values, profile.TargetFile);
    }

    public ProfileDefinition CreateFromCurrent(string name, string description, string cfgDirectory, IEnumerable<string>? commandNames = null)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var commands = commandNames?.ToList()
                       ?? _dataService.GetCommands().Select(c => c.Name).ToList();

        foreach (var commandName in commands)
        {
            var command = _dataService.FindCommand(commandName);
            var value = _configService.GetCurrentValue(cfgDirectory, commandName, command?.File);
            if (value is not null)
                values[commandName] = value;
        }

        var profile = new ProfileDefinition
        {
            Name = name,
            Description = description,
            Values = values
        };

        return SaveProfile(profile);
    }

    public ProfileDefinition Import(string filePath)
    {
        var profile = LoadProfileFile(filePath)
                      ?? throw new InvalidOperationException($"Could not import profile from '{filePath}'.");

        return SaveProfile(profile);
    }

    public void Export(ProfileDefinition profile, string destinationPath)
    {
        var json = JsonSerializer.Serialize(profile, DataService.SharedJsonOptions);
        File.WriteAllText(destinationPath, json);
    }

    private static ProfileDefinition? LoadProfileFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);

            // Full profile schema
            if (doc.RootElement.TryGetProperty("values", out _) || doc.RootElement.TryGetProperty("Values", out _))
            {
                var profile = JsonSerializer.Deserialize<ProfileDefinition>(json, DataService.SharedJsonOptions);
                if (profile is not null)
                {
                    profile.FilePath = filePath;
                    if (string.IsNullOrWhiteSpace(profile.Name))
                        profile.Name = Path.GetFileNameWithoutExtension(filePath);
                    return profile;
                }
            }

            // Flat preset map: { "bot_quota": 10, ... }
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name is "name" or "description" or "targetFile")
                    continue;

                values[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.ToString(),
                    JsonValueKind.True => "1",
                    JsonValueKind.False => "0",
                    _ => prop.Value.ToString()
                };
            }

            return new ProfileDefinition
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Description = string.Empty,
                Values = values,
                FilePath = filePath
            };
        }
        catch
        {
            return null;
        }
    }
}
