namespace CSGOConfigManager.Core.Models;

/// <summary>
/// In-memory representation of a CS:GO .cfg file.
/// </summary>
public sealed class ConfigDocument
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public List<ConfigEntry> Entries { get; set; } = new();
    public bool IsDirty { get; set; }

    public IReadOnlyDictionary<string, string> GetCommandValues()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Entries.Where(e => e.Kind == ConfigLineKind.Command && !string.IsNullOrWhiteSpace(e.Command)))
        {
            map[entry.Command!] = entry.Value ?? string.Empty;
        }

        return map;
    }

    public string? GetValue(string commandName)
    {
        for (var i = Entries.Count - 1; i >= 0; i--)
        {
            var entry = Entries[i];
            if (entry.Kind == ConfigLineKind.Command &&
                string.Equals(entry.Command, commandName, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }

    public void SetValue(string commandName, string value)
    {
        for (var i = Entries.Count - 1; i >= 0; i--)
        {
            var entry = Entries[i];
            if (entry.Kind == ConfigLineKind.Command &&
                string.Equals(entry.Command, commandName, StringComparison.OrdinalIgnoreCase))
            {
                entry.Value = value;
                entry.RawLine = FormatCommandLine(commandName, value);
                IsDirty = true;
                return;
            }
        }

        Entries.Add(new ConfigEntry
        {
            Kind = ConfigLineKind.Command,
            Command = commandName,
            Value = value,
            RawLine = FormatCommandLine(commandName, value),
            LineNumber = Entries.Count + 1
        });
        IsDirty = true;
    }

    public void RemoveCommand(string commandName)
    {
        var removed = Entries.RemoveAll(e =>
            e.Kind == ConfigLineKind.Command &&
            string.Equals(e.Command, commandName, StringComparison.OrdinalIgnoreCase));

        if (removed > 0)
            IsDirty = true;
    }

    public static string FormatCommandLine(string command, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return command;

        // Quote values that contain spaces
        if (value.Contains(' ') && !(value.StartsWith('"') && value.EndsWith('"')))
            return $"{command} \"{value}\"";

        return $"{command} {value}";
    }
}
