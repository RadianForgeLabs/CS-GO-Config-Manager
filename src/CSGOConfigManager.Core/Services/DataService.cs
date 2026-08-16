using System.Text.Json;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Loads data-driven JSON metadata (commands, game modes, launchers, presets).
/// </summary>
public sealed class DataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private readonly AppPaths _paths;
    private List<CommandDefinition>? _commands;
    private Dictionary<string, string>? _gameModes;
    private Dictionary<string, LauncherDefinition>? _launchers;

    public DataService(AppPaths paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<CommandDefinition> GetCommands(bool forceReload = false)
    {
        if (_commands is not null && !forceReload)
            return _commands;

        if (!File.Exists(_paths.CommandsFile))
        {
            _commands = new List<CommandDefinition>();
            return _commands;
        }

        var json = File.ReadAllText(_paths.CommandsFile);
        _commands = JsonSerializer.Deserialize<List<CommandDefinition>>(json, JsonOptions) ?? new List<CommandDefinition>();
        return _commands;
    }

    public IReadOnlyDictionary<string, string> GetGameModes(bool forceReload = false)
    {
        if (_gameModes is not null && !forceReload)
            return _gameModes;

        if (!File.Exists(_paths.GameModesFile))
        {
            _gameModes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return _gameModes;
        }

        var json = File.ReadAllText(_paths.GameModesFile);
        _gameModes = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                     ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return _gameModes;
    }

    public IReadOnlyDictionary<string, LauncherDefinition> GetLaunchers(bool forceReload = false)
    {
        if (_launchers is not null && !forceReload)
            return _launchers;

        if (!File.Exists(_paths.LaunchersFile))
        {
            _launchers = new Dictionary<string, LauncherDefinition>(StringComparer.OrdinalIgnoreCase);
            return _launchers;
        }

        var json = File.ReadAllText(_paths.LaunchersFile);
        _launchers = JsonSerializer.Deserialize<Dictionary<string, LauncherDefinition>>(json, JsonOptions)
                     ?? new Dictionary<string, LauncherDefinition>(StringComparer.OrdinalIgnoreCase);
        return _launchers;
    }

    public IReadOnlyList<CommandDefinition> GetCommandsForMode(string mode) =>
        GetCommands().Where(c => !c.Hidden && c.AppliesToMode(mode)).ToList();

    public IReadOnlyList<CommandDefinition> GetCommandsByCategory(string category) =>
        GetCommands()
            .Where(c => !c.Hidden && string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public IReadOnlyList<string> GetCategories() =>
        GetCommands()
            .Where(c => !c.Hidden)
            .Select(c => c.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

    public CommandDefinition? FindCommand(string name) =>
        GetCommands().FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyDictionary<string, string> LoadPreset(string presetName)
    {
        var path = Path.Combine(_paths.Presets, $"{presetName}.json");
        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.Value.ToString(),
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                _ => prop.Value.ToString()
            };
        }

        return result;
    }

    public IReadOnlyList<string> GetPresetNames()
    {
        if (!Directory.Exists(_paths.Presets))
            return Array.Empty<string>();

        return Directory.GetFiles(_paths.Presets, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .OrderBy(n => n)
            .ToList();
    }

    public static JsonSerializerOptions SharedJsonOptions => JsonOptions;
}
