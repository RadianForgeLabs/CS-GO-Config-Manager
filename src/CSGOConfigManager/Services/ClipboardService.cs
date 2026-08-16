namespace CSGOConfigManager.Services;

/// <summary>
/// Provides clipboard operations for the WPF application.
/// </summary>
public sealed class ClipboardService
{
    /// <summary>
    /// Sets the specified text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null or empty.</exception>
    /// <exception cref="System.Runtime.InteropServices.ExternalException">Thrown when clipboard operation fails.</exception>
    public void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));

        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            throw new System.Runtime.InteropServices.ExternalException(
                "Failed to copy text to clipboard. The clipboard may be in use by another application.", ex);
        }
    }

    /// <summary>
    /// Attempts to set the specified text to the system clipboard without throwing exceptions.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard.</param>
    /// <returns>True if the operation succeeded, false otherwise.</returns>
    public bool TrySetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            System.Windows.Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
