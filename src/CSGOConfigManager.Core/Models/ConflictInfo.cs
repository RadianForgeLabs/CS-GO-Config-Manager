namespace CSGOConfigManager.Core.Models;

/// <summary>
/// Describes where a command is set across config layers and which value wins.
/// </summary>
public sealed class ConflictInfo
{
    public string CommandName { get; init; } = string.Empty;
    public List<ConfigSourceValue> Sources { get; init; } = new();
    public string? EffectiveValue { get; init; }
    public string? EffectiveSource { get; init; }
    public bool HasConflict => Sources.Count > 1;
}

public sealed class ConfigSourceValue
{
    public string SourceFile { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int Priority { get; init; }
    public bool IsEffective { get; init; }
}
