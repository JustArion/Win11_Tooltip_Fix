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
        try
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

            if (ourTask.State != TaskState.Running)
                ourTask.Run();
        }
        catch (Exception e)
        {
            Errors.Panic(e);
        }

    }
    
    

    private void EditService(Task task, FixConfigureOptions options)
    {
        if (task.State == TaskState.Running)
            task.Stop();
        task.Enabled = true;        
        var definition = task.Definition;
        definition.Settings.Enabled = true;
        
        definition.Principal.RunLevel = options.RunAsAdmin ? TaskRunLevel.Highest : TaskRunLevel.LUA;
        
        var userId = WindowsIdentity.GetCurrent().Name;

        if (definition.Triggers.FirstOrDefault(x => x.TriggerType == TaskTriggerType.Logon) is not LogonTrigger logonTrigger)
            definition.Triggers.Add(new LogonTrigger
            {
                UserId = options.RunAsAdmin ? null : userId,
                Delay = TimeSpan.FromMinutes(1)
            });
        else
        {
                logonTrigger.UserId = options.RunAsAdmin ? null : userId;
                logonTrigger.Delay = TimeSpan.FromMinutes(1);
        }
        

        DoTaskSchedulerEditPatch(task);

        if (definition.Actions.FirstOrDefault(x => x.ActionType == TaskActionType.Execute) is not ExecAction execAction)
        {
            definition.Actions.Add(new ExecAction(options.ProgramPath));
            task.RegisterChanges();
            return;
        }

        execAction.Path = options.ProgramPath;
        
        task.RegisterChanges();

        if (task.State != TaskState.Running)
            task.Run();
    }

    private void DoTaskSchedulerEditPatch(Task task)
    {
        if (task.Definition.Principal.UserId == Environment.UserName)
            task.Definition.Principal.UserId = task.Definition.Principal.Account;
    }

    public void UninstallService(FixConfigureOptions options)
    {
        try
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
        catch (Exception e)
        {
            Errors.Panic(e);
        }

    }

    public bool IsInstalled(FixConfigureOptions options)
    {
        var task = TaskService.Instance.GetTask(options.Name);

        if (task?.Definition.Actions.FirstOrDefault(x => x.ActionType == TaskActionType.Execute) is not ExecAction execAction)
            return false; // Out of Sync - NOT_INSTALLED

        if (!IsExecutionLevelSynced(options, task)) 
            return false; // Out of Sync - NOT_INSTALLED

        if (task.State != TaskState.Running)
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