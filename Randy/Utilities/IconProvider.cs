using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

public static class IconProvider
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(IconProvider));

    public static Icon? GetDefaultIconSafely()
    {
        try
        {
            return new Icon(
                Application.ExecutablePath.Replace("Randy.exe", "") + Constants.DefaultIconFile
            );
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to load default icon: {Message}", e.Message);
            return null;
        }
    }

    public static Icon? GetActionIconSafely()
    {
        try
        {
            return new Icon(
                Application.ExecutablePath.Replace("Randy.exe", "") + Constants.ActionIconFile
            );
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to load action icon: {Message}", e.Message);
            return null;
        }
    }
}
