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

    // UX : on garde les notifications courtes (évite “pollution” dans le centre de notifications).
    private const int BalloonTimeoutMs = 5000;

    public TrayApplicationContext()
    {
        var initial = _settings.Load();
        StartupManager.Apply(initial.StartWithWindows);

        _hotkeyForm = new HotkeyForm(ProcessRegionCapture, _settings);

        _trayCustomIcon = TrayIconLoader.TryLoadTrayIcon(AppContext.BaseDirectory);

        _tray = new NotifyIcon
        {
            Icon = _trayCustomIcon ?? SystemIcons.Application,
            Visible = true,
            Text = BuildTrayText(initial),
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Lire l’image du presse-papiers (QR / OCR)", null, (_, _) => ProcessClipboardImage());
        menu.Items.Add("Aide…", null, (_, _) => OpenHelp());
        menu.Items.Add("Options…", null, (_, _) => OpenOptions());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => QuitFromTray());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ProcessRegionCapture();
        _tray.BalloonTipClicked += (_, _) => OpenLastUrlIfAny();

        MainForm = _hotkeyForm;
        _hotkeyForm.Show();
    }

    private static string BuildTrayText(AppSettings s)
    {
        var hk = s.HotkeyEnabled ? HotkeyFormatter.Format(s) : "raccourci désactivé";
        return "CopyDecrypt — " + hk + " : zone écran · menu : presse-papiers";
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
            // Ignore : on ne veut pas d’erreur intrusive depuis une notification.
        }
    }

    private void OpenHelp()
    {
        using var h = new HelpForm();
        h.ShowDialog(_hotkeyForm);
    }

    private void OpenOptions()
    {
        using var form = new OptionsForm(_settings, RefreshTrayFromSettings);
        form.ShowDialog();
    }

    private void RefreshTrayFromSettings()
    {
        _hotkeyForm.ReloadHotkey();
        _tray.Text = BuildTrayText(_settings.Load());
    }

    /// <summary>Raccourci / double-clic : sélection d’une zone à l’écran.</summary>
    private void ProcessRegionCapture()
    {
        if (_regionCaptureBusy)
            return;

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

    /// <summary>Menu : image déjà dans le presse-papiers.</summary>
    private void ProcessClipboardImage()
    {
        if (!QrClipboardReader.TryGetBitmapFromClipboard(out Bitmap? bitmap))
        {
            _tray.ShowBalloonTip(
                BalloonTimeoutMs,
                "CopyDecrypt",
                "Le presse-papiers ne contient pas d'image. Faites une capture (Win+Maj+S) puis choisissez cette commande, ou utilisez le raccourci pour encadrer une zone.",
                ToolTipIcon.Info);
            return;
        }

        using (bitmap)
        {
            var outcome = ClipboardImageTextExtractor.TryExtractForClipboard(bitmap, _settings.Load());
            ApplyExtractionOutcome(outcome);
        }
    }

    private void ApplyExtractionOutcome(ClipboardImageTextExtractor.ExtractOutcome outcome)
    {
        _lastClickableUrl = null;

        if (string.IsNullOrEmpty(outcome.Text))
        {
            var msg = string.IsNullOrWhiteSpace(outcome.FailureMessage)
                ? "Aucun QR lisible et aucun texte exploitable (OCR) dans l’image."
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
