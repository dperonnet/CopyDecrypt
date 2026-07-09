namespace CopyDecrypt;

internal sealed class AppSettings
{
    /// <summary>Raccourci : sélection d'une zone à l'écran.</summary>
    public bool HotkeyEnabled { get; set; } = true;

    public uint HotkeyModifiers { get; set; } = NativeMethods.ModAlt | NativeMethods.ModShift;

    public uint HotkeyVirtualKey { get; set; } = HotkeyVirtualKeys.DefaultVirtualKey;

    /// <summary>Raccourci : lire l'image du presse-papiers.</summary>
    public bool ClipboardHotkeyEnabled { get; set; }

    public uint ClipboardHotkeyModifiers { get; set; } = NativeMethods.ModAlt | NativeMethods.ModShift;

    public uint ClipboardHotkeyVirtualKey { get; set; } = 0x56; // V

    public TrayDoubleClickAction DoubleClickAction { get; set; } = TrayDoubleClickAction.RegionCapture;

    public bool UrlModeEnabled { get; set; } = true;

    public bool StartWithWindows { get; set; }

    internal bool AreHotkeysIdentical() =>
        HotkeyEnabled
        && ClipboardHotkeyEnabled
        && HotkeyModifiers == ClipboardHotkeyModifiers
        && HotkeyVirtualKey == ClipboardHotkeyVirtualKey;

    internal void Sanitize()
    {
        HotkeyModifiers &= NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModAlt;
        if (HotkeyEnabled && HotkeyModifiers == 0)
            HotkeyModifiers = NativeMethods.ModAlt | NativeMethods.ModShift;

        if (!HotkeyVirtualKeys.IsKnown(HotkeyVirtualKey))
            HotkeyVirtualKey = HotkeyVirtualKeys.DefaultVirtualKey;

        ClipboardHotkeyModifiers &= NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModAlt;
        if (ClipboardHotkeyEnabled && ClipboardHotkeyModifiers == 0)
            ClipboardHotkeyModifiers = NativeMethods.ModAlt | NativeMethods.ModShift;

        if (!HotkeyVirtualKeys.IsKnown(ClipboardHotkeyVirtualKey))
            ClipboardHotkeyVirtualKey = 0x56;

        if (!Enum.IsDefined(typeof(TrayDoubleClickAction), DoubleClickAction))
            DoubleClickAction = TrayDoubleClickAction.RegionCapture;
    }
}
