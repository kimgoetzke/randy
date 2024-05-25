using Microsoft.Extensions.Logging;
using Randy.Utilities;

namespace Randy;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!Directory.Exists(Constants.Path.DataFolder))
        {
            Directory.CreateDirectory(Constants.Path.DataFolder);
        }
        using var logFileWriter = new StreamWriter(Constants.Path.Log, append: false);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("Program", LogLevel.Debug)
                .AddProvider(new TextFileLoggerProvider(logFileWriter))
                .AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    options.SingleLine = true;
                });
        });

        LoggerProvider.loggerFactory = loggerFactory;
        Application.Run(new MainForm());
    }
}
