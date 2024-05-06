using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Randy.Utilities;

namespace Randy.Core;

/**
 * This form contains the logic for the hotkey. It is permanently invisible but always active.
 */
public class InvisibleForm : Form
{
    private const int SwMaximize = 3;
    private const uint WmPaint = 0x000F;
    private const int SwShowNormal = 1;
    private const int HotkeyId = 1; // ID for the hotkey
    private const uint WmHotkey = 0x0312; // Windows message for hotkey
    private const int ExtraYPadding = 10;
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(InvisibleForm));
    private readonly MainForm _mainForm;
    private readonly UserSettings _userSettings;

    public InvisibleForm(MainForm mainForm, UserSettings userSettings)
    {
        _mainForm = mainForm;
        _userSettings = userSettings;
        InitialiseForm();
        RegisterHotKey();
    }

    private void InitialiseForm()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
    }

    private void RegisterHotKey()
    {
        logger.LogInformation("Registering hotkey");
        NativeApi.RegisterHotKey(
            Handle,
            HotkeyId,
            _userSettings.key.modifierKey,
            _userSettings.key.otherKey
        );
    }

    public void UnregisterHotKey(object? sender, FormClosingEventArgs e)
    {
        logger.LogInformation("Unregistering hotkey");
        NativeApi.UnregisterHotKey(Handle, HotkeyId);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WmHotkey || m.WParam.ToInt32() != HotkeyId)
        {
            return;
        }

        logger.LogInformation("Hotkey pressed");

        var window = NativeApi.GetForegroundWindow();
        if (window == _mainForm.Handle)
        {
            logger.LogWarning("Using hotkey on main form is not allowed, ignoring request");
            return;
        }

        _mainForm.ChangeTrayIconTemporarily();
        NativeApi.ShowWindow(window, SwMaximize); // Maximize the window

        if (!NativeApi.GetWindowRect(window, out var rect))
        {
            logger.LogWarning("Failed to get window rect");
            return;
        }

        // Calculate new window size
        var newX = rect.Left + _userSettings.padding;
        var newY = rect.Top + _userSettings.padding + ExtraYPadding;
        var newWidth = rect.Right - rect.Left - _userSettings.padding * 2;
        var newHeight = rect.Bottom - rect.Top - (_userSettings.padding + ExtraYPadding) * 2;

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

        logger.LogInformation("Updating window placement");
        NativeApi.SetWindowPlacement(window, ref wp);
        NativeApi.SendMessage(window, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
    }
}
