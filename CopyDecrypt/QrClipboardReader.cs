using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace CopyDecrypt;

internal static class QrClipboardReader
{
    private static readonly Regex UrlInText = new(
        @"https?://[^\s<>""']+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>URL sans schéma, souvent lue par l’OCR sur une seule ligne.</summary>
    private static readonly Regex WwwUrlInText = new(
        @"www\.[^\s<>""']+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly BarcodeReader Reader = new()
    {
        AutoRotate = true,
        Options = new DecodingOptions
        {
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            TryHarder = true,
            TryInverted = true,
        },
    };

    internal static bool TryGetBitmapFromClipboard([NotNullWhen(true)] out Bitmap? bitmap)
    {
        bitmap = null;
        if (!Clipboard.ContainsImage())
            return false;

        using var img = Clipboard.GetImage();
        if (img is null)
            return false;

        if (img is Bitmap bmp)
        {
            bitmap = (Bitmap)bmp.Clone();
            return true;
        }

        bitmap = new Bitmap(img);
        return true;
    }

    /// <summary>
    /// Décode un QR depuis une image et retourne une chaîne à mettre dans le presse-papiers (URL ou texte utile).
    /// </summary>
    internal static string? TryDecodeQrToClipboardText(Bitmap bitmap)
    {
        using var prepared = EnsureRgb24(bitmap);
        var result = Reader.Decode(prepared);
        if (result is null || string.IsNullOrWhiteSpace(result.Text))
            return null;

        return NormalizeForClipboard(result.Text.Trim());
    }

    /// <summary>Extrait une URL http(s) si possible, sinon renvoie le texte brut.</summary>
    internal static string NormalizeForClipboard(string text)
    {
        return NormalizeClipboardPayload(text) ?? text;
    }

    private static Bitmap EnsureRgb24(Bitmap source)
    {
        if (source.PixelFormat == PixelFormat.Format24bppRgb)
            return (Bitmap)source.Clone();

        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(clone))
        {
            g.DrawImage(source, 0, 0, source.Width, source.Height);
        }

        return clone;
    }

    private static string? NormalizeClipboardPayload(string text)
    {
        var trimmed = text.Trim();
        if (TryAsHttpUri(TrimUrlArtifacts(trimmed), out var absolute))
            return absolute;

        var match = UrlInText.Match(trimmed);
        if (match.Success && TryAsHttpUri(TrimUrlArtifacts(match.Value), out var fromRegex))
            return fromRegex;

        var wwwMatch = WwwUrlInText.Match(trimmed);
        if (wwwMatch.Success)
        {
            var withScheme = "https://" + TrimUrlArtifacts(wwwMatch.Value);
            if (TryAsHttpUri(withScheme, out var fromWww))
                return fromWww;
        }

        if (!trimmed.Contains("://", StringComparison.Ordinal)
            && (trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains('.', StringComparison.Ordinal)))
        {
            var candidate = "https://" + trimmed;
            if (TryAsHttpUri(candidate, out var withScheme))
                return withScheme;
        }

        return trimmed;
    }

    private static string TrimUrlArtifacts(string s)
    {
        return s.TrimEnd('.', ',', ';', ':', ')', ']', '}', '"', '\'', '»', '›');
    }

    private static bool TryAsHttpUri(string text, out string normalized)
    {
        normalized = text;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        normalized = uri.ToString();
        return true;
    }
}
