; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "EQLogParser"
#define MyAppVersion "2.2.38"
#define MyAppPublisher "Kizant"
#define MyAppURL "https://github.com/kauffman12/EQLogParser"
#define MyAppExeName "EQLogParser.exe"
#define MyReleaseDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser\bin\Release\net8.0-windows10.0.17763.0"
; #define MyReleaseDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser\bin\x64\Debug\net8.0-windows10.0.17763.0"
#define MySrcDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser"
#define BackupUtilDir "C:\Users\kauff\code\github\EQLogParser\BackupUtil\bin\Release\net8.0-windows10.0.17763.0"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
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
  // Specify the directory containing the log files
  LogDir := ExpandConstant('{userappdata}\EQLogParser\logs');

  // Find and delete all .log files
  if FindFirst(LogDir + '\*.log', FindRec) then
  begin
    repeat
      DeleteFile(LogDir + '\' + FindRec.Name);
    until not FindNext(FindRec);
    FindClose(FindRec);
  end;

  // Find and delete all .log.XXXX-XX-XX files
  if FindFirst(LogDir + '\*.log.*', FindRec) then
  begin
    repeat
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
  ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.7-windows-x64-installer', '', '', SW_SHOW, ewNoWait, ErrorCode);
end;

procedure ShowDotNetDownloadPage;
var
  Form: TSetupForm;
  InfoLabel: TLabel;
  LabelLink: TMemo;
  OkButton: TButton;
begin
  // Create the form
  Form := CreateCustomForm;
  Form.ClientWidth := 358;
  Form.ClientHeight := 118; // Adjusted for extra text
  Form.Font.Size := 10
  Form.Caption := 'Additional Components Required';
  Form.Position := poScreenCenter;

  // Create an informational label
  InfoLabel := TLabel.Create(Form);
  InfoLabel.Parent := Form;
  InfoLabel.Caption := 'EQLogParser requires .NET 8.0 x64 Desktop Runtime. Found here:';
  InfoLabel.Font.Size := 9;
  InfoLabel.Top := 18;
  InfoLabel.Left := 15;
  InfoLabel.AutoSize := True;
  InfoLabel.WordWrap := True; // Enable word wrapping

  // Create a clickable label for the link
  LabelLink := TMemo.Create(Form);
  LabelLink.Parent := Form;
  LabelLink.Text := 'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.7-windows-x64-installer';
  LabelLink.Font.Style := [fsUnderline];
  LabelLink.Font.Color := clBlue;
  LabelLink.Font.Size := 8;
  LabelLink.ReadOnly := True;
  LabelLink.ScrollBars := ssNone;
  LabelLink.Cursor := crHand;
  LabelLink.OnClick := @LabelLinkClick;
  LabelLink.Top := InfoLabel.Top + InfoLabel.Height + 10; // Position below the informational text
  LabelLink.Left := 15;
  LabelLink.height := 40;
  LabelLink.Width := 400;

  // Create an OK button
  OkButton := TButton.Create(Form);
  OkButton.Parent := Form;
  OkButton.Caption := 'Exit';
  OkButton.ModalResult := mrOk; // Sets the button to close the form when clicked
  OkButton.Top := 100;
  OkButton.Left := 342; // Center the button

  // Show the form
  Form.ShowModal;
end;

function CreatePowerShellScript(): string;
var
  PSFileName: string;
  ScriptContent: string;
begin
  // Define the path of the PowerShell script file
  PSFileName := ExpandConstant('{tmp}\FindProductCode.ps1');

  // Prepare the PowerShell script content
  ScriptContent :=
    'Get-WmiObject -Class Win32_Product | ' +
    'Where-Object {$_.Name -match "EQLogParser"} | ' +
    'Select-Object -Property IdentifyingNumber | ' +
    'ForEach-Object { Write-Output $_.IdentifyingNumber }';

  // Write the script to the file
  SaveStringToFile(PSFileName, ScriptContent, False);

  // Return the path of the PowerShell script file
  Result := PSFileName;
end;

function ExecutePowerShellScriptAndGetOutput(PSFilePath: string; var Output: AnsiString): Boolean;
var
  OutputFile: string;
  ErrorCode: Integer;
begin
  // Initialize the result as false
  Result := False;

  // Define the output file path
  OutputFile := ExpandConstant('{tmp}\UninstallString.txt');

  // Construct the PowerShell command
  // Note: Using 'Exec' with 'cmd.exe /C' to wait for PowerShell script completion and redirect output to a file
  if Exec('cmd.exe', '/C powershell.exe -ExecutionPolicy Bypass -File "' + PSFilePath + '" > "' + OutputFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode) then
  begin
    // Check if the output file exists and is not empty
    if FileExists(OutputFile) then
    begin
      // Attempt to load the output from the file
      if LoadStringFromFile(OutputFile, Output) then
      begin
        // Check if Output is not empty
        if Output <> '' then
        begin
          Result := True; // Successfully loaded and output is not empty
        end;
      end;
      
      // Clean up: Delete the output file after reading it
      DeleteFile(OutputFile);
    end;
  end;
end;

procedure CreateBatchFile;
var
  CmdFileName: string;
  BatchCommands: TStringList;
begin
  // Determine the full path to the batch file in the temporary directory
  CmdFileName := ExpandConstant('{tmp}\CheckDotNetVersion.cmd');
  
  // Initialize TStringList and add the commands
  BatchCommands := TStringList.Create;
  try
    BatchCommands.Add('@echo off');
    BatchCommands.Add('dotnet --list-runtimes > "' + ExpandConstant('{tmp}\dotnet_runtimes.txt') + '"');
    
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
  Result := False;
  // Construct the path to the .NET shared directory for x64 installations
  Path := ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\');
  // Check if the directory exists and iterate
  if FindFirst(Path + '*', FindResult) then
  begin
    repeat
      // Check if the found item is a directory and starts with '8'
      if (FindResult.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and
         (Pos('8', FindResult.Name) = 1) then
      begin
        Result := True;
        Break;  // Found a directory, no need to continue
      end;
    until not FindNext(FindResult);
    FindClose(FindResult);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  PSFilePath, Output, ProductCode: AnsiString;
  ErrorCode: Integer;
  Lines: TStringList;
  i: Integer;  // Index for looping over the lines
begin
  if CurStep = ssInstall then
  begin
    PSFilePath := CreatePowerShellScript();

    // Execute the PowerShell script and load the output into 'Output'
    if ExecutePowerShellScriptAndGetOutput(PSFilePath, Output) then
    begin
      Lines := TStringList.Create;
      try
        Lines.Text := Output; // Converts the entire output into a list of lines
        // Iterate through each line, assuming each line is a potential ProductCode
        for i := 0 to Lines.Count - 1 do
        begin
          ProductCode := Trim(Lines[i]);  // Access each line using an index
          if ProductCode <> '' then
          begin
            // Construct and execute the silent uninstall command for each ProductCode
            Exec('msiexec.exe', '/x' + ProductCode + ' /qn', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
          end;
        end;
      finally
        Lines.Free;
      end;
    end;
  end;
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
    ShowDotNetDownloadPage;
    Result := False;
  end
  else
    // Proceed with the new installation
    Result := True;
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

