namespace CSGOConfigManager.Core.Models;

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string? NormalizedValue { get; init; }

    public static ValidationResult Success(string normalizedValue) =>
        new() { IsValid = true, NormalizedValue = normalizedValue };

    public static ValidationResult Fail(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
