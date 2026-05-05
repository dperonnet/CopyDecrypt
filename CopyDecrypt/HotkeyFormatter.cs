namespace CopyDecrypt;

internal static class HotkeyFormatter
{
    internal static string Format(AppSettings s)
    {
        var parts = new List<string>();
        if ((s.HotkeyModifiers & NativeMethods.ModAlt) != 0)
            parts.Add("Alt");
        if ((s.HotkeyModifiers & NativeMethods.ModControl) != 0)
            parts.Add("Ctrl");
        if ((s.HotkeyModifiers & NativeMethods.ModShift) != 0)
            parts.Add("Maj");
        parts.Add(FormatVirtualKey(s.HotkeyVirtualKey));
        return string.Join("+", parts);
    }

    private static string FormatVirtualKey(uint vk) => HotkeyVirtualKeys.FormatLabel(vk);
}
