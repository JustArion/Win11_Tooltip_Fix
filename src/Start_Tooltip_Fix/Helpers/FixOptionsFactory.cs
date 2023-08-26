namespace Start_Tooltip_Fix;

using System.Diagnostics;
using System.Security.Principal;
using Contracts;

public static class FixOptionsFactory
{
    private const string SERVICE_NAME = "Tooltip Fix";
    private const string BINARY_NAME = "Tooltip_Fix.exe";
    private const string FILE_DESCRIPTION = "Tooltip_Fix";
    public static FixConfigureOptions Create()
    {
        var serviceDirectoryPath = new DirectoryInfo(AppContext.BaseDirectory).EnumerateDirectories().FirstOrDefault(d => d.Name == "Service");
        
        if (serviceDirectoryPath?.GetFiles().FirstOrDefault(x => GetFileDescription(x) == FILE_DESCRIPTION) is null)
        {
            MessageBox.Show($"Service Folder or 'Service/{BINARY_NAME}' was not found. Make sure to extract all files from the .zip and try again", SERVICE_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
        
        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        return new FixConfigureOptions
        {
            Name = SERVICE_NAME,
            ProgramPath = Path.Combine(serviceDirectoryPath.FullName, BINARY_NAME),
            RunAsAdmin = isAdmin
        };
    }

    private static string? GetFileDescription(FileSystemInfo f) => FileVersionInfo.GetVersionInfo(f.FullName).FileDescription;
}