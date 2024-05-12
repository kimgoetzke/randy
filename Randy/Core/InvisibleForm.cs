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
    private readonly Dictionary<string, WindowPlacement> _knownWindows = new();

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

        var window = NativeApi.GetForegroundWindow();
        var previousPlacementKnown = _knownWindows.ContainsKey(window.ToString());
        logger.LogInformation("Hotkey pressed for window #{Window}", window.ToString());

        if (window == _mainForm.Handle)
        {
            logger.LogInformation("Using hotkey on main form is not allowed, ignoring request");
            return;
        }

        // Get current window placement
        var placement = new WindowPlacement();
        NativeApi.GetWindowPlacement(window, ref placement);

        if (IsNearMaximized(placement, window))
        {
            if (previousPlacementKnown)
            {
                RestorePreviousPlacement(window);
                return;
            }

            logger.LogInformation("No previous placement found for #{Window}", window.ToString());
            NearMaximiseWindow(window);
            return;
        }

        AddOrUpdatePreviousPlacement(previousPlacementKnown, window, placement);
        NearMaximiseWindow(window);
    }

    private bool IsNearMaximized(WindowPlacement placement, IntPtr window)
    {
        var area = Screen.FromHandle(window).WorkingArea;
        var expectedX = _userSettings.padding;
        var expectedY = _userSettings.padding + ExtraYPadding;
        var expectedWidth = area.Width - _userSettings.padding;
        var expectedHeight = area.Height - _userSettings.padding;
        var result =
            placement.RcNormalPosition.Left == expectedX
            && placement.RcNormalPosition.Top == expectedY
            && placement.RcNormalPosition.Width == expectedWidth
            && placement.RcNormalPosition.Height == expectedHeight;

        logger.LogDebug("Working area: {X}x{Y}", area.Width, area.Height);
        logger.LogInformation(
            "#{Window} {Result} near-maximised",
            window.ToString(),
            result ? "is currently" : "is currently NOT"
        );

        return result;
    }

    private void RestorePreviousPlacement(IntPtr window)
    {
        _mainForm.ChangeTrayIconTemporarily();
        var previousPlacement = _knownWindows[window.ToString()];
        logger.LogInformation("Restoring previous placement for #{Window}", window.ToString());
        NativeApi.SetWindowPlacement(window, ref previousPlacement);
        NativeApi.SendMessage(window, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
    }

    private void AddOrUpdatePreviousPlacement(
        bool isPreviousPlacementKnown,
        IntPtr window,
        WindowPlacement placement
    )
    {
        if (isPreviousPlacementKnown)
        {
            _knownWindows.Remove(window.ToString());
            logger.LogInformation(
                "Removing previous placement for #{Window} so a new value can be added",
                window.ToString()
            );
        }

        _knownWindows.Add(window.ToString(), placement);
        logger.LogInformation(
            "Adding/updating previous placement for #{Window}",
            window.ToString()
        );
    }

    private void NearMaximiseWindow(IntPtr window)
    {
        _mainForm.ChangeTrayIconTemporarily();
        var area = Screen.FromHandle(window).WorkingArea;
        NativeApi.ShowWindow(window, SwMaximize); // Maximize the window to get the animation

        if (!NativeApi.GetWindowRect(window, out var rect))
        {
            logger.LogWarning("Failed to get window rect");
            return;
        }

        // Calculate new window size
        var newX = area.Top + _userSettings.padding;
        var newY = area.Top + _userSettings.padding + ExtraYPadding;
        var newWidth = area.Right - area.Left - _userSettings.padding * 2;
        var newHeight = area.Bottom - area.Top - _userSettings.padding * 2 - ExtraYPadding;

        // Set the new window placement
        var placement = new WindowPlacement
        {
            Length = Marshal.SizeOf(typeof(WindowPlacement)),
            Flags = 0,
            ShowCmd = SwShowNormal,
            PtMaxPosition = new Point(0, 0),
            PtMinPosition = new Point(-1, -1),
            RcNormalPosition = new Rectangle(newX, newY, newX + newWidth, newY + newHeight)
        };

        logger.LogInformation(
            "Set to: {ActualX}, {ActualY}, {ActualWidth}, {ActualHeight} (based on rect)",
            rect.Left,
            rect.Top,
            rect.Right,
            rect.Bottom
        );

        logger.LogInformation("Near-maximising #{Window}", window.ToString());
        NativeApi.SetWindowPlacement(window, ref placement);
        NativeApi.SendMessage(window, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
    }
}
