using System.Diagnostics.CodeAnalysis;

namespace Randy.Core;

[SuppressMessage("ReSharper", "NotAccessedField.Global")]
public struct WindowPlacement(
    int length,
    int flags,
    int showCmd,
    Point ptMinPosition,
    Point ptMaxPosition,
    Rectangle rcNormalPosition
)
{
    public int Length = length;
    public int Flags = flags;
    public int ShowCmd = showCmd;
    public Point PtMinPosition = ptMinPosition;
    public Point PtMaxPosition = ptMaxPosition;
    public Rectangle RcNormalPosition = rcNormalPosition;
}
