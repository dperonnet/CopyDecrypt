using System.Runtime.InteropServices;

namespace CopyDecrypt;

/// <summary>
/// Fenêtre invisible pour la boucle de messages Windows et l'enregistrement des raccourcis globaux.
/// </summary>
internal sealed class HotkeyForm : Form
{
    private readonly Action _onRegion;
    private readonly Action _onClipboard;
    private readonly Action _onSmartCombined;
    private readonly SettingsStore _settings;
    private bool _registeredRegion;
    private bool _registeredClipboard;

    internal HotkeyForm(
        Action onRegion,
        Action onClipboard,
        Action onSmartCombined,
        SettingsStore settings)
    {
        _onRegion = onRegion;
        _onClipboard = onClipboard;
        _onSmartCombined = onSmartCombined;
        _settings = settings;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
        Size = new Size(1, 1);
        Text = "CopyDecrypt";
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Visible = false;
        Register();
    }

    internal void ReloadHotkey()
    {
        if (IsDisposed)
            return;

        UnregisterAll();
        Register();
    }

    private void UnregisterAll()
    {
        if (!IsHandleCreated)
            return;

        if (_registeredRegion)
        {
            NativeMethods.UnregisterHotKey(Handle, NativeMethods.HotkeyIdRegion);
            _registeredRegion = false;
        }

        if (_registeredClipboard)
        {
            NativeMethods.UnregisterHotKey(Handle, NativeMethods.HotkeyIdClipboard);
            _registeredClipboard = false;
        }
    }

    private void Register()
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        var cfg = _settings.Load();

        if (cfg.AreHotkeysIdentical())
        {
            RegisterOne(
                NativeMethods.HotkeyIdRegion,
                cfg.HotkeyModifiers,
                cfg.HotkeyVirtualKey,
                HotkeyFormatter.FormatRegion(cfg),
                ref _registeredRegion);
            return;
        }

        if (cfg.HotkeyEnabled)
        {
            RegisterOne(
                NativeMethods.HotkeyIdRegion,
                cfg.HotkeyModifiers,
                cfg.HotkeyVirtualKey,
                HotkeyFormatter.FormatRegion(cfg),
                ref _registeredRegion);
        }

        if (cfg.ClipboardHotkeyEnabled)
        {
            RegisterOne(
                NativeMethods.HotkeyIdClipboard,
                cfg.ClipboardHotkeyModifiers,
                cfg.ClipboardHotkeyVirtualKey,
                HotkeyFormatter.FormatClipboard(cfg),
                ref _registeredClipboard);
        }
    }

    private void RegisterOne(int id, uint modifiers, uint vk, string label, ref bool registeredFlag)
    {
        uint mods = modifiers | NativeMethods.ModNorepeat;
        if (!NativeMethods.RegisterHotKey(Handle, id, mods, vk))
        {
            var err = Marshal.GetLastWin32Error();
            MessageBox.Show(
                $"Impossible d'enregistrer le raccourci {label} (erreur {err}). Un autre programme l'utilise peut‑être.",
                "CopyDecrypt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        registeredFlag = true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey)
        {
            var id = m.WParam.ToInt32();
            try
            {
                var cfg = _settings.Load();
                if (cfg.AreHotkeysIdentical() && id == NativeMethods.HotkeyIdRegion)
                {
                    _onSmartCombined();
                    return;
                }

                if (id == NativeMethods.HotkeyIdRegion)
                {
                    _onRegion();
                    return;
                }

                if (id == NativeMethods.HotkeyIdClipboard)
                {
                    _onClipboard();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CopyDecrypt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnregisterAll();
        base.OnFormClosed(e);
    }
}
