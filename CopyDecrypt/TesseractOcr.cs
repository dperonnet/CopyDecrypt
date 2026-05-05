using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Tesseract;

namespace CopyDecrypt;

internal static class TesseractOcr
{
    private const string DefaultLanguage = "eng";
    private const string UrlWhitelist =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~:/?#[]@!$&'()*+,;=%";

    // Les petites captures d’écran (URL dans une page) sont souvent trop petites pour un OCR fiable.
    private const int MinLongEdgePx = 900;

    // TesseractEngine est cher à créer. On le réutilise tant que le process vit.
    private static readonly object EngineGate = new();
    private static TesseractEngine? _engine;

    internal static OcrRecognizeResult TryRecognize(Bitmap bitmap, AppSettings settings)
    {
        try
        {
            using var prepared = PrepareForOcr(bitmap, settings);
            using var ms = new MemoryStream();
            prepared.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var png = ms.ToArray();
            if (png.Length == 0)
                return new OcrRecognizeResult(null, OcrFailureKind.PngExport);

            using var pix = Pix.LoadFromMemory(png);

            var engine = GetOrCreateEngine();
            ConfigureEngineFor(settings);

            using var page = engine.Process(
                pix,
                settings.UrlModeEnabled ? PageSegMode.SingleLine : PageSegMode.Auto);

            var text = page.GetText();
            return string.IsNullOrWhiteSpace(text)
                ? new OcrRecognizeResult(null, OcrFailureKind.EmptyResult)
                : new OcrRecognizeResult(text.Trim(), OcrFailureKind.None);
        }
        catch (Exception ex)
        {
            return new OcrRecognizeResult(null, OcrFailureKind.Exception, ex.Message);
        }
    }

    private static TesseractEngine GetOrCreateEngine()
    {
        lock (EngineGate)
        {
            if (_engine is not null)
                return _engine;

            var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            if (!Directory.Exists(dataPath) || !File.Exists(Path.Combine(dataPath, DefaultLanguage + ".traineddata")))
            {
                throw new InvalidOperationException(
                    "Données Tesseract manquantes. Exécutez scripts/fetch-tessdata.ps1 (eng) ou placez eng.traineddata dans le dossier 'tessdata' à côté de l'application.");
            }

            _engine = new TesseractEngine(dataPath, DefaultLanguage, EngineMode.Default);

            return _engine;
        }
    }

    private static void ConfigureEngineFor(AppSettings settings)
    {
        // Important : une whitelist active supprime les espaces (et d’autres caractères) du résultat.
        // Donc on ne l’applique que pour le mode URL.
        lock (EngineGate)
        {
            if (_engine is null)
                return;

            _engine.SetVariable("tessedit_char_whitelist", settings.UrlModeEnabled ? UrlWhitelist : string.Empty);
        }
    }

    private static Bitmap PrepareForOcr(Bitmap source, AppSettings settings)
    {
        using var normalized = NormalizePixelFormat(source);

        // 1) Upscale si trop petit
        using var scaled = UpscaleIfNeeded(normalized, MinLongEdgePx);

        // 2) Niveaux de gris
        using var gray = ToGrayscale(scaled);

        // 3) Autocontraste léger (stretch)
        using var contrasted = AutoContrast(gray);

        // La binarisation “dure” peut supprimer des détails fins (ex: point du i) et empirer les confusions i/l.
        // On laisse donc Tesseract faire son seuillage interne, sauf si on est en mode URL (souvent une seule ligne très contrastée).
        if (!settings.UrlModeEnabled)
            return (Bitmap)contrasted.Clone();

        using var binary = ThresholdOtsu(contrasted);
        return (Bitmap)binary.Clone();
    }

    private static Bitmap UpscaleIfNeeded(Bitmap src, int minLongEdgePx)
    {
        int max = Math.Max(src.Width, src.Height);
        if (max >= minLongEdgePx)
            return (Bitmap)src.Clone();

        double scale = (double)minLongEdgePx / max;
        int w = Math.Max(1, (int)Math.Round(src.Width * scale));
        int h = Math.Max(1, (int)Math.Round(src.Height * scale));
        var scaled = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(src, 0, 0, w, h);
        }

