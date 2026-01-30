#define MyAppName "DirectStart"
#define MyAppVersion "3.0"
#define MyAppPublisher "Lixkote"
#define MyAppURL "https://github.com/Lixkote/DirectStart"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{359C4262-A59B-4407-AFDF-5F468F588DA6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=.\license.txt
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=..\InstallerBin
OutputBaseFilename=DirectStart3.0-installer
SetupIconFile=.\install.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
WizardImageFile=.\WizardImage.bmp
MinVersion=6.1sp1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\DirectStart\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\MenuShortcuts\*"; DestDir: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commondesktop}\Launch DirectStart"; Filename: "{autopf}\{#MyAppName}\DirectStart.exe"; 

