using Microsoft.Extensions.Logging;
using Timer = System.Windows.Forms.Timer;

namespace Randy;

public sealed partial class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;
    private readonly NotifyIcon _trayIcon;
    private readonly Timer _timer = new();
    private readonly Size _initialSize;
    private bool _canBeVisible;

    public MainForm(ILogger<MainForm> logger)
    {
        InitializeComponent();
        _logger = logger;
        
        // Create invisible form to manage hotkey & behaviour
        var invisibleForm = new InvisibleForm(logger, this);
        invisibleForm.RegisterHotKey();

        // Initialise visible main form and minimise it after 1 second
        var formHandler = new MainFormHandler(logger, this);
        formHandler.InitialiseForm();
        Minimise(1000);

        // Register events
        Resize += MinimiseOnResize;
        FormClosing += invisibleForm.UnregisterHotKey;

        // Initialize tray icon & context menu
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(Constants.DefaultIconFile),
            Visible = true
        };
        _trayIcon.MouseDoubleClick += Open;
        UpdateTrayContextMenu();
        
        // Set window size
        var workingArea = Screen.GetWorkingArea(this);
        var centerPoint = new Point(workingArea.Width / 2, workingArea.Height / 2);
        Location = new Point(centerPoint.X - Size.Width / 2, centerPoint.Y - Size.Height / 2);
        _initialSize = Size;
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
        if (WindowState == FormWindowState.Minimized && !_canBeVisible)
        {
            value = false;
        }

        base.SetVisibleCore(value);
    }

    public void ChangeTrayIconTemporarily()
    {
        _logger.LogInformation("Changing icon temporarily");
        _trayIcon.Icon = new Icon(Constants.ActionIconFile);
        _timer.Interval = 1000;
        _timer.Tick += (_, _) =>
        {
            ResetTimerIfEnabled();
            _trayIcon.Icon = new Icon(Constants.DefaultIconFile);
        };
        _timer.Start();
    }

    private void MinimiseOnResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Minimise(null, EventArgs.Empty);
        }
    }

    private void Minimise(int interval)
    {
        _timer.Interval = interval;
        _timer.Tick += Minimise;
        _timer.Start();
        _logger.LogInformation("Starting timer");
    }

    private void ResetTimerIfEnabled()
    {
        if (!_timer.Enabled)
            return;

        _logger.LogInformation("Stopping timer");
        _timer.Stop();
        _timer.Tick -= Minimise;
        _timer.Dispose();
    }


    #region Tray icon context menu actions

    private void Open(object? sender, EventArgs e)
    {
        _logger.LogInformation("Opening window");
        _canBeVisible = true;
        SetVisibleCore(true);
        Size = _initialSize;
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        FormBorderStyle = Constants.DefaultFormStyle;
        Activate();
        UpdateTrayContextMenu();
    }

    private void Minimise(object? sender, EventArgs e)
    {
        _logger.LogInformation("Minimising window");
        _canBeVisible = false;
        ResetTimerIfEnabled();
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        UpdateTrayContextMenu();
    }

    private void Exit(object? sender, EventArgs e) => Close();

    #endregion
}