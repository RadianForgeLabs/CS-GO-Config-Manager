using System.Diagnostics;
using System.Text;

namespace CSGOConfigManager.Services;

/// <summary>
/// Locates the CS:GO Legacy window and, when needed, converts exclusive
/// fullscreen into a borderless window so a WPF overlay can sit on top.
/// </summary>
public sealed class GameWindowService
{
    private static readonly string[] ProcessNames = { "csgo", "csgo_linux64" };
    private static readonly string[] WindowClasses = { "Valve001", "SDL_app" };
    private int _borderlessAppliedPid;

    public bool TryFind(out GameWindow game)
    {
        game = default;

        foreach (var name in ProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try
                {
                    if (TryFindWindowForProcess(process.Id, out game))
                        return true;
                }
                catch
                {
                    // process may have exited while enumerating
                }
            }
        }

        return false;
    }

    public bool EnsureBorderless(GameWindow game)
    {
        if (game.Handle == IntPtr.Zero || game.ProcessId == _borderlessAppliedPid)
            return game.ProcessId == _borderlessAppliedPid;

        if (NativeMethods.IsIconic(game.Handle))
            NativeMethods.ShowWindow(game.Handle, NativeMethods.SW_RESTORE);

        var style = NativeMethods.GetWindowLong(game.Handle, NativeMethods.GWL_STYLE);
        style &= ~(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME |
                   NativeMethods.WS_SYSMENU | NativeMethods.WS_MINIMIZEBOX |
                   NativeMethods.WS_MAXIMIZEBOX);
        style |= NativeMethods.WS_POPUP | NativeMethods.WS_VISIBLE;
        NativeMethods.SetWindowLong(game.Handle, NativeMethods.GWL_STYLE, style);

        var monitor = NativeMethods.MonitorFromWindow(game.Handle, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            var r = info.rcMonitor;
            NativeMethods.SetWindowPos(
                game.Handle,
                IntPtr.Zero,
                r.Left, r.Top, r.Width, r.Height,
                NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOZORDER);
        }
        else
        {
            NativeMethods.SetWindowPos(
                game.Handle,
                IntPtr.Zero,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOZORDER);
        }

        _borderlessAppliedPid = game.ProcessId;
        return true;
    }

    public static void ForceTopmost(IntPtr overlayHwnd, bool useAggressive = false)
    {
        if (overlayHwnd == IntPtr.Zero) return;

        // Use less aggressive topmost setting to avoid triggering anti-cheat
        // unless aggressive mode is requested for transparent overlays
        var ex = NativeMethods.GetWindowLong(overlayHwnd, NativeMethods.GWL_EXSTYLE);
        ex |= NativeMethods.WS_EX_TOPMOST;
        
        if (useAggressive)
        {
            ex |= NativeMethods.WS_EX_TOOLWINDOW; // Use tool window for transparent overlays
        }
        else
        {
            ex &= ~NativeMethods.WS_EX_TOOLWINDOW; // Remove tool window flag for normal windows
        }
        
        NativeMethods.SetWindowLong(overlayHwnd, NativeMethods.GWL_EXSTYLE, ex);

        NativeMethods.SetWindowPos(
            overlayHwnd,
            NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_SHOWWINDOW);
    }

    public static bool FocusWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;

        var foreground = NativeMethods.GetForegroundWindow();
        var foreThread = NativeMethods.GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var appThread = NativeMethods.GetCurrentThreadId();

        if (foreThread != appThread)
            NativeMethods.AttachThreadInput(foreThread, appThread, true);

        if (NativeMethods.IsIconic(hwnd))
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);

        var ok = NativeMethods.SetForegroundWindow(hwnd);

        if (foreThread != appThread)
            NativeMethods.AttachThreadInput(foreThread, appThread, false);

        return ok;
    }

    private static bool TryFindWindowForProcess(int processId, out GameWindow game)
    {
        IntPtr found = IntPtr.Zero;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid != (uint)processId)
                return true;

            var cls = new StringBuilder(64);
            NativeMethods.GetClassName(hWnd, cls, cls.Capacity);
            var className = cls.ToString();

            var title = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, title, title.Capacity);
            var text = title.ToString();

            var isSource = WindowClasses.Any(c => className.Equals(c, StringComparison.OrdinalIgnoreCase));
            var looksLikeCsgo = text.Contains("Counter-Strike", StringComparison.OrdinalIgnoreCase)
                                || text.Contains("CS:GO", StringComparison.OrdinalIgnoreCase)
                                || text.Equals("CSGO", StringComparison.OrdinalIgnoreCase);

            if (isSource || looksLikeCsgo || found == IntPtr.Zero)
            {
                found = hWnd;
                if (isSource)
                    return false;
            }

            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero)
        {
            game = default;
            return false;
        }

        NativeMethods.GetWindowRect(found, out var rect);
        game = new GameWindow
        {
            Handle = found,
            ProcessId = processId,
            Left = rect.Left,
            Top = rect.Top,
            Width = Math.Max(rect.Width, 1),
            Height = Math.Max(rect.Height, 1),
            IsMinimized = NativeMethods.IsIconic(found)
        };
        return true;
    }
}

public readonly struct GameWindow
{
    public IntPtr Handle { get; init; }
    public int ProcessId { get; init; }
    public int Left { get; init; }
    public int Top { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsMinimized { get; init; }

    public int Right => Left + Width;
    public int Bottom => Top + Height;
}