        return scaled;
    }

    private static Bitmap NormalizePixelFormat(Bitmap source)
    {
        if (source.PixelFormat is PixelFormat.Format32bppArgb or PixelFormat.Format24bppRgb)
            return (Bitmap)source.Clone();

        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(clone))
        {
            g.DrawImage(source, 0, 0, source.Width, source.Height);
        }

        return clone;
    }

    private static Bitmap ToGrayscale(Bitmap src)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            var cm = new ColorMatrix(
            [
                [0.299f, 0.299f, 0.299f, 0, 0],
                [0.587f, 0.587f, 0.587f, 0, 0],
                [0.114f, 0.114f, 0.114f, 0, 0],
                [0, 0, 0, 1, 0],
                [0, 0, 0, 0, 1],
            ]);

            using var ia = new ImageAttributes();
            ia.SetColorMatrix(cm);
            g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
        }

        return bmp;
    }

    private static Bitmap AutoContrast(Bitmap src)
    {
        // Simple stretch min/max basé sur luminance : robuste et rapide pour du texte écran.
        var rect = new Rectangle(0, 0, src.Width, src.Height);
        var data = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            int min = 255;
            int max = 0;
            var bytes = new byte[Math.Abs(data.Stride) * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (int y = 0; y < data.Height; y++)
            {
                var rowOff = y * data.Stride;
                for (int x = 0; x < data.Width; x++)
                {
                    var i = rowOff + (x * 3);
                    // format BGR
                    var b = bytes[i + 0];
                    var g = bytes[i + 1];
                    var r = bytes[i + 2];
                    var l = ((r * 299) + (g * 587) + (b * 114)) / 1000;
                    if (l < min)
                        min = l;
                    if (l > max)
                        max = l;
                }
            }

            if (max <= min + 8)
                return (Bitmap)src.Clone();

            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            var outData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                var scale = 255.0 / (max - min);
                var outBytes = new byte[Math.Abs(outData.Stride) * outData.Height];

                for (int y = 0; y < data.Height; y++)
                {
                    var inRowOff = y * data.Stride;
                    var outRowOff = y * outData.Stride;
                    for (int x = 0; x < data.Width; x++)
                    {
                        var i = inRowOff + (x * 3);
                        var b = bytes[i + 0];
                        var g = bytes[i + 1];
                        var r = bytes[i + 2];
                        var l = ((r * 299) + (g * 587) + (b * 114)) / 1000;
                        int v = (int)Math.Round((l - min) * scale);
                        if (v < 0)
                            v = 0;
                        if (v > 255)
                            v = 255;

                        var o = outRowOff + (x * 3);
                        outBytes[o + 0] = (byte)v;
                        outBytes[o + 1] = (byte)v;
                        outBytes[o + 2] = (byte)v;
                    }
                }

                Marshal.Copy(outBytes, 0, outData.Scan0, outBytes.Length);
            }
            finally
            {
                dst.UnlockBits(outData);
            }

            return dst;
        }
        finally
        {
            src.UnlockBits(data);
        }
    }

    private static Bitmap ThresholdOtsu(Bitmap src)
    {
        // src attendu en 24bpp “déjà gris”
        var hist = new int[256];

        var rect = new Rectangle(0, 0, src.Width, src.Height);
        var data = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var bytes = new byte[Math.Abs(data.Stride) * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (int y = 0; y < data.Height; y++)
            {
                var rowOff = y * data.Stride;
                for (int x = 0; x < data.Width; x++)
                {
                    var v = bytes[rowOff + (x * 3) + 0];
                    hist[v]++;
                }
            }
        }
        finally
        {
            src.UnlockBits(data);
        }

        int total = src.Width * src.Height;
        double sum = 0;
        for (int t = 0; t < 256; t++)
            sum += t * hist[t];

        double sumB = 0;
        int wB = 0;
        int wF = 0;
        double varMax = 0;
        int threshold = 128;

        for (int t = 0; t < 256; t++)
        {
            wB += hist[t];
            if (wB == 0)
                continue;
            wF = total - wB;
            if (wF == 0)
                break;

            sumB += t * hist[t];

            double mB = sumB / wB;
            double mF = (sum - sumB) / wF;
            double varBetween = (double)wB * wF * (mB - mF) * (mB - mF);

            if (varBetween > varMax)
            {
                varMax = varBetween;
                threshold = t;
            }
        }

        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
        var inData = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var outData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            var inBytes = new byte[Math.Abs(inData.Stride) * inData.Height];
            Marshal.Copy(inData.Scan0, inBytes, 0, inBytes.Length);

            var outBytes = new byte[Math.Abs(outData.Stride) * outData.Height];
            for (int y = 0; y < src.Height; y++)
            {
                var inRowOff = y * inData.Stride;
                var outRowOff = y * outData.Stride;
                for (int x = 0; x < src.Width; x++)
                {
                    var v = inBytes[inRowOff + (x * 3) + 0];
                    var b = (byte)(v >= threshold ? 255 : 0);
                    var o = outRowOff + (x * 3);
                    outBytes[o + 0] = b;
                    outBytes[o + 1] = b;
                    outBytes[o + 2] = b;
                }
            }

            Marshal.Copy(outBytes, 0, outData.Scan0, outBytes.Length);
        }
        finally
        {
            src.UnlockBits(inData);
            dst.UnlockBits(outData);
        }

        return dst;
    }
}

