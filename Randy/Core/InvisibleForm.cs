using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Randy.Utilities;

namespace Randy.Core;

/**
 * This form contains the logic for the hotkey. It is permanently invisible but always active.
 */
public class InvisibleForm : Form
{
    private const uint WmPaint = 0x000F;
    private const int SwShowNormal = 1;
    private const int SwMaximized = 3;
    private const int HotkeyId = 1; // ID for the hotkey
    private const uint WmHotkey = 0x0312; // Windows message for hotkey
    private const int ToleranceInPx = 4; // Tolerance to be considered near-maximised
    private const int ExtraYPadding = 10;
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(InvisibleForm));
    private readonly MainForm _mainForm;
    private readonly Config _config;
    private readonly Dictionary<string, WindowPlacement> _knownWindows = new();

    public InvisibleForm(MainForm mainForm, Config config)
    {
        _mainForm = mainForm;
        _config = config;
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
        NativeApi.RegisterHotKey(Handle, HotkeyId, _config.key.modifierKey, _config.key.otherKey);
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

        if (IsNearMaximised(placement, window))
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

    private bool IsNearMaximised(WindowPlacement placement, IntPtr window)
    {
        var area = Screen.FromHandle(window).WorkingArea;
        var expectedX = area.Left + _config.padding;
        var expectedY = area.Top + _config.padding + ExtraYPadding;
        var expectedWidth = area.Right - _config.padding;
        var expectedHeight = area.Bottom - _config.padding;
        var result =
            Math.Abs(placement.RcNormalPosition.Left - expectedX) <= ToleranceInPx
            && Math.Abs(placement.RcNormalPosition.Top - expectedY) <= ToleranceInPx
            && Math.Abs(placement.RcNormalPosition.Width - expectedWidth) <= ToleranceInPx
            && Math.Abs(placement.RcNormalPosition.Height - expectedHeight) <= ToleranceInPx;

        logger.LogInformation(
            "Expected size of #{Name}: ({X},{Y})x({W},{H})",
            window.ToString(),
            expectedX,
            expectedY,
            expectedWidth,
            expectedHeight
        );
        logger.LogInformation(
            "Actual size of #{Name}: ({X},{Y})x({W},{H})",
            window.ToString(),
            placement.RcNormalPosition.Left,
            placement.RcNormalPosition.Top,
            placement.RcNormalPosition.Width,
            placement.RcNormalPosition.Height
        );
        logger.LogDebug("Working area: {X}x{Y}", area.Width, area.Height);
        logger.LogInformation(
            "#{Window} {Result} near-maximised (tolerance: {Tolerance})",
            window.ToString(),
            result ? "is currently" : "is currently NOT",
            ToleranceInPx
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
        LogWindowSize(window);
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
                "Removing previous placement for #{Window} so that a new value can be added",
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
        NativeApi.ShowWindow(window, SwMaximized); // Maximize the window to get the animation

        // Calculate new window size
        var newX = area.Left + _config.padding;
        var newY = area.Top + _config.padding + ExtraYPadding;
        var newWidth = area.Right - area.Left - _config.padding * 2;
        var newHeight = area.Bottom - area.Top - _config.padding * 2 - ExtraYPadding;

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

        logger.LogInformation("Near-maximising #{Window}", window.ToString());
        NativeApi.SetWindowPlacement(window, ref placement);
        NativeApi.SendMessage(window, WmPaint, IntPtr.Zero, IntPtr.Zero); // Force a repaint of the window
        LogWindowSize(window);
    }

    private static void LogWindowSize(IntPtr window)
    {
        NativeApi.GetWindowRect(window, out var rect);
        logger.LogInformation(
            "New size of #{Name}: ({X},{Y})x({W},{H}) ",
            window.ToString(),
            rect.Left,
            rect.Top,
            rect.Right,
            rect.Bottom
        );
    }
}
