using System.Drawing;

namespace CopyDecrypt;

internal sealed class HelpForm : Form
{
    internal HelpForm()
    {
        Text = "Aide — CopyDecrypt";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(520, 460);
        Padding = new Padding(12);

        var body = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
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

        var bottom = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0),
        };
        bottom.Controls.Add(btn);

        AcceptButton = btn;

        Controls.Add(body);
        Controls.Add(bottom);
    }

    private const string HelpText =
"""
CopyDecrypt lit un QR code ou du texte (OCR) dans une image, puis place le résultat (souvent une URL) dans le presse-papiers pour que vous puissiez le coller (Ctrl+V).

• Raccourci global (défaut : Alt+Maj+C, modifiable dans Options)
  L’écran s’assombrit : tracez un rectangle avec le bouton gauche de la souris autour de la zone à analyser (comme l’outil Capture d’écran Windows). Relâchez pour capturer cette zone et lancer la lecture. Échap ou clic droit annule.

• Double-clic sur l’icône dans la zone de notification
  Même action que le raccourci (sélection d’une zone à l’écran).

• Menu contextuel (clic droit sur l’icône)
  — « Lire l’image du presse-papiers » : utilise l’image déjà copiée dans le presse-papiers (par ex. après Win+Maj+S puis collage automatique de la capture). Aucun cadre à tracer.
  — « Aide… » : cette fenêtre.
  — « Options… » : raccourci global (Ctrl / Maj / Alt + touche), lancement au démarrage de l’ordinateur.
  — « Quitter » : ferme l’application (icône disparaît).

• Résultat
  Si une URL est détectée, seule l’adresse est copiée. Sinon, le texte lu (QR ou OCR) est copié tel quel. Une bulle de notification résume le résultat ou l’erreur.

• Conseils
  Pour l’OCR : texte net, bon contraste, langues de reconnaissance installées dans Windows. Pour le QR : le code doit être entièrement visible dans la zone ou l’image.

• Licence
  CopyDecrypt est distribué sous licence MIT (voir le fichier LICENSE).
""";
}
