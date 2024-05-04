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
    private readonly ILogger<MainForm> _logger;
    private readonly NotifyIcon _trayIcon;
    private readonly Timer _minimiseTimer = new();
    private readonly Timer _trayIconTimer = new();
    private readonly MainFormHandler _formHandler;
    private readonly Colours _colours = new();
    private bool _canBeVisible;
    private bool _isMinimised;

    public MainForm(ILogger<MainForm> logger)
    {
        InitializeComponent();
        _logger = logger;
        _trayIconTimer.Tag = "Tray icon timer";
        _minimiseTimer.Tag = "Minimise timer";
        var config = new UserSettings();

        // Create invisible form to manage hotkey & behaviour
        var invisibleForm = new InvisibleForm(logger, this, config);
        invisibleForm.RegisterHotKey();

        // Initialise visible main form and minimise it after 1 second
        _formHandler = new MainFormHandler(this, config);
        _formHandler.InitialiseForm();

        // Auto-minimise if not in development environment
        var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";
        _logger.LogInformation("Environment: {E}", env);
        if (env != "Development")
        {
            Minimise();
        }

        // Initialize tray icon & context menu
        _trayIcon = new NotifyIcon { Icon = new Icon(Constants.DefaultIconFile), Visible = true };
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
        _logger.LogInformation("Changing icon temporarily");
        _trayIcon.Icon = new Icon(Constants.ActionIconFile);
        _trayIconTimer.Interval = 1000;
        _trayIconTimer.Tick += (_, _) =>
        {
            ResetTimer(_trayIconTimer);
            _trayIcon.Icon = new Icon(Constants.DefaultIconFile);
        };
        _trayIconTimer.Start();
    }

    private void Minimise()
    {
        _minimiseTimer.Interval = Constants.MinimiseInterval;
        _minimiseTimer.Tick += (_, _) =>
        {
            ResetTimer(_minimiseTimer);
            Minimise(null, EventArgs.Empty);
        };
        _minimiseTimer.Start();
        _logger.LogInformation("Starting timer");
    }

    private void ResetTimer(Timer timer)
    {
        if (!timer.Enabled)
            return;

        _logger.LogInformation("Stopping: {TimerName}", timer.Tag);
        timer.Stop();
        timer.Dispose();
    }

    #region Window state events

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

        _logger.LogInformation("Opening window");
        _canBeVisible = true;
        _isMinimised = false;
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        FormBorderStyle = Constants.DefaultFormStyle;
        Icon = new Icon(Constants.DefaultIconFile);
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

        _logger.LogInformation("Minimising window");
        _isMinimised = true;
        _canBeVisible = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ResetTimer(_minimiseTimer);
        UpdateTrayContextMenu();
        SetVisibleCore(false);
    }

    private void Exit(object? sender, EventArgs e) => Close();

    #endregion
}
