namespace Randy;

using  System.Drawing;

public static class Notifier
{
    public static void SetMessage(Form form, string message)
    {
        form.Controls.Clear();
        var label = new Label
        {
            Text = message,
            Font = new Font(form.Font.FontFamily, 20, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ColorTranslator.FromHtml("#5E81AC")
        };
        form.Controls.Add(label);
    }
}