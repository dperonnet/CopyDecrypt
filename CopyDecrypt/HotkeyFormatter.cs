namespace CopyDecrypt;

internal static class HotkeyFormatter
{
    internal static string FormatRegion(AppSettings s) => Format(s.HotkeyModifiers, s.HotkeyVirtualKey);

    internal static string FormatClipboard(AppSettings s) =>
        Format(s.ClipboardHotkeyModifiers, s.ClipboardHotkeyVirtualKey);

    internal static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & NativeMethods.ModAlt) != 0)
            parts.Add("Alt");
        if ((modifiers & NativeMethods.ModControl) != 0)
            parts.Add("Ctrl");
        if ((modifiers & NativeMethods.ModShift) != 0)
            parts.Add("Maj");
        parts.Add(HotkeyVirtualKeys.FormatLabel(virtualKey));
        return string.Join("+", parts);
    }
}
