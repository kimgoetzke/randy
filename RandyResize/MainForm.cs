using System.Diagnostics.CodeAnalysis;

namespace RandyResize;

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);
    
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
    private Rectangle? previousSize = null;

    public MainForm()
    {
        InitializeComponent();
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        Text = "Resize Randy";
        BackColor = ColorTranslator.FromHtml("#3B4252");
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(300, 150);
        FormBorderStyle = FormBorderStyle.FixedSingle; 
        var label = new Label
        {
            Text = "Ready for action!",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ColorTranslator.FromHtml("#5E81AC")
        };
        Controls.Add(label);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        
        if (m.Msg != WmHotkey || m.WParam.ToInt32() != HotkeyId) 
            return;
        
        var hWnd = GetForegroundWindow();

        // Maximize the window
        ShowWindow(hWnd, SwMaximize);

        if (!GetWindowRect(hWnd, out var rect)) 
            return;
        
        // Calculate new window size
        var newX = rect.Left + 20;
        var newY = rect.Top + 20;
        var newWidth = rect.Right - rect.Left - 40;
        var newHeight = rect.Bottom - rect.Top - 40;

        
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
        
        // Force a repaint of the window
        SendMessage(hWnd, WmPaint, IntPtr.Zero, IntPtr.Zero);
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        // Console.Out.WriteLine("Registering hotkey");
        RegisterHotKey(Handle, HotkeyId, WindowsKeyModifier, (uint)Keys.Oem5); // Win + Backslash
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Console.Out.WriteLine("Unregistering hotkey");
        UnregisterHotKey(Handle, HotkeyId);
    }
    
    private struct Rect(int left, int top, int right, int bottom)
    {
        public readonly int Left = left;
        public readonly int Top = top;
        public readonly int Right = right;
        public readonly int Bottom = bottom;
    }
    
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct WindowPlacement(
        int length,
        int flags,
        int showCmd,
        Point ptMinPosition,
        Point ptMaxPosition,
        Rectangle rcNormalPosition)
    {
        public int Length = length;
        public int Flags = flags;
        public int ShowCmd = showCmd;
        public Point PtMinPosition = ptMinPosition;
        public Point PtMaxPosition = ptMaxPosition;
        public Rectangle RcNormalPosition = rcNormalPosition;
    }
}