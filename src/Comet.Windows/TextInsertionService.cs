using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Text;
namespace Comet.Windows;
internal sealed class TextInsertionService(FocusGuard guard)
{
    int verifying;
    internal async Task<string?> Insert(nint target,nint focus,ReplacementPlan plan,Func<bool> unchanged)
    {
        var expectedRuntime=guard.Snapshot.RuntimeId;
        bool Context() => unchanged() && Native.GetForegroundWindow()==target && Native.FocusInfo().Focus==focus && guard.IsSafe(target,focus) && guard.Snapshot.RuntimeId.SequenceEqual(expectedRuntime);
        if(!Context())return "Insertion cancelled because the target field changed or is protected.";
        // A shifted closing trigger may still be held. Wait briefly without synthesizing modifier releases.
        for(int i=0;i<10 && (Native.Modifiers || Native.Down(0x10));i++)await Task.Delay(25);
        if(!Context() || Native.Modifiers || Native.Down(0x10))return "Insertion cancelled. Release modifiers and avoid typing until selection completes.";
        if(plan.DeleteCount>0)
        {
            if(Interlocked.CompareExchange(ref verifying,1,0)!=0)return "The destination is still responding. Try again in a moment.";
            var verification=Task.Run(()=>
            {
                try
                {
                    var element=AutomationElement.FocusedElement;
                    if(element==null || element.Current.IsPassword || !element.Current.HasKeyboardFocus || !element.GetRuntimeId().SequenceEqual(expectedRuntime))return false;
                    if(!element.TryGetCurrentPattern(TextPattern.Pattern,out var pattern))return false;
                    var ranges=((TextPattern)pattern).GetSelection();
                    if(ranges.Length!=1 || ranges[0].GetText(1).Length!=0)return false;
                    var range=ranges[0].Clone();
                    if(range.MoveEndpointByUnit(TextPatternRangeEndpoint.Start,TextUnit.Character,-plan.DeleteCount)!=-plan.DeleteCount)return false;
                    // Read only the bounded candidate range, never the field's full value or document.
                    return range.GetText(plan.DeleteCount+1)==plan.Expected;
                }
                catch{return false;}
                finally{Interlocked.Exchange(ref verifying,0);}
            });
            if(await Task.WhenAny(verification,Task.Delay(400))!=verification || !await verification)
                return "Replacement cancelled: the preceding shortcode could not be verified through accessibility. Use the picker’s Copy button in this application.";
        }
        if(!Context() || Native.Modifiers || Native.Down(0x10))return "Insertion cancelled because focus, typing, or modifiers changed.";
        var inputs=new List<Native.Input>();
        void Key(ushort vk,uint flags=0)=>inputs.Add(new(){Type=1,Data=new(){Key=new(){Vk=vk,Flags=flags,Extra=Native.InjectionTag}}});
        for(int i=0;i<plan.DeleteCount;i++){Key(8);Key(8,2);}
        foreach(char c in plan.Text.Replace("\r\n","\n").Replace('\r','\n'))
        {
            if(c=='\n'){Key(13);Key(13,2);}
            else{inputs.Add(new(){Type=1,Data=new(){Key=new(){Scan=c,Flags=4,Extra=Native.InjectionTag}}});inputs.Add(new(){Type=1,Data=new(){Key=new(){Scan=c,Flags=6,Extra=Native.InjectionTag}}});}
        }
        if(Native.SendInput((uint)inputs.Count,inputs.ToArray(),Marshal.SizeOf<Native.Input>())!=inputs.Count)
            return "Windows rejected some input. Check the destination; automatic retry is disabled to avoid duplicate text.";
        return null;
    }
}
