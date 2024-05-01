using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Randy.Utilities;

public static class RegistryKeyHandler
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(RegistryKeyHandler));
    private const string AutoStartRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Randy";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey);

        if (key == null)
        {
            logger.LogWarning("Could not open autostart registry key: {Key}", AutoStartRegistryKey);
            return false;
        }

        var value = (string?)key.GetValue(AppName);

        if (string.IsNullOrEmpty(value))
        {
            logger.LogInformation("Autostart is not enabled");
            return false;
        }

        logger.LogInformation("Autostart registry key is set to: {State}", value);
        return StringComparer.OrdinalIgnoreCase.Compare(value, Application.ExecutablePath) == 0;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey, true);

        if (key == null)
            return;

        if (enabled)
        {
            key.SetValue(AppName, Application.ExecutablePath);
        }
        else
        {
            key.DeleteValue(AppName, false);
        }

        logger.LogInformation("Set autostart registry key to: {State}", enabled);
    }
}