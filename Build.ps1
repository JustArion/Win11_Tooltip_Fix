$ErrorActionPreference = "Stop";


dotnet publish $(Join-Path $PSScriptRoot "\src\Tooltip_Fix") --runtime win-x86 -p:PublishReadyToRun=true -p:PublishSingleFile=true --output $(Join-Path $PSScriptRoot "\src\Output\Service") --no-self-contained --configuration Release;


dotnet publish $(Join-Path $PSScriptRoot "\src\Start_Tooltip_Fix") --runtime win-x86 -p:PublishReadyToRun=true -p:PublishSingleFile=true --output $(Join-Path $PSScriptRoot "\src\Output") --no-self-contained --configuration Release;

Write-Output "Built Files to '$(Join-Path $PSScriptRoot "\src\Output")'";
