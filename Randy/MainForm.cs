using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Timer = System.Windows.Forms.Timer;

namespace Randy;

public sealed partial class MainForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);
    
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    private const int WindowsKeyModifier = 0x0008;
    private const int WmHotkey = 0x0312; // Windows message for hotkey
    private const int HotkeyId = 1; // ID for the hotkey
    private const int SwMaximize = 3;
    private const uint WmPaint = 0x000F;
    private const int SwShowNormal = 1;
    private const FormBorderStyle DefaultFormStyle = FormBorderStyle.FixedDialog;
    private const string IconFile = "../../../assets/randy.ico";
    // private Rectangle? _previousSize;
    private readonly NotifyIcon _trayIcon;
    private readonly Timer _timer = new();
    private readonly ILogger<MainForm> _logger;

    public MainForm(ILogger<MainForm> logger)
    {
        InitializeComponent();
        _logger = logger;
        
        // Events
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        Minimise(1000);
        
        // Initialize NotifyIcon
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(IconFile),
            Visible = true
        };

        // Create a context menu for tray icon
        var cm = new ContextMenuStrip();
        cm.Items.Add("Open", null, Open);
        cm.Items.Add("Minimise", null, Minimise);
        cm.Items.Add("Exit", null, Exit);
        _trayIcon.ContextMenuStrip = cm;

        // Create form
        CreateDefaultForm();
    }

    private void CreateDefaultForm()
    {
        ShowInTaskbar = true;
        Icon = new Icon(IconFile);
        Text = "Randy";
        BackColor = ColorTranslator.FromHtml("#3B4252");
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(300, 90);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Notifier.SetMessage(this, "Ready for action!");
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var workingArea = Screen.GetWorkingArea(this);
        Location = new Point(workingArea.Right - Size.Width - 20, workingArea.Bottom - Size.Height - 40);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WmHotkey || m.WParam.ToInt32() != HotkeyId)
            return;

        var hWnd = GetForegroundWindow();
        ShowWindow(hWnd, SwMaximize); // Maximize the window

        if (!GetWindowRect(hWnd, out var rect))
        {
            _logger.LogWarning("Failed to get window rect");
            return;
        }
        
        ResetTimerIfEnabled();
        WindowState = FormWindowState.Normal;
        Notifier.SetMessage(this, "Maximise!");
        Minimise(700);
        
        // Calculate new window size
        var newX = rect.Left + 30;
        var newY = rect.Top + 30;
        var newWidth = rect.Right - rect.Left - 60;
        var newHeight = rect.Bottom - rect.Top - 60;

        // Set the new window placement
        var wp = new WindowPlacement
        {
            Length = Marshal.SizeOf(typeof(WindowPlacement)),
            Flags = 0,
            ShowCmd = SwShowNormal,
            PtMaxPosition = new Point(0, 0),
            PtMinPosition = new Point(-1, -1),
            RcNormalPosition = new Rectangle(newX, newY, newX + newWidth, newY + newHeight)
        };

        SetWindowPlacement(hWnd, ref wp);
        SendMessage(hWnd, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
    }
    
    private void Minimise(int interval)
    {
        _timer.Interval = interval;
        _timer.Tick += Minimise;
        _timer.Start();
        _logger.LogInformation("Starting timer");
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        _logger.LogInformation("Registering hotkey and start timer");
        RegisterHotKey(Handle, HotkeyId, WindowsKeyModifier, (uint)Keys.Oem5); // Win + Backslash
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _logger.LogInformation("Unregistering hotkey");
        UnregisterHotKey(Handle, HotkeyId);
    }

    #region Tray icon context menu actions
    private void Open(object? sender, EventArgs e)
    {
        WindowState = FormWindowState.Normal;
        Activate();
    }
    
    private void Minimise(object? sender, EventArgs e)
    {
        ResetTimerIfEnabled();
        _logger.LogInformation("Minimising window");
        WindowState = FormWindowState.Minimized;
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