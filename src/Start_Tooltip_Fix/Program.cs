using Start_Tooltip_Fix;
using Start_Tooltip_Fix.Contracts;
using Start_Tooltip_Fix.Implementations;

var fixInfo = FixConfigureFactory.Create();
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

// We run the commands as admin to provide clarity on what is happening rather than asking the user to run the program as admin themselves.
// ReSharper disable once HeuristicUnreachableCode
const string TwoCommandsMessage = "\n\n" +
                                  "The process will ask for 2 Admin prompts." +
                                  "\n" +
                                  "For more info: https://github.com/JustArion/Win11_Tooltip_Fix/blob/master/SC.md";
bool QueryInstall()
{
    var queryAnswer = MessageBox.Show("Tooltip Fix is not installed, do you want to install it?" + TwoCommandsMessage, fixInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    return queryAnswer == DialogResult.Yes;
}

void Install()
{
    installer.UninstallService(fixInfo);
    MessageBox.Show("Tooltip Fix has been uninstalled", fixInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
}

bool QueryUninstall()
{
    var queryAnswer = MessageBox.Show("Tooltip Fix is already installed, do you want to uninstall it?" + TwoCommandsMessage, fixInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    return queryAnswer == DialogResult.Yes;
}

void Uninstall()
{
    installer.InstallService(fixInfo);
    MessageBox.Show("Tooltip Fix has been installed", fixInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
}