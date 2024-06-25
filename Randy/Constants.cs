namespace Randy;

public static class Constants
{
    public const FormBorderStyle DefaultFormStyle = FormBorderStyle.Fixed3D;
    public const int OneSecondInMs = 1000;
    internal const string ConfigFile = "config.json";
    private const string DefaultIconFile = "randy.ico";
    private const string ActionIconFile = "randy_go.ico";
    private const string LogFile = "console_log.txt";
    private const string DataPath = "data/";
    private const string AssetsPath = "assets/";

    public static class Path
    {
        public static readonly string DefaultIcon = GetPath(AssetsPath) + DefaultIconFile;
        public static readonly string ActionIcon = GetPath(AssetsPath) + ActionIconFile;
        public static readonly string Log = GetPath(DataPath) + LogFile;
        public static readonly string Config = GetPath(DataPath) + ConfigFile;
        public static readonly string DataFolder = GetPath(DataPath);

        private static string GetPath(string path) =>
            Application.ExecutablePath.Replace("Randy.exe", path);
    }
}
