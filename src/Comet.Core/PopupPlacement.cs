namespace Comet.Core;
public readonly record struct ScreenRect(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public bool Valid => double.IsFinite(Left) && double.IsFinite(Top) && double.IsFinite(Right) && double.IsFinite(Bottom) && Width >= 0 && Height > 0;
}
public readonly record struct PopupLocation(double X, double Y, bool Above, double TipX);
public static class PopupPlacement
{
    // All arguments are physical pixels. DPI conversion happens once at the WPF boundary.
    public static PopupLocation Calculate(ScreenRect caret, ScreenRect work, double width, double height, double scale)
    {
        if (!caret.Valid || !work.Valid || width <= 0 || height <= 0 || scale <= 0) throw new ArgumentException("Invalid popup geometry.");
        double x = Math.Clamp(caret.Left - 72 * scale, work.Left, Math.Max(work.Left, work.Right - width));
        double gap = (3 - 14) * scale; // Transparent shadow inset: the visible pointer stays 3 DIP from the caret.
        bool above = caret.Bottom + gap + height > work.Bottom && caret.Top - gap - height >= work.Top;
        double y = above ? caret.Top - gap - height : caret.Bottom + gap;
        y = Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - height));
        return new(x, y, above, Math.Clamp(caret.Left - x, 24 * scale, Math.Max(24 * scale, width - 24 * scale)));
    }
}
