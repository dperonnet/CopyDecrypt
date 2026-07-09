using System.Drawing;

namespace CopyDecrypt;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly HotkeyForm _hotkeyForm;
    private readonly SettingsStore _settings = new();
    private Icon? _trayCustomIcon;
    private bool _regionCaptureBusy;
    private string? _lastClickableUrl;
    private ToolStripMenuItem? _menuRegion;
    private ToolStripMenuItem? _menuClipboard;
    private bool _optionsDialogOpen;

    // UX : on garde les notifications courtes (évite “pollution” dans le centre de notifications).
    private const int BalloonTimeoutMs = 5000;

    public TrayApplicationContext()
    {
        var initial = _settings.Load();
        StartupManager.Apply(initial.StartWithWindows);

        _hotkeyForm = new HotkeyForm(
            ProcessRegionCapture,
            () => ProcessClipboardImage(showErrorIfEmpty: true),
            ProcessSmartHotkey,
            _settings);

        _trayCustomIcon = TrayIconLoader.TryLoadTrayIcon(AppContext.BaseDirectory);

        _tray = new NotifyIcon
        {
            Icon = _trayCustomIcon ?? SystemIcons.Application,
            Visible = true,
            Text = BuildTrayText(initial),
        };

        var menu = new ContextMenuStrip();
        _menuRegion = new ToolStripMenuItem("Sélectionner une zone à l'écran…", null, (_, _) => ProcessRegionCapture());
        _menuClipboard = new ToolStripMenuItem("Lire l'image du presse-papiers…", null, (_, _) => ProcessClipboardImage(showErrorIfEmpty: true));
        menu.Items.Add(_menuRegion);
        menu.Items.Add(_menuClipboard);
        menu.Items.Add("Aide…", null, (_, _) => OpenHelp());
        menu.Items.Add("Options…", null, (_, _) => OpenOptions());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => QuitFromTray());
        _tray.ContextMenuStrip = menu;
        UpdateMenuShortcuts(initial);
        _tray.DoubleClick += (_, _) => ProcessDoubleClick();
        _tray.BalloonTipClicked += (_, _) => OpenLastUrlIfAny();

        MainForm = _hotkeyForm;
        _hotkeyForm.Show();
    }

    private static string BuildTrayText(AppSettings s)
    {
        if (s.AreHotkeysIdentical())
            return "CopyDecrypt — " + HotkeyFormatter.FormatRegion(s) + " : zone ou presse-papiers";

        var parts = new List<string>();
        if (s.HotkeyEnabled)
            parts.Add(HotkeyFormatter.FormatRegion(s) + " : zone");
        if (s.ClipboardHotkeyEnabled)
            parts.Add(HotkeyFormatter.FormatClipboard(s) + " : presse-papiers");

        return parts.Count == 0
            ? "CopyDecrypt — raccourcis désactivés"
            : "CopyDecrypt — " + string.Join(" · ", parts);
    }

    private void ProcessDoubleClick()
    {
        var action = _settings.Load().DoubleClickAction;
        if (action == TrayDoubleClickAction.ClipboardImage)
            ProcessClipboardImage(showErrorIfEmpty: true);
        else
            ProcessRegionCapture();
    }

    /// <summary>Raccourci unique pour les deux actions : image dans le presse-papiers ou sélection de zone.</summary>
    private void ProcessSmartHotkey()
    {
        if (QrClipboardReader.ClipboardContainsImage())
            ProcessClipboardImage(showErrorIfEmpty: false);
        else
            ProcessRegionCapture();
    }

    private void OpenLastUrlIfAny()
    {
        var url = _lastClickableUrl;
        _lastClickableUrl = null;
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore : on ne veut pas d'erreur intrusive depuis une notification.
        }
    }

    private void OpenHelp()
    {
        using var h = new HelpForm();
        h.ShowDialog(_hotkeyForm);
    }

    private void OpenOptions()
    {
        if (_optionsDialogOpen)
            return;

        _optionsDialogOpen = true;
        try
        {
            using var form = new OptionsForm(_settings, RefreshTrayFromSettings);
            form.ShowDialog();
        }
        finally
        {
            _optionsDialogOpen = false;
        }
    }

    private void RefreshTrayFromSettings()
    {
        var settings = _settings.Load();
        _hotkeyForm.ReloadHotkey();
        _tray.Text = BuildTrayText(settings);
        UpdateMenuShortcuts(settings);
    }

    private void UpdateMenuShortcuts(AppSettings settings)
    {
        if (_menuRegion is not null)
        {
            _menuRegion.ShortcutKeyDisplayString = settings.HotkeyEnabled
                ? HotkeyFormatter.FormatRegion(settings)
                : string.Empty;
        }

        if (_menuClipboard is not null)
        {
            _menuClipboard.ShortcutKeyDisplayString = settings.ClipboardHotkeyEnabled
                ? HotkeyFormatter.FormatClipboard(settings)
                : string.Empty;
        }
    }

    /// <summary>Sélection d'une zone à l'écran.</summary>
    private void ProcessRegionCapture()
    {
        if (_regionCaptureBusy)
            return;

        DismissActiveBalloon();
        _regionCaptureBusy = true;
        try
        {
            Bitmap? bmp = null;
            using (var cap = new RegionCaptureForm())
            {
                if (cap.ShowDialog() != DialogResult.OK)
                    return;
                bmp = cap.ResultBitmap;
            }

            if (bmp is null)
                return;

            using (bmp)
            {
                var outcome = ClipboardImageTextExtractor.TryExtractForClipboard(bmp, _settings.Load());
                ApplyExtractionOutcome(outcome);
            }
        }
        finally
        {
            _regionCaptureBusy = false;
        }
    }

    /// <summary>Image déjà dans le presse-papiers.</summary>
    private void ProcessClipboardImage(bool showErrorIfEmpty)
    {
        DismissActiveBalloon();

        if (!QrClipboardReader.TryGetBitmapFromClipboard(out Bitmap? bitmap))
        {
            if (showErrorIfEmpty)
            {
                _tray.ShowBalloonTip(
                    BalloonTimeoutMs,
                    "CopyDecrypt",
                    "Le presse-papiers ne contient pas d'image.",
                    ToolTipIcon.Info);
            }

            return;
        }

        using (bitmap)
        {
            var outcome = ClipboardImageTextExtractor.TryExtractForClipboard(bitmap, _settings.Load());
            ApplyExtractionOutcome(outcome);
        }
    }

    private void DismissActiveBalloon()
    {
        _lastClickableUrl = null;
        if (!_tray.Visible)
            return;

        // WinForms n'expose pas de fermeture explicite : masquer/réafficher l'icône retire la bulle/toast en cours.
        _tray.Visible = false;
        _tray.Visible = true;
    }

    private void ApplyExtractionOutcome(ClipboardImageTextExtractor.ExtractOutcome outcome)
    {
        _lastClickableUrl = null;

        if (string.IsNullOrEmpty(outcome.Text))
        {
            var msg = string.IsNullOrWhiteSpace(outcome.FailureMessage)
                ? "Aucun contenu lisible dans l'image."
                : outcome.FailureMessage;
            _tray.ShowBalloonTip(BalloonTimeoutMs, "CopyDecrypt", msg, ToolTipIcon.Warning);
            return;
        }

        try
        {
            Clipboard.SetText(outcome.Text);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(BalloonTimeoutMs, "CopyDecrypt", "Impossible d'écrire dans le presse-papiers : " + ex.Message, ToolTipIcon.Error);
            return;
        }

        var preview = outcome.Text.Length > 120 ? outcome.Text[..117] + "…" : outcome.Text;
        if (TryGetHttpUrl(outcome.Text, out var url))
        {
            _lastClickableUrl = url;
            _tray.ShowBalloonTip(BalloonTimeoutMs, "CopyDecrypt", "Lien copié (clic pour ouvrir) :\n" + preview, ToolTipIcon.Info);
        }
        else
        {
            _tray.ShowBalloonTip(BalloonTimeoutMs, "CopyDecrypt", "Collé dans le presse-papiers :\n" + preview, ToolTipIcon.Info);
        }
    }

    private static bool TryGetHttpUrl(string text, out string url)
    {
        url = string.Empty;
        if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;
        url = uri.ToString();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Visible = false;
            _tray.Icon = null;
            _trayCustomIcon?.Dispose();
            _trayCustomIcon = null;
            _tray.Dispose();
        }

        base.Dispose(disposing);
    }

    private void QuitFromTray()
    {
        _tray.Visible = false;
        ExitThread();
    }
}
