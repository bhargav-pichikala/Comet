using System.Diagnostics;
using System.Windows.Automation;
namespace Comet.Windows;
internal sealed record FocusSnapshot(nint Window, nint Focus, int[] RuntimeId, long CheckedAt, bool Safe, string Reason);
// UI Automation runs only on this background thread. No Value/Text properties are read.
internal sealed class FocusGuard : IDisposable
{
    volatile FocusSnapshot snapshot = new(0,0,[],0,false,"Checking field accessibility");
    readonly Thread thread;
    readonly AutomationFocusChangedEventHandler changed;
    volatile bool stopped;
    readonly Func<Settings> settings;
    internal FocusSnapshot Snapshot => snapshot;
    internal FocusGuard(Func<Settings> settings)
    { this.settings=settings; changed=(sender,_)=>
    {
        var previous=snapshot;
        try
        {
            // Duplicate focus notifications for the same field must not cancel a new shortcode.
            if(sender is AutomationElement element && previous.Safe && !element.Current.IsPassword && element.Current.HasKeyboardFocus && element.GetRuntimeId().SequenceEqual(previous.RuntimeId) && Native.GetForegroundWindow()==previous.Window && Native.FocusInfo().Focus==previous.Focus)return;
        }
        catch { }
        snapshot=new(0,0,[],0,false,"Focus changed");
    }; thread=new Thread(Poll){ IsBackground=true, Name="Comet field privacy guard" }; thread.SetApartmentState(ApartmentState.MTA); thread.Start(); }
    internal bool IsSafe(nint window, nint focus) { var current=snapshot;return current.Safe && current.Window==window && current.Focus==focus && Environment.TickCount64-current.CheckedAt<500; }
    void Poll()
    {
        try { Automation.AddAutomationFocusChangedEventHandler(changed); } catch { }
        while(!stopped)
        {
            var window=Native.GetForegroundWindow(); var g=Native.FocusInfo();
            try
            {
                Native.GetWindowThreadProcessId(window,out uint pid);
                using var process=Process.GetProcessById((int)pid);
                if(pid == Environment.ProcessId || ApplicationExclusions.Contains(settings().Exclusions,process.ProcessName)) { Deny(window,g.Focus,"Application excluded"); }
                else
                {
                    var element=AutomationElement.FocusedElement;
                    if(element == null) Deny(window,g.Focus,"Field inaccessible");
                    else
                    {
                        var c=element.Current;
                        bool safe=c.ProcessId==(int)pid && c.HasKeyboardFocus && c.IsEnabled && !c.IsPassword;
                        // Require an explicitly exposed editable field; unknown controls fail closed.
                        bool hasValue=element.TryGetCurrentPattern(ValuePattern.Pattern,out var vp);
                        bool hasText=false;bool? textReadOnly=null;
                        if(!hasValue && (c.ControlType==ControlType.Edit || c.ControlType==ControlType.Document) && element.TryGetCurrentPattern(TextPattern.Pattern,out var tp))
                        {
                            hasText=true;
                            if(((TextPattern)tp).DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute) is bool readOnly)textReadOnly=readOnly;
                        }
                        bool editable=FieldEligibility.CanEdit(hasValue,hasValue && ((ValuePattern)vp).Current.IsReadOnly,hasText,textReadOnly);
                        safe &= editable && !Native.Ime(g.Focus);
                        var runtime=element.GetRuntimeId();
                        if(window != Native.GetForegroundWindow() || g.Focus != Native.FocusInfo().Focus) safe=false;
                        snapshot=new(window,g.Focus,runtime,Environment.TickCount64,safe,safe?"Ready":c.IsPassword?"Password field":"Unsupported or protected field");
                    }
                }
            }
            catch { Deny(window,g.Focus,"Field inaccessible"); }
            Thread.Sleep(120);
        }
        try { Automation.RemoveAutomationFocusChangedEventHandler(changed); } catch { }
    }
    void Deny(nint window,nint focus,string reason) => snapshot=new(window,focus,[],Environment.TickCount64,false,reason);
    public void Dispose() { stopped=true; }
}
