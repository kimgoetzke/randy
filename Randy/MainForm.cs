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
        Load += RegisterHotKey;
        FormClosing += UnregisterHotKey;
        Resize += MinimiseOnResize;
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

        _logger.LogInformation("Updating window placement");
        SetWindowPlacement(hWnd, ref wp);
        SendMessage(hWnd, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
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

    private void RegisterHotKey(object? sender, EventArgs e)
    {
        _logger.LogInformation("Registering hotkey and start timer");
        RegisterHotKey(Handle, HotkeyId, WindowsKeyModifier, (uint)Keys.Oem5); // Win + Backslash
    }

    private void UnregisterHotKey(object? sender, FormClosingEventArgs e)
    {
        _logger.LogInformation("Unregistering hotkey");
        UnregisterHotKey(Handle, HotkeyId);
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