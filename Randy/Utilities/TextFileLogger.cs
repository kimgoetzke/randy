using Microsoft.Extensions.Logging;

namespace Randy.Utilities;

public class TextFileLogger(string name, TextWriter logFileWriter) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        logFileWriter.WriteLine($"{DateTime.Now} [{logLevel}] [{name}] {message}");
        logFileWriter.Flush();
    }
}
