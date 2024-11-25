; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "EQLogParser"
#define MyAppVersion "2.2.54"
#define MyAppPublisher "Kizant"
#define MyAppURL "https://github.com/kauffman12/EQLogParser"
#define MyAppExeName "EQLogParser.exe"
#define MyReleaseDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser\bin\Release\net8.0-windows10.0.17763.0"
;#define MyReleaseDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser\bin\x64\Debug\net8.0-windows10.0.17763.0"
#define MySrcDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser"
#define BackupUtilDir "C:\Users\kauff\code\github\EQLogParser\BackupUtil\bin\Release\net8.0-windows10.0.17763.0"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
AppId={{EBB73706-893E-4CD4-96D7-FE2E864EE327}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\{#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=yes
InfoBeforeFile={#MyReleaseDir}\data\releasenotes.rtf
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputBaseFilename=EQLogParser-install-{#MyAppVersion}
SetupIconFile={#MySrcDir}\src\ui\main\EQLogParser.ico
MinVersion=10.0
UninstallDisplayIcon={#MySrcDir}\src\ui\main\EQLogParser.ico
Compression=lzma
SolidCompression=yes
SignTool=signtool
WizardImageFile=background.bmp
WizardSmallImageFile=graphic.bmp
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyReleaseDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\AutoMapper.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\DotLiquid.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\EQLogParser.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\EQLogParser.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\EQLogParser.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\EQLogParser.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\FontAwesome5.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\FontAwesome5.Net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\LiteDB.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\log4net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Microsoft.WindowsAPICodePack.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Microsoft.WindowsAPICodePack.Shell.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Microsoft.Windows.SDK.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\NAudio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\NAudio.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\NAudio.Wasapi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\NAudio.WinMM.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\SoundTouch.Net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\SoundTouch.Net.NAudioSupport.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Compression.Base.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Data.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.DocIO.Base.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Edit.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.GridCommon.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Licensing.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.OfficeChart.Base.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.PropertyGrid.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfBusyIndicator.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfChart.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfGrid.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfGridCommon.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfInput.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfProgressBar.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfRichTextBoxAdv.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfSkinManager.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.SfTreeView.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Shared.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Themes.MaterialDark.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Themes.MaterialDarkCustom.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Themes.MaterialLight.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Tools.WPF.Classic.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\Syncfusion.Tools.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\System.Drawing.Common.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\WinRT.Runtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyReleaseDir}\data\*"; DestDir: "{app}\data"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BackupUtilDir}\BackupUtil.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BackupUtilDir}\BackupUtil.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BackupUtilDir}\BackupUtil.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BackupUtilDir}\BackupUtil.deps.json"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
// Delete old logs
procedure DeleteLogFiles;
var
  LogDir: string;
  FindRec: TFindRec;
begin
  Log('Delete Old Log Files');
  // Specify the directory containing the log files
  LogDir := ExpandConstant('{userappdata}\EQLogParser\logs');

  // Find and delete all .log files
  if FindFirst(LogDir + '\*.log', FindRec) then
  begin
    repeat
      Log('Deleting ' + FindRec.Name)
      DeleteFile(LogDir + '\' + FindRec.Name);
    until not FindNext(FindRec);
    FindClose(FindRec);
  end;

  // Find and delete all .log.XXXX-XX-XX files
  if FindFirst(LogDir + '\*.log.*', FindRec) then
  begin
    repeat
      Log('Deleting ' + FindRec.Name)
      DeleteFile(LogDir + '\' + FindRec.Name);
    until not FindNext(FindRec);
    FindClose(FindRec);
  end;
end;

// Event handler for the label click
procedure LabelLinkClick(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.10-windows-x64-installer', '', '', SW_SHOW, ewNoWait, ErrorCode);
end;

function ShowDotNetDownloadPage: Boolean;
var
  Form: TSetupForm;
  InfoLabel: TLabel;
  LabelLink: TMemo;
  OkButton: TButton;
begin
  // Create the form
  Form := CreateCustomForm;
  Form.ClientWidth := ScaleX(358);
  Form.ClientHeight := ScaleY(118);
  Form.Font.Size := 10
  Form.Caption := 'Additional Components Required';
  Form.Position := poScreenCenter;

  // Create an informational label
  InfoLabel := TLabel.Create(Form);
  InfoLabel.Parent := Form;
  InfoLabel.Caption := 'EQLogParser requires .NET 8.0 x64 Desktop Runtime. Please install ' + #13#10 +
  'before continuing. A recent version can be found here:';
  InfoLabel.Font.Size := 9;
  InfoLabel.Top := ScaleY(10);
  InfoLabel.Left := ScaleX(15);
  InfoLabel.AutoSize := True;
  InfoLabel.WordWrap := True; // Enable word wrapping

  // Create a clickable label for the link
  LabelLink := TMemo.Create(Form);
  LabelLink.Parent := Form;
  LabelLink.Text := 'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.10-windows-x64-installer';
  LabelLink.Font.Style := [fsUnderline];
  LabelLink.Font.Color := clBlue;
  LabelLink.Font.Size := 8;
  LabelLink.ReadOnly := True;
  LabelLink.ScrollBars := ssNone;
  LabelLink.Cursor := crHand;
  LabelLink.OnClick := @LabelLinkClick;
  LabelLink.Top := InfoLabel.Top + InfoLabel.Height + ScaleY(14);
  LabelLink.Left := ScaleX(15);
  LabelLink.Height := ScaleY(40);
  LabelLink.Width := ScaleX(400);

  // Create an OK button
  OkButton := TButton.Create(Form);
  OkButton.Parent := Form;
  OkButton.Caption := 'Continue';
  OkButton.ModalResult := mrOk; // Sets the button to close the form when clicked
  OkButton.Top := ScaleY(100);
  OkButton.Left := ScaleX(342); // Center the button
  OkButton.Width := ScaleX(80);
  OkButton.Height := ScaleY(24);

  // Show the form
  Result := (Form.ShowModal = mrOk);
end;

procedure CreateBatchFile;
var
  CmdFileName: string;
  BatchCommands: TStringList;
begin
  Log('Creating cmd file for checking dotnet version')
  // Determine the full path to the batch file in the temporary directory
  CmdFileName := ExpandConstant('{tmp}\CheckDotNetVersion.cmd');
  
  // Initialize TStringList and add the commands
  BatchCommands := TStringList.Create;
  try
    BatchCommands.Add('@echo off');
    BatchCommands.Add('dotnet --list-runtimes > "' + ExpandConstant('{tmp}\dotnet_runtimes.txt') + '"');
    
    Log('Saving dotnet versions to file')
    // Write the commands to the batch file
    BatchCommands.SaveToFile(CmdFileName);
  finally
    BatchCommands.Free;
  end;
end;

function IsDotNet8Installed: Boolean;
var
  FindResult: TFindRec;
  Path: string;
begin
  Log('Checking if dotnet is installed')
  Result := False;
  // Construct the path to the .NET shared directory for x64 installations
  Path := ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\');
  // Check if the directory exists and iterate
  if FindFirst(Path + '*', FindResult) then
  begin
    repeat
      Log('Checking against ' + FindResult.Name)
      // Check if the found item is a directory and starts with '8'
      if (FindResult.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and
         (Pos('8', FindResult.Name) = 1) then
      begin
        Log('dotnet found')
        Result := True;
        Break;  // Found a directory, no need to continue
      end;
    until not FindNext(FindResult);
    FindClose(FindResult);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  // Run the log file deletion at the end of the installation
  if CurStep = ssPostInstall then
  begin
    DeleteLogFiles;
  end;
end;

function InitializeSetup: Boolean;
begin
  // Check if .NET 8 is installed
  if not IsDotNet8Installed then
  begin
    Log('dotnet version not found. showing error dialog')
    Result := ShowDotNetDownloadPage;
  end
  else
    // Proceed with the new installation
    Result := True;
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

