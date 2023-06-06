using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using CliWrap;

[RunInstaller(true)]
public class ServiceInstaller : Installer
{
    
    private const string SERVICE_NAME = "Tooltip Fix Service";
    private const string BINARY_NAME = "Tooltip_Fix.exe";
    public override void Install(IDictionary stateSaver)
    {
        base.Install(stateSaver);
        var serviceDirectoryPath = new DirectoryInfo(AppContext.BaseDirectory).Parent?.EnumerateDirectories().FirstOrDefault(d => d.Name == "Service");

         if (serviceDirectoryPath is null)
         {
             MessageBox.Show("Service Directory not found", SERVICE_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
             return;
         }

         try
         {
             Cli.Wrap("sc")
                 .WithArguments(new[]
                 {
                     "create",
                     SERVICE_NAME,
                     $"binPath={Path.Combine(serviceDirectoryPath.FullName, BINARY_NAME)}",
                     "start=auto"
                 }).ExecuteAsync().GetAwaiter().GetResult();
         
             Cli.Wrap("sc")
                 .WithArguments(new[]
                 {
                     "start",
                     SERVICE_NAME
                 }).ExecuteAsync().GetAwaiter().GetResult();
         }
         catch (Exception e)
         {
             MessageBox.Show(e.Message, SERVICE_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

    }
    
    public override void Uninstall(IDictionary savedState)
    {
        base.Uninstall(savedState);
        
        Cli.Wrap("sc")
             .WithArguments(new[]
             {
                 "stop",
                 SERVICE_NAME
             }).ExecuteAsync().GetAwaiter().GetResult();
         
        Cli.Wrap("sc")
             .WithArguments(new[]
             {
                 "delete",
                 SERVICE_NAME
             }).ExecuteAsync().GetAwaiter().GetResult();
    }

    static void Main()
    {
        
    }
}

// using CliWrap;
//
// const string SERVICE_NAME = "Tooltip Fix Service";
// const string BINARY_NAME = "Tooltip_Fix.exe";
//
// if (args is not {Length: 1})
//     return;
//
// switch (args[0])
// {
//     case "/Install":
//     {
//         var serviceDirectoryPath = new DirectoryInfo(AppContext.BaseDirectory).Parent?.EnumerateDirectories().FirstOrDefault(d => d.Name == "Service");
//
//         if (serviceDirectoryPath is null)
//         {
//             MessageBox.Show("Service Directory not found", SERVICE_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
//             return;
//         }
//
//         await Cli.Wrap("sc")
//             .WithArguments(new[]
//             {
//                 "create", 
//                 SERVICE_NAME, 
//                 $"binPath={Path.Combine(serviceDirectoryPath.FullName, BINARY_NAME)}", 
//                 "start=auto"
//             }).ExecuteAsync();
//         
//         await Cli.Wrap("sc")
//             .WithArguments(new[]
//             {
//                 "start",
//                 SERVICE_NAME
//             }).ExecuteAsync();
//         break;
//     }
//     case "/Uninstall":
//         await Cli.Wrap("sc")
//             .WithArguments(new[]
//             {
//                 "stop",
//                 SERVICE_NAME
//             }).ExecuteAsync();
//     
//         await Cli.Wrap("sc")
//             .WithArguments(new[]
//             {
//                 "delete",
//                 SERVICE_NAME
//             }).ExecuteAsync();
//         break;
// }
//     
