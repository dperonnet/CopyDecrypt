using System.Drawing;

namespace CopyDecrypt;

internal sealed class OptionsForm : Form
{
    private const int LabelColumnWidthPx = 200;
    private const int HotkeyComboWidthPx = 96;
    private const int HotkeyDropdownWidthPx = 220;
    private const int RowPaddingY = 6;
    private const int HotkeyInnerGapX = 6;
    private const int FormClientWidthPx = 654;
    private const int FormClientHeightPx = 375;
    private const int DoubleClickComboWidthPx = 260;

    private readonly SettingsStore _store;
    private readonly Action _onApplied;
    private readonly HotkeyEditor _regionHotkey = new();
    private readonly HotkeyEditor _clipboardHotkey = new();
    private readonly ComboBox _cmbDoubleClick = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = DoubleClickComboWidthPx,
        Margin = new Padding(0, RowPaddingY, 0, 0),
    };
    private readonly CheckBox _chkUrlMode = new()
    {
        Text = "Optimiser la lecture d'URL",
        AutoSize = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Left,
        Dock = DockStyle.None,
        Margin = new Padding(0, RowPaddingY, 0, 0),
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
        ClientSize = new Size(FormClientWidthPx, FormClientHeightPx);
        Padding = new Padding(16);

        _regionHotkey.FillKeyCombo();
        _clipboardHotkey.FillKeyCombo();
        FillDoubleClickCombo();

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

        var shortcutsTable = CreateOptionTable(4);
        shortcutsTable.Controls.Add(OptionLabel("Sélection de zone :", RowPaddingY), 0, 0);
        shortcutsTable.Controls.Add(_regionHotkey.EnabledCheckBox, 1, 0);
        shortcutsTable.Controls.Add(OptionLabel(string.Empty, RowPaddingY), 0, 1);
        shortcutsTable.Controls.Add(_regionHotkey.OptionsHost, 1, 1);
        shortcutsTable.Controls.Add(OptionLabel("Analyse du presse-papiers :", RowPaddingY), 0, 2);
        shortcutsTable.Controls.Add(_clipboardHotkey.EnabledCheckBox, 1, 2);
        shortcutsTable.Controls.Add(OptionLabel(string.Empty, RowPaddingY), 0, 3);
        shortcutsTable.Controls.Add(_clipboardHotkey.OptionsHost, 1, 3);

        var behaviorTable = CreateOptionTable(3);
        behaviorTable.Controls.Add(OptionLabel("Mode URL :", RowPaddingY), 0, 0);
        behaviorTable.Controls.Add(_chkUrlMode, 1, 0);
        behaviorTable.Controls.Add(OptionLabel("Démarrage :", RowPaddingY), 0, 1);
        behaviorTable.Controls.Add(_chkStartup, 1, 1);
        behaviorTable.Controls.Add(OptionLabel("Double-clic :", RowPaddingY), 0, 2);
        behaviorTable.Controls.Add(_cmbDoubleClick, 1, 2);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Width = FormClientWidthPx - 32,
        };
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Controls.Add(CreateGroup("Raccourcis", shortcutsTable), 0, 0);
        stack.Controls.Add(CreateGroup("Comportement", behaviorTable), 0, 1);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = Padding.Empty,
        };
        content.Controls.Add(stack);

        Controls.Add(btnPanel);
        Controls.Add(content);

        btnOk.Click += (_, _) =>
        {
            if (TrySave())
                Close();
        };
        btnCancel.Click += (_, _) => Close();

        _regionHotkey.EnabledCheckBox.CheckedChanged += (_, _) => _regionHotkey.UpdateEnabledUi();
        _clipboardHotkey.EnabledCheckBox.CheckedChanged += (_, _) => _clipboardHotkey.UpdateEnabledUi();
    }

    private static TableLayoutPanel CreateOptionTable(int rowCount)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = rowCount,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Dock = DockStyle.Top,
            Width = FormClientWidthPx - 56,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidthPx));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (var i = 0; i < rowCount; i++)
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return panel;
    }

    private static GroupBox CreateGroup(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 6, 10, 10),
            Margin = new Padding(0, 0, 0, 8),
        };
        group.Controls.Add(content);
        return group;
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

    private void FillDoubleClickCombo()
    {
        _cmbDoubleClick.Items.Clear();
        _cmbDoubleClick.Items.Add(new DoubleClickChoice(
            TrayDoubleClickAction.RegionCapture,
            "Sélectionner une zone à l'écran"));
        _cmbDoubleClick.Items.Add(new DoubleClickChoice(
            TrayDoubleClickAction.ClipboardImage,
            "Lire l'image du presse-papiers"));
        _cmbDoubleClick.SelectedIndex = 0;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _store.Load();
        _regionHotkey.Load(s.HotkeyEnabled, s.HotkeyModifiers, s.HotkeyVirtualKey);
        _clipboardHotkey.Load(s.ClipboardHotkeyEnabled, s.ClipboardHotkeyModifiers, s.ClipboardHotkeyVirtualKey);
        _chkUrlMode.Checked = s.UrlModeEnabled;
        _chkStartup.Checked = s.StartWithWindows;
        SelectDoubleClick(s.DoubleClickAction);
    }

    private void SelectDoubleClick(TrayDoubleClickAction action)
    {
        for (var i = 0; i < _cmbDoubleClick.Items.Count; i++)
        {
            if (_cmbDoubleClick.Items[i] is DoubleClickChoice c && c.Action == action)
            {
                _cmbDoubleClick.SelectedIndex = i;
                return;
            }
        }

        _cmbDoubleClick.SelectedIndex = 0;
    }

    private bool TrySave()
    {
        if (!_regionHotkey.TryValidate("sélection de zone", out var regionMods, out var regionVk))
            return false;

        if (!_clipboardHotkey.TryValidate("analyse du presse-papiers", out var clipboardMods, out var clipboardVk))
            return false;

        if (_cmbDoubleClick.SelectedItem is not DoubleClickChoice doubleClick)
        {
            MessageBox.Show("Choisissez une action pour le double-clic.", "CopyDecrypt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        var settings = new AppSettings
        {
            HotkeyEnabled = _regionHotkey.EnabledCheckBox.Checked,
            HotkeyModifiers = regionMods,
            HotkeyVirtualKey = regionVk,
            ClipboardHotkeyEnabled = _clipboardHotkey.EnabledCheckBox.Checked,
            ClipboardHotkeyModifiers = clipboardMods,
            ClipboardHotkeyVirtualKey = clipboardVk,
            DoubleClickAction = doubleClick.Action,
            UrlModeEnabled = _chkUrlMode.Checked,
            StartWithWindows = _chkStartup.Checked,
        };
        settings.Sanitize();
        _store.Save(settings);
        StartupManager.Apply(settings.StartWithWindows);
        _onApplied();
        return true;
    }

    private readonly record struct DoubleClickChoice(TrayDoubleClickAction Action, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed class HotkeyEditor
    {
        internal CheckBox EnabledCheckBox { get; } = new()
        {
            Text = "Activer le raccourci",
            AutoSize = true,
            Margin = new Padding(0, RowPaddingY, 0, 0),
        };

        internal Control OptionsHost { get; }

        private readonly CheckBox _chkCtrl = new() { Text = "Ctrl", AutoSize = true, Margin = Padding.Empty };
        private readonly CheckBox _chkShift = new() { Text = "Maj", AutoSize = true, Margin = Padding.Empty };
        private readonly CheckBox _chkAlt = new() { Text = "Alt", AutoSize = true, Margin = Padding.Empty };
        private readonly ComboBox _cmbKey = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownWidth = HotkeyDropdownWidthPx,
            Width = HotkeyComboWidthPx,
            Margin = new Padding(HotkeyInnerGapX, 0, 0, 0),
        };

        internal HotkeyEditor()
        {
            OptionsHost = BuildOptions();
        }

        private FlowLayoutPanel BuildOptions()
        {
            var f = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Padding = Padding.Empty,
                Margin = new Padding(0, RowPaddingY, 0, 0),
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

        internal void FillKeyCombo()
        {
            _cmbKey.Items.Clear();
            foreach (var (vk, label) in HotkeyVirtualKeys.Choices)
                _cmbKey.Items.Add(new HotkeyKeyChoice(vk, label));
        }

        internal void Load(bool enabled, uint modifiers, uint virtualKey)
        {
            EnabledCheckBox.Checked = enabled;
            _chkCtrl.Checked = (modifiers & NativeMethods.ModControl) != 0;
            _chkShift.Checked = (modifiers & NativeMethods.ModShift) != 0;
            _chkAlt.Checked = (modifiers & NativeMethods.ModAlt) != 0;
            SelectKey(virtualKey);
            UpdateEnabledUi();
        }

        internal void UpdateEnabledUi()
        {
            var enabled = EnabledCheckBox.Checked;
            _chkCtrl.Enabled = enabled;
            _chkShift.Enabled = enabled;
            _chkAlt.Enabled = enabled;
            _cmbKey.Enabled = enabled;
        }

        internal bool TryValidate(string label, out uint modifiers, out uint virtualKey)
        {
            modifiers = 0;
            virtualKey = 0;

            if (!EnabledCheckBox.Checked)
                return true;

            if (!_chkCtrl.Checked && !_chkShift.Checked && !_chkAlt.Checked)
            {
                MessageBox.Show(
                    $"Raccourci {label} : choisissez au moins une touche de modification (Ctrl, Maj ou Alt).",
                    "CopyDecrypt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            if (_cmbKey.SelectedItem is not HotkeyKeyChoice choice)
            {
                MessageBox.Show(
                    $"Raccourci {label} : choisissez une touche.",
                    "CopyDecrypt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            if (_chkCtrl.Checked)
                modifiers |= NativeMethods.ModControl;
            if (_chkShift.Checked)
                modifiers |= NativeMethods.ModShift;
            if (_chkAlt.Checked)
                modifiers |= NativeMethods.ModAlt;
            virtualKey = choice.VirtualKey;
            return true;
        }

        private void SelectKey(uint vk)
        {
            if (TrySelectVk(vk))
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

        private static Label Plus() => new()
        {
            Text = "+",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(HotkeyInnerGapX, 0, HotkeyInnerGapX, 0),
        };
    }

    private readonly record struct HotkeyKeyChoice(uint VirtualKey, string Label)
    {
        public override string ToString() => Label;
    }
}
