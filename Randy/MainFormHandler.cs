using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Randy.Controls;

namespace Randy;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class MainFormHandler(ILogger<Form> logger, Form form)
{
    private const string AutoStartRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Randy";

    private readonly Color _nordDark0 = ColorTranslator.FromHtml("#2e3440");
    private readonly Color _nordDark3 = ColorTranslator.FromHtml("#4c566a"); 
    private readonly Color _nordDark9 = ColorTranslator.FromHtml("#6d7a96"); 
    private readonly Color _nordBright4 = ColorTranslator.FromHtml("#d8dee9");
    private readonly Color _nordBright5 = ColorTranslator.FromHtml("#e5e9f0");
    private readonly Color _nordBrightX = ColorTranslator.FromHtml("#909aaf");
    private readonly Color _nordAccent9 = ColorTranslator.FromHtml("#81a1c1");

    public void InitialiseForm()
    {
        InitialiseFormSettings();
        InitialiseContent();
    }

    private void InitialiseFormSettings()
    {
        form.ShowInTaskbar = true;
        form.Icon = new Icon(Constants.DefaultIconFile);
        form.Text = "Randy";
        form.BackColor = _nordDark0;
        // components = new System.ComponentModel.Container();
        form.AutoScaleMode = AutoScaleMode.Font;
        form.ClientSize = new Size(500, 200);
        form.FormBorderStyle = Constants.DefaultFormStyle;
        form.MaximizeBox = false;
        form.Padding = new Padding(10);
    }

    private void InitialiseContent()
    {
        form.Controls.Clear();
        var label = new Label
        {
            Text = "Randy is ready for action!",
            Font = new Font(form.Font.FontFamily, 15, FontStyle.Bold),
            Height = 50,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = _nordBright4
        };
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        var autoStartCheckBox = new CheckBox
        {
            Text = "Start with Windows (recommended)",
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Margin = new Padding(0, 15, 0, 15),
            Checked = IsAutoStartEnabled(),
            ForeColor = _nordBrightX
        };
        var shortCutLabel = new Label
        {
            Text = "Keyboard shortcut:",
            Height = 20,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 15, 0, 15),
            ForeColor = _nordBrightX
        };
        var modifierPanel = KeyPanel($"{GetKeyName(InvisibleForm.ModifierKey)}");
        var plusLabel = new Label
        {
            Text = " + ",
            AutoSize = true,
            Font = new Font(form.Font.FontFamily, 12, FontStyle.Regular),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _nordBrightX
        };
        var otherKeyLabel = KeyPanel($"{GetKeyName(InvisibleForm.OtherKey)}");
        var hotKeyPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 25
        };
        hotKeyPanel.Controls.Add(otherKeyLabel);
        hotKeyPanel.Controls.Add(plusLabel);
        hotKeyPanel.Controls.Add(modifierPanel);
        panel.Controls.Add(hotKeyPanel);
        panel.Controls.Add(shortCutLabel);
        panel.Controls.Add(autoStartCheckBox);
        form.Controls.Add(panel);
        form.Controls.Add(label);
        autoStartCheckBox.CheckedChanged += (_, _) => SetAutoStart(autoStartCheckBox.Checked);
    }

    private PanelWithBorder KeyPanel(string text)
    {
        var label = new Label 
        {
            Text = text,
            AutoSize = true,
            Font = new Font(form.Font.FontFamily, 8, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = _nordDark0
        };
        var panel = new PanelWithBorder(_nordDark9)
        {
            BackColor = _nordDark3,
            Padding = new Padding(5),
            AutoSize = true,
            Dock = DockStyle.Left
        };
        panel.Controls.Add(label);
        return panel;
    }


    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey);

        if (key == null)
            return false;

        var value = (string?)key.GetValue(AppName);

        if (string.IsNullOrEmpty(value))
            return false;

        return StringComparer.OrdinalIgnoreCase.Compare(value, Application.ExecutablePath) == 0;
    }
    
    private void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey, true);
        
        if (key == null)
            return;

        if (enabled)
        {
            var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(AppName, executablePath);
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
        logger.LogInformation("Set autostart to: {State}", enabled);
    }

    private static string GetKeyName(uint key)
    {
        if (ProcessSpecialKeys(key) is {} keyName)
            return keyName;
        
        try
        {
            var keyCode = (Keys)key;
            return keyCode.ToString();
        }
        catch
        {
            return "Unknown key";
        }
    }
    
    private static string? ProcessSpecialKeys(uint key)
    {
        return key switch
        {
            0x0001 => "Left Shift",
            0x0002 => "Right Shift",
            0x0004 => "Control",
            0x0008 => "Windows key",
            0x0010 => "Alt",
            _ => null
        };
    }
}