namespace Randy.Controls;

public class PanelWithBorder(Color borderColor) : Panel
{
    private const int BorderWidth = 2;
    
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            borderColor, BorderWidth, ButtonBorderStyle.Solid,
            borderColor, BorderWidth, ButtonBorderStyle.Solid,
            borderColor, BorderWidth, ButtonBorderStyle.Solid,
            borderColor, BorderWidth, ButtonBorderStyle.Solid);
    }
}