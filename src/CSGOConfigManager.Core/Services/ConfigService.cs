using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Core.Parsing;

namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Loads, edits, and saves CS:GO .cfg files with optional auto-backup.
/// </summary>
public sealed class ConfigService
{
    private readonly BackupService _backupService;
    private readonly SettingsService _settingsService;
    private readonly DataService _dataService;

    public ConfigService(BackupService backupService, SettingsService settingsService, DataService dataService)
    {
        _backupService = backupService;
        _settingsService = settingsService;
        _dataService = dataService;
    }

    public ConfigDocument Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new ConfigDocument
            {
                FilePath = filePath,
                Entries =
                {
                    new ConfigEntry
                    {
                        Kind = ConfigLineKind.Comment,
                        RawLine = "// Created by CS:GO Config Manager",
                        LineNumber = 1
                    }
                }
            };
        }

        return CfgParser.ParseFile(filePath);
    }

    public void Save(ConfigDocument document, bool createBackup = true)
    {
        if (createBackup && _settingsService.Current.AutoBackupOnChange && File.Exists(document.FilePath))
            _backupService.CreateAutoBackup(new[] { document.FilePath });

        CfgParser.WriteFile(document);
    }

    public IReadOnlyList<string> ListConfigFiles(string? cfgDirectory)
    {
        if (string.IsNullOrWhiteSpace(cfgDirectory) || !Directory.Exists(cfgDirectory))
            return Array.Empty<string>();

        var known = new[]
        {
            "autoexec.cfg",
            "config.cfg",
            "practice.cfg",
            "gamemode_casual.cfg",
            "gamemode_competitive.cfg",
            "gamemode_deathmatch.cfg",
            "gamemode_armsrace.cfg",
            "gamemode_demolition.cfg",
            "gamemode_cooperative.cfg",
            "gamemode_custom.cfg"
        };

        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in known)
        {
            var path = Path.Combine(cfgDirectory, name);
            if (File.Exists(path))
                files.Add(path);
        }

        foreach (var path in Directory.GetFiles(cfgDirectory, "*.cfg", SearchOption.TopDirectoryOnly))
            files.Add(path);

        return files.OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase).ToList();
    }

    public string GetConfigPath(string cfgDirectory, string fileName) =>
        Path.Combine(cfgDirectory, fileName);

    /// <summary>
    /// Applies a set of command values, routing each command to its target .cfg file.
    /// </summary>
    public IReadOnlyList<string> ApplyValues(
        string cfgDirectory,
        IReadOnlyDictionary<string, string> values,
        string? forceTargetFile = null)
    {
        var touched = new List<string>();
        var documents = new Dictionary<string, ConfigDocument>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in values)
        {
            var name = pair.Key;
            var valueToWrite = pair.Value;
            var command = _dataService.FindCommand(name);
            var targetFile = forceTargetFile
                             ?? command?.File
                             ?? "autoexec.cfg";

            if (command is not null)
            {
                var validation = CommandValidator.Validate(command, valueToWrite);
                if (!validation.IsValid)
                    throw new InvalidOperationException($"Invalid value for '{name}': {validation.ErrorMessage}");

                valueToWrite = validation.NormalizedValue ?? valueToWrite;
            }

            var path = GetConfigPath(cfgDirectory, targetFile);
            if (!documents.TryGetValue(path, out var doc))
            {
                doc = Load(path);
                documents[path] = doc;
            }

            doc.SetValue(name, valueToWrite);
        }

        foreach (var doc in documents.Values)
        {
            Save(doc);
            touched.Add(doc.FilePath);
        }

        return touched;
    }

    /// <summary>
    /// Applies a set of command values to multiple target config files.
    /// </summary>
    public IReadOnlyList<string> ApplyValuesToMultipleFiles(
        string cfgDirectory,
        IReadOnlyDictionary<string, string> values,
        IEnumerable<string> targetFiles)
    {
        var touched = new List<string>();

        foreach (var targetFile in targetFiles)
        {
            var fileTouched = ApplyValues(cfgDirectory, values, targetFile);
            touched.AddRange(fileTouched);
        }

        return touched;
    }

    public string? GetCurrentValue(string cfgDirectory, string commandName, string? preferredFile = null)
    {
        // First, check the command's default file (where values are written)
        var command = _dataService.FindCommand(commandName);
        if (command is not null)
        {
            var commandPath = GetConfigPath(cfgDirectory, command.File);
            if (File.Exists(commandPath))
            {
                var value = Load(commandPath).GetValue(commandName);
                if (value is not null)
                    return value;
            }
        }

        // Then, check the preferred file if specified
        if (!string.IsNullOrWhiteSpace(preferredFile))
        {
            var preferredPath = GetConfigPath(cfgDirectory, preferredFile);
            if (File.Exists(preferredPath))
            {
                var doc = Load(preferredPath);
                var value = doc.GetValue(commandName);
                if (value is not null)
                    return value;
            }
        }

        // Search all configs (last write wins by known priority)
        foreach (var file in ListConfigFiles(cfgDirectory))
        {
            var value = Load(file).GetValue(commandName);
            if (value is not null)
                return value;
        }

        return command?.DefaultAsString();
    }
}
