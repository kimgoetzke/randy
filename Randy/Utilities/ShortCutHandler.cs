using Microsoft.Extensions.Logging;
using Vanara.Windows.Shell;

namespace Randy.Utilities;

public static class ShortCutHandler
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(ShortCutHandler));
    private const string FileName = "Randy.lnk";

    public static bool Exists()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var fullPath = Path.Combine(folder, FileName);
        var result = File.Exists(fullPath);
        logger.LogInformation("Startup shortcut {Outcome} at: {Path}", result ? "exists" : "does NOT exist", fullPath);
        return result;
    }

    public static void Create()
    {
        var startUpFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        logger.LogInformation("Creating startup shortcut...");
        logger.LogInformation(" - in folder: {Destination}", startUpFolder);
        logger.LogInformation(" - linking to: {Target}", Application.ExecutablePath);
        try
        {
            using var lnk = new ShellLink(
                Application.ExecutablePath, "/p", startUpFolder, "Startup link for Randy"
            );
            lnk.RunAsAdministrator = true;
            lnk.Properties.ReadOnly = false;
            lnk.SaveAs(Path.Combine(startUpFolder, FileName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create shortcut");
        }
    }

    public static void Delete()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var fullPath = Path.Combine(folder, FileName);
        try
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted startup shortcut: {Path}", fullPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete shortcut at {Path}", fullPath);
        }
    }
}