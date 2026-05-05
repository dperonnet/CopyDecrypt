param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "CopyDecrypt/CopyDecrypt.csproj"
$outDir = Join-Path $root "installer/work/publish"

New-Item -ItemType Directory -Force $outDir | Out-Null

Write-Host "Publish -> $outDir"
dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
  /p:PublishSingleFile=false `
  /p:PublishReadyToRun=true `
  /p:DebugType=None `
  -o $outDir

Write-Host "OK: publié."

