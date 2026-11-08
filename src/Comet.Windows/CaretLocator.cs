using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Text;
namespace Comet.Windows;
internal sealed record CaretSnapshot(nint Window, nint Focus, int[] RuntimeId, ScreenRect Rect, long CheckedAt, string Source);

// Provider calls can hang: keep them off both the keyboard thread and WPF dispatcher.
internal sealed class CaretLocator : IDisposable
{
    readonly FocusGuard guard;
    readonly Thread worker;
    readonly AutoResetEvent wake = new(false);
    volatile bool stopped, requested;
    CaretSnapshot? snapshot;
    internal event Action? Updated;
    internal CaretLocator(FocusGuard guard)
    {
        this.guard = guard;
        worker = new Thread(Poll) { IsBackground = true, Name = "Comet accessibility caret locator" };
        worker.SetApartmentState(ApartmentState.MTA); worker.Start();
    }
    internal void Track(bool active) { requested = active; if (active) wake.Set(); }
    internal bool TryGet(nint window, nint focus, out ScreenRect rect)
    {
        rect = default;
        if (!guard.IsSafe(window, focus) || Native.GetForegroundWindow() != window) return false;
        if (TryWin32(window, focus, out rect)) return true;
        var current = Volatile.Read(ref snapshot);
        if (current == null || current.Window != window || current.Focus != focus || Environment.TickCount64 - current.CheckedAt > 300 || !current.RuntimeId.SequenceEqual(guard.Snapshot.RuntimeId)) return false;
        rect = current.Rect; return rect.Valid;
    }
    internal static bool TryWin32(nint window, nint focus, out ScreenRect rect)
    {
        rect = default;
        var g = Native.FocusInfo();
        if (Native.GetForegroundWindow() != window || g.Focus != focus || g.Caret == 0 || g.CaretRect.Bottom <= g.CaretRect.Top) return false;
        var a = new Native.Point { X = g.CaretRect.Left, Y = g.CaretRect.Top };
        var b = new Native.Point { X = g.CaretRect.Right, Y = g.CaretRect.Bottom };
        if (!Native.ClientToScreen(g.Caret, ref a) || !Native.ClientToScreen(g.Caret, ref b)) return false;
        rect = new(a.X, a.Y, Math.Max(a.X + 1, b.X), b.Y); return rect.Valid;
    }
    void Poll()
    {
        while (!stopped)
        {
            if (!requested) { wake.WaitOne(200); continue; }
            var current = guard.Snapshot;
            try
            {
                if (guard.IsSafe(current.Window, current.Focus))
                {
                    ScreenRect rect; string source = "Win32 caret";
                    bool found = TryWin32(current.Window, current.Focus, out rect);
                    if (!found) { found = TryAutomation(current, out rect); source = "UI Automation caret"; }
                    if (found && requested && guard.IsSafe(current.Window, current.Focus) && current.RuntimeId.SequenceEqual(guard.Snapshot.RuntimeId))
                    {
                        Volatile.Write(ref snapshot, new(current.Window, current.Focus, current.RuntimeId, rect, Environment.TickCount64, source));
                        Updated?.Invoke();
                    }
                }
            }
            catch { /* A failing provider never disables subsequent polling or the hook. */ }
            wake.WaitOne(60);
        }
    }
    static bool TryAutomation(FocusSnapshot expected, out ScreenRect rect)
    {
        rect = default; var element = AutomationElement.FocusedElement;
        if (element == null || element.Current.IsPassword || !element.Current.HasKeyboardFocus || !element.GetRuntimeId().SequenceEqual(expected.RuntimeId)) return false;
        TextPatternRange? range = null;
        // The managed .NET UIA wrapper exposes TextPattern (not native TextPattern2).
        // Its collapsed selection range provides caret geometry without reading field text.
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var v1))
        {
            var selections = ((TextPattern)v1).GetSelection();
            if (selections.Length == 1 && selections[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selections[0], TextPatternRangeEndpoint.End) == 0) range = selections[0];
        }
        if (range == null) return TryMsaa(expected, out rect);
        var bounds = range.GetBoundingRectangles();
        if (bounds.Length > 0 && bounds[0].Height > 0) { rect = new(bounds[0].Left, bounds[0].Top, bounds[0].Left + 1, bounds[0].Bottom); return rect.Valid; }
        // Collapsed ranges commonly have no rectangles. Expand geometry only; never request text.
        var next = range.Clone();
        if (next.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 1) == 1)
        {
            bounds = next.GetBoundingRectangles();
            if (bounds.Length > 0 && bounds[0].Height > 0) { rect = new(bounds[0].Left, bounds[0].Top, bounds[0].Left + 1, bounds[0].Bottom); return rect.Valid; }
        }
        var previous = range.Clone();
        if (previous.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1) == -1)
        {
            bounds = previous.GetBoundingRectangles();
            if (bounds.Length > 0 && bounds[0].Height > 0)
            {
                var last = bounds[^1]; double edge = last.Right;
                rect = new(edge, last.Top, edge + 1, last.Bottom); return rect.Valid;
            }
        }
        return TryMsaa(expected, out rect);
    }
    [DllImport("oleacc.dll")]
    static extern int AccessibleObjectFromWindow(nint hwnd, uint objectId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out Accessibility.IAccessible accessible);
    static bool TryMsaa(FocusSnapshot expected, out ScreenRect rect)
    {
        rect=default;
        foreach(var hwnd in new[]{expected.Focus,expected.Window})
        {
            Accessibility.IAccessible? accessible=null;
            try
            {
                var iid=new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
                if(AccessibleObjectFromWindow(hwnd,unchecked((uint)-8),ref iid,out accessible)!=0 || accessible==null)continue; // OBJID_CARET, not the whole control.
                accessible.accLocation(out int x,out int y,out int width,out int height,0);
                rect=new(x,y,x+Math.Max(1,width),y+height);
                if(rect.Valid)return true;
            }
            catch { }
            finally{if(accessible!=null && Marshal.IsComObject(accessible))Marshal.ReleaseComObject(accessible);}
        }
        return false;
    }
    public void Dispose() { stopped = true; wake.Set(); /* Do not block exit on a hung external UIA provider. */ }
}
