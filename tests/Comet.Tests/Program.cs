using Comet.Core;
using System.Diagnostics;
using System.Text.Json;
int passed=0,failed=0;
void Check(bool condition,string reason="Assertion failed") { if(!condition)throw new Exception(reason); }
void Test(string name,Action test) { try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);} }
void Throws(Action act){try{act();}catch{return;}throw new Exception("Expected rejection");}
ShortcutParser Parse(string text,string prefix=":") { var p=new ShortcutParser();foreach(char c in text)p.Feed(c,prefix);return p; }
var watch=Stopwatch.StartNew();var engine=new EmojiSearchEngine();watch.Stop();Console.WriteLine($"Dataset: {engine.Emojis.Count} entries; index construction {watch.ElapsedMilliseconds} ms");
Test("Parser recognizes a shortcode",()=>Check(Parse(":rocket").Query(":")=="rocket"));
Test("Ordinary text never buffered",()=>Check(Parse("ordinary typing without trigger").Candidate==""));
Test("Trigger requires word boundary",()=>Check(!Parse("email:rocket").Active));
Test("Whitespace permits trigger",()=>Check(Parse("hello :rocket").Candidate==":rocket"));
Test("Opening punctuation permits trigger",()=>Check(Parse("(:rocket").Candidate==":rocket"));
Test("Custom trigger",()=>Check(Parse(";coffee",";").Query(";")=="coffee"));
Test("Double trigger",()=>Check(Parse("::rocket","::").Query("::")=="rocket"));
Test("Incomplete double trigger has no query",()=>Check(Parse(":r","::").Query("::")==""));
Test("Backspace edits candidate",()=>{var p=Parse(":rocket");p.Backspace();Check(p.Query(":")=="rocke");});
Test("Backspace removes trigger",()=>{var p=Parse(":");p.Backspace();Check(!p.Active);});
Test("Movement reset invalidates revision",()=>{var p=Parse(":rocket");long rev=p.Revision;p.Reset();Check(!p.Active && p.Revision>rev);});
Test("Whitespace cancels candidate",()=>Check(!Parse(":rocket ").Active));
Test("Non-ASCII typing cancels candidate",()=>Check(!Parse(":rocké").Active));
Test("Candidate length is bounded",()=>Check(Parse(":"+new string('a',10000)).Candidate.Length<=65));
Test("Rapid repeated shortcode parsing",()=>{var p=new ShortcutParser();for(int i=0;i<10000;i++){foreach(char c in " :rocket")p.Feed(c,":");Check(p.Query(":")=="rocket");p.Reset();}});
Test("Exact emoji search",()=>Check(engine.Search("rocket",new()).First().Text=="🚀"));
Test("Partial emoji search",()=>Check(engine.Search("roc",new()).Any(e=>e.Text=="🚀")));
Test("Case insensitive search",()=>Check(engine.Search("ROCKET",new()).First().Text=="🚀"));
Test("Fuzzy edit distance",()=>Check(engine.Search("roket",new()).Any(e=>e.Text=="🚀")));
Test("Fuzzy can be disabled",()=>{engine.Invalidate();Check(!engine.Search("roket",new(){Fuzzy=false}).Any(e=>e.Text=="🚀"));});
Test("Aliases match",()=>Check(engine.Search("+1",new()).Any(e=>e.Text.Contains("👍"))));
Test("Dataset has gender and ZWJ sequences",()=>Check(engine.Emojis.Any(e=>e.Text.Contains('\u200d') && e.Name.Contains("woman"))));
Test("Dataset has variation selectors",()=>Check(engine.Emojis.Any(e=>e.Text.Contains('\ufe0f'))));
Test("Tone preference selects variation",()=>{engine.Invalidate();Check(engine.Search("thumbsup",new(){SkinTone=3},30).Any(e=>e.Text.Contains("🏽")));});
Test("Custom emoji takes precedence",()=>{engine.Invalidate();var s=new Settings{Expansions=[new("rocket","🛸")]};Check(engine.Search("rocket",s).First().Text=="🛸");});
Test("Multiline snippet expansion",()=>{engine.Invalidate();var s=new Settings{Expansions=[new("signature","Hello\n世界\n🚀","Snippet")]};Check(engine.Search("signature",s).First().Text=="Hello\n世界\n🚀");});
Test("Duplicate shortcuts rejected ignoring case",()=>Throws(()=>new Settings{Expansions=[new("Hi","a"),new("HI","b")]}.Validate()));
Test("Invalid shortcut rejected",()=>Throws(()=>new Settings{Expansions=[new("has space","a")]}.Validate()));
Test("Empty snippet rejected",()=>Throws(()=>new Settings{Expansions=[new("empty","","Snippet")]}.Validate()));
Test("Unsafe replacement candidate rejected",()=>Throws(()=>ReplacementPlan.Create(":rocket OTHER",":","🚀")));
Test("Replacement preserves surrounding text",()=>{var p=ReplacementPlan.Create(":rocket",":","🚀");Check(p.Apply("before :rocket after",14)=="before 🚀 after");});
Test("ZWJ replacement preserves UTF16 sequence",()=>{var p=ReplacementPlan.Create(":family",":","👩‍👩‍👧‍👦");Check(p.Apply(":family",7)=="👩‍👩‍👧‍👦");});
Test("Replacement requires a query",()=>Throws(()=>ReplacementPlan.Create(":",":","x")));
Test("Replacement caret bounds",()=>Throws(()=>ReplacementPlan.Create(":rocket",":","🚀").Apply("tiny",2)));
Test("Keyboard navigation wraps",()=>Check(Navigation.Move(0,-1,7)==6 && Navigation.Move(6,1,7)==0 && Navigation.Move(0,1,0)==-1));
Test("Executable exclusions exact match",()=>Check(ApplicationExclusions.Contains(["CODE.exe"],"code") && !ApplicationExclusions.Contains(["code"],"vscode")));
Test("Exclusion normalization",()=>{var s=new Settings{Exclusions=[" Code.exe ","CODE","", "notepad.exe"]};s.Validate();Check(s.Exclusions.SequenceEqual(new[]{"code","notepad"}));});
Test("Settings roundtrip preserves Unicode and newlines",()=>{
 var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");try{var m=new SettingsManager(path);var s=new Settings{Expansions=[new("signature","a\r\n日本語 👨‍👩‍👧‍👦","Snippet")]};m.Save(s);Check(m.Load().Expansions[0]==s.Expansions[0]);Check(!File.Exists(path+".tmp"));}finally{File.Delete(path);}
});
Test("Corrupt settings rejected",()=>Throws(()=>SettingsManager.Read("{broken")));
Test("Unknown settings version rejected",()=>Throws(()=>SettingsManager.Read("{\"Version\":99}")));
Test("Invalid trigger rejected",()=>Throws(()=>new Settings{Trigger="a"}.Validate()));
Test("Invalid popup count rejected",()=>Throws(()=>new Settings{MaxResults=999}.Validate()));
Test("Recent items capped and deduplicated",()=>{var s=new Settings();for(int i=0;i<70;i++)s.Used(i.ToString());s.Used("69");Check(s.Recent.Count==50 && s.Recent[0]=="69" && s.Frequency["69"]==2);});
Test("Favorites filter",()=>{engine.Invalidate();var s=new Settings{Favorites=["rocket"]};Check(engine.Search("",s,400,"Favorites").Single().Text=="🚀");});
Test("Recent ordering",()=>{engine.Invalidate();var s=new Settings();s.Used("rocket");s.Used("heart");Check(engine.Search("",s,100,"Recent").First().Id=="heart");});
Test("Category filter",()=>{engine.Invalidate();Check(engine.Search("",new(),100,"Animals & Nature").All(e=>e.Category=="Animals & Nature"));});
Test("Search cache invalidation",()=>{engine.Invalidate();var s=new Settings();engine.Search("newshort",s);s.Expansions.Add(new("newshort","✅"));engine.Invalidate();Check(engine.Search("newshort",s).First().Text=="✅");});
Test("Random surrounding text is preserved",()=>{
 var r=new Random(7);for(int i=0;i<1000;i++){string before=new('x',r.Next(0,100)),after=new('y',r.Next(0,100));var p=ReplacementPlan.Create(":rocket",":","🚀");Check(p.Apply(before+":rocket"+after,before.Length+7)==before+"🚀"+after);}
});
Test("Win32 INPUT x64 ABI layout",()=>Check(System.Runtime.InteropServices.Marshal.SizeOf<Comet.Windows.Native.Input>()==40 && System.Runtime.InteropServices.Marshal.SizeOf<Comet.Windows.Native.KeyInput>()==24));
Test("Win32 GUI thread info x64 ABI layout",()=>Check(System.Runtime.InteropServices.Marshal.SizeOf<Comet.Windows.Native.GuiInfo>()==72));
Test("Snippet variable deterministic expansion",()=>{var now=new DateTimeOffset(2026,11,8,14,5,6,TimeSpan.FromHours(5.5));var t=SnippetExpander.Expand("{{ time_24h }} {{ date_medium }}",now,System.Globalization.CultureInfo.InvariantCulture);Check(t=="14:05 08 Nov 2026");});
Test("Unknown snippet variables preserved",()=>Check(SnippetExpander.Expand("{{ unknown }}",DateTimeOffset.Now)=="{{ unknown }}"));
Test("Snippet ISO timestamp retains offset",()=>Check(SnippetExpander.Expand("{{date_time_iso}}",new DateTimeOffset(2026,11,8,14,5,6,TimeSpan.FromHours(5.5))).EndsWith("+05:30")));

