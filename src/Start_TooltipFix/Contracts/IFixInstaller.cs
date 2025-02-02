namespace Start_TooltipFix.Contracts;

public interface IFixInstaller
{
    public void InstallService(FixConfigureOptions options);

    public void UninstallService(FixConfigureOptions options);

    public bool IsInstalled(FixConfigureOptions options);
}