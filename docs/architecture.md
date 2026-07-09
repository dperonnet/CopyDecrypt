## Architecture (vue d’ensemble)

CopyDecrypt est une app WinForms “tray-first” : aucune fenêtre principale, mais une **icône de notification** + des **raccourcis globaux**.

### Flux principal (capture zone écran)
1. `HotkeyForm` reçoit `WM_HOTKEY` (enregistré via `RegisterHotKey`).
2. `TrayApplicationContext.ProcessRegionCapture()` ouvre `RegionCaptureForm` (overlay plein écran), récupère le `Bitmap`.
3. `ClipboardImageTextExtractor.TryExtractForClipboard(bitmap, settings)` :
   - tente d’abord le **QR** (`QrClipboardReader.TryDecodeQrToClipboardText`)
   - sinon fait l’**OCR** (`TesseractOcr.TryRecognize`)
   - normalise pour presse‑papiers (`QrClipboardReader.NormalizeForClipboard`)
4. Résultat : copié via `Clipboard.SetText`, puis notification via `NotifyIcon.ShowBalloonTip`.
5. Si le texte est une URL http/https : clic sur la notification → ouverture navigateur (`Process.Start` avec `UseShellExecute=true`).

### Flux “image du presse‑papiers”
- Menu tray, raccourci dédié ou double-clic (selon options) → `TrayApplicationContext.ProcessClipboardImage()` → `QrClipboardReader.TryGetBitmapFromClipboard()` → même extraction.

### Raccourcis combinés
Si les deux raccourcis (zone écran et presse‑papiers) sont **activés avec la même combinaison**, un seul `RegisterHotKey` est enregistré et `ProcessSmartHotkey()` choisit l’action : image dans le presse‑papiers → lecture directe, sinon → sélection de zone.

## Rôles des fichiers clés
- `Program.cs` : point d’entrée, lance `TrayApplicationContext`.
- `TrayApplicationContext.cs` : orchestration tray/menu, notifications, double-clic, appels capture/extraction, ouverture Options/Aide.
- `HotkeyForm.cs` : fenêtre invisible qui enregistre/écoute les raccourcis globaux (Win32 hotkey).
- `RegionCaptureForm.cs` : overlay de sélection de rectangle + capture `CopyFromScreen`.
- `ClipboardImageTextExtractor.cs` : pipeline QR/OCR et préparation du texte pour presse‑papiers.
- `QrClipboardReader.cs` : lecture QR (ZXing), accès image presse‑papiers, normalisation URL/texte.
- `TesseractOcr.cs` : OCR Tesseract (hors‑ligne) + préparation image.
- `OptionsForm.cs` : UI options (raccourcis zone/presse‑papiers, double-clic, démarrage, mode URL).
- `AppSettings.cs` : modèle de settings (raccourcis, double-clic, démarrage, mode URL).
- `TrayDoubleClickAction.cs` : énumération de l’action au double-clic sur l’icône.
- `SettingsStore.cs` : persistance JSON dans `%AppData%\\CopyDecrypt\\settings.json`.
- `StartupManager.cs` : inscription au démarrage via registre HKCU Run.
- `TrayIconLoader.cs` : charge `icon.svg` (rendu via lib Svg) ou fallback.
- `NativeMethods.cs` : P/Invoke `RegisterHotKey`, `UnregisterHotKey`.
- `HotkeyFormatter.cs` : affichage lisible des combinaisons de touches.

## Principes / contraintes notables
- **Capture écran** : `RegionCaptureForm` se masque avant `CopyFromScreen` (sinon l’overlay sombre apparaît dans la capture).
- **Mode URL** : normalisation ponctuation/espaces volontairement **non destructive** (pas de substitutions `1/l/I`).