Test("100 consecutive sessions replace rocket, heart, fire without restart",()=>{
    var session=new AutocompleteSession();var suppression=new KeySuppression();var settings=new Settings();string text="",expected="";
    string[] queries=["rocket","heart","fire"]; string[] output=["🚀","❤️","🔥"];
    for(int cycle=0;cycle<100;cycle++)
    {
        string candidate=":"+queries[cycle%3];
        foreach(char c in candidate){session.Feed(c,":");text+=c;}
        long revision=session.Revision;
        Check(session.ApplyResults(revision,engine.Search(queries[cycle%3],settings)),"Current search discarded");
        session.SetVisible(revision,true);suppression.Down(13);
        var pending=session.BeginInsertion(0,":");Check(pending!=null,"Insertion missing at cycle "+cycle);
        Check(!session.Active && session.Inserting,"Session did not enter insertion state");
        // Real monitor applies this policy before processing any synthetic deletion or Unicode key.
        for(int key=0;key<pending!.Plan.DeleteCount*2+pending.Plan.Text.Length*2;key++)
            Check(InjectedInputPolicy.Ignore(0x10,InjectedInputPolicy.Tag,false),"Own synthetic input leaked");
        text=pending.Plan.Apply(text,text.Length);expected+=output[cycle%3];
        Check(text==expected,"Wrong surrounding text at cycle "+cycle);
        Check(session.CompleteInsertion(pending.Token) && !session.Inserting,"Completion stuck");
        Check(suppression.Up(13) && !suppression.IsSuppressed(13),"Confirm key remained suppressed");
    }
});
Test("Completion never erases the next shortcut",()=>{
    var s=new AutocompleteSession();foreach(char c in ":rocket")s.Feed(c,":");s.ApplyResults(s.Revision,engine.Search("rocket",new()));s.SetVisible(s.Revision,true);
    var pending=s.BeginInsertion(0,":")!;foreach(char c in ":heart")s.Feed(c,":");
    s.CompleteInsertion(pending.Token);Check(s.Query(":")=="heart");
});
Test("Stale completion cannot clear a newer insertion",()=>{
    var s=new AutocompleteSession();
    PendingInsertion Start(string query){foreach(char c in ":"+query)s.Feed(c,":");s.ApplyResults(s.Revision,engine.Search(query,new()));s.SetVisible(s.Revision,true);return s.BeginInsertion(0,":")!;}
    var first=Start("rocket");var second=Start("heart");Check(!s.CompleteInsertion(first.Token) && s.Inserting);Check(s.CompleteInsertion(second.Token) && !s.Inserting);
});
Test("100 cancellations and failed insertions recover",()=>{
    var s=new AutocompleteSession();
    for(int n=0;n<100;n++)
    {
        foreach(char c in ":rocket")s.Feed(c,":");
        long old=s.Revision;s.Reset();Check(!s.ApplyResults(old,engine.Search("rocket",new())),"Cancelled search reappeared");
        foreach(char c in ":hearts")s.Feed(c,":");s.Backspace();Check(s.Query(":")=="heart");
        s.ApplyResults(s.Revision,engine.Search("heart",new()));s.SetVisible(s.Revision,true);
        var pending=s.BeginInsertion(0,":")!;Check(s.CompleteInsertion(pending.Token)); // Same finally path for success, timeout, rejection, exception.
        Check(!s.Inserting && !s.Active);
    }
});
Test("Old search results cannot overwrite a newer query",()=>{
    var s=new AutocompleteSession();foreach(char c in ":roc")s.Feed(c,":");long old=s.Revision;s.Feed('k',":");
    Check(!s.ApplyResults(old,engine.Search("roc",new())));Check(s.Query(":")=="rock");
});
Test("Results from a completed session cannot reopen the popup",()=>{
    var s=new AutocompleteSession();foreach(char c in ":rocket")s.Feed(c,":");long old=s.Revision;
    s.ApplyResults(old,engine.Search("rocket",new()));s.SetVisible(old,true);s.BeginInsertion(0,":");
    Check(!s.ApplyResults(old,engine.Search("rocket",new())) && !s.Visible);
});
Test("Invisible suggestions cannot intercept confirmation",()=>{
    var s=new AutocompleteSession();foreach(char c in ":rocket")s.Feed(c,":");s.ApplyResults(s.Revision,engine.Search("rocket",new()));
    Check(s.BeginInsertion(0,":")==null && s.Active);
});
Test("Search-more row participates in keyboard navigation",()=>{
    var s=new AutocompleteSession();foreach(char c in ":rocket")s.Feed(c,":");s.ApplyResults(s.Revision,engine.Search("rocket",new(),1));
    s.Move(1);Check(s.Selected==s.Results.Count);s.Move(1);Check(s.Selected==0);s.Move(-1);Check(s.Selected==s.Results.Count);
});
Test("Cancelled session still swallows the matching key-up",()=>{
    var keys=new KeySuppression();keys.Down(13);var s=new AutocompleteSession();s.Reset();Check(keys.IsSuppressed(13));Check(keys.Up(13));Check(!keys.Up(13));
});
Test("Physical confirm autorepeat remains suppressed until release",()=>{
    var keys=new KeySuppression();keys.Down(13);for(int n=0;n<100;n++)Check(keys.IsSuppressed(13));keys.Up(13);Check(!keys.IsSuppressed(13));
});
Test("Own injected events are ignored even in test mode",()=>Check(InjectedInputPolicy.Ignore(0x10,InjectedInputPolicy.Tag,true)));
Test("External injected events require explicit test mode",()=>Check(InjectedInputPolicy.Ignore(0x10,0,false) && !InjectedInputPolicy.Ignore(0x10,0,true)));
Test("Physical keystrokes pass injection filter",()=>Check(!InjectedInputPolicy.Ignore(0,0,false)));
Test("Stale visibility acknowledgement cannot activate new query",()=>{
    var s=new AutocompleteSession();foreach(char c in ":roc")s.Feed(c,":");long old=s.Revision;s.Feed('k',":");s.SetVisible(old,true);Check(!s.Visible);
});
Test("Search-more replacement preserves original candidate range",()=>{
    var plan=ReplacementPlan.Create(":roc",":","🚀");Check(plan.Apply("hello :roc",10)=="hello 🚀");
});
Test("Caret placement flips above the bottom screen edge",()=>{
    var p=PopupPlacement.Calculate(new(400,1040,401,1060),new(0,0,1920,1080),288,180,1);Check(p.Above && p.Y>=0 && p.Y+180<=1080);
});
Test("Caret placement stays on a negative-coordinate monitor",()=>{
    var p=PopupPlacement.Calculate(new(-1918,80,-1917,100),new(-1920,0,0,1080),288,180,1);Check(p.X>=-1920 && p.X+288<=0 && !p.Above);
});
Test("Right-edge caret placement clamps and keeps pointer on card",()=>{
    var p=PopupPlacement.Calculate(new(1918,80,1919,100),new(0,0,1920,1080),288,180,1);Check(p.X+288<=1920 && p.TipX<=264 && p.TipX>=24);
});
Test("Geometry respects multiple DPI scales",()=>{
    foreach(double scale in new[]{1.0,1.25,1.5,2.0})
    {
        var p=PopupPlacement.Calculate(new(450,300,451,322),new(0,0,1920,1080),288*scale,200*scale,scale);
        Check(p.X>=0 && p.Y>=0 && p.X+288*scale<=1920 && p.Y+200*scale<=1080);
    }
});
Test("Missing caret geometry is rejected rather than using mouse",()=>Throws(()=>PopupPlacement.Calculate(default,new(0,0,1920,1080),288,180,1)));
Test("Popup clamps to top edge on tiny work areas",()=>{
    var p=PopupPlacement.Calculate(new(8,20,9,40),new(0,0,200,100),288,180,1);Check(p.X==0 && p.Y==0);
});

