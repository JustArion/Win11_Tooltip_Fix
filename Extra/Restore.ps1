$ErrorActionPreference = "Stop";

dotnet restore $($PSScriptRoot + "\..\src\Tooltip_Fix");

dotnet restore $($PSScriptRoot + "\..\src\Start_Tooltip_Fix");

Write-Output "Built Files to '$($PSScriptRoot + "\..\src\Output")'";