using System.Runtime.InteropServices;

namespace CopyDecrypt;

internal static class NativeMethods
{
    internal const int WmHotkey = 0x0312;

    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    /// <summary>Windows 7+ : pas de répétition auto si la touche reste enfoncée.</summary>
    internal const uint ModNorepeat = 0x4000;

    internal const int HotkeyIdRegion = 0x4C44; // 'LD'
    internal const int HotkeyIdClipboard = 0x4C45; // 'LE'

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}
