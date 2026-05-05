; Inno Setup script (compile with ISCC)

#define AppName "CopyDecrypt"
#define AppExeName "CopyDecrypt.exe"
#define AppPublisher "Dams"
#define AppURL "https://github.com/<owner>/<repo>"

[Setup]
AppId={{C8F1A2C6-0B2A-4D6B-9A72-9A7D4B33B2E0}
AppName={#AppName}
AppVersion=1.0.0
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=CopyDecrypt-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer une icône sur le Bureau"; GroupDescription: "Raccourcis:"; Flags: unchecked

[Files]
; Le contenu publié (self-contained) est généré par scripts/publish.ps1 dans installer/work/publish
Source: "work\\publish\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Désinstaller {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Lancer {#AppName}"; Flags: nowait postinstall skipifsilent

