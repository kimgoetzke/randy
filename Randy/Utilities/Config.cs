using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

public class Config
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(RegistryKeyHandler));
    private int _padding = 30;

    public int padding
    {
        get => _padding;
        set
        {
            logger.LogInformation("Padding set to: {Padding}", value);
            _padding = value;
        }
    }
}