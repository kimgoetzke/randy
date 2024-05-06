using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

using Microsoft.Win32.TaskScheduler;

public static class StartupTaskHandler
{
    private const string TaskName = "RandyStartupTask";
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(StartupTaskHandler));

    public static bool Exists()
    {
        IsUserAdministrator();
        using var taskService = new TaskService();
        var result = false;
        try
        {
            result = taskService.GetTask(TaskName) != null;
        }
        catch (Exception)
        {
            // Ignored
        }

        logger.LogInformation("Startup task task exists: {Result}", result);
        return result;
    }

    public static void Schedule()
    {
        using var taskService = new TaskService();
        var taskDefinition = taskService.NewTask();
        taskDefinition.Settings.RestartCount = 3;
        taskDefinition.Settings.RestartInterval = new TimeSpan(0, 1, 0);
        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
        taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
        taskDefinition.RegistrationInfo.Author = "Kim Goetzke";
        taskDefinition.RegistrationInfo.Description = "Start Randy on startup";
        taskDefinition.Actions.Add(new ExecAction(Application.ExecutablePath, ""));
        var bootTrigger = new BootTrigger();
        var logonTrigger = new LogonTrigger();
        taskDefinition.Triggers.Add(logonTrigger);
        taskDefinition.Triggers.Add(bootTrigger);
        try
        {
            taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
            logger.LogInformation("Startup task registered successfully");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to register task: {Message}", e.Message);
        }
    }

    public static void Cancel()
    {
        using var taskService = new TaskService();

        try
        {
            taskService.RootFolder.DeleteTask(TaskName);
            logger.LogInformation("Startup task cancelled");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to delete task: {Message}", e.Message);
        }
    }

    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
    private static bool IsUserAdministrator()
    {
        try
        {
            var user = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(user);

            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            logger.LogInformation(
                "User {Name} is admin: {IsAdmin}",
                principal.Identity.Name,
                isAdmin
            );
            return isAdmin;
        }
        catch (Exception e)
        {
            logger.LogInformation("Unauthorized access: {Message}", e.Message);
            return false;
        }
    }
}
