using Microsoft.Extensions.Logging;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Randy.Utilities;

public class ShortCutHandler(ILogger logger)
{
    public void Create()
    {
        logger.LogInformation("Creating startup shortcut for Randy");
        try
        {
            const string fileName = "Randy.lnk";
            var destination = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var targetItem = Shell32.SHCreateItemFromParsingName<Shell32.IShellItem>(Application.ExecutablePath);
            logger.LogInformation("Destination: {D}", destination);
            logger.LogInformation("Target name: {T}", Application.ExecutablePath);
            logger.LogInformation("Target item: {T}", targetItem);

            var shellLink = new ShellLink(Application.ExecutablePath);
            shellLink.Description = "Startup link for Randy";
            shellLink.WorkingDirectory = destination;
            shellLink.SaveAs(Path.Combine(destination, fileName));
        }
        catch (Exception ex)
        {
         logger.LogError(ex, "Failed to create shortcut");   
        }
    }
}