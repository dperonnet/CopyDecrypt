using System.Runtime.InteropServices;

namespace CopyDecrypt;

/// <summary>
/// Fenêtre invisible pour la boucle de messages Windows et l'enregistrement du raccourci global.
/// </summary>
internal sealed class HotkeyForm : Form
{
    private readonly Action _onHotkey;
    private readonly SettingsStore _settings;
    private bool _registered;

    internal HotkeyForm(Action onHotkey, SettingsStore settings)
    {
        _onHotkey = onHotkey;
        _settings = settings;
        // Fenêtre “fantôme” : doit exister pour recevoir WM_HOTKEY, mais ne doit jamais apparaître.
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

        if (_registered && IsHandleCreated)
        {
            NativeMethods.UnregisterHotKey(Handle, NativeMethods.HotkeyId);
            _registered = false;
        }

        Register();
    }

    private void Register()
    {
        if (_registered || IsDisposed || !IsHandleCreated)
            return;

        var cfg = _settings.Load();
        if (!cfg.HotkeyEnabled)
            return;

        // RegisterHotKey exige des VK_* + MOD_* ; MOD_NOREPEAT évite les répétitions tant que la touche reste enfoncée.
        uint modifiers = cfg.HotkeyModifiers | NativeMethods.ModNorepeat;
        uint vk = cfg.HotkeyVirtualKey;

        if (!NativeMethods.RegisterHotKey(Handle, NativeMethods.HotkeyId, modifiers, vk))
        {
            var err = Marshal.GetLastWin32Error();
            var label = HotkeyFormatter.Format(cfg);
            MessageBox.Show(
                $"Impossible d'enregistrer le raccourci {label} (erreur {err}). Un autre programme l'utilise peut‑être.",
                "CopyDecrypt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _registered = true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey && m.WParam.ToInt32() == NativeMethods.HotkeyId)
        {
            try
            {
                _onHotkey();
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
        if (_registered && IsHandleCreated && !IsDisposed)
        {
            NativeMethods.UnregisterHotKey(Handle, NativeMethods.HotkeyId);
            _registered = false;
        }

        base.OnFormClosed(e);
    }
}
