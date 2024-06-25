using Microsoft.Extensions.Logging;
using Randy.Core;
using Randy.Utilities;
using Timer = System.Windows.Forms.Timer;
using ToolStripRenderer = Randy.Renderers.ToolStripRenderer;

namespace Randy;

/**
 * Main form of the application which is responsible for managing the tray icon and the window state.
 */
public sealed partial class MainForm : Form
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(MainForm));
    private readonly NotifyIcon _trayIcon;
    private readonly Timer _minimiseTimer = new() { Tag = "Minimise timer" };
    private readonly Timer _trayIconTimer = new() { Tag = "Tray icon timer" };
    private readonly MainFormHandler _formHandler;
    private readonly Colours _colours = new();
    private bool _isMinimised;

    public MainForm()
    {
        InitializeComponent();
        var config = ConfigManager.Load();

        // Create invisible form to manage hotkey & all behaviours
        var invisibleForm = new InvisibleForm(this, config);

        // Initialise visible main form that allows configuring settings
        _formHandler = new MainFormHandler(this, config);

        // Auto-minimise main form if not in development environment
        var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";
        logger.LogInformation("Environment: {E}", env);
        if (env != "Development")
        {
            Minimise();
        }

        // Initialize tray icon & context menu
        _trayIcon = new NotifyIcon
        {
            Icon = IconProvider.GetDefaultIconSafely(),
            Visible = true,
            Text = "Randy"
        };
        UpdateTrayContextMenu();

        // Register events
        FormClosing += invisibleForm.UnregisterHotKey;
        Resize += ProcessResizeEvent;
        _trayIcon.MouseDoubleClick += Open;
    }

    // Updated based on the current state of the window
    private void UpdateTrayContextMenu()
    {
        _trayIcon.ContextMenuStrip?.Dispose();
        var cm = new ContextMenuStrip();
        cm.BackColor = _colours.NordDark0;
        cm.ForeColor = _colours.NordBright4;
        cm.ShowImageMargin = false;
        cm.Renderer = new ToolStripRenderer();
        if (WindowState == FormWindowState.Minimized)
        {
            cm.Items.Add("Open", null, Open);
        }
        else
        {
            cm.Items.Add("Minimise", null, Minimise);
        }

        cm.Items.Add("Exit", null, Exit);
        _trayIcon.ContextMenuStrip = cm;
    }

    public void ChangeTrayIconTemporarily()
    {
        logger.LogDebug("Changing icon temporarily");
        ResetTimer(_trayIconTimer);
        _trayIcon.Icon = IconProvider.GetActionIconSafely();
        _trayIconTimer.Interval = Constants.OneSecondInMs;
        _trayIconTimer.Tick += (_, _) =>
        {
            ResetTimer(_trayIconTimer);
            _trayIcon.Icon = IconProvider.GetDefaultIconSafely();
        };
        _trayIconTimer.Start();
    }

    private void Minimise()
    {
        _minimiseTimer.Interval = Constants.OneSecondInMs;
        _minimiseTimer.Tick += (_, _) =>
        {
            ResetTimer(_minimiseTimer);
            Minimise(null, EventArgs.Empty);
        };
        _minimiseTimer.Start();
        logger.LogInformation("Starting: {Name}", _minimiseTimer.Tag);
    }

    private static void ResetTimer(Timer timer)
    {
        if (!timer.Enabled)
            return;

        logger.LogDebug("Stopping: {Name}", timer.Tag);
        timer.Stop();
        timer.Dispose();
    }

    private void ProcessResizeEvent(object? sender, EventArgs e)
    {
        switch (WindowState)
        {
            case FormWindowState.Minimized:
                Minimise(null, EventArgs.Empty);
                _isMinimised = true;
                break;
            case FormWindowState.Normal:
                _isMinimised = false;
                break;
            case FormWindowState.Maximized:
                _isMinimised = false;
                break;
            default:
                return;
        }
    }

    private void Open(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Normal && !_isMinimised)
        {
            return;
        }

        logger.LogInformation("Opening window");
        _isMinimised = false;
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        FormBorderStyle = Constants.DefaultFormStyle;
        Icon = IconProvider.GetDefaultIconSafely();
        UpdateTrayContextMenu();
        SetVisibleCore(true);
        Activate();
        _formHandler.SetWindowSizeAndPosition();
    }

    private void Minimise(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized && _isMinimised)
        {
            return;
        }

        logger.LogInformation("Minimising window");
        _isMinimised = true;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        UpdateTrayContextMenu();
        SetVisibleCore(false);
    }

    private void Exit(object? sender, EventArgs e) => Close();
}
