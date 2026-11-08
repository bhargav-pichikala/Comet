using System.Runtime.InteropServices;
using System.Text;
namespace Comet.Windows;
internal sealed class KeyboardMonitor : IDisposable
{
    readonly Native.HookProc keyProc, mouseProc;
    nint keyHook,mouseHook;
    readonly FocusGuard guard;
    readonly Func<Settings> settings;
    readonly Func<string,IReadOnlyList<Suggestion>> search;
    readonly Action<IReadOnlyList<Suggestion>,int> show;
    readonly Action hide;
    readonly Action<Suggestion,nint,nint,ReplacementPlan> insert;
    readonly Func<Native.Point,bool> insidePopup;
    readonly ShortcutParser parser=new();
    readonly HashSet<uint> suppressed=[];
    IReadOnlyList<Suggestion> results=[];
    nint target,focus,layout;
    int selected;
    long lastInput;
    internal long InputVersion { get; private set; }
    int[] runtime=[];
    internal KeyboardMonitor(FocusGuard guard,Func<Settings> settings,Func<string,IReadOnlyList<Suggestion>> search,Action<IReadOnlyList<Suggestion>,int> show,Action hide,Action<Suggestion,nint,nint,ReplacementPlan> insert,Func<Native.Point,bool> insidePopup)
    {
        this.guard=guard;this.settings=settings;this.search=search;this.show=show;this.hide=hide;this.insert=insert;this.insidePopup=insidePopup;
        keyProc=Key;mouseProc=Mouse;
        keyHook=Native.SetWindowsHookEx(13,keyProc,Native.GetModuleHandle(null),0);
        mouseHook=Native.SetWindowsHookEx(14,mouseProc,Native.GetModuleHandle(null),0);
        if(keyHook==0 || mouseHook==0) { Dispose(); throw new InvalidOperationException("Windows could not install input hooks: "+Marshal.GetLastWin32Error()); }
    }
    internal void Reset() { parser.Reset();results=[];selected=0;hide(); }
    internal void CheckContext()
    {
        if(!parser.Active)return;
        var g=Native.FocusInfo();
        if(Native.GetForegroundWindow()!=target || g.Focus!=focus || !guard.IsSafe(target,focus) || !runtime.SequenceEqual(guard.Snapshot.RuntimeId) || Environment.TickCount64-lastInput>15000) Reset();
    }
    nint Key(int code,nint w,nint l)
    {
        if(code<0)return Native.CallNextHookEx(keyHook,code,w,l);
        var k=Marshal.PtrToStructure<Native.KeyData>(l);
        if((k.Flags&0x10)!=0 && (!Environment.GetCommandLineArgs().Contains("--test-input") || k.Extra==Native.InjectionTag))return Native.CallNextHookEx(keyHook,code,w,l);
        bool up=w==0x101 || w==0x105;
        if(up) { if(suppressed.Remove(k.Vk))return 1;return Native.CallNextHookEx(keyHook,code,w,l); }
        InputVersion++;
        if(suppressed.Contains(k.Vk))return 1;
        try
        {
            if(Handle(k)) { suppressed.Add(k.Vk);return 1; }
        }
        catch { Reset(); }
        return Native.CallNextHookEx(keyHook,code,w,l);
    }
    bool Handle(Native.KeyData k)
    {
        var s=settings(); var win=Native.GetForegroundWindow();var g=Native.FocusInfo();
        if(!s.Enabled || !guard.IsSafe(win,g.Focus)) { Reset(); return false; }
        var currentLayout=Native.GetKeyboardLayout(Native.GetWindowThreadProcessId(win,out _));
        if(parser.Active && (win!=target || g.Focus!=focus || layout!=currentLayout || !runtime.SequenceEqual(guard.Snapshot.RuntimeId) || Environment.TickCount64-lastInput>15000)) Reset();
        if(Native.Modifiers || Native.Ime(g.Focus)) { Reset();return false; }
        // Shift is allowed for trigger punctuation; it cannot confirm or navigate.
        if(k.Vk is 0x10 or 0xa0 or 0xa1) return false;
        lastInput=Environment.TickCount64;
        if(results.Count>0 && !Native.Down(0x10))
        {
            if(k.Vk==0x26 || k.Vk==0x28) { selected=Navigation.Move(selected,k.Vk==0x26?-1:1,results.Count);var current=results;int index=selected;System.Windows.Application.Current.Dispatcher.BeginInvoke(()=>{if(ReferenceEquals(results,current))show(current,index);});return true; }
            if(k.Vk==13 || k.Vk==9 && s.TabConfirms) { Confirm(selected);return true; }
            if(k.Vk==27) { Reset();return true; }
        }
        if(k.Vk==8) { parser.Backspace(); Update(s);return false; }
        if(k.Vk is 0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x2d or 0x2e or 0x1b or 0x09 or 0x0d) { Reset();return false; }
        var state=new byte[256];
        if(Native.Down(0x10)) state[0x10]=0x80;
        state[0x14]=(byte)(Native.GetKeyState(0x14)&1);
        var b=new StringBuilder(8);
        int count=Native.ToUnicodeEx(k.Vk,k.Scan,state,b,8,4,currentLayout);
        if(count!=1) { Reset();return false; }
        char c=b[0];
        // Closing trigger is swallowed only for an exact shortcode; it is not part of deletion.
        if(results.Count>0 && c==s.Trigger[0] && s.ClosingTriggerConfirms)
        {
            int exact=results.ToList().FindIndex(x=>x.Shortcut.Equals(parser.Query(s.Prefix),StringComparison.OrdinalIgnoreCase));
            if(exact>=0) { Confirm(exact);return true; }
        }
        bool was=parser.Active;parser.Feed(c,s.Prefix);
        if(!was && parser.Active) { target=win;focus=g.Focus;layout=currentLayout;runtime=guard.Snapshot.RuntimeId; }
        Update(s);return false;
    }
    void Update(Settings s)
    {
        var q=parser.Query(s.Prefix);
        results=[];selected=0;hide();
        if(q.Length<s.MinQuery)return;
        long revision=parser.Revision;
        System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,()=>
        {
            if(parser.Revision!=revision || !parser.Active)return;
            results=search(q);
            if(results.Count>0)show(results,selected);else hide();
        });
    }
    internal void Confirm(int index)
    {
        CheckContext();
        if(index<0 || index>=results.Count || !parser.Active)return;
        var item=results[index];var plan=ReplacementPlan.Create(parser.Candidate,settings().Prefix,item.Text);
        var win=target;var field=focus;Reset();
        insert(item,win,field,plan);
    }
    nint Mouse(int code,nint w,nint l)
    {
        if(code>=0 && w is 0x201 or 0x204 or 0x207 or 0x20a or 0x20e)
        { var m=Marshal.PtrToStructure<Native.MouseData>(l);if(!insidePopup(m.Point)){InputVersion++;Reset();} }
        return Native.CallNextHookEx(mouseHook,code,w,l);
    }
    public void Dispose() { if(keyHook!=0)Native.UnhookWindowsHookEx(keyHook);if(mouseHook!=0)Native.UnhookWindowsHookEx(mouseHook);keyHook=mouseHook=0; }
}
