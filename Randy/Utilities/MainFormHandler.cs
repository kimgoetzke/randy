using System.Diagnostics.CodeAnalysis;
using Randy.Controls;
using CheckBox = System.Windows.Forms.CheckBox;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;

namespace Randy.Utilities;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class MainFormHandler(Form form, Config config)
{
    private const int Multiplier = 10;
    private Size _defaultWindowSize;
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
        form.AutoScaleMode = AutoScaleMode.Font;
        var screen = Screen.PrimaryScreen?.WorkingArea;
        var width = screen != null ? screen.Value.Width * 25 / 100 : 600;
        var height = screen != null ? screen.Value.Height * 20 / 100 : 250;
        form.ClientSize = new Size(width, height);
        form.FormBorderStyle = Constants.DefaultFormStyle;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ControlBox = true;
        form.Padding = new Padding(20);
        form.StartPosition = FormStartPosition.CenterScreen;
        _defaultWindowSize = form.Size;
    }

    private void InitialiseContent()
    {
        form.Controls.Clear();
        var tableLayoutPanel = new TableLayoutPanel
        {
            RowCount = 4,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Dock = DockStyle.Fill
        };
        tableLayoutPanel.Controls.Add(HotKeyPanel(), 0, 0);
        var (sliderLabel, slider) = PaddingSlider();
        tableLayoutPanel.Controls.Add(sliderLabel, 0, 1);
        tableLayoutPanel.Controls.Add(slider, 0, 2);
        tableLayoutPanel.Controls.Add(AutoStartCheckBox(), 0, 3);
        form.Controls.Add(tableLayoutPanel);
    }

    private Panel HotKeyPanel()
    {
        var shortCutLabel = new Label
        {
            Text = "Keyboard shortcut: ",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            ForeColor = _nordBrightX
        };
        var modifierPanel = KeyPanel($"{GetKeyName(config.key.modifierKey)}");
        var plusLabel = new Label
        {
            Text = " + ",
            Font = new Font(form.Font.FontFamily, 12, FontStyle.Regular),
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _nordBrightX
        };
        var hotKeyPanel = new Panel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 10, 0, 10),
            MinimumSize = new Size(0, 30)
        };
        var otherKeyLabel = KeyPanel($"{GetKeyName(config.key.otherKey)}");
        hotKeyPanel.Controls.Add(otherKeyLabel);
        hotKeyPanel.Controls.Add(plusLabel);
        hotKeyPanel.Controls.Add(modifierPanel);
        hotKeyPanel.Controls.Add(shortCutLabel);
        return hotKeyPanel;
    }

    private (Panel sliderLabel, TrackBar slider) PaddingSlider()
    {
        var titleLabel = new Label
        {
            Text = "Padding: ",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            ForeColor = _nordBrightX
        };
        var valueLabel = new Label
        {
            Text = config.padding + " px",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            ForeColor = _nordBrightX
        };
        var slider = new TrackBar
        {
            Minimum = 1,
            Maximum = 10,
            Value = config.padding / Multiplier,
            TickFrequency = 1,
            TickStyle = TickStyle.BottomRight,
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = _nordAccent9
        };
        slider.ValueChanged += (_, _) =>
        {
            config.padding = slider.Value * Multiplier;
            valueLabel.Text = slider.Value * Multiplier + " px";
        };
        var sliderLabel = new Panel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 10),
            MinimumSize = new Size(0, 30)
        };
        sliderLabel.Controls.Add(valueLabel);
        sliderLabel.Controls.Add(titleLabel);
        return (sliderLabel, slider);
    }

    private PanelWithBorder KeyPanel(string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(form.Font.FontFamily, 8, FontStyle.Bold),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.TopCenter,
            ForeColor = _nordDark0
        };
        var panel = new PanelWithBorder(_nordDark9, _nordDark3);
        panel.Controls.Add(label);
        return panel;
    }

    private CheckBox AutoStartCheckBox()
    {
        var autoStartCheckBox = new CheckBox
        {
            Text = "Start with Windows (recommended)",
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Checked = IsAutoStartEnabled(),
            ForeColor = _nordBrightX
        };
        autoStartCheckBox.CheckedChanged += (_, _) => SetAutoStart(autoStartCheckBox.Checked);
        return autoStartCheckBox;
    }

    // TODO: Replace with value stored on disk
    private static bool IsAutoStartEnabled()
    {
        var enabledInRegistry = RegistryKeyHandler.IsEnabled();
        var shortcutExists = ShortCutHandler.Exists();
        return enabledInRegistry || shortcutExists;
    }

    private static void SetAutoStart(bool enabled)
    {
        RegistryKeyHandler.SetEnabled(enabled);
        if (enabled)
        {
            ShortCutHandler.Create();
        }
        else
        {
            ShortCutHandler.Delete();
        }
    }

    /**
     * Returns human-readable keyboard key names based on unit inputs.
     */
    private static string GetKeyName(uint key)
    {
        if (ProcessSpecialKeys(key) is { } keyName)
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

    public void SetWindowSizeAndPosition()
    {
        form.Size = _defaultWindowSize;
        if (Screen.PrimaryScreen is not { } screen)
        {
            return;
        }

        var area = screen.WorkingArea;
        form.Location = new Point((area.Width - form.Width) / 2, (area.Height - form.Height) / 2);
    }
}