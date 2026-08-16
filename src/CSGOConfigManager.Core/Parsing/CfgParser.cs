using System.Text;
using System.Text.RegularExpressions;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Parsing;

/// <summary>
/// Parses and serializes CS:GO .cfg files while preserving comments and blank lines.
/// </summary>
public static partial class CfgParser
{
    // Matches: command value, command "quoted value", command alone
    [GeneratedRegex(@"^\s*(?<cmd>[a-zA-Z_][\w.]*)\s*(?<val>.*?)?\s*(?://.*)?$", RegexOptions.Compiled)]
    private static partial Regex CommandLineRegex();

    public static ConfigDocument Parse(string filePath, string content)
    {
        var document = new ConfigDocument { FilePath = filePath };
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNumber = i + 1;
            var trimmed = raw.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                document.Entries.Add(new ConfigEntry
                {
                    Kind = ConfigLineKind.Blank,
                    RawLine = raw,
                    LineNumber = lineNumber
                });
                continue;
            }

            if (trimmed.StartsWith("//") || trimmed.StartsWith('#'))
            {
                document.Entries.Add(new ConfigEntry
                {
                    Kind = ConfigLineKind.Comment,
                    RawLine = raw,
                    LineNumber = lineNumber
                });
                continue;
            }

            var match = CommandLineRegex().Match(trimmed);
            if (match.Success)
            {
                var command = match.Groups["cmd"].Value;
                var value = match.Groups["val"].Success
                    ? Unquote(match.Groups["val"].Value.Trim())
                    : string.Empty;

                // Strip trailing inline comments that weren't handled by the regex val group fully
                value = StripInlineComment(value);

                document.Entries.Add(new ConfigEntry
                {
                    Kind = ConfigLineKind.Command,
                    Command = command,
                    Value = value,
                    RawLine = raw,
                    LineNumber = lineNumber
                });
            }
            else
            {
                document.Entries.Add(new ConfigEntry
                {
                    Kind = ConfigLineKind.Other,
                    RawLine = raw,
                    LineNumber = lineNumber
                });
            }
        }

        return document;
    }

    public static ConfigDocument ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Config file not found.", filePath);

        var content = File.ReadAllText(filePath, Encoding.UTF8);
        return Parse(filePath, content);
    }

    public static string Serialize(ConfigDocument document)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < document.Entries.Count; i++)
        {
            var entry = document.Entries[i];
            if (entry.Kind == ConfigLineKind.Command && !string.IsNullOrWhiteSpace(entry.Command))
            {
                sb.Append(ConfigDocument.FormatCommandLine(entry.Command, entry.Value ?? string.Empty));
            }
            else
            {
                sb.Append(entry.RawLine);
            }

            if (i < document.Entries.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void WriteFile(ConfigDocument document)
    {
        var content = Serialize(document);
        var directory = Path.GetDirectoryName(document.FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(document.FilePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.IsDirty = false;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            return value[1..^1];
        return value;
    }

    private static string StripInlineComment(string value)
    {
        // Only strip // comments outside of quotes
        var inQuotes = false;
        for (var i = 0; i < value.Length - 1; i++)
        {
            if (value[i] == '"')
                inQuotes = !inQuotes;
            else if (!inQuotes && value[i] == '/' && value[i + 1] == '/')
                return value[..i].TrimEnd();
        }

        return value.TrimEnd();
    }
}
