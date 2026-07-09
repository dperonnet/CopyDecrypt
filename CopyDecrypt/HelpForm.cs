using System.Drawing;

namespace CopyDecrypt;

internal sealed class HelpForm : Form
{
    private const int HelpContentWidthPx = 600;
    private const int ExtraVerticalPaddingPx = 20;

    private readonly TextBox _body;
    private readonly FlowLayoutPanel _bottomBar;

    internal HelpForm()
    {
        Text = "Aide — CopyDecrypt";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        // Positionnement manuel : centré sur l'écran du curseur (icône tray / menu).
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        Padding = new Padding(12);

        _body = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.None,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Regular, GraphicsUnit.Point),
            Text = HelpText,
            BackColor = SystemColors.Window,
        };

        var btn = new Button
        {
            Text = "Fermer",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right,
        };

        _bottomBar = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0),
        };
        _bottomBar.Controls.Add(btn);

        AcceptButton = btn;

        Controls.Add(_body);
        Controls.Add(_bottomBar);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ClientSize = MeasureClientSize();
        CenterOnCursorScreen();
    }

    private void CenterOnCursorScreen()
    {
        var workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            workArea.Left + (workArea.Width - Width) / 2,
            workArea.Top + (workArea.Height - Height) / 2);
    }

    private Size MeasureClientSize()
    {
        var clientWidth = HelpContentWidthPx + Padding.Horizontal;

        var textHeight = TextRenderer.MeasureText(
            HelpText,
            _body.Font,
            new Size(HelpContentWidthPx, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;

        var bottomHeight = MeasureBottomBarHeight(clientWidth);

        return new Size(
            clientWidth,
            textHeight + bottomHeight + Padding.Vertical + ExtraVerticalPaddingPx);
    }

    private int MeasureBottomBarHeight(int clientWidth)
    {
        _bottomBar.PerformLayout();
        var preferred = _bottomBar.GetPreferredSize(new Size(clientWidth, 0)).Height;
        if (preferred > _bottomBar.Padding.Vertical)
            return preferred;

        if (_bottomBar.Controls.Count > 0)
        {
            var button = _bottomBar.Controls[0];
            return button.GetPreferredSize(Size.Empty).Height + _bottomBar.Padding.Vertical;
        }

        return _bottomBar.Padding.Vertical;
    }

    private const string HelpText =
"""
CopyDecrypt lit le texte ou le QR code d'une image dans le presse-papiers ou de la zone d'écran sélectionnée.
Le texte ou le QR code est ensuite copié dans le presse-papiers. 

Lorsque le texte ou le QR code est détecté, une notification résume le résultat et propose de l'ouvrir lorsqu'il s'agit d'un lien.

Attention : Soyez prudent lorsque vous utilisez cette application. Un QR code peut contenir un lien vers un site web malveillant.

L'application propose deux modes de fonctionnement :
- Sélection de zone écran : tracez un rectangle de sélection (Échap ou clic droit pour annuler).
- Analyse du presse-papiers : analyse l'image actuellement copiée dans le presse-papiers.

Vous pouvez définir un raccourcis pour chaque mode.

Si le même raccourcis est configuré pour les deux modes (Sélection de zone écran et Analyse du presse-papiers):
- l'application tente de décrypter l'image dans le presse-papiers si elle est présente, 
- sinon l'application active le mode sélection de zone.

""";
}
