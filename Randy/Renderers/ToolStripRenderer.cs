using Randy.Utilities;

namespace Randy.Renderers;

public class ToolStripRenderer : ToolStripProfessionalRenderer
{
    private readonly Colours _colours = new();

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
            base.OnRenderMenuItemBackground(e);
        else
        {
            var rectangle = new Rectangle(Point.Empty, e.Item.Size);
            var transparentColour = Color.FromArgb(50, _colours.NordDark9);
            using var brush = new SolidBrush(transparentColour);
            e.Graphics.FillRectangle(brush, rectangle);
        }
    }
}
