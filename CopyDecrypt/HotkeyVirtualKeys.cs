namespace CopyDecrypt;

/// <summary>Touches enregistrables via RegisterHotKey (VK + libellé affichage FR).</summary>
internal static class HotkeyVirtualKeys
{
    internal const uint DefaultVirtualKey = 0x43; // C

    /// <summary>Ordre affiché dans la liste déroulante.</summary>
    internal static readonly (uint Vk, string Label)[] Choices =
    [
        .. FnKeys(),
        .. Digits(),
        .. Letters(),
        .. EditingNav(),
        .. Numpad(),
        .. OemAndMisc(),
    ];

    internal static string FormatLabel(uint vk)
    {
        foreach (var (v, label) in Choices)
        {
            if (v == vk)
                return label;
        }

        return "0x" + vk.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static bool IsKnown(uint vk)
    {
        foreach (var (v, _) in Choices)
        {
            if (v == vk)
                return true;
        }

        return false;
    }

    private static IEnumerable<(uint Vk, string Label)> FnKeys()
    {
        for (var n = 1; n <= 24; n++)
            yield return ((uint)(0x6F + n), "F" + n);
    }

    private static IEnumerable<(uint Vk, string Label)> Digits()
    {
        for (var vk = 0x30u; vk <= 0x39u; vk++)
            yield return (vk, ((char)vk).ToString());
    }

    private static IEnumerable<(uint Vk, string Label)> Letters()
    {
        for (var vk = 0x41u; vk <= 0x5Au; vk++)
            yield return (vk, ((char)vk).ToString());
    }

    private static IEnumerable<(uint Vk, string Label)> EditingNav()
    {
        yield return (0x08, "Retour arrière");
        yield return (0x09, "Tab");
        yield return (0x0D, "Entrée");
        yield return (0x1B, "Échap");
        yield return (0x20, "Espace");
        yield return (0x21, "Page préc.");
        yield return (0x22, "Page suiv.");
        yield return (0x23, "Fin");
        yield return (0x24, "Début");
        yield return (0x25, "←");
        yield return (0x26, "↑");
        yield return (0x27, "→");
        yield return (0x28, "↓");
        yield return (0x2C, "Impr. écran");
        yield return (0x2D, "Insertion");
        yield return (0x2E, "Suppr");
        yield return (0x13, "Pause");
        yield return (0x14, "Verr. maj");
    }

    private static IEnumerable<(uint Vk, string Label)> Numpad()
    {
        for (var vk = 0x60u; vk <= 0x69u; vk++)
        {
            var d = (char)('0' + (vk - 0x60));
            yield return (vk, "Num " + d);
        }

        yield return (0x6A, "Num *");
        yield return (0x6B, "Num +");
        yield return (0x6D, "Num −");
        yield return (0x6E, "Num .");
        yield return (0x6F, "Num /");
    }

    private static IEnumerable<(uint Vk, string Label)> OemAndMisc()
    {
        yield return (0xBA, ";");
        yield return (0xBB, "=");
        yield return (0xBC, ",");
        yield return (0xBD, "−");
        yield return (0xBE, ".");
        yield return (0xBF, "/");
        yield return (0xC0, "²");
        yield return (0xDB, "[");
        yield return (0xDC, "\\");
        yield return (0xDD, "]");
        yield return (0xDE, "ù");
        yield return (0xDF, "`");
        yield return (0xE2, "<");
    }
}
