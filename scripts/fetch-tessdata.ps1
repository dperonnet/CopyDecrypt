param(
    [string]$Language = "eng"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$tessdata = Join-Path $root "tessdata"
$outFile = Join-Path $tessdata "$Language.traineddata"

New-Item -ItemType Directory -Force $tessdata | Out-Null

if (Test-Path $outFile) {
    Write-Host "OK: $outFile déjà présent."
    exit 0
}

# tessdata_fast : plus léger et généralement suffisant pour texte écran/URLs.
$url = "https://github.com/tesseract-ocr/tessdata_fast/raw/main/$Language.traineddata"
Write-Host "Téléchargement: $url"
Invoke-WebRequest -Uri $url -OutFile $outFile
Write-Host "OK: téléchargé -> $outFile"