Test("Exact heart shortcode outranks other heart keyword matches",()=>Check(engine.Search("heart",new()).First().Text=="❤️"));
Test("Search cache cannot leak custom aliases between settings instances",()=>{
    var first=new Settings{Expansions=[new("rocket","🛸")]};Check(engine.Search("rocket",first).First().Text=="🛸");Check(engine.Search("rocket",new()).First().Text=="🚀");
});
Test("Exact built-in name outranks a longer custom prefix",()=>Check(engine.Search("rocket",new(){Expansions=[new("rocketlong","x")]}).First().Text=="🚀"));
Test("Native and core injection markers agree",()=>Check(Comet.Windows.Native.InjectionTag==InjectedInputPolicy.Tag));
Test("Erase entire shortcut then immediately type next trigger",()=>{
    var p=Parse(":rocket");for(int n=0;n<7;n++)p.Backspace();foreach(char c in ":heart")p.Feed(c,":");Check(p.Query(":")=="heart");
});
Test("100 rapid popup reopens cancel old fades and restore opacity",()=>{
    var state=new PopupTransitionState();state.Show();
    for(int n=0;n<100;n++){long fading=state.Hide();Check(state.FadingOut);Check(state.Show(),"Opacity restoration not requested");Check(state.Requested && !state.FadingOut && !state.CanFinishHide(fading),"Old fade can hide the next session");}
});
Test("Completed popup dismissal may hide only its own epoch",()=>{
    var state=new PopupTransitionState();state.Show();long first=state.Hide();state.Show();long last=state.Hide();Check(!state.CanFinishHide(first) && state.CanFinishHide(last));
});
Test("Explicit read-only value provider cannot be overridden by text fallback",()=>Check(!FieldEligibility.CanEdit(true,true,true,false)));
Test("Unknown or read-only text metadata fails closed",()=>Check(!FieldEligibility.CanEdit(false,false,true,null) && !FieldEligibility.CanEdit(false,false,true,true) && !FieldEligibility.CanEdit(false,false,false,false)));
Test("Explicit writable value or text metadata allows editing",()=>Check(FieldEligibility.CanEdit(true,false,false,null) && FieldEligibility.CanEdit(false,false,true,false)));
var times=new List<double>();foreach(var q in new[]{"rocket","heart","thumb","woman","roket","coffee","smile","flag","family","dance"}){engine.Invalidate();var sw=Stopwatch.StartNew();engine.Search(q,new());sw.Stop();times.Add(sw.Elapsed.TotalMilliseconds);}
Console.WriteLine($"Uncached search: median {times.Order().ElementAt(times.Count/2):F2} ms, max {times.Max():F2} ms. Process working set {Environment.WorkingSet/1024/1024} MB (Linux test runner, not Windows app).");
Console.WriteLine($"RESULT {passed} passed, {failed} failed");return failed==0?0:1;
