using System.Globalization;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Validates command values against JSON metadata before writing to configs.
/// </summary>
public static class CommandValidator
{
    public static ValidationResult Validate(CommandDefinition command, string? rawValue)
    {
        var value = (rawValue ?? string.Empty).Trim();
        var type = command.Type.Trim().ToLowerInvariant();

        return type switch
        {
            "boolean" or "bool" => ValidateBoolean(value),
            "integer" or "int" => ValidateInteger(command, value),
            "float" or "double" or "number" => ValidateFloat(command, value),
            "enum" or "dropdown" => ValidateEnum(command, value),
            "string" or "text" or "keybind" or "color" => ValidationResult.Success(value),
            "action" => ValidationResult.Success(string.Empty),
            _ => ValidationResult.Success(value)
        };
    }

    private static ValidationResult ValidateBoolean(string value)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Fail("Boolean value is required (0/1 or true/false).");

        if (value is "0" or "1")
            return ValidationResult.Success(value);

        if (bool.TryParse(value, out var b))
            return ValidationResult.Success(b ? "1" : "0");

        if (string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Success("1");

        if (string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Success("0");

        return ValidationResult.Fail($"Invalid boolean value '{value}'. Use 0, 1, true, or false.");
    }

    private static ValidationResult ValidateInteger(CommandDefinition command, string value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return ValidationResult.Fail($"'{value}' is not a valid integer.");

        if (command.Min is not null && number < command.Min)
            return ValidationResult.Fail($"Value must be >= {command.Min}.");

        if (command.Max is not null && number > command.Max)
            return ValidationResult.Fail($"Value must be <= {command.Max}.");

        return ValidationResult.Success(number.ToString(CultureInfo.InvariantCulture));
    }

    private static ValidationResult ValidateFloat(CommandDefinition command, string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return ValidationResult.Fail($"'{value}' is not a valid number.");

        if (command.Min is not null && number < command.Min)
            return ValidationResult.Fail($"Value must be >= {command.Min}.");

        if (command.Max is not null && number > command.Max)
            return ValidationResult.Fail($"Value must be <= {command.Max}.");

        return ValidationResult.Success(number.ToString(CultureInfo.InvariantCulture));
    }

    private static ValidationResult ValidateEnum(CommandDefinition command, string value)
    {
        if (command.EnumValues is null || command.EnumValues.Count == 0)
            return ValidationResult.Success(value);

        var match = command.EnumValues.FirstOrDefault(e =>
            string.Equals(e, value, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return ValidationResult.Fail(
                $"'{value}' is not allowed. Valid values: {string.Join(", ", command.EnumValues)}.");

        return ValidationResult.Success(match);
    }
}
