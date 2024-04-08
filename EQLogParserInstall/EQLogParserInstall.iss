; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "EQLogParser"
#define MyAppVersion "2.2.11"
#define MyAppPublisher "Kizant"
#define MyAppURL "https://github.com/kauffman12/EQLogParser"
#define MyAppExeName "EQLogParser.exe"
#define MyReleaseDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser\bin\Release\net8.0-windows10.0.17763.0"
#define MySrcDir "C:\Users\kauff\code\github\EQLogParser\EQLogParser"

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
OutputBaseFilename=EQLogParser-{#MyAppVersion}
SetupIconFile={#MySrcDir}\src\ui\main\EQLogParser.ico
UninstallDisplayIcon={#MySrcDir}\src\ui\main\EQLogParser.ico
Compression=lzma
SolidCompression=yes
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
Source: "{#MyReleaseDir}\runtimes\win\lib\net8.0\System.Speech.dll"; DestDir: "{app}\runtimes\win\lib\net8.0"; Flags: ignoreversion
Source: "{#MyReleaseDir}\data\*"; DestDir: "{app}\data"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
// Event handler for the label click
procedure LabelLinkClick(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.3-windows-x64-installer', '', '', SW_SHOW, ewNoWait, ErrorCode);
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
  LabelLink.Text := 'https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.3-windows-x64-installer';
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
    'Where-Object {$_.Name -eq "EQLogParser"} | ' +
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
  TempFile: string;
  OutputLines: TStringList;
  I: Integer;
  ExitCode: Integer;
begin
  // Assume .NET 8 is not installed
  Result := False;

  // Generate a path for the temp file where the output will be stored
  TempFile := ExpandConstant('{tmp}\dotnet_runtimes.txt');

  // Initialize ExitCode
  ExitCode := 0;

  // Create the batch file dynamically
  CreateBatchFile;

  // Execute the batch file that checks for .NET version
  if Exec(ExpandConstant('{tmp}\CheckDotNetVersion.cmd'), '', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then
  begin
    // Check if the command was successful based on ExitCode
    if ExitCode = 0 then
    begin
      // Check if the output file was created
      if FileExists(TempFile) then
      begin
        OutputLines := TStringList.Create;
        try
          // Load the output from the temp file
          OutputLines.LoadFromFile(TempFile);
          
          // Search each line for the presence of .NET 8
          for I := 0 to OutputLines.Count - 1 do
          begin
            if Pos('Microsoft.WindowsDesktop.App 8.', OutputLines[I]) > 0 then
            begin
              // If found, set Result to True and exit
              Result := True;
              Break;
            end;
          end;
        finally
          OutputLines.Free;
          // Clean up by deleting the temp file
          DeleteFile(TempFile);
        end;
      end;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  PSFilePath, ProductCode: AnsiString;
  ErrorCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Create the PowerShell script that identifies the product code
    PSFilePath := CreatePowerShellScript();

    // Execute the PowerShell script and get the ProductCode as output
    if ExecutePowerShellScriptAndGetOutput(PSFilePath, ProductCode) then
    begin
      ProductCode := Trim(ProductCode);
      if ProductCode <> '' then
      begin
        // Construct and execute the silent uninstall command using the obtained ProductCode
        Exec('msiexec.exe', '/x' + ProductCode + ' /qn', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
      end;
    end;
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

