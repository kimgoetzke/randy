namespace Randy.Controls;

public class PanelWithBorder(Color borderColor, Color backColour) : Panel
{
    private const int BorderWidth = 2;
    
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(5);
        Dock = DockStyle.Left;
        Padding = new Padding(5);
        BackColor = backColour;
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            borderColor, BorderWidth, ButtonBorderStyle.Solid,
            borderColor, BorderWidth, ButtonBorderStyle.Solid,
            borderColor, BorderWidth, ButtonBorderStyle.Solid,
            borderColor, BorderWidth, ButtonBorderStyle.Solid);
    }
}