using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
namespace Comet.Windows;
internal sealed class AutocompletePopup : Window
{
    const double CardWidth = 260, Shadow = 14;
    readonly CaretLocator caret;
    readonly Action<int, long> confirm;
    readonly Action<long> browse;
    readonly Action<ScreenRect> boundsChanged;
    readonly Action<long, bool> visibilityChanged;
    readonly Grid root = new();
    readonly Border card = new();
    readonly StackPanel rows = new();
    readonly ScrollViewer scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
    readonly TextBlock query = new();
    readonly Border header = new(), footer = new();
    readonly System.Windows.Shapes.Path pointer = new() { IsHitTestVisible = false, Stretch = Stretch.None };
    readonly List<Border> resultRows = [];
    readonly System.Windows.Threading.DispatcherTimer follow;
    Brush surface = Brushes.White, line = Brushes.LightGray, ink = Brushes.Black, selection = Brushes.WhiteSmoke;
    nint handle, target, focus;
    long revision;
    readonly PopupTransitionState transition=new();
    bool requested;
    internal long Revision => revision;
    internal AutocompletePopup(CaretLocator caret, Action<int, long> confirm, Action<long> browse, Action<ScreenRect> boundsChanged, Action<long, bool> visibilityChanged)
    {
        this.caret = caret; this.confirm = confirm; this.browse = browse; this.boundsChanged = boundsChanged; this.visibilityChanged = visibilityChanged;
        Width = CardWidth + Shadow * 2; SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true;
        Background = Brushes.Transparent; ShowInTaskbar = false; ShowActivated = false; Topmost = true; Focusable = false;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14; UseLayoutRounding = true; SnapsToDevicePixels = true;
        query.FontSize = 14; query.FontWeight = FontWeights.SemiBold; query.TextTrimming = TextTrimming.CharacterEllipsis;
        query.VerticalAlignment = VerticalAlignment.Center;
        header.Height = 48; header.Padding = new Thickness(18, 0, 18, 0); header.BorderThickness = new Thickness(0, 0, 0, 1); header.Child = query;
        scroll.Content = rows; scroll.MaxHeight = 432;
        footer.Height = 36; footer.Margin = new Thickness(6, 1, 6, 8); footer.CornerRadius = new CornerRadius(5); footer.Padding = new Thickness(11, 0, 8, 0);
        var plus = Text("+", 19); plus.Width = 25; plus.FontWeight = FontWeights.Normal;
        var more = Text("Search for more…", 14); more.FontWeight = FontWeights.SemiBold;
        var footerRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        footerRow.Children.Add(plus); footerRow.Children.Add(more); footer.Child = footerRow;
        footer.MouseLeftButtonUp += (_, e) => { browse(revision); e.Handled = true; };
        footer.MouseEnter += (_, _) => footer.Background = selection;
        footer.MouseLeave += (_, _) => footer.Background = footerSelected ? selection : Brushes.Transparent;
        var stack = new StackPanel(); stack.Children.Add(header); stack.Children.Add(scroll); stack.Children.Add(footer);
        card.Width = CardWidth; card.CornerRadius = new CornerRadius(10); card.BorderThickness = new Thickness(1);
        card.Margin = new Thickness(Shadow, Shadow + 8, Shadow, Shadow);
        card.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = .16, Color = Colors.Black };
        card.Child = stack; root.Children.Add(card); root.Children.Add(pointer); Content = root;
        SourceInitialized += (_, _) =>
        {
            handle = new WindowInteropHelper(this).Handle;
            Native.SetWindowLongPtr(handle, -20, Native.GetWindowLongPtr(handle, -20) | 0x08000080);
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
        };
        follow = new() { Interval = TimeSpan.FromMilliseconds(60) };
        follow.Tick += (_, _) => { if (requested) Reposition(); };
        RefreshTheme();
    }
    bool footerSelected;
    TextBlock Text(string value, double size) => new() { Text = value, FontSize = size, VerticalAlignment = VerticalAlignment.Center, Foreground = ink, TextTrimming = TextTrimming.CharacterEllipsis };
    static Brush ColorBrush(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    internal void RefreshTheme()
    {
        bool dark = Ui.Bg is SolidColorBrush b && b.Color.R < 100;
        surface = ColorBrush(dark ? "#29282C" : "#F8F6F6"); line = ColorBrush(dark ? "#49474C" : "#DAD6D6");
        ink = ColorBrush(dark ? "#F7F4F4" : "#171616"); selection = ColorBrush(dark ? "#444247" : "#ECEAEA");
        card.Background = surface; card.BorderBrush = line; header.BorderBrush = ColorBrush(dark ? "#403E43" : "#E6E3E3");
        query.Foreground = ink; pointer.Fill = surface; pointer.Stroke = line; pointer.StrokeThickness = 1;
        if (footer.Child is StackPanel p) foreach (TextBlock t in p.Children) t.Foreground = ink;
    }
    nint WndProc(nint h, int m, nint w, nint l, ref bool handled)
    {
        if (m == 0x21) { handled = true; return 3; } // MA_NOACTIVATE; clicks remain deliverable.
        return 0;
    }
    internal bool Present(string phrase, IReadOnlyList<Suggestion> items, int selected, nint window, nint field, long sessionRevision)
    {
        if(transition.Show()){BeginAnimation(OpacityProperty,null);Opacity=1;IsHitTestVisible=true;}
        requested = true; target = window; focus = field; revision = sessionRevision; query.Text = phrase;
        rows.Children.Clear(); resultRows.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            int index = i; long rowRevision = revision; var item = items[i];
            var glyph = Text(Preview(item.Text), item.Category == "Snippet" ? 12 : 18);
            glyph.FontFamily = new FontFamily("Segoe UI Emoji"); glyph.Width = 26; glyph.MaxHeight = 24;
            var name = Text(item.Shortcut.Replace('_', ' '), 14); name.FontWeight = FontWeights.SemiBold;
            var grid = new Grid(); grid.ColumnDefinitions.Add(new() { Width = new GridLength(26) }); grid.ColumnDefinitions.Add(new());
            Grid.SetColumn(name, 1); grid.Children.Add(glyph); grid.Children.Add(name);
            var row = new Border { Height = 36, Margin = new Thickness(6, i == 0 ? 8 : 0, 6, 0), Padding = new Thickness(11, 0, 9, 0), CornerRadius = new CornerRadius(5), Background = i == selected ? selection : Brushes.Transparent, Child = grid, ToolTip = item.Name };
            row.MouseLeftButtonUp += (_, e) => { confirm(index, rowRevision); e.Handled = true; };
            row.MouseEnter += (_, _) => row.Background = selection;
            row.MouseLeave += (_, _) => row.Background = index == selected ? selection : Brushes.Transparent;
            rows.Children.Add(row); resultRows.Add(row);
        }
        footerSelected = selected == items.Count; footer.Background = footerSelected ? selection : Brushes.Transparent;
        caret.Track(true); follow.Start();
        bool visible = Reposition();
        if (selected >= 0 && selected < resultRows.Count) resultRows[selected].BringIntoView();
        return visible;
    }
    static string Preview(string text)
    {
        var value = text.Replace("\r", "").Replace("\n", " ↵ ");
        var info = new System.Globalization.StringInfo(value);
        return info.LengthInTextElements > 3 ? info.SubstringByTextElements(0, 3) + "…" : value;
    }
    internal bool Reposition()
    {
        if (!requested) return false;
        if (!caret.TryGet(target, focus, out var anchor))
        {
            if (IsVisible) Hide(); boundsChanged(default); visibilityChanged(revision, false); return false;
        }
        // Create the HWND without showing a misplaced frame at the mouse or screen origin.
        if (handle == 0) new WindowInteropHelper(this).EnsureHandle();
        var point = new Native.Point { X = (int)anchor.Left, Y = (int)anchor.Bottom };
        var monitor = Native.MonitorFromPoint(point, 2);
        var info = new Native.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<Native.MonitorInfo>() };
        if (!Native.GetMonitorInfo(monitor, ref info)) return false;
        double scale = Native.GetDpiForWindow(handle) / 96.0;
        try { if (Native.GetDpiForMonitor(monitor, 0, out uint x, out _) == 0) scale = x / 96.0; } catch (DllNotFoundException) { } catch (EntryPointNotFoundException) { }
        if (scale <= 0) scale = 1;
        scroll.MaxHeight = Math.Max(36, (info.Work.Bottom - info.Work.Top) / scale - 48 - 45 - 36);
        Measure(new Size(Width, double.PositiveInfinity)); var size = DesiredSize;
        double height = size.Height * scale, width = Width * scale;
        var location = PopupPlacement.Calculate(anchor, new(info.Work.Left, info.Work.Top, info.Work.Right, info.Work.Bottom), width, height, scale);
        card.Margin = location.Above ? new Thickness(Shadow, Shadow, Shadow, Shadow + 8) : new Thickness(Shadow, Shadow + 8, Shadow, Shadow);
        double tip = location.TipX / scale;
        double yTip = location.Above ? size.Height - Shadow : Shadow;
        double yBase = location.Above ? yTip - 8 : yTip + 8;
        pointer.Data = Geometry.Parse(FormattableString.Invariant($"M {tip - 8},{yBase} L {tip},{yTip} L {tip + 8},{yBase}"));
        Native.SetWindowPos(handle, new nint(-1), (int)Math.Round(location.X), (int)Math.Round(location.Y), 0, 0, 0x0011);
        if (!IsVisible)
        {
            IsHitTestVisible = true; BeginAnimation(OpacityProperty, null); Opacity = 0;
            Show();
            Native.SetWindowPos(handle,new nint(-1),(int)Math.Round(location.X),(int)Math.Round(location.Y),0,0,0x0011);
            Opacity=1;BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(85)) { FillBehavior = FillBehavior.Stop });
            // Show/WM_DPICHANGED can update scale. Follow timer refines the next frame.
        }
        boundsChanged(new(location.X + Shadow * scale, location.Y + Shadow * scale, location.X + width - Shadow * scale, location.Y + height - Shadow * scale));
        visibilityChanged(revision, true); return true;
    }
    // Actual WPF render export for Windows visual comparison; this is not a simulated input test.
    internal void CapturePreview(string path)
    {
        follow.Stop();caret.Track(false);requested=false;
        card.Margin=new Thickness(Shadow,Shadow+8,Shadow,Shadow);
        pointer.Data=Geometry.Parse("M 86,22 L 94,14 L 102,22");
        root.Measure(new Size(Width,double.PositiveInfinity));
        root.Arrange(new Rect(new Point(0,0),new Size(Width,root.DesiredSize.Height)));root.UpdateLayout();
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(Width),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream=File.Create(path);encoder.Save(stream);
    }
    internal void Dismiss()
    {
        requested = false; caret.Track(false); follow.Stop(); boundsChanged(default); visibilityChanged(revision, false);
        long epoch = transition.Hide(); IsHitTestVisible = false;
        if (!IsVisible) return;
        double from = Opacity; Opacity = 0;
        var fade = new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(65)) { FillBehavior = FillBehavior.Stop };
        fade.Completed += (_, _) => { if (transition.CanFinishHide(epoch) && !requested) Hide(); };
        BeginAnimation(OpacityProperty, fade);
    }
}
