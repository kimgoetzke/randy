using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

public static class LoggerProvider
{
    internal static ILoggerFactory loggerFactory { private get; set; } = null!;

    internal static ILogger CreateLogger(string name) => loggerFactory.CreateLogger(name);
}
