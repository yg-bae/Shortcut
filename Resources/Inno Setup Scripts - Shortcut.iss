; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define SourceDir "D:\Shortcut\bin\Release"
#define MyAppVersion() \
   ParseVersion(SourceDir + '\Shortcut.exe', \
   Local[0], Local[1], Local[2], Local[3]), \
   Str(Local[0]) + "." + Str(Local[1]) + "." + Str(Local[2]) + "." + Str(Local[3])

#define MyAppName "Shortcut"
#define MyAppPublisher "Yonggyung Bae"
#define MyAppURL "https://github.com/yg-bae/Shortcut"
#define MyAppExeName "Shortcut.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{ECD9DA9E-8AD5-4CF2-9E30-6F0A3EED4A47}
SourceDir={#SourceDir}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf64}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#SourceDir}
OutputBaseFilename=ShortcutSetup
Compression=lzma
SolidCompression=yes
AppCopyright=Yonggyung Bae
LicenseFile=D:\Shortcut\Resources\License.txt
AppContact=yonggyung.bae@gmail.com
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=D:\Shortcut\Resources\Icon\Shortcut.ico
UninstallDisplayIcon={app}\Shortcut.exe
VersionInfoCompany=Yonggyung Bae
VersionInfoCopyright=Yonggyung Bae
VersionInfoProductName=Shortcut {#MyAppVersion}
UninstallDisplayName=Shortcut {#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "D:\Shortcut\bin\Release\Shortcut.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Shortcut\bin\Release\AutoUpdater.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Shortcut\bin\Release\init_cfg.bin"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Shortcut\bin\Release\Microsoft.WindowsAPICodePack.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Shortcut\bin\Release\Microsoft.WindowsAPICodePack.Shell.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Shortcut\bin\Release\Shortcut.exe"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
