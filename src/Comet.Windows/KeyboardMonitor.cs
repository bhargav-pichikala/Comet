using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
namespace Comet.Windows;
internal sealed class KeyboardMonitor : IDisposable
{
    readonly Native.HookProc keyProc, mouseProc;
    readonly Thread hookThread;
    readonly Dispatcher ui = Application.Current.Dispatcher;
    Dispatcher? input;
    nint keyHook, mouseHook;
    readonly FocusGuard guard;
    readonly Func<Settings> settings;
    readonly Func<string, IReadOnlyList<Suggestion>> search;
    readonly Func<string, IReadOnlyList<Suggestion>, int, nint, nint, long, bool> show;
    readonly Action hide;
    readonly Action<Suggestion, nint, nint, ReplacementPlan, long, Action> insert;
    readonly Action<string, nint, nint, string, string> browse;
    readonly AutocompleteSession session = new();
    readonly KeySuppression suppressed = new();
    readonly bool testMode = Environment.GetCommandLineArgs().Contains("--test-input");
    nint target, focus, layout;
    long lastInput, inputVersion, presentation, events, errors, attempts, completions, searchRequest;
    int[] runtime = [];
    volatile bool disposed;
    sealed record HitArea(ScreenRect Bounds);
    HitArea hitArea = new(default);
    internal long InputVersion => Interlocked.Read(ref inputVersion);
    internal string Diagnostics => $"Hook thread alive: {hookThread.IsAlive}\nPhysical keydowns: {Interlocked.Read(ref events)}\nCallback failures: {Interlocked.Read(ref errors)}\nInsertions requested/completed: {Interlocked.Read(ref attempts)}/{Interlocked.Read(ref completions)}\nField check age: {Environment.TickCount64-guard.Snapshot.CheckedAt} ms\nField: {guard.Snapshot.Reason}";

