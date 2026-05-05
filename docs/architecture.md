## Architecture (vue d’ensemble)

CopyDecrypt est une app WinForms “tray-first” : aucune fenêtre principale, mais une **icône de notification** + un **hotkey global**.

### Flux principal (capture zone écran)
1. `HotkeyForm` reçoit `WM_HOTKEY` (enregistré via `RegisterHotKey`).
2. `TrayApplicationContext.ProcessRegionCapture()` ouvre `RegionCaptureForm` (overlay plein écran), récupère le `Bitmap`.
3. `ClipboardImageTextExtractor.TryExtractForClipboard(bitmap, settings)` :
   - tente d’abord le **QR** (`QrClipboardReader.TryDecodeQrToClipboardText`)
   - sinon fait l’**OCR** (`WindowsMediaOcr.TryRecognize`)
   - normalise pour presse‑papiers (`QrClipboardReader.NormalizeForClipboard`)
4. Résultat : copié via `Clipboard.SetText`, puis notification via `NotifyIcon.ShowBalloonTip`.
5. Si le texte est une URL http/https : clic sur la notification → ouverture navigateur (`Process.Start` avec `UseShellExecute=true`).

### Flux “menu tray : image déjà dans le presse‑papiers”
- `TrayApplicationContext.ProcessClipboardImage()` → `QrClipboardReader.TryGetBitmapFromClipboard()` → même extraction.

## Rôles des fichiers clés
- `Program.cs` : point d’entrée, lance `TrayApplicationContext`.
- `TrayApplicationContext.cs` : orchestration tray/menu, notifications, appels capture/extraction, ouverture Options/Aide.
- `HotkeyForm.cs` : fenêtre invisible qui enregistre/écoute le raccourci global (Win32 hotkey).
- `RegionCaptureForm.cs` : overlay de sélection de rectangle + capture `CopyFromScreen`.
- `ClipboardImageTextExtractor.cs` : pipeline QR/OCR et préparation du texte pour presse‑papiers.
- `QrClipboardReader.cs` : lecture QR (ZXing) + normalisation URL/texte.
- `WindowsMediaOcr.cs` : OCR Windows (WinRT) + préparation image (pixel format + upscale).
- `OptionsForm.cs` : UI options (hotkey, démarrage, mode URL).
- `AppSettings.cs` : modèle de settings (hotkey, démarrage, URL mode).
- `SettingsStore.cs` : persistance JSON dans `%AppData%\\CopyDecrypt\\settings.json`.
- `StartupManager.cs` : inscription au démarrage via registre HKCU Run.
- `TrayIconLoader.cs` : charge `icon.svg` (rendu via lib Svg) ou fallback.
- `NativeMethods.cs` : P/Invoke `RegisterHotKey`, `UnregisterHotKey`.

## Principes / contraintes notables
- **Capture écran** : `RegionCaptureForm` se masque avant `CopyFromScreen` (sinon l’overlay sombre apparaît dans la capture).
- **OCR** : l’API Windows ne fournit pas d’info de police, et le “Mode URL” reste volontairement **non destructeur** (pas de substitutions `1/l/I`).

