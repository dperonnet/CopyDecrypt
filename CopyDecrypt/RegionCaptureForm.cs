using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;

namespace CopyDecrypt;

/// <summary>
/// Plein écran semi-transparent : clic-glisser pour encadrer une zone (comme l’outil capture Windows).
/// </summary>
internal sealed class RegionCaptureForm : Form
{
    private const int MinEdgePx = 8;

    private Point _start;
    private Point _end;
    private bool _dragging;

    /// <summary>Bitmap capturé ; propriétaire = l’appelant après <see cref="ShowDialog"/> si DialogResult = OK.</summary>
    internal Bitmap? ResultBitmap { get; private set; }

    internal RegionCaptureForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.Manual;
        var vs = SystemInformation.VirtualScreen;
        Bounds = new Rectangle(vs.Left, vs.Top, vs.Width, vs.Height);
        TopMost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        BackColor = Color.Black;
        Opacity = 0.42;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    private Rectangle NormalizedRect()
    {
        var x1 = Math.Min(_start.X, _end.X);
        var y1 = Math.Min(_start.Y, _end.Y);
        var x2 = Math.Max(_start.X, _end.X);
        var y2 = Math.Max(_start.Y, _end.Y);
        return Rectangle.FromLTRB(x1, y1, x2, y2);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (e.Button != MouseButtons.Left)
            return;

        _dragging = true;
        _start = e.Location;
        _end = e.Location;
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            _end = e.Location;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left)
        {
            base.OnMouseUp(e);
            return;
        }

        _dragging = false;
        _end = e.Location;
        var clientRect = NormalizedRect();
        if (clientRect.Width < MinEdgePx || clientRect.Height < MinEdgePx)
        {
            Invalidate();
            base.OnMouseUp(e);
            return;
        }

        var screenRect = RectangleToScreen(clientRect);
        try
        {
            // Masquer l’overlay avant la copie écran, sinon la capture contiendrait l’assombrissement.
            Visible = false;
            TopMost = false;
            Application.DoEvents();
            Thread.Sleep(72);

            var bmp = new Bitmap(screenRect.Width, screenRect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(screenRect.Left, screenRect.Top, 0, 0, screenRect.Size, CopyPixelOperation.SourceCopy);
            }

            ResultBitmap = bmp;
            DialogResult = DialogResult.OK;
        }
        catch
        {
            ResultBitmap?.Dispose();
            ResultBitmap = null;
            DialogResult = DialogResult.Cancel;
        }

        Close();
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_dragging || (!_dragging && NormalizedRect().Width >= MinEdgePx && NormalizedRect().Height >= MinEdgePx))
        {
            var r = NormalizedRect();
            if (r.Width > 0 && r.Height > 0)
            {
                using var pen = new Pen(Color.White, 2)
                {
                    DashStyle = DashStyle.Dash,
                    Alignment = PenAlignment.Center,
                };
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        base.OnPaint(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            ResultBitmap?.Dispose();
            ResultBitmap = null;
        }

        base.OnFormClosed(e);
    }
}
