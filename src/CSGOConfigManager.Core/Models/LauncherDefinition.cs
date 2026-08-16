using System.Text.Json.Serialization;

namespace CSGOConfigManager.Core.Models;

public sealed class LauncherDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("exe")]
    public string Exe { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public string Args { get; set; } = string.Empty;
}
