using Start_Tooltip_Fix;
using Start_Tooltip_Fix.Contracts;
using Start_Tooltip_Fix.Implementations;

Interop.FreeConsole(); // Sometimes a ghost-console appears. This is a workaround.
var fixInfo = FixOptionsFactory.Create();
IFixInstaller installer = new TaskScheduler_FixInstaller();
var launchArgs = new LaunchArgs(args);

// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
switch (launchArgs.Hotpath)
{
    case Hotpath.Install:
        Install();
        break;
    case Hotpath.Uninstall:
        Uninstall();
        break;
}


if (installer.IsInstalled(fixInfo))
{
    if (!launchArgs.SkipPrompts)
    {
        var uninstall = QueryUninstall();
    
        if (!uninstall)
            return;
    }
    
    Uninstall();
}

else
{
    if (!launchArgs.SkipPrompts)
    {
        var install = QueryInstall();
    
        if (!install)
            return;
    }
    
    Install();
}

return;

#pragma warning disable CS0162 // Unreachable code detected

bool QueryInstall()
{
    var queryAnswer = MessageBox.Show("Tooltip Fix is not installed, do you want to install it?", fixInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    return queryAnswer == DialogResult.Yes;
}
bool QueryUninstall()
{
    var queryAnswer = MessageBox.Show("Tooltip Fix is already installed, do you want to uninstall it?", fixInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    return queryAnswer == DialogResult.Yes;
}

void Install()
{
    installer.InstallService(fixInfo);
    MessageBox.Show("Tooltip Fix has been installed", fixInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
}
void Uninstall()
{
    installer.UninstallService(fixInfo);
    MessageBox.Show("Tooltip Fix has been uninstalled", fixInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
}