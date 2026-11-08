using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms=System.Windows.Forms;
namespace Comet.Windows;
internal sealed record PickerReplacement(nint Window,nint Focus,string Candidate,string Prefix);
internal sealed class AppHost : IDisposable
{
    internal static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Comet");
    internal Settings Settings { get; private set; }
    internal EmojiSearchEngine Search { get; } = new();
    readonly SettingsManager manager=new(Path.Combine(DataDirectory,"settings.json"));
    readonly FocusGuard guard;
    readonly TextInsertionService insertion;
    readonly CaretLocator caret;
    readonly KeyboardMonitor keyboard;
    readonly AutocompletePopup popup;
    readonly Forms.NotifyIcon tray;
    readonly HwndSource messageWindow;
    readonly DispatcherTimer contextTimer;
    SettingsWindow? settingsWindow;
    PickerWindow? picker;
    GifWindow? gifs;
    nint lastExternal;
    bool disposed;
    bool settingsUnreadable;
    internal AppHost()
    {
        string? warning=null;
        try{Settings=manager.Load();}catch(Exception e){settingsUnreadable=true;Settings=new();Settings.Enabled=false;warning="Settings could not be read; autocomplete is paused. The original file has been kept.\n"+e.Message;}
        Ui.Theme(Settings.Theme);guard=new(()=>Settings);insertion=new(guard);
        caret=new(guard);
        popup=new(caret,(i,revision)=>keyboard!.Confirm(i,revision),revision=>keyboard!.Browse(revision),bounds=>keyboard?.SetPopupBounds(bounds),(revision,visible)=>keyboard?.SetVisible(revision,visible));
        keyboard=new(guard,()=>Settings,q=>Search.Search(q,Settings,Settings.MaxResults),popup.Present,popup.Dismiss,Insert,
            (query,target,focus,candidate,prefix)=>OpenPicker(query,new(target,focus,candidate,prefix)));
        messageWindow=new HwndSource(new HwndSourceParameters("Comet hotkey receiver"){Width=0,Height=0,WindowStyle=0,ParentWindow=new nint(-3)});messageWindow.AddHook(Messages);
        tray=new Forms.NotifyIcon{Text="Comet · Emoji autocomplete",Visible=true,Icon=new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/comet.ico")).Stream)};
        tray.DoubleClick+=(_,_)=>OpenPicker();UpdateTray();
        if(!RegisterHotkey(Settings.PickerHotkey))warning="The picker hotkey is already registered by another application. You can open the picker from the tray or choose another shortcut in Settings.";
        contextTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(140)};contextTimer.Tick+=(_,_)=>{var w=Native.GetForegroundWindow();Native.GetWindowThreadProcessId(w,out uint pid);if(pid!=Environment.ProcessId && guard.IsSafe(w,Native.FocusInfo().Focus))lastExternal=w;};contextTimer.Start();
        if(warning!=null)Ui.Error(warning);
        if(!File.Exists(Path.Combine(DataDirectory,"settings.json"))) { Save();OpenSettings(); }
    }
    nint Messages(nint h,int m,nint w,nint l,ref bool handled){if(m==0x312 && w==1){handled=true;OpenPicker();}return 0;}
    bool RegisterHotkey(string name)
    {
        Native.UnregisterHotKey(messageWindow.Handle,1);
        (uint mods,uint key)=name switch {"Ctrl+Alt+E"=>(3u,0x45u),"Alt+Shift+E"=>(5u,0x45u),_=>(6u,0x20u)};
        return Native.RegisterHotKey(messageWindow.Handle,1,mods|0x4000,key);
    }
    internal void ApplySettings(Settings next)
    {
        next.Validate();
        var old=Settings;
        if(!RegisterHotkey(next.PickerHotkey)){RegisterHotkey(old.PickerHotkey);throw new InvalidOperationException("That global shortcut is in use. Choose another.");}
        try
        {
            using var run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if(next.StartAtLogin)run.SetValue("Comet","\""+Environment.ProcessPath+"\"");else run.DeleteValue("Comet",false);
            manager.Save(next);Settings=next;settingsUnreadable=false;
        }
        catch {RegisterHotkey(old.PickerHotkey);throw;}
        Ui.Theme(Settings.Theme);popup.RefreshTheme();Search.Invalidate();keyboard.Reset();UpdateTray();
    }
    internal void Save(){if(settingsUnreadable)return;try{manager.Save(Settings);}catch(Exception e){tray?.ShowBalloonTip(4000,"Comet · Could not save",e.Message,Forms.ToolTipIcon.Warning);}}
    internal void Used(string id){Settings.Used(id);Search.Invalidate();Save();}
    internal void Copy(string text){try{Clipboard.SetText(text);}catch(Exception e){Ui.Error("Clipboard unavailable: "+e.Message);}}
    async void Insert(Suggestion item,nint target,nint focus,ReplacementPlan plan,long version,Action completed)
    {
        try
        {
            if(item.Category=="Snippet")plan=plan with {Text=SnippetExpander.Expand(plan.Text,DateTimeOffset.Now)};
            var error=await insertion.Insert(target,focus,plan,()=>keyboard.InputVersion==version);
            if(error!=null)tray.ShowBalloonTip(3500,"Comet",error,Forms.ToolTipIcon.Info);
            else {Used(item.Id);if(Settings.LeaveOnClipboard)Copy(plan.Text);}
        }
        catch(Exception e){tray.ShowBalloonTip(3500,"Comet · Insertion cancelled",e.Message,Forms.ToolTipIcon.Warning);}
        finally{completed();}
    }
    internal async void InsertFromPicker(Suggestion item,nint target,PickerReplacement? replacement=null)
    {
        keyboard.Reset();
        if(target==0 || !Native.SetForegroundWindow(target)){Ui.Error("The destination could not be focused. Use Copy instead.");return;}
        try
        {
            for(int i=0;i<10;i++)
            {
                await Task.Delay(60);
                if(Native.GetForegroundWindow()!=target)break;
                var g=Native.FocusInfo();
                if(guard.IsSafe(target,g.Focus) && !Native.Modifiers && !Native.Down(0x10))
                {
                    if(replacement!=null && g.Focus!=replacement.Focus){Ui.Error("The original text field changed. Insertion cancelled.");return;}
                    string text=item.Category=="Snippet"?SnippetExpander.Expand(item.Text,DateTimeOffset.Now):item.Text;
                    var plan=replacement==null?new ReplacementPlan(0,text):ReplacementPlan.Create(replacement.Candidate,replacement.Prefix,text);
                    long version=keyboard.InputVersion;
                    var error=await insertion.Insert(target,g.Focus,plan,()=>keyboard.InputVersion==version);
                    if(error==null){Used(item.Id);if(Settings.LeaveOnClipboard)Copy(text);}else Ui.Error(error);
                    return;
                }
            }
            Ui.Error("Insertion is unavailable in this field. Open the picker and use Copy.");
        }
        catch(Exception e){Ui.Error("Insertion cancelled: "+e.Message);}
    }
    internal void OpenPicker(string initialQuery="",PickerReplacement? replacement=null)
    {
        keyboard.Reset();if(picker?.IsVisible==true){picker.Activate();return;}
        var target=replacement?.Window??Native.GetForegroundWindow();
        if(replacement==null){Native.GetWindowThreadProcessId(target,out uint pid);if(pid==Environment.ProcessId || !guard.IsSafe(target,Native.FocusInfo().Focus))target=lastExternal;}
        picker=new(this,target,initialQuery,replacement);picker.Closed+=(_,_)=>picker=null;picker.Show();
    }
    internal void OpenSettings(){keyboard.Reset();if(settingsWindow?.IsVisible==true){settingsWindow.Activate();return;}settingsWindow=new(this);settingsWindow.Closed+=(_,_)=>settingsWindow=null;settingsWindow.Show();}
    internal void OpenGifs(){keyboard.Reset();if(gifs?.IsVisible==true){gifs.Activate();return;}gifs=new(this);gifs.Closed+=(_,_)=>gifs=null;gifs.Show();}
    void UpdateTray()
    {
        var old=tray.ContextMenuStrip;var menu=new Forms.ContextMenuStrip();
        menu.Items.Add("Open emoji picker",null,(_,_)=>OpenPicker());menu.Items.Add("Settings",null,(_,_)=>OpenSettings());menu.Items.Add("GIF library",null,(_,_)=>OpenGifs());
        menu.Items.Add(Settings.Enabled?"Pause autocomplete":"Resume autocomplete",null,(_,_)=>{Settings.Enabled=!Settings.Enabled;keyboard.Reset();Save();UpdateTray();});
        menu.Items.Add("Input diagnostics",null,(_,_)=>Ui.Error(keyboard.Diagnostics));
        menu.Items.Add("Quit Comet",null,(_,_)=>Application.Current.Shutdown());tray.ContextMenuStrip=menu;old?.Dispose();tray.Text=Settings.Enabled?"Comet · Autocomplete enabled":"Comet · Autocomplete paused";
    }
    public void Dispose(){if(disposed)return;disposed=true;contextTimer.Stop();keyboard.Dispose();caret.Dispose();guard.Dispose();Native.UnregisterHotKey(messageWindow.Handle,1);messageWindow.Dispose();tray.Visible=false;tray.Dispose();popup.Close();Save();}
}
internal static class Program
{
    static void ExportPopupPreview()
    {
        var previewApp=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        previewApp.Startup+=(_,_)=>
        {
            try
            {
                Ui.Theme("Light");using var guard=new FocusGuard(()=>new Settings());using var caret=new CaretLocator(guard);
                var popup=new AutocompletePopup(caret,(_,_)=>{},_=>{},_=>{},(_,_)=>{});
                popup.Present("roc",[new("rocket","rocket","🚀","Travel & Places","rocket")],0,0,0,1);
                string path=Path.Combine(AppContext.BaseDirectory,"Comet-popup-preview.png");popup.CapturePreview(path);popup.Close();
                MessageBox.Show("Saved actual WPF popup render:\n"+path+"\nThis preview does not test keyboard input or caret placement.","Comet popup preview");
            }
            catch(Exception e){MessageBox.Show("Preview could not be saved: "+e.Message,"Comet");}
            previewApp.Shutdown();
        };
        previewApp.Run();
    }
    [STAThread] public static void Main()
    {
        if(Environment.GetCommandLineArgs().Contains("--popup-preview")){ExportPopupPreview();return;}
        using var singleton=new Mutex(true,@"Local\Comet.EmojiAutocomplete",out bool first);
        if(!first){MessageBox.Show("Comet is already running. Open it from the system tray.","Comet");return;}
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};AppHost? host=null;
        app.Startup+=(_,_)=>{try{host=new();}catch(Exception e){MessageBox.Show("Comet could not start:\n"+e.Message,"Comet",MessageBoxButton.OK,MessageBoxImage.Error);app.Shutdown(1);}};
        app.Exit+=(_,_)=>host?.Dispose();app.Run();
    }
}
