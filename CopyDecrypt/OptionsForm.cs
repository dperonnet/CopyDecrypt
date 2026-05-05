using System.Drawing;

namespace CopyDecrypt;

internal sealed class OptionsForm : Form
{
    private const int LabelColumnWidthPx = 118;
    private const int HotkeyComboWidthPx = 88;
    private const int HotkeyDropdownWidthPx = 220;
    private const int RowPaddingY = 6;
    private const int HotkeyInnerGapX = 6;

    private readonly SettingsStore _store;
    private readonly Action _onApplied;
    private readonly CheckBox _chkHotkeyEnabled = new()
    {
        Text = "Activer le raccourci",
        AutoSize = true,
        Margin = new Padding(0, RowPaddingY, 0, 0),
    };
    private readonly CheckBox _chkUrlMode = new()
    {
        Text = "Mode URL (corriger les erreurs OCR d’URL)",
        AutoSize = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Left,
        Dock = DockStyle.None,
        Margin = new Padding(0, RowPaddingY, 0, 0),
    };
    private readonly CheckBox _chkCtrl = new()
    {
        Text = "Ctrl",
        AutoSize = true,
        Margin = Padding.Empty,
    };
    private readonly CheckBox _chkShift = new()
    {
        Text = "Maj",
        AutoSize = true,
        Margin = Padding.Empty,
    };
    private readonly CheckBox _chkAlt = new()
    {
        Text = "Alt",
        AutoSize = true,
        Margin = Padding.Empty,
    };
    private readonly ComboBox _cmbKey = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        DropDownWidth = HotkeyDropdownWidthPx,
        Width = HotkeyComboWidthPx,
        Margin = new Padding(HotkeyInnerGapX, 0, 0, 0),
    };
    private readonly CheckBox _chkStartup = new()
    {
        Text = "Lancer l'application au démarrage",
        AutoSize = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Left,
        Dock = DockStyle.None,
        Margin = new Padding(0, RowPaddingY, 0, 0),
    };

    internal OptionsForm(SettingsStore store, Action onApplied)
    {
        _store = store;
        _onApplied = onApplied;
        Text = "Options — CopyDecrypt";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(520, 216);
        Padding = new Padding(16);

        FillKeyCombo();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 14, 0, 0),
        };
        var btnOk = new Button { Text = "OK", AutoSize = true };
        var btnCancel = new Button { Text = "Annuler", AutoSize = true };
        btnPanel.Controls.Add(btnOk);
        btnPanel.Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidthPx));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // activation hotkey
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // config hotkey
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // démarrage
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // mode URL

        panel.Controls.Add(OptionLabel("Raccourcis :", RowPaddingY), 0, 0);
        panel.Controls.Add(_chkHotkeyEnabled, 1, 0);
        panel.Controls.Add(OptionLabel("Raccourcis :", RowPaddingY), 0, 1);
        panel.Controls.Add(HotkeyOptionsHost(), 1, 1);
        panel.Controls.Add(OptionLabel("Démarrage :", RowPaddingY), 0, 2);
        panel.Controls.Add(_chkStartup, 1, 2);
        panel.Controls.Add(OptionLabel("OCR :", RowPaddingY), 0, 3);
        panel.Controls.Add(_chkUrlMode, 1, 3);

        Controls.Add(btnPanel);
        Controls.Add(panel);

        btnOk.Click += (_, _) =>
        {
            if (TrySave())
                Close();
        };
        btnCancel.Click += (_, _) => Close();

        _chkHotkeyEnabled.CheckedChanged += (_, _) => UpdateHotkeyEnabledUi();
    }

    private static Label OptionLabel(string text, int marginTop) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        Dock = DockStyle.None,
        TextAlign = ContentAlignment.TopRight,
        MaximumSize = new Size(LabelColumnWidthPx - 8, 0),
        Margin = new Padding(0, marginTop, 8, 0),
    };

    private Control HotkeyOptionsHost()
    {
        var host = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = new Padding(0, RowPaddingY, 0, 0),
        };
        host.Controls.Add(HotkeyOptions());
        return host;
    }

    private FlowLayoutPanel HotkeyOptions()
    {
        var f = new FlowLayoutPanel
        {
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        f.Controls.Add(_chkCtrl);
        f.Controls.Add(Plus());
        f.Controls.Add(_chkShift);
        f.Controls.Add(Plus());
        f.Controls.Add(_chkAlt);
        f.Controls.Add(Plus());
        f.Controls.Add(_cmbKey);
        return f;
    }

    private static Label Plus() => new()
    {
        Text = "+",
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(HotkeyInnerGapX, 0, HotkeyInnerGapX, 0),
    };

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        LoadFromSettings();
    }

    private void FillKeyCombo()
    {
        _cmbKey.Items.Clear();
        foreach (var (vk, label) in HotkeyVirtualKeys.Choices)
            _cmbKey.Items.Add(new HotkeyKeyChoice(vk, label));
    }

    private void LoadFromSettings()
    {
        var s = _store.Load();
        _chkHotkeyEnabled.Checked = s.HotkeyEnabled;
        _chkUrlMode.Checked = s.UrlModeEnabled;
        _chkCtrl.Checked = (s.HotkeyModifiers & NativeMethods.ModControl) != 0;
        _chkShift.Checked = (s.HotkeyModifiers & NativeMethods.ModShift) != 0;
        _chkAlt.Checked = (s.HotkeyModifiers & NativeMethods.ModAlt) != 0;
        _chkStartup.Checked = s.StartWithWindows;
        SelectKey(s.HotkeyVirtualKey);

        UpdateHotkeyEnabledUi();
    }

    private void UpdateHotkeyEnabledUi()
    {
        var enabled = _chkHotkeyEnabled.Checked;
        _chkCtrl.Enabled = enabled;
        _chkShift.Enabled = enabled;
        _chkAlt.Enabled = enabled;
        _cmbKey.Enabled = enabled;
    }

    private void SelectKey(uint vk)
    {
        if (TrySelectVk(vk))
            return;
        if (vk != HotkeyVirtualKeys.DefaultVirtualKey && TrySelectVk(HotkeyVirtualKeys.DefaultVirtualKey))
            return;
        if (_cmbKey.Items.Count > 0)
            _cmbKey.SelectedIndex = 0;
    }

    private bool TrySelectVk(uint vk)
    {
        for (var i = 0; i < _cmbKey.Items.Count; i++)
        {
            if (_cmbKey.Items[i] is HotkeyKeyChoice c && c.VirtualKey == vk)
            {
                _cmbKey.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool TrySave()
    {
        if (_chkHotkeyEnabled.Checked && !_chkCtrl.Checked && !_chkShift.Checked && !_chkAlt.Checked)
        {
            MessageBox.Show(
                "Choisissez au moins une touche de modification (Ctrl, Maj ou Alt).",
                "CopyDecrypt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        if (_cmbKey.SelectedItem is not HotkeyKeyChoice choice)
        {
            MessageBox.Show("Choisissez une touche.", "CopyDecrypt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        uint mods = 0;
        if (_chkCtrl.Checked)
            mods |= NativeMethods.ModControl;
        if (_chkShift.Checked)
            mods |= NativeMethods.ModShift;
        if (_chkAlt.Checked)
            mods |= NativeMethods.ModAlt;

        var settings = new AppSettings
        {
            HotkeyEnabled = _chkHotkeyEnabled.Checked,
            UrlModeEnabled = _chkUrlMode.Checked,
            HotkeyModifiers = mods,
            HotkeyVirtualKey = choice.VirtualKey,
            StartWithWindows = _chkStartup.Checked,
        };
        settings.Sanitize();
        _store.Save(settings);
        StartupManager.Apply(settings.StartWithWindows);
        _onApplied();
        return true;
    }

    private readonly record struct HotkeyKeyChoice(uint VirtualKey, string Label)
    {
        public override string ToString() => Label;
    }
}
