using System.Drawing;
using Svg;

namespace CopyDecrypt;

internal static class TrayIconLoader
{
    private const int RasterSizePx = 256;

    /// <summary>Charge <c>icon.svg</c> si présent, sinon <c>icon.png</c>.</summary>
    internal static Icon? TryLoadTrayIcon(string baseDirectory)
    {
        var svgPath = Path.Combine(baseDirectory, "icon.svg");
        if (File.Exists(svgPath))
        {
            var bmp = TryBitmapFromSvg(svgPath);
            if (bmp is not null)
                return BitmapToIcon(bmp);
        }

        var pngPath = Path.Combine(baseDirectory, "icon.png");
        if (!File.Exists(pngPath))
            return null;

        try
        {
            return BitmapToIcon(new Bitmap(pngPath));
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? TryBitmapFromSvg(string path)
    {
        try
        {
            var doc = SvgDocument.Open(path);
            return doc.Draw(RasterSizePx, RasterSizePx);
        }
        catch
        {
            return null;
        }
    }

    private static Icon? BitmapToIcon(Bitmap bitmap)
    {
        try
        {
            if (bitmap.Width < 1 || bitmap.Height < 1)
                return null;

            var hIcon = bitmap.GetHicon();
            try
            {
                using var tmp = Icon.FromHandle(hIcon);
                return (Icon)tmp.Clone();
            }
            finally
            {
                NativeMethods.DestroyIcon(hIcon);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            bitmap.Dispose();
        }
    }
}
