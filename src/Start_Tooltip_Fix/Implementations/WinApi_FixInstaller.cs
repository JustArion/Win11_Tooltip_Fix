namespace Start_Tooltip_Fix.Implementations;

using Contracts;
using Vanara.PInvoke;

[RequireAdmin(RequireAdminType.Admin)]
[Obsolete("This installer type only works with the ASP version of Tooltip_Fix")]
public class WinApi_FixInstaller : IFixInstaller
{
    public void InstallService(FixConfigureOptions options)
    {
        var hSCManager = EnsureSCManagerCreated(options, AdvApi32.ScManagerAccessTypes.SC_MANAGER_CREATE_SERVICE);

        var hService = AdvApi32.CreateService(
            hSCManager,
            options.Name,
            options.Name,
            (uint)AdvApi32.ServiceAccessTypes.SERVICE_ALL_ACCESS,
            AdvApi32.ServiceTypes.SERVICE_WIN32_OWN_PROCESS,
            AdvApi32.ServiceStartType.SERVICE_AUTO_START,
            AdvApi32.ServiceErrorControlType.SERVICE_ERROR_NORMAL,
            options.ProgramPath);
        
        if (hService.IsInvalid)
            Errors.Panic(Win32Error.GetLastError().GetException());

        try
        {
            var successState = AdvApi32.StartService(hService);

            if (!successState)
                Errors.Panic(Win32Error.GetLastError().GetException());
        }
        finally
        {
            AdvApi32.CloseServiceHandle(hService);
        }
    }

    public void UninstallService(FixConfigureOptions options)
    {
        var hSCManager = EnsureSCManagerCreated(options, AdvApi32.ScManagerAccessTypes.SC_MANAGER_ALL_ACCESS);
        
        var hService = AdvApi32.OpenService(hSCManager, options.Name, AdvApi32.ServiceAccessTypes.SERVICE_ALL_ACCESS);
        
        if (hService.IsInvalid)
            Errors.Panic(Win32Error.GetLastError().GetException());

        try
        {
            var successState = AdvApi32.DeleteService(hService);

            if (!successState)
                Errors.Panic(Win32Error.GetLastError().GetException());
        }
        finally
        {
            AdvApi32.CloseServiceHandle(hService);
        }
    }

    public bool IsInstalled(FixConfigureOptions options)
    {
        var hSCManager = OpenServiceControlManager(AdvApi32.ScManagerAccessTypes.SC_MANAGER_CONNECT);

        if (hSCManager.IsInvalid)
            return false;
        
        var hService = AdvApi32.OpenService(hSCManager, options.Name, AdvApi32.ServiceAccessTypes.SERVICE_QUERY_STATUS);

        if (hService.IsInvalid)
            return false;
        try
        {
            var successState = AdvApi32.QueryServiceStatus(hService, out _);

            return successState;
        }
        finally
        {
            AdvApi32.CloseServiceHandle(hService);
        }  
    }

    private AdvApi32.SafeSC_HANDLE EnsureSCManagerCreated(FixConfigureOptions options, AdvApi32.ScManagerAccessTypes desiredAccess)
    {
        var hSCManager = OpenServiceControlManager(desiredAccess);
        
        if (hSCManager.IsInvalid)
            Errors.ThrowUnauthorizedError(options.Name);

        return hSCManager;
    }
    
    private static AdvApi32.SafeSC_HANDLE OpenServiceControlManager(AdvApi32.ScManagerAccessTypes desiredAccess) => AdvApi32.OpenSCManager(null, null, desiredAccess);
}