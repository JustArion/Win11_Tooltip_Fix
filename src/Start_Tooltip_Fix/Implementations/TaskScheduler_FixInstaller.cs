namespace Start_Tooltip_Fix.Implementations;

using System.Diagnostics;
using System.Security.Principal;
using Contracts;
using Microsoft.Win32.TaskScheduler;

[RequireAdmin(RequireAdminType.None)]
public class TaskScheduler_FixInstaller : IFixInstaller
{
    public void InstallService(FixConfigureOptions options)
    {
        using var task = TaskService.Instance.GetTask(options.Name);
        if (task != null)
        {
            EditService(task, options);
            return;
        }
        
        using var taskDefinition = TaskService.Instance.NewTask();
        taskDefinition.Actions.Add(new ExecAction(options.ProgramPath));
        taskDefinition.Principal.RunLevel = options.RunAsAdmin ? TaskRunLevel.Highest : TaskRunLevel.LUA;
        var userId = WindowsIdentity.GetCurrent().Name;
        taskDefinition.Triggers.Add(new LogonTrigger
        {
            UserId = options.RunAsAdmin ? null : userId,
            Delay = TimeSpan.FromMinutes(1)
        });
        
        TaskService.Instance.RootFolder.RegisterTaskDefinition(options.Name, taskDefinition);
        
        var ourTask = TaskService.Instance.GetTask(options.Name);

        if (ourTask == null)
            Errors.Panic(new Exception("Failed to create task."));

        Debug.Assert(ourTask != null, nameof(ourTask) + " != null");
        ourTask.Enabled = true;
        ourTask.RegisterChanges();
    }
    
    

    private void EditService(Task task, FixConfigureOptions options)
    {
        task.Enabled = true;
        task.Definition.Settings.Enabled = true;
        
        task.Definition.Principal.RunLevel = options.RunAsAdmin ? TaskRunLevel.Highest : TaskRunLevel.LUA;

        DoTaskSchedulerEditPatch(task);

        if (task.Definition.Actions.FirstOrDefault(x => x.ActionType == TaskActionType.Execute) is not ExecAction execAction)
        {
            task.Definition.Actions.Add(new ExecAction(options.ProgramPath));
            task.RegisterChanges();
            return;
        }

        if (execAction.Path == options.ProgramPath) 
            return;
        
        execAction.Path = options.ProgramPath;
        task.RegisterChanges();
    }

    private void DoTaskSchedulerEditPatch(Task task)
    {
        if (task.Definition.Principal.UserId == Environment.UserName)
            task.Definition.Principal.UserId = task.Definition.Principal.Account;
    }

    public void UninstallService(FixConfigureOptions options)
    {
        using var task = TaskService.Instance.GetTask(options.Name);

        if (task == null)
            return;
        
        var isSynced = IsExecutionLevelSynced(options, task);
        var isAdmin = options.RunAsAdmin;

        if (!isSynced && !isAdmin)
            ElevateRequest(Hotpath.Uninstall);
        
        // We should now be able to delete this task, seeing as we are either admin or the task is not admin.
        TaskService.Instance.RootFolder.DeleteTask(options.Name);
    }

    public bool IsInstalled(FixConfigureOptions options)
    {
        var task = TaskService.Instance.GetTask(options.Name);

        if (task?.Definition.Actions.FirstOrDefault(x => x.ActionType == TaskActionType.Execute) is not ExecAction execAction)
            return false; // Out of Sync - NOT_INSTALLED

        if (!IsExecutionLevelSynced(options, task)) 
            return false; // Out of Sync - NOT_INSTALLED
        
        // [true] INSTALLED || [false] Out of Sync - NOT_INSTALLED
        return execAction.Path == options.ProgramPath;
        
    }

    /// <summary>
    /// If the Process is Admin, and the Task is not set to Admin, it is out of sync.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="task"></param>
    /// <returns></returns>
    private static bool IsExecutionLevelSynced(FixConfigureOptions options, Task task)
    {
        if (options.RunAsAdmin)
        {
            if (task.Definition.Principal.RunLevel == TaskRunLevel.Highest)
                return true;
        }
        else
        {
            if (task.Definition.Principal.RunLevel == TaskRunLevel.LUA)
                return true;
        }

        return false;
    }

    private static void ElevateRequest(Hotpath path)
    {
        var pInfo = new ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException(),
            Verb = "runas",
        }.WithArgs(new LaunchArgs
        {
            Hotpath = path,
            SkipPrompts = true
        });
        
        Process.Start(pInfo);
        Environment.Exit(0);
    }
}