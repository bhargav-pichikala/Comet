namespace Comet.Windows;
internal sealed class AutocompletePopup : Window
{
    readonly StackPanel rows=new();
    readonly Action<int> confirm;
    nint handle;
    internal AutocompletePopup(Action<int> confirm)
    {
        this.confirm=confirm;Width=350;SizeToContent=SizeToContent.Height;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;
        Ui.Window(this);Background=Brushes.Transparent;
        Content=new Border{CornerRadius=new CornerRadius(12),Background=Ui.Bg,BorderBrush=Ui.Muted,BorderThickness=new Thickness(1),Padding=new Thickness(8),Child=rows};
        SourceInitialized+=(_,_)=> { handle=new WindowInteropHelper(this).Handle;Native.SetWindowLongPtr(handle,-20,Native.GetWindowLongPtr(handle,-20)|0x08000080);HwndSource.FromHwnd(handle)?.AddHook(WndProc); };
    }
    internal void RefreshTheme(){if(Content is Border border){border.Background=Ui.Bg;border.BorderBrush=Ui.Muted;}}
    nint WndProc(nint h,int m,nint w,nint l,ref bool handled) { if(m==0x21){handled=true;return 3;}return 0; }
    internal void Present(IReadOnlyList<Suggestion> items,int selected)
    {
        rows.Children.Clear();
        for(int i=0;i<items.Count;i++)
        {
            int index=i;var item=items[i];
            string preview=item.Text.Replace("\r","").Replace("\n"," ↵ ");if(preview.Length>22)preview=preview[..22]+"…";
            var glyph=Ui.Label(preview,24);glyph.Width=68;glyph.FontFamily=new FontFamily("Segoe UI Emoji");glyph.MaxHeight=40;
            var name=Ui.Label(item.Name,13);name.MaxWidth=240;name.TextTrimming=TextTrimming.CharacterEllipsis;
            var sub=Ui.Label(":"+item.Shortcut,11,Ui.Muted);sub.MaxWidth=240;sub.TextTrimming=TextTrimming.CharacterEllipsis;
            var row=new Border{CornerRadius=new CornerRadius(7),Padding=new Thickness(8,3,8,3),Background=i==selected?Ui.Panel:Brushes.Transparent,Child=Ui.Row(glyph,Ui.Column(name,sub))};
            row.MouseLeftButtonUp+=(_,e)=>{confirm(index);e.Handled=true;};rows.Children.Add(row);
        }
        rows.Children.Add(Ui.Label("↑ ↓ choose    Enter / Tab insert    Esc dismiss",11,Ui.Muted));
        if(!IsVisible)Show();UpdateLayout();Position();
    }
    void Position()
    {
        var g=Native.FocusInfo();var point=new Native.Point{X=g.CaretRect.Left,Y=g.CaretRect.Bottom};
        if(g.Caret==0 || !Native.ClientToScreen(g.Caret,ref point))Native.GetCursorPos(out point);
        var monitor=new Native.MonitorInfo{Size=System.Runtime.InteropServices.Marshal.SizeOf<Native.MonitorInfo>()};Native.GetMonitorInfo(Native.MonitorFromPoint(point,2),ref monitor);
        double scale=Native.GetDpiForWindow(handle)/96.0;if(scale<=0)scale=1;
        int width=(int)Math.Ceiling(ActualWidth*scale),height=(int)Math.Ceiling(ActualHeight*scale);
        int x=Math.Clamp(point.X,monitor.Work.Left,Math.Max(monitor.Work.Left,monitor.Work.Right-width));
        int y=point.Y+8;if(y+height>monitor.Work.Bottom)y=point.Y-height-24;y=Math.Max(monitor.Work.Top,y);
        Native.SetWindowPos(handle,new nint(-1),x,y,0,0,0x0011);
    }
    internal bool Contains(Native.Point point)
    {
        if(!IsVisible)return false;
        var top=PointToScreen(new System.Windows.Point(0,0));var bottom=PointToScreen(new System.Windows.Point(ActualWidth,ActualHeight));
        return point.X>=top.X && point.X<=bottom.X && point.Y>=top.Y && point.Y<=bottom.Y;
    }
}