    internal KeyboardMonitor(FocusGuard guard, Func<Settings> settings, Func<string, IReadOnlyList<Suggestion>> search,
        Func<string, IReadOnlyList<Suggestion>, int, nint, nint, long, bool> show, Action hide,
        Action<Suggestion, nint, nint, ReplacementPlan, long, Action> insert,
        Action<string, nint, nint, string, string> browse)
    {
        this.guard = guard; this.settings = settings; this.search = search; this.show = show; this.hide = hide; this.insert = insert; this.browse = browse;
        keyProc = Key; mouseProc = Mouse;
        using var started = new ManualResetEventSlim(); Exception? failure = null; bool initialized=false;
        hookThread = new Thread(() =>
        {
            try
            {
                input = Dispatcher.CurrentDispatcher;
                keyHook = Native.SetWindowsHookEx(13, keyProc, Native.GetModuleHandle(null), 0);
                mouseHook = Native.SetWindowsHookEx(14, mouseProc, Native.GetModuleHandle(null), 0);
                if (keyHook == 0 || mouseHook == 0) throw new InvalidOperationException("Windows could not install input hooks: " + Marshal.GetLastWin32Error());
                var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, (_, _) => CheckContextCore(), input);
                initialized=true; started.Set(); Dispatcher.Run(); timer.Stop();
            }
            catch (Exception e) { if(!initialized){failure = e; started.Set();}else Interlocked.Increment(ref errors); }
            finally
            {
                if (keyHook != 0) Native.UnhookWindowsHookEx(keyHook);
                if (mouseHook != 0) Native.UnhookWindowsHookEx(mouseHook);
            }
        }) { IsBackground = true, Name = "Comet keyboard message pump" };
        hookThread.SetApartmentState(ApartmentState.STA); hookThread.Start(); started.Wait();
        if (failure != null) throw failure;
    }
    void QueueUi(Action action)
    {
        if(disposed || ui.HasShutdownStarted)return;
        try{ui.BeginInvoke(action);}catch(InvalidOperationException){/* Dispatcher is shutting down. */}
    }
    void Post(Action action)
    {
        if (disposed || input == null || input.HasShutdownStarted) return;
        void Safe(){try{action();}catch{Interlocked.Increment(ref errors);ResetCore();}}
        try{if (input.CheckAccess()) Safe(); else input.BeginInvoke(DispatcherPriority.Send, (Action)Safe);}
        catch(InvalidOperationException){/* Input pump is shutting down. */}
    }
    internal void SetPopupBounds(ScreenRect rect) => Volatile.Write(ref hitArea,new(rect));
    internal void SetVisible(long revision, bool visible) => Post(() => session.SetVisible(revision, visible));
    internal void Reset() => Post(ResetCore);
    void ResetCore() { Interlocked.Increment(ref searchRequest);session.Reset(); Hide(); }
    void Hide()
    {
        long version = Interlocked.Increment(ref presentation);
        QueueUi(() => { if (version == Interlocked.Read(ref presentation)) hide(); });
    }
    internal void CheckContext() => Post(CheckContextCore);
    void CheckContextCore()
    {
        if (!session.Active) return;
        var g = Native.FocusInfo();
        if (Native.GetForegroundWindow() != target || g.Focus != focus || !guard.IsSafe(target, focus) || !runtime.SequenceEqual(guard.Snapshot.RuntimeId) || Environment.TickCount64 - lastInput > 15000) ResetCore();
    }
    nint Key(int code, nint w, nint l)
    {
        if (code < 0) return Native.CallNextHookEx(keyHook, code, w, l);
        try
        {
            var k = Marshal.PtrToStructure<Native.KeyData>(l);
            if (InjectedInputPolicy.Ignore(k.Flags, k.Extra, testMode)) return Native.CallNextHookEx(keyHook, code, w, l);
            bool up = w == 0x101 || w == 0x105;
            if (up) return suppressed.Up(k.Vk) ? 1 : Native.CallNextHookEx(keyHook, code, w, l);
            Interlocked.Increment(ref events);
            if (suppressed.IsSuppressed(k.Vk)) return 1;
            // Typematic repeats of an already swallowed confirm are not new user edits.
            Interlocked.Increment(ref inputVersion);
            if (Handle(k)) { suppressed.Down(k.Vk); return 1; }
        }
        catch { Interlocked.Increment(ref errors); ResetCore(); }
        return Native.CallNextHookEx(keyHook, code, w, l);
    }
    bool Handle(Native.KeyData k)
    {
        var s = settings(); var win = Native.GetForegroundWindow(); var g = Native.FocusInfo();
        if (!s.Enabled || !guard.IsSafe(win, g.Focus)) { ResetCore(); return false; }
        var currentLayout = Native.GetKeyboardLayout(Native.GetWindowThreadProcessId(win, out _));
        if (session.Active && (win != target || g.Focus != focus || layout != currentLayout || !runtime.SequenceEqual(guard.Snapshot.RuntimeId) || Environment.TickCount64 - lastInput > 15000)) ResetCore();
        if (Native.Modifiers || Native.Ime(g.Focus)) { ResetCore(); return false; }
        if (k.Vk is 0x10 or 0xa0 or 0xa1) return false;
        lastInput = Environment.TickCount64;
        if (session.Visible && !Native.Down(0x10))
        {
            if (k.Vk is 0x26 or 0x28) { session.Move(k.Vk == 0x26 ? -1 : 1); Present(); return true; }
            if (k.Vk == 13 || k.Vk == 9 && s.TabConfirms) { ConfirmCore(session.Selected); return true; }
            if (k.Vk == 27) { ResetCore(); return true; }
        }
        if (k.Vk == 8) { session.Backspace(); Update(s); return false; }
        if (k.Vk is 0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x2d or 0x2e or 0x1b or 0x09 or 0x0d) { ResetCore(); return false; }
        var state = new byte[256]; if (Native.Down(0x10)) state[0x10] = 0x80; state[0x14] = (byte)(Native.GetKeyState(0x14) & 1);
        var b = new StringBuilder(8);
        if (Native.ToUnicodeEx(k.Vk, k.Scan, state, b, 8, 4, currentLayout) != 1) { ResetCore(); return false; }
        char c = b[0];
        if (session.Visible && c == s.Trigger[0] && s.ClosingTriggerConfirms)
        {
            int exact = session.Results.ToList().FindIndex(x => x.Shortcut.Equals(session.Query(s.Prefix), StringComparison.OrdinalIgnoreCase));
            if (exact >= 0) { ConfirmCore(exact); return true; }
        }
        bool was = session.Active; session.Feed(c, s.Prefix);
        if (!was && session.Active) { target = win; focus = g.Focus; layout = currentLayout; runtime = guard.Snapshot.RuntimeId; }
        Update(s); return false;
    }
    void Update(Settings s)
    {
        var q = session.Query(s.Prefix);
        Interlocked.Increment(ref presentation);
        if (q.Length < s.MinQuery) { Hide(); return; }
        long revision = session.Revision, request=Interlocked.Increment(ref searchRequest);
        QueueUi(() =>
        {
            if(request!=Interlocked.Read(ref searchRequest))return;
            // Search and WPF work never run in a hook callback or block the input thread.
            var found = search(q);
            Post(() => { if (session.ApplyResults(revision, found)) Present(); });
        });
    }
    void Present()
    {
        long version = Interlocked.Increment(ref presentation), revision = session.Revision;
        var q = session.Query(settings().Prefix); var items = session.Results; int selected = session.Selected;
        var win = target; var field = focus;
        QueueUi(() =>
        {
            if (version != Interlocked.Read(ref presentation)) return;
            bool visible = show(q, items, selected, win, field, revision);
            Post(() => session.SetVisible(revision, visible));
        });
    }
    internal void Confirm(int index, long revision) => Post(() => {if(session.Revision==revision)ConfirmCore(index);});
    internal void Browse(long revision) => Post(() => {if(session.Revision==revision)ConfirmCore(session.Results.Count);});
    void ConfirmCore(int index)
    {
        CheckContextCore();
        if (!session.Active || !session.Visible) return;
        if (index == session.Results.Count)
        {
            string q = session.Query(settings().Prefix), candidate = session.Candidate, prefix = settings().Prefix;
            var win = target; var field = focus; ResetCore();
            QueueUi(() => browse(q, win, field, candidate, prefix)); return;
        }
        var pending = session.BeginInsertion(index, settings().Prefix); if (pending == null) return;
        Interlocked.Increment(ref attempts);
        var window = target; var focused = focus; long inputAtConfirm = InputVersion; Hide();
        QueueUi(() =>
        {
            try { insert(pending.Item, window, focused, pending.Plan, inputAtConfirm, () => {Interlocked.Increment(ref completions);Post(() => session.CompleteInsertion(pending.Token));}); }
            catch { Interlocked.Increment(ref errors);Interlocked.Increment(ref completions);Post(() => session.CompleteInsertion(pending.Token)); }
        });
    }
    nint Mouse(int code, nint w, nint l)
    {
        try
        {
            if (code >= 0 && w is 0x201 or 0x204 or 0x207 or 0x20a or 0x20e)
            {
                var m = Marshal.PtrToStructure<Native.MouseData>(l); var bounds=Volatile.Read(ref hitArea).Bounds;
                bool inside = bounds.Valid && m.Point.X >= bounds.Left && m.Point.X <= bounds.Right && m.Point.Y >= bounds.Top && m.Point.Y <= bounds.Bottom;
                if (!inside) { Interlocked.Increment(ref inputVersion); ResetCore(); }
            }
        }
        catch { Interlocked.Increment(ref errors); ResetCore(); }
        return Native.CallNextHookEx(mouseHook, code, w, l);
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        input?.BeginInvokeShutdown(DispatcherPriority.Send);
        // Never wait on WPF from the input thread; shutdown is bounded.
        if (Thread.CurrentThread != hookThread) hookThread.Join(2000);
    }
}
