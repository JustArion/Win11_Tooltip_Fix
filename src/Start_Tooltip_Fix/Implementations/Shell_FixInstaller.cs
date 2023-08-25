namespace Start_Tooltip_Fix.Implementations;

using Contracts;

[RequireAdmin(RequireAdminType.External)]
public class Shell_FixInstaller : IFixInstaller
{
    public void InstallService(FixConfigureOptions options)
    {
        var hResultCreate = ServiceControlExecuteAsAdmin(
            "create",
            Strings.SurroundQuotes(options.Name),
            $"binPath= {Strings.SurroundQuotes(options.ProgramPath)}",
            "start= auto",
            "type= own"
        );
            
        if (hResultCreate == Errors.UNAUTHORIZED)
            Errors.ThrowUnauthorizedError(options.Name);
            
        var hResultStart = ServiceControlExecuteAsAdmin(
            "start",
            Strings.SurroundQuotes(options.Name));

        if (hResultStart == Errors.UNAUTHORIZED)
            Errors.ThrowUnauthorizedError(options.Name);
    }

    public void UninstallService(FixConfigureOptions options)
    {
        var hResultStop = ServiceControlExecuteAsAdmin(
            "stop",
            Strings.SurroundQuotes(options.Name));
            
        if (hResultStop == Errors.UNAUTHORIZED)
            Errors.ThrowUnauthorizedError(options.Name);
            
        var hResultDelete = ServiceControlExecuteAsAdmin(
            "delete",
            Strings.SurroundQuotes(options.Name));
            
        if (hResultDelete == Errors.UNAUTHORIZED)
            Errors.ThrowUnauthorizedError(options.Name);
    }

    public bool IsInstalled(FixConfigureOptions options)
    {
        var hResultQuery = ServiceControlExecuteAsAdmin(
            "query",
            options.Name);

        return hResultQuery != Errors.DOES_NOT_EXIST && hResultQuery != Errors.UNAUTHORIZED;
    }

    private static int ServiceControlExecuteAsAdmin(params string[] arguments) => Interop.ShellExecuteW(IntPtr.Zero, "runas", "sc", string.Join(" ", arguments), null, Interop.SW_HIDE);

}