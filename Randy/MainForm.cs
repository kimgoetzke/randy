using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Timer = System.Windows.Forms.Timer;

namespace Randy;

public sealed partial class MainForm : Form
{
    private const int WindowsKeyModifier = 0x0008;
    private const int HotkeyId = 1; // ID for the hotkey
    private const Keys HotKey = Keys.Oem5; // Backslash

    private const FormBorderStyle DefaultFormStyle = FormBorderStyle.FixedDialog;
    private const string IconFile = "../../../assets/randy.ico";
    private readonly NotifyIcon _trayIcon;
    private readonly Timer _timer = new();
    private readonly ILogger<MainForm> _logger;

    public MainForm(ILogger<MainForm> logger)
    {
        InitializeComponent();
        _logger = logger;
        var invisibleForm = new InvisibleForm(logger, this);
        invisibleForm.RegisterHotKey(HotkeyId, WindowsKeyModifier, (uint)HotKey);
        
        // Events
        Resize += MinimiseOnResize;
        FormClosing += invisibleForm.UnregisterHotKey;
        Minimise(1000);
        
        // Initialize tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(IconFile),
            Visible = true
        };
        _trayIcon.MouseDoubleClick += Open;
        UpdateTrayContextMenu();

        // Create form
        CreateDefaultForm();
    }

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

    private void CreateDefaultForm()
    {
        ShowInTaskbar = true;
        Icon = new Icon(IconFile);
        Text = "Randy";
        BackColor = ColorTranslator.FromHtml("#3B4252");
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(500, 200);
        FormBorderStyle = DefaultFormStyle;
        MaximizeBox = false;
        Notifier.SetMessage(this, "Randy is ready for action!");
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var workingArea = Screen.GetWorkingArea(this);
        var centerPoint = new Point(workingArea.Width / 2, workingArea.Height / 2);
        Location = new Point(centerPoint.X - Size.Width / 2, centerPoint.Y - Size.Height / 2);
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

    #region Tray icon context menu actions
    private void Open(object? sender, EventArgs e)
    {
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        FormBorderStyle = DefaultFormStyle;
        Activate();
        UpdateTrayContextMenu();
    }
    
    private void Minimise(object? sender, EventArgs e)
    {
        _logger.LogInformation("Minimising window");
        ResetTimerIfEnabled();
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        UpdateTrayContextMenu();
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

    private void Exit(object? sender, EventArgs e) => Close();
    
    #endregion
}