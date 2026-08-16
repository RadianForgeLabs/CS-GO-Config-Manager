using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace CSGOConfigManager.Services;

/// <summary>
/// Registers a global hotkey (e.g., F10) that works even when another application (like CS:GO) has focus.
/// Uses Windows API RegisterHotKey / UnregisterHotKey.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private readonly IntPtr _hwnd;
    private readonly int _hotkeyId;
    private readonly HwndSourceHook _hook;
    private bool _disposed;

    public event Action? HotkeyPressed;

    public GlobalHotkeyService(Window window, Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        // Window handle is often still 0 in the constructor. Force-create it
        // so RegisterHotKey works before the window is shown.
        _hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Window handle is not available.");

        _hotkeyId = GetHashCode();

        var vk = KeyInterop.VirtualKeyFromKey(key);
        var mod = (uint)modifiers;

        if (!RegisterHotKey(_hwnd, _hotkeyId, mod, (uint)vk))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to register global hotkey {key} with modifiers {modifiers}. Error code: {error}. It may already be in use.");
        }

        var source = HwndSource.FromHwnd(_hwnd)
                     ?? throw new InvalidOperationException("Failed to obtain HwndSource for hotkey hook.");
        _hook = HwndHook;
        source.AddHook(_hook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
        {
            System.Diagnostics.Debug.WriteLine("Global hotkey triggered");
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            var source = HwndSource.FromHwnd(_hwnd);
            source?.RemoveHook(_hook);
            UnregisterHotKey(_hwnd, _hotkeyId);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}