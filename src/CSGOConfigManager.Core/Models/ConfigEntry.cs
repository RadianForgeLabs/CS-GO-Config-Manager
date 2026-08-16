namespace CSGOConfigManager.Core.Models;

/// <summary>
/// A single parsed line from a .cfg file (command, comment, or blank).
/// </summary>
public sealed class ConfigEntry
{
    public ConfigLineKind Kind { get; init; }
    public string? Command { get; set; }
    public string? Value { get; set; }
    public string RawLine { get; set; } = string.Empty;
    public int LineNumber { get; init; }
}

public enum ConfigLineKind
{
    Blank,
    Comment,
    Command,
    Other
}
