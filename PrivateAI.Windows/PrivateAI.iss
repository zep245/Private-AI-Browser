#define MyAppName "Private AI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Private AI"
#define MyAppExeName "PrivateAI.Windows.exe"

[Setup]
AppId={{8A8B2C6E-2F75-4F13-A8B4-PRIVATEAI001}}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://privateai.example

DefaultDirName={localappdata}\Programs\Private AI
DefaultGroupName=Private AI

OutputDir=installer
OutputBaseFilename=PrivateAI-Setup

Compression=lzma
SolidCompression=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

WizardStyle=modern

PrivilegesRequired=lowest

UninstallDisplayName=Private AI

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; \
    Description: "Create a desktop shortcut"; \
    GroupDescription: "Additional shortcuts:"; \
    Flags: unchecked

[Files]
Source: "bin\Release\net10.0-windows\win-x64\publish\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Private AI"; \
    Filename: "{app}\{#MyAppExeName}"

Name: "{commondesktop}\Private AI"; \
    Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Launch Private AI"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; \
    Name: "{localappdata}\PrivateAI\WebView2"