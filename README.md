## CopyDecrypt
Application Windows (WinForms) en zone de notification qui lit **un QR code** ou du **texte via OCR** depuis une image (capture d’une zone écran ou image déjà dans le presse‑papiers), puis copie le résultat (souvent une **URL normalisée**) dans le presse‑papiers.

### Fonctionnalités
- **Raccourcis globaux** (activables/désactivables) : zone écran et image du presse‑papiers.
- Si les deux raccourcis sont identiques : image dans le presse‑papiers → lecture directe, sinon → sélection de zone.
- **Double-clic** sur l’icône : action configurable (zone ou presse‑papiers).
- **Menu tray** (clic droit) : sélection de zone, presse‑papiers, options, aide.
- **QR** via ZXing, **OCR** via Tesseract (hors‑ligne).
- **Option “Mode URL (OCR)”** : normalise la ponctuation/espaces d’une URL OCR, sans “deviner” de caractères.
- **Démarrage Windows** optionnel (HKCU Run).
- Notification : copie effectuée + **clic** sur la notification pour ouvrir un lien http/https, fermeture auto ~5s.

### Structure du code
Voir `docs/architecture.md`.

### Build

```bash
pwsh -File scripts/fetch-tessdata.ps1
dotnet build -c Debug
```

### Installer (Windows)
Pré-requis : Inno Setup 6 (pour compiler l’installeur).

```bash
pwsh -File scripts/fetch-tessdata.ps1
pwsh -File scripts/publish.ps1 -Configuration Release -Runtime win-x64
iscc installer/CopyDecrypt.iss
```

### Licence
Projet distribué sous licence MIT. Voir `LICENSE`.

