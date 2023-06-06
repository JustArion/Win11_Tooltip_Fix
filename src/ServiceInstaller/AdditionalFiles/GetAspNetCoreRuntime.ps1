$LINK = 'https://download.visualstudio.microsoft.com/download/pr/02d08d3a-c846-40a9-a75d-4dcfa12b2d8d/c9d48b7ce67ad4e1356d9f3630f51cf4/aspnetcore-runtime-7.0.5-win-x64.exe'
$CHECKSUM = '2f62d0033f89473e8fd22b5134fa8a26163b0d66dd9256cfd0ed8ef1eb0ef6e72bbe107e64491c50c322c738ffafa92fdbaedc5f2f3261ba3bfb2060c8261ab0'

$CurrentDirectory = $PSScriptRoot

$DownloadPath = Join-Path -Path $CurrentDirectory -ChildPath 'AspNetCoreRuntime.exe'

if (Test-Path -Path $DownloadPath) {
    Remove-Item -Path $DownloadPath -Force
}

Invoke-WebRequest -Uri $LINK -OutFile $DownloadPath