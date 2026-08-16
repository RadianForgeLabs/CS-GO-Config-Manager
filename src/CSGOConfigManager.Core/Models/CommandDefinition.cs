using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSGOConfigManager.Core.Models;

/// <summary>
/// Metadata for a single CS:GO console command or ConVar, loaded from Commands.json.
/// </summary>
public sealed class CommandDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("default")]
    public JsonElement? Default { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    [JsonPropertyName("enum")]
    public List<string>? EnumValues { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("requires_restart")]
    public bool RequiresRestart { get; set; }

    [JsonPropertyName("requires_sv_cheats")]
    public bool RequiresSvCheats { get; set; }

    [JsonPropertyName("mode")]
    public List<string> Modes { get; set; } = new();

    [JsonPropertyName("file")]
    public string File { get; set; } = "autoexec.cfg";

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }

    public string DefaultAsString()
    {
        if (Default is null || Default.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return string.Empty;

        return Default.Value.ValueKind switch
        {
            JsonValueKind.String => Default.Value.GetString() ?? string.Empty,
            JsonValueKind.Number => Default.Value.ToString(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            _ => Default.Value.ToString()
        };
    }

    public bool AppliesToMode(string mode) =>
        Modes.Count == 0 ||
        Modes.Any(m => string.Equals(m, mode, StringComparison.OrdinalIgnoreCase));
}
