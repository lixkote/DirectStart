#define MyAppName "DirectStart"
#define MyAppVersion "3.0 for Windows 8.1"
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
OutputBaseFilename=DirectStart3.0-installer-win81
SetupIconFile=.\install.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
WizardImageFile=.\WizardImage.bmp
MinVersion=6.3

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
  // Windows 8.1 only
  if (GetWindowsVersion < $06030000) or (GetWindowsVersion >= $06040000) then
  begin
    MsgBox(
      'This version of DirectStart is only for Windows 8.1.',
      mbCriticalError,
      MB_OK
    );
    Result := False;
    Exit;
  end;

  // en-US only
  UILang := GetUILanguage;
  if UILang <> 1033 then
  begin
    MsgBox(
      'This version of DirectStart requires Windows 8.1 with en-US (English - United States) system language.',
      mbCriticalError,
      MB_OK
    );
    Result := False;
    Exit;
  end;

  // Windhawk installed
  if not IsWindhawkInstalled() then
  begin
    MsgBox(
      'Windhawk is not installed.'#13#10#13#10 +
      'Please install Windhawk before installing DirectStart.',
      mbCriticalError,
      MB_OK
    );
    Result := False;
    Exit;
  end;

  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    MsgBox(
      'Make sure to install the WHDStart.cpp mod in Windhawk!'#13#10#13#10 +
      'The file has been copied to your Desktop.'#13#10#13#10 +
      'Additionally, install .NET Framework 4.8',
      mbError,
      MB_OK
    );
    
    MsgBox(
      'If you want to use the Windows 8.1 start screen, disable the mod in Windhawk.'#13#10#13#10 +
      'To use the start menu again, enable it.',
      mbInformation,
      MB_OK
    );    
    
    MsgBox(
      'Winaero Colorsync is recommended for automatic metro colorization.'#13#10#13#10 +
      'You can grab it from releases.',
      mbInformation,
      MB_OK
    );
    
    MsgBox(
      'Now, restart your PC and DirectStart will be run automatically.',
      mbInformation,
      MB_OK
    );
  end;
end;




[Files]
Source: "C:\Users\komp\Documents\GitHub\DirectStart\DirectStart\bin\Debug\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\komp\Documents\GitHub\DirectStart\Installer\MenuShortcuts\*"; DestDir: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\komp\Documents\GitHub\DirectStart\Installer\3\WHDStart.cpp"; DestDir: "{commondesktop}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userappdata}\Microsoft\Windows\Start Menu\Programs\Startup\StartMenuLauncher"; Filename: "{autopf}\{#MyAppName}\StartMenu.exe"; 

