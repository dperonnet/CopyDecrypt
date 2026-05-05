namespace CopyDecrypt;

internal sealed class AppSettings
{
    public bool HotkeyEnabled { get; set; } = true;

    public bool UrlModeEnabled { get; set; } = true;

    /// <summary>Combinaison MOD_* (sans MOD_NOREPEAT, ajouté à l’enregistrement).</summary>
    public uint HotkeyModifiers { get; set; } = NativeMethods.ModAlt | NativeMethods.ModShift;

    public uint HotkeyVirtualKey { get; set; } = HotkeyVirtualKeys.DefaultVirtualKey;

    public bool StartWithWindows { get; set; }

    internal void Sanitize()
    {
        HotkeyModifiers &= NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModAlt;
        if (HotkeyEnabled && HotkeyModifiers == 0)
            HotkeyModifiers = NativeMethods.ModAlt | NativeMethods.ModShift;

        if (!HotkeyVirtualKeys.IsKnown(HotkeyVirtualKey))
            HotkeyVirtualKey = HotkeyVirtualKeys.DefaultVirtualKey;
    }
}
