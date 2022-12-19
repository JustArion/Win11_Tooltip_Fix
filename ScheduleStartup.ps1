# Script: ScheduleStartup.ps1
# Author: arion-Kun

# Description: This script will schedule the startup of the ClickThroughPatch script if found with the relevant parameters.

function Query-Path
{
    Write-Host "Please Drag and Drop the Program 'PopupHost_ClickThroughPatch.exe' into the Terminal Window and Press 'Enter'." -ForegroundColor Green
    $path = Read-Host

    # Check if the file exists and ends with the correct name
    while (-not ($(Test-Path -Path $path)) -or -not ($path -match 'PopupHost_ClickThroughPatch.exe$'))
    {
        Write-Host "The file '$path' does not exist or is not the correct file. Please try again." -ForegroundColor Red
        $path = Read-Host
    }
    return $path
}
function Query-Admin
{
    Write-Host "To cancel during any part of the process, Press 'Ctrl + C'" -ForegroundColor Green
    Write-Host

    Write-Host 'Would you like to Start the Patch as Administrator?'
#    sleep 1
    Write-Host 'Starting the Patch as Administrator will allow it to change the Popups of all Apps. '
#    sleep 2
    Write-Host 'An example of this would be Task Manager. (Y/N): ' -NoNewLine
    
    return $($(Read-Host) -eq 'Y') # The 'Y' here can be lower-case or upper-case
}

function Get-Settings
{
    $settings = @{}
    
    $settings['RunAsAdmin'] = Query-Admin
    $settings['Path'] = Query-Path
    
    return $settings
}

$Settings = Get-Settings

$StartAction = New-ScheduledTaskAction -Execute $Settings['Path'].ToString()
$Trigger = New-ScheduledTaskTrigger -AtLogOn


if ($Settings['RunAsAdmin'])
{

    $IsElevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if ($IsElevated)
    {
        Register-ScheduledTask -Action $StartAction -Trigger $Trigger -RunLevel Highest -TaskName 'ClickThroughPatch' -Description 'ClickThroughPatch Startup' -User 'SYSTEM' -Force
    }
    else
    {
        # Start a New powershell with Admin Privileges and pass the current Path to it.

        $commandBlock = {
            param($path)
            Register-ScheduledTask -Action $(New-ScheduledTaskAction -Execute $Path) -Trigger $(New-ScheduledTaskTrigger -AtLogOn) -RunLevel Highest -TaskName 'ClickThroughPatch' -Description 'ClickThroughPatch Startup' -User 'SYSTEM' -Force
        }
        
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = 'powershell.exe'
        # Run the command in the commandBlock with the parameter $Settings['Path']
        $startInfo.Arguments = "-Command `"& { $( $commandBlock ) } -path '$($Settings['Path'])'`""
        $startInfo.Verb = 'runas'
        $startInfo.WindowStyle = 'Hidden'
        $startInfo.UseShellExecute = $true
        $startInfo.CreateNoWindow = $true

        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo

        $process.Start() > $null # This outputs "True" to the console, which is not needed.
        
        $process.WaitForExit()
        
        if ($process.ExitCode -ne 0)
        {
            Write-Host "The Scheduled Task was not created. Please try again." -ForegroundColor Red
            exit -1
        }

        
        
    }

}
else
{
    $StartAction = New-ScheduledTaskAction -Execute $Settings['Path']
    $Trigger = New-ScheduledTaskTrigger -AtLogOn
    Register-ScheduledTask -Action $StartAction -Trigger $Trigger -TaskName 'ClickThroughPatch' -Description 'ClickThroughPatch Startup' -User 'SYSTEM' -Force
}


Write-Host
Write-Host 'The Startup Task has been Scheduled.' -ForegroundColor Green
Write-Host 'To remove the ClickThroughPatch Startup Task run the following command:' -ForegroundColor DarkYellow

Write-Host "    Unregister-ScheduledTask -TaskName 'ClickThroughPatch' -Confirm:$false" -ForegroundColor DarkYellow

Write-Host
Write-Host "If the Command fails with an error similar to" -ForegroundColor DarkGray
Write-Host "Unregister-ScheduledTask: No MSFT_ScheduledTask objects found with property `'TaskName`' equal to `'ClickThroughPatch`'.  Verify the value of the property and retry." -ForegroundColor Gray
Write-Host "You may not have enough permissions to remove the Task or it's already been removed. To get higher permissions, run Powershell as Admin and try again." -ForegroundColor DarkGray
