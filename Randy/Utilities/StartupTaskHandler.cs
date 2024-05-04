using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

using Microsoft.Win32.TaskScheduler;

public static class StartupTaskHandler
{
    private const string TaskName = "RandyStartupTask";
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(StartupTaskHandler));

    public static bool Exists()
    {
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
        taskDefinition.RegistrationInfo.Description = "Start Randy on startup";
        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
        taskDefinition.Actions.Add(new ExecAction(Application.ExecutablePath, ""));
        var trigger = new BootTrigger();
        trigger.Delay = TimeSpan.FromMinutes(1);
        taskDefinition.Triggers.Add(trigger);
        try
        {
            taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
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
        }
        catch (Exception e)
        {
            logger.LogError("Failed to delete task: {Message}", e.Message);
        }
    }
}
