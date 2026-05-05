using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace CopyDecrypt;

internal readonly record struct OcrRecognizeResult(string? Text, OcrFailureKind Failure, string? Detail = null);

internal enum OcrFailureKind
{
    None,
    NoEngine,
    EmptyResult,
    PngExport,
    Exception,
}

/// <summary>
/// OCR via l’API Windows (hors ligne). Prépare l’image (taille, format) pour de meilleurs résultats.
/// </summary>
internal static class WindowsMediaOcr
{
    /// <summary>Les petites captures d’URL sont souvent illisibles sans agrandissement.</summary>
    private const int MinLongEdgePx = 720;

    internal static OcrRecognizeResult TryRecognize(Bitmap bitmap)
    {
        try
        {
            using var prepared = PrepareBitmap(bitmap);
            using var ms = new MemoryStream();
            prepared.Save(ms, ImageFormat.Png);
            var png = ms.ToArray();
            if (png.Length == 0)
                return new OcrRecognizeResult(null, OcrFailureKind.PngExport);

            var (text, failure) = Task.Run(async () => await RecognizeFromPngAsync(png).ConfigureAwait(false))
                .GetAwaiter()
                .GetResult();

            if (failure != OcrFailureKind.None)
                return new OcrRecognizeResult(null, failure);

            return new OcrRecognizeResult(text!.Trim(), OcrFailureKind.None);
        }
        catch (Exception ex)
        {
            return new OcrRecognizeResult(null, OcrFailureKind.Exception, ex.Message);
        }
    }

    private static Bitmap PrepareBitmap(Bitmap source)
    {
        using var normalized = NormalizePixelFormat(source);
        int max = Math.Max(normalized.Width, normalized.Height);
        if (max >= MinLongEdgePx)
            return (Bitmap)normalized.Clone();

        double scale = (double)MinLongEdgePx / max;
        int w = Math.Max(1, (int)Math.Round(normalized.Width * scale));
        int h = Math.Max(1, (int)Math.Round(normalized.Height * scale));
        var scaled = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(normalized, 0, 0, w, h);
        }

        return scaled;
    }

    private static Bitmap NormalizePixelFormat(Bitmap source)
    {
        if (source.PixelFormat is PixelFormat.Format32bppArgb or PixelFormat.Format24bppRgb)
            return (Bitmap)source.Clone();

        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(clone))
            g.DrawImage(source, 0, 0, source.Width, source.Height);

        return clone;
    }

    private static async Task<(string? Text, OcrFailureKind Failure)> RecognizeFromPngAsync(byte[] pngBytes)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(pngBytes);
            await writer.StoreAsync().AsTask().ConfigureAwait(false);
        }

        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask().ConfigureAwait(false);
        using var rawBitmap = await decoder.GetSoftwareBitmapAsync().AsTask().ConfigureAwait(false);

        using var softwareBitmap = SoftwareBitmap.Convert(
            rawBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        // en-US reconnaît souvent mieux les URL / caractères ASCII.
        // On préfère néanmoins d’abord les langues du profil utilisateur (meilleur pour le texte “normal”).
        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.TryCreateFromLanguage(new Language("en-US"))
            ?? OcrEngine.TryCreateFromLanguage(new Language("fr-FR"))
            ?? OcrEngine.TryCreateFromLanguage(new Language("fr-CA"));

        if (engine is null)
            return (null, OcrFailureKind.NoEngine);

        var ocr = await engine.RecognizeAsync(softwareBitmap).AsTask().ConfigureAwait(false);
        var text = ocr.Text;
        return string.IsNullOrWhiteSpace(text) ? (null, OcrFailureKind.EmptyResult) : (text, OcrFailureKind.None);
    }
}
