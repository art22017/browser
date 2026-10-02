; This Source Code Form is subject to the terms of the Mozilla Public
; License, v. 2.0. https://mozilla.org/MPL/2.0/
[Setup]
AppId={{68B08056-9275-49CC-AEE6-356764CC347A}
AppName=Argon
AppVersion=0.1.0
AppPublisher=Argon
AppPublisherURL=https://github.com/art22017/browser
DefaultDirName={localappdata}\Programs\Argon
DefaultGroupName=Argon
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\dist
OutputBaseFilename=Argon-0.1.0-alpha-windows-x64-setup
SetupIconFile=..\..\configs\branding\release\firefox.ico
UninstallDisplayIcon={app}\Argon.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Files]
Source: "..\staging\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Argon"; Filename: "{app}\Argon.exe"
Name: "{autodesktop}\Argon"; Filename: "{app}\Argon.exe"

[Run]
Filename: "{app}\Argon.exe"; Description: "Open Argon"; Flags: nowait postinstall skipifsilent
