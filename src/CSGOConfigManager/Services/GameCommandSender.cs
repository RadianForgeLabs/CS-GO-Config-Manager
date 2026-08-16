using System.Runtime.InteropServices;
using System.Text;

namespace CSGOConfigManager.Services;

/// <summary>
/// Pushes overlay changes into a running CS:GO session by writing
/// <c>overlay_live.cfg</c> and sending <c>exec overlay_live</c> to the game console.
/// </summary>
public sealed class GameCommandSender
{
    public const string LiveCfgName = "overlay_live.cfg";

    private readonly GameWindowService _windows;

    public GameCommandSender(GameWindowService windows)
    {
        _windows = windows;
    }

    public void WriteLiveCfg(string cfgDirectory, IEnumerable<string> commands)
    {
        Directory.CreateDirectory(cfgDirectory);
        var path = Path.Combine(cfgDirectory, LiveCfgName);
        var sb = new StringBuilder();
        sb.AppendLine("con_enable 1");
        sb.AppendLine("bind F8 \"exec overlay_live\"");
        foreach (var line in commands)
        {
            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendLine(line.Trim());
        }
        sb.AppendLine("echo [CSGOConfigManager] overlay applied");
        File.WriteAllText(path, sb.ToString());
    }

    public void EnsureAutoexecHook(string cfgDirectory)
    {
        var autoexec = Path.Combine(cfgDirectory, "autoexec.cfg");
        const string hook = "bind F8 \"exec overlay_live\"";
        const string enable = "con_enable 1";

        var existing = File.Exists(autoexec) ? File.ReadAllText(autoexec) : string.Empty;
        var needsWrite = false;
        var next = existing;

        if (!next.Contains("exec overlay_live", StringComparison.OrdinalIgnoreCase))
        {
            if (next.Length > 0 && !next.EndsWith('\n'))
                next += Environment.NewLine;
            next += Environment.NewLine + "// CS:GO Config Manager overlay" + Environment.NewLine + hook + Environment.NewLine;
            needsWrite = true;
        }

        if (!next.Contains("con_enable", StringComparison.OrdinalIgnoreCase))
        {
            next = enable + Environment.NewLine + next;
            needsWrite = true;
        }

        if (needsWrite)
            File.WriteAllText(autoexec, next);
    }

    public async Task<bool> ExecLiveAsync(string cfgDirectory, IEnumerable<string> commands)
    {
        EnsureAutoexecHook(cfgDirectory);
        WriteLiveCfg(cfgDirectory, commands);

        if (!_windows.TryFind(out var game) || game.IsMinimized)
            return false;

        GameWindowService.FocusWindow(game.Handle);
        await Task.Delay(60);

        Tap(NativeMethods.VK_OEM_3);
        await Task.Delay(70);
        TypeText("exec overlay_live");
        Tap(NativeMethods.VK_RETURN);
        await Task.Delay(70);
        Tap(NativeMethods.VK_OEM_3);
        return true;
    }

    private static void Tap(byte vk)
    {
        SendVk(vk, keyUp: false);
        SendVk(vk, keyUp: true);
    }

    private static void TypeText(string text)
    {
        foreach (var ch in text)
        {
            SendUnicode(ch, keyUp: false);
            SendUnicode(ch, keyUp: true);
        }
    }

    private static void SendVk(byte vk, bool keyUp)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendUnicode(char ch, bool keyUp)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE | (keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
