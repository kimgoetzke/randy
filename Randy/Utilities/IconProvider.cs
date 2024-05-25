using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

public static class IconProvider
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(IconProvider));

    public static Icon? GetDefaultIconSafely()
    {
        try
        {
            return new Icon(Constants.Path.DefaultIcon);
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
            return new Icon(Constants.Path.ActionIcon);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to load action icon: {Message}", e.Message);
            return null;
        }
    }
}
