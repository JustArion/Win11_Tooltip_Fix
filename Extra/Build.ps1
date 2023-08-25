$ErrorActionPreference = "Stop";

dotnet publish $($PSScriptRoot + "\..\src\Tooltip_Fix") --no-restore --runtime win-x86 -p:PublishReadyToRun=true -p:PublishSingleFile=true --output $($PSScriptRoot + "\..\src\Output\Service") --no-self-contained --configuration Release;

dotnet publish $($PSScriptRoot + "\..\src\Start_Tooltip_Fix") --no-restore --runtime win-x86 -p:PublishReadyToRun=true -p:PublishSingleFile=true --output $($PSScriptRoot + "\..\src\Output") --no-self-contained --configuration Release;

Write-Output "Built Files to '$($PSScriptRoot + "\..\src\Output")'";