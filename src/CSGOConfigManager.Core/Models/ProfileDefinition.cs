using System.Text.Json.Serialization;

namespace CSGOConfigManager.Core.Models;

/// <summary>
/// A named set of command values that can be applied to config files.
/// </summary>
public sealed class ProfileDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Target config file (optional). When empty, each command uses its default file from metadata.
    /// </summary>
    [JsonPropertyName("targetFile")]
    public string? TargetFile { get; set; }

    [JsonPropertyName("values")]
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public string? FilePath { get; set; }
}
