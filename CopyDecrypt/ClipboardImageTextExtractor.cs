using System.Drawing;
using System.Text.RegularExpressions;

namespace CopyDecrypt;

/// <summary>
/// 1) QR code si présent ; 2) sinon OCR Windows ; 3) si une URL apparaît dans le texte, on ne garde que l’URL.
/// </summary>
internal static class ClipboardImageTextExtractor
{
    private static readonly Regex LineBreaks = new(@"\r\n?|\n", RegexOptions.Compiled);
    private static readonly Regex CollapseSpaces = new(@"[ \t]{2,}", RegexOptions.Compiled);
    private static readonly Regex UrlLikeToken = new(@"(?i)\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled);

    internal readonly record struct ExtractOutcome(string? Text, string? FailureMessage);

    internal static ExtractOutcome TryExtractForClipboard(Bitmap bitmap, AppSettings settings)
    {
        var fromQr = QrClipboardReader.TryDecodeQrToClipboardText(bitmap);
        if (!string.IsNullOrEmpty(fromQr))
            return new ExtractOutcome(fromQr, null);

        var ocr = TesseractOcr.TryRecognize(bitmap, settings);
        if (ocr.Failure != OcrFailureKind.None)
            return new ExtractOutcome(null, FormatOcrFailure(ocr.Failure, ocr.Detail));

        // L’OCR peut insérer des sauts de ligne au milieu d’une URL affichée sur plusieurs lignes.
        var flattened = LineBreaks.Replace(ocr.Text!.Trim(), " ");
        var collapsed = CollapseSpaces.Replace(flattened, " ");
        var normalized = QrClipboardReader.NormalizeForClipboard(collapsed);

        if (settings.UrlModeEnabled)
            normalized = FixUrlIfLikely(normalized);

        return new ExtractOutcome(normalized, null);
    }

    private static string FixUrlIfLikely(string text)
    {
        // On ne touche qu’aux cas qui ressemblent fortement à une URL : éviter les “corrections” destructrices sur du texte normal.
        var m = UrlLikeToken.Match(text);
        if (!m.Success)
            return text;

        var token = m.Value;

        // Normalisations OCR typiques (ponctuation/espaces Unicode).
        token = token
            .Replace('：', ':')
            .Replace('／', '/')
            .Replace('∕', '/')
            .Replace('．', '.')
            .Replace('。', '.')
            .Replace('，', ',');

        // Suppression des espaces indésirables autour des séparateurs.
        token = token.Replace(" : ", ":").Replace(" ://", "://").Replace(" //", "//");
        token = token.Replace(" /", "/").Replace("/ ", "/");
        token = token.Replace(" .", ".").Replace(". ", ".");

        // Certains OCR insèrent des espaces entre caractères : on enlève tous les espaces dans le token URL.
        token = token.Replace(" ", string.Empty);

        // Compléter www. si nécessaire.
        if (token.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            token = "https://" + token;

        // Mode URL strict : on ne “devine” pas les caractères, on normalise uniquement la ponctuation/espaces
        // puis on valide l’URL.
        if (!Uri.TryCreate(token, UriKind.Absolute, out var uri) || uri is null)
            return text;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return text;

        // Remplacer uniquement le token trouvé (pas tout le texte).
        return text[..m.Index] + uri.ToString() + text[(m.Index + m.Length)..];
    }

    private static string FormatOcrFailure(OcrFailureKind kind, string? detail)
    {
        var baseMsg = kind switch
        {
            OcrFailureKind.NoEngine =>
                "OCR Windows indisponible : installez au moins une langue avec reconnaissance optique (Paramètres Windows → Heure et langue → Langue → Options de langue → reconnaissance optique de caractères).",
            OcrFailureKind.EmptyResult =>
                "L’OCR n’a lu aucun texte dans l’image. Zoomez l’URL, augmentez le contraste ou recadrez plus serré autour du texte.",
            OcrFailureKind.PngExport =>
                "Impossible d’exporter l’image pour l’OCR.",
            OcrFailureKind.Exception =>
                "L’OCR Windows a échoué.",
            _ => "Lecture du texte impossible.",
        };

        if (kind == OcrFailureKind.Exception && !string.IsNullOrWhiteSpace(detail))
        {
            var shortDetail = detail.Length > 120 ? detail[..117] + "…" : detail;
            return baseMsg + " " + shortDetail;
        }

        return baseMsg;
    }
}
