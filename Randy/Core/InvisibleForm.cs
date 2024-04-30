using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Randy.Core;

/**
 * This form contains the logic for the hotkey. It is permanently invisible.
 */
public class InvisibleForm : Form
{
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

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // General
    private const int SwMaximize = 3;
    private const uint WmPaint = 0x000F;
    private const int SwShowNormal = 1;
    private readonly ILogger _logger;
    private readonly MainForm _mainForm;

    // Hot key
    private const int HotkeyId = 1; // ID for the hotkey
    private const uint WmHotkey = 0x0312; // Windows message for hotkey
    public const uint ModifierKey = 0x0008; // Windows key
    public const uint OtherKey = (uint)Keys.Oem5; // Backslash

    public InvisibleForm(ILogger logger, MainForm mainForm)
    {
        _logger = logger;
        _mainForm = mainForm;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
    }

    public void RegisterHotKey()
    {
        _logger.LogInformation("Registering hotkey");
        RegisterHotKey(Handle, HotkeyId, ModifierKey, OtherKey);
    }

    public void UnregisterHotKey(object? sender, FormClosingEventArgs e)
    {
        _logger.LogInformation("Unregistering hotkey");
        UnregisterHotKey(Handle, HotkeyId);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WmHotkey || m.WParam.ToInt32() != HotkeyId)
        {
            return;
        }

        _logger.LogInformation("Hotkey pressed");

        var window = GetForegroundWindow();
        if (window == _mainForm.Handle)
        {
            _logger.LogWarning("Using hotkey on main form is not allowed, ignoring request");
            return;
        }

        _mainForm.ChangeTrayIconTemporarily();
        ShowWindow(window, SwMaximize); // Maximize the window

        if (!GetWindowRect(window, out var rect))
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
        SetWindowPlacement(window, ref wp);
        SendMessage(window, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
    }
}