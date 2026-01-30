#define MyAppName "DirectStart"
#define MyAppVersion "3.0 for Windows 10"
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
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\InstallerOutput
OutputBaseFilename=DirectStart3.0-installer-win10
SetupIconFile=.\install.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
WizardImageFile=.\WizardImage.bmp
MinVersion=6.1sp1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Code]
function IsWindhawkInstalled(): Boolean;
begin
  Result :=
    FileExists(ExpandConstant('{autopf}\Windhawk\windhawk.exe')) or
    FileExists(ExpandConstant('{pf}\Windhawk\windhawk.exe'));
end;

function InitializeSetup(): Boolean;
var
  UILang: Cardinal;
begin
  // Block Windows 8.1 only
  if (GetWindowsVersion >= $06030000) and (GetWindowsVersion < $06040000) then
  begin
    MsgBox(
      'Please use the appropriate version for Windows 8.1.'#13#10#13#10 +
      'This version is for Windows 10 and other systems only!',
      mbCriticalError,
      MB_OK
    );
    Result := False;
    Exit;
  end;
  // en-US recommended
  UILang := GetUILanguage;
  if UILang <> 1033 then
  begin
    MsgBox(
      'Using en-US (English - United States) system language is recommended for better experience',
      mbError,
      MB_OK
    );    
  end;
  MsgBox(
    'DirectStart offers the best experience on Windows 8.1'#13#10#13#10 +
    'Please consider using it.',
    mbInformation,
      MB_OK
    );   
        Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin    
    MsgBox(
      'Please restart your PC and DirectStart will be run automatically.',
      mbInformation,
      MB_OK
    );
  end;
end;

[Files]
Source: "C:\Users\komp\Documents\GitHub\DirectStart\DirectStart\bin\Debug\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\komp\Documents\GitHub\DirectStart\Installer\MenuShortcuts\*"; DestDir: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userappdata}\Microsoft\Windows\Start Menu\Programs\Startup\StartMenuLauncher"; Filename: "{autopf}\{#MyAppName}\StartMenu.exe"; 

