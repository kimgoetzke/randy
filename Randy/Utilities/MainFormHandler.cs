using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CheckBox = System.Windows.Forms.CheckBox;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;

namespace Randy.Utilities;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class MainFormHandler
{
    private const int Multiplier = 10;
    private readonly Colours _colours = new();
    private Size _defaultWindowSize;
    private readonly Form _form;
    private readonly Config _config;

    public MainFormHandler(Form form, Config config)
    {
        _form = form;
        _config = config;
        InitialiseFormSettings();
        InitialiseContent();
    }

    private void InitialiseFormSettings()
    {
        _form.ShowInTaskbar = true;
        _form.Icon = IconProvider.GetDefaultIconSafely();
        _form.Text = "Randy" + VersionToString();
        _form.BackColor = _colours.NordDark0;
        _form.AutoScaleMode = AutoScaleMode.Font;
        var screen = Screen.PrimaryScreen?.WorkingArea;
        var width = screen != null ? screen.Value.Width * 25 / 100 : 600; // 1/4 of screen width or 600px
        var height = screen != null ? screen.Value.Height * 20 / 100 : 250; // 1/5 of screen height or 250px
        _form.ClientSize = new Size(width, height);
        _form.FormBorderStyle = Constants.DefaultFormStyle;
        _form.MaximizeBox = false;
        _form.MinimizeBox = true;
        _form.ControlBox = true;
        _form.Padding = new Padding(20);
        _form.StartPosition = FormStartPosition.CenterScreen;
        _defaultWindowSize = _form.Size;
    }

    private void InitialiseContent()
    {
        _form.Controls.Clear();
        var tableLayoutPanel = new TableLayoutPanel
        {
            RowCount = 4,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill
        };
        tableLayoutPanel.Controls.Add(HotKeyPanel(), 0, 0);
        var (sliderLabel, slider) = PaddingSlider();
        tableLayoutPanel.Controls.Add(sliderLabel, 0, 1);
        tableLayoutPanel.Controls.Add(slider, 0, 2);
        tableLayoutPanel.Controls.Add(AutoStartCheckBox(), 0, 3);
        _form.Controls.Add(tableLayoutPanel);
    }

    private static string VersionToString()
    {
        var version = "";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version = " v" + Assembly.GetExecutingAssembly().GetName().Version!.Major;
        }

        return version;
    }

    private Panel HotKeyPanel()
    {
        var shortCutLabel = new Label
        {
            Text = "Keyboard shortcut: ",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            ForeColor = _colours.NordBrightX
        };
        var modifierPanel = KeyPanel($"{GetKeyName(_config.key.modifierKey)}");
        var plusLabel = new Label
        {
            Text = " + ",
            Font = new Font(_form.Font.FontFamily, 9, FontStyle.Regular),
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.TopCenter,
            ForeColor = _colours.NordBrightX
        };
        var hotKeyPanel = new Panel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 10, 0, 10),
            MinimumSize = new Size(0, 30)
        };
        var otherKeyLabel = KeyPanel($"{GetKeyName(_config.key.otherKey)}");
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
            ForeColor = _colours.NordBrightX
        };
        var valueLabel = new Label
        {
            Text = _config.padding + " px",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            ForeColor = _colours.NordBrightX
        };
        var slider = new TrackBar
        {
            Minimum = 1,
            Maximum = 10,
            Value = _config.padding / Multiplier,
            TickFrequency = 1,
            LargeChange = 1,
            TickStyle = TickStyle.BottomRight,
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        slider.ValueChanged += (_, _) =>
        {
            _config.padding = slider.Value * Multiplier;
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

    private Label KeyPanel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(_form.Font.FontFamily, 9, FontStyle.Bold),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = _colours.NordDark0,
            BackColor = _colours.NordBrightX
        };
    }

    private CheckBox AutoStartCheckBox()
    {
        var autoStartCheckBox = new CheckBox
        {
            Text = "Start with Windows (recommended)",
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Checked = IsAutoStartEnabled(),
            ForeColor = _colours.NordBrightX
        };
        autoStartCheckBox.CheckedChanged += (_, _) => SetAutoStart(autoStartCheckBox.Checked);
        return autoStartCheckBox;
    }

    private static bool IsAutoStartEnabled()
    {
        var enabledInRegistry = RegistryKeyHandler.IsEnabled();
        var shortcutExists = ShortCutHandler.Exists();
        var taskExists = StartupTaskHandler.Exists();
        return enabledInRegistry || shortcutExists || taskExists;
    }

    private static void SetAutoStart(bool enabled)
    {
        RegistryKeyHandler.SetEnabled(enabled);
        if (enabled)
        {
            ShortCutHandler.Create();
            StartupTaskHandler.Schedule();
        }
        else
        {
            ShortCutHandler.Delete();
            StartupTaskHandler.Cancel();
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
        _form.Size = _defaultWindowSize;
        if (Screen.PrimaryScreen is not { } screen)
        {
            return;
        }

        var area = screen.WorkingArea;
        _form.Location = new Point(
            (area.Width - _form.Width) / 2,
            (area.Height - _form.Height) / 2
        );
    }
}
