using Microsoft.Extensions.Logging;
using Randy.Core;
using Randy.Utilities;
using Timer = System.Windows.Forms.Timer;

namespace Randy;

public sealed partial class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;
    private readonly NotifyIcon _trayIcon;
    private readonly Timer _minimiseTimer = new();
    private readonly Timer _trayIconTimer = new();
    private readonly MainFormHandler _formHandler;
    private readonly Colours _colours = new();
    private bool _canBeVisible;

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
            Minimise(1000);
        }

        // Register events
        FormClosing += invisibleForm.UnregisterHotKey;

        // Initialize tray icon & context menu
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(Constants.DefaultIconFile),
            Visible = true
        };
        _trayIcon.MouseDoubleClick += Open;
        UpdateTrayContextMenu();
    }

    // Updated based on the current state of the window
    private void UpdateTrayContextMenu()
    {
        _trayIcon.ContextMenuStrip?.Dispose();
        var cm = new ContextMenuStrip();
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

    protected override void SetVisibleCore(bool value)
    {
        value = WindowState switch
        {
            FormWindowState.Minimized when !_canBeVisible => false,
            FormWindowState.Normal when _canBeVisible => true,
            _ => value
        };

        base.SetVisibleCore(value);
    }

    public void ChangeTrayIconTemporarily()
    {
        _logger.LogInformation("Changing icon temporarily");
        _trayIcon.Icon = new Icon(Constants.ActionIconFile);
        _trayIconTimer.Interval = 1000;
        _trayIconTimer.Tick += (_, _) =>
        {
            ResetTimerIfEnabled(_trayIconTimer);
            _trayIcon.Icon = new Icon(Constants.DefaultIconFile);
        };
        _trayIconTimer.Start();
    }

    private void Minimise(int interval)
    {
        _minimiseTimer.Interval = interval;
        _minimiseTimer.Tick += Minimise;
        _minimiseTimer.Start();
        _logger.LogInformation("Starting timer");
    }

    private void ResetTimerIfEnabled(Timer timer)
    {
        if (!timer.Enabled)
            return;

        _logger.LogInformation("Stopping: {TimerName}", timer.Tag);
        timer.Stop();
        timer.Tick -= Minimise;
        timer.Dispose();
    }


    #region Tray icon context menu actions

    private void Open(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Normal)
        {
            _logger.LogInformation("Window is already open");
            return;
        }

        _logger.LogInformation("Opening window");
        _canBeVisible = true;
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        FormBorderStyle = Constants.DefaultFormStyle;
        Icon = new Icon(Constants.DefaultIconFile);
        SetVisibleCore(true);
        Activate();
        _formHandler.SetWindowSizeAndPosition();
        UpdateTrayContextMenu();
    }

    // TODO: Allow minimising from window itself, not just tray icon
    private void Minimise(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            _logger.LogInformation("Window is already minimised");
            return;
        }

        _logger.LogInformation("Minimising window");
        _canBeVisible = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        ResetTimerIfEnabled(_minimiseTimer);
        UpdateTrayContextMenu();
        SetVisibleCore(false);
    }

    private void Exit(object? sender, EventArgs e) => Close();

    #endregion
}