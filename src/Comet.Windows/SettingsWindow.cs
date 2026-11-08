using Microsoft.Win32;
namespace Comet.Windows;
internal sealed class SettingsWindow : Window
{
    readonly AppHost host;
    Settings draft;
    readonly CheckBox enabled=new(){Content="Enable system-wide autocomplete"},startup=new(){Content="Launch when I sign in"},fuzzy=new(){Content="Fuzzy search"},tab=new(){Content="Tab confirms selection"},closing=new(){Content="Closing trigger confirms exact shortcode"},doubleTrigger=new(){Content="Require two trigger characters"},clipboard=new(){Content="Also leave selected text on clipboard"};
    readonly TextBox trigger=new(){Width=70},minimum=new(){Width=70},maximum=new(){Width=70},exclusions=new(){AcceptsReturn=true,Height=180,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    readonly ComboBox theme=new(){Width=160},hotkey=new(){Width=200};
    readonly DataGrid expansions=new(){AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,Height=260,SelectionMode=DataGridSelectionMode.Single};
    readonly TextBox filter=new();
    readonly TextBlock status=Ui.Label("",12,Ui.Muted);
    readonly StackPanel favoriteList=new();
    internal SettingsWindow(AppHost host)
    {
        this.host=host;draft=SettingsManager.Clone(host.Settings);Title="Comet · Settings";Width=790;Height=740;MinWidth=650;MinHeight=580;Ui.Window(this);
        theme.ItemsSource=new[]{"System","Light","Dark"};hotkey.ItemsSource=new[]{"Ctrl+Shift+Space","Ctrl+Alt+E","Alt+Shift+E"};
        foreach(var c in new[]{enabled,startup,fuzzy,tab,closing,doubleTrigger,clipboard}){c.Margin=new Thickness(0,7,0,7);c.Foreground=Ui.Fg;}
        var tabs=new TabControl{Background=Ui.Bg,Foreground=Ui.Fg};
        void Page(string title,UIElement content) => tabs.Items.Add(new TabItem{Header=title,Content=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=new Border{Padding=new Thickness(18),Child=content}}});
        Page("General",Ui.Column(Ui.Label("Make room for expression",25),enabled,startup,Field("Picker shortcut",hotkey),Field("Appearance (new windows)",theme),Field("Trigger",trigger),doubleTrigger,Field("Minimum letters",minimum),Field("Maximum suggestions",maximum),fuzzy,tab,closing,clipboard,Ui.Label("Autocomplete stays off in password, excluded, unsupported, and inaccessible fields. No keyboard history or telemetry is stored.",12,Ui.Muted)));
        expansions.Columns.Add(new DataGridTextColumn{Header="Shortcut",Binding=new System.Windows.Data.Binding("Shortcut"),Width=180});expansions.Columns.Add(new DataGridTextColumn{Header="Type",Binding=new System.Windows.Data.Binding("Kind"),Width=90});expansions.Columns.Add(new DataGridTextColumn{Header="Expansion",Binding=new System.Windows.Data.Binding("Text"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});expansions.IsReadOnly=true;
        filter.ToolTip="Filter custom shortcuts";filter.TextChanged+=(_,_)=>RefreshExpansions();
        Page("Shortcuts & snippets",Ui.Column(Ui.Label("Your words, a little faster",23),Ui.Label("Custom shortcuts take priority over built-in names. Use letters, digits, +, - or _. Multiline snippets send Enter key events; use them in multiline editors. Snippets can use {{ date_short }}, {{ time_24h }}, and {{ date_time_iso }}.",12,Ui.Muted),filter,expansions,Ui.Row(Ui.Button("Add emoji",()=>Edit(false,"Emoji")),Ui.Button("Add snippet",()=>Edit(false,"Snippet")),Ui.Button("Edit",()=>Edit(true,"")),Ui.Button("Delete",Delete))));
        Page("Exclusions",Ui.Column(Ui.Label("Pause in these applications",23),Ui.Label("One executable name per line, for example code.exe. Browser exclusions apply to the whole browser, not individual websites.",12,Ui.Muted),exclusions));
        Page("Favorites",Ui.Column(Ui.Label("Saved expressions",23),favoriteList));
        Page("Data & about",Ui.Column(Ui.Label("Local by design",23),Ui.Label("Settings, favorites, recents, and custom expansions are stored in %LOCALAPPDATA%\\Comet\\settings.json. Exported settings may contain private snippets. GIFs stay on your computer. Comet makes no network requests.",14),Ui.Row(Ui.Button("Export saved settings",Export),Ui.Button("Import settings",Import)),Ui.Label("Comet 0.1 · Independent Windows adaptation\nWindows 10/11 x64 · .NET 8 WPF\nEmoji metadata: iamcal/emoji-data, MIT\nWindows integration requires validation on your applications. Elevated apps, IMEs, remote desktops, and custom text controls may be unsupported.",13,Ui.Muted),Ui.Button("Open GIF library",()=>host.OpenGifs())));
        var root=new DockPanel{Margin=new Thickness(14)};var footer=Ui.Column(status,Ui.Row(Ui.Button("Save",Save),Ui.Button("Cancel",Close)));DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);root.Children.Add(tabs);Content=root;Populate();
    }
    static UIElement Field(string label,UIElement editor) { var l=Ui.Label(label);l.Width=220;var row=Ui.Row(l,editor);row.Margin=new Thickness(0,7,0,7);return row; }
    void Populate()
    {
        enabled.IsChecked=draft.Enabled;startup.IsChecked=draft.StartAtLogin;fuzzy.IsChecked=draft.Fuzzy;tab.IsChecked=draft.TabConfirms;closing.IsChecked=draft.ClosingTriggerConfirms;doubleTrigger.IsChecked=draft.DoubleTrigger;clipboard.IsChecked=draft.LeaveOnClipboard;trigger.Text=draft.Trigger;minimum.Text=draft.MinQuery.ToString();maximum.Text=draft.MaxResults.ToString();theme.SelectedItem=draft.Theme;hotkey.SelectedItem=draft.PickerHotkey;exclusions.Text=string.Join(Environment.NewLine,draft.Exclusions);RefreshExpansions();
        favoriteList.Children.Clear();foreach(var id in draft.Favorites.ToArray()) { var e=host.Search.Emojis.FirstOrDefault(x=>x.Id==id);favoriteList.Children.Add(Ui.Row(Ui.Label(e==null?id:e.Text+" "+e.Name),Ui.Button("Remove",()=>{draft.Favorites.Remove(id);Populate();}))); }
    }
    void RefreshExpansions() => expansions.ItemsSource=draft.Expansions.Where(e=>e.Shortcut.Contains(filter.Text,StringComparison.OrdinalIgnoreCase)||e.Text.Contains(filter.Text,StringComparison.OrdinalIgnoreCase)).ToArray();
    void Edit(bool edit,string kind)
    {
        var old=expansions.SelectedItem as Expansion;if(edit && old==null)return;
        var name=new TextBox{Text=edit?old!.Shortcut:""};var text=new TextBox{Text=edit?old!.Text:"",AcceptsReturn=true,Height=170,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,TextWrapping=TextWrapping.Wrap};
        var w=new Window{Title=edit?"Edit expansion":"Add "+kind,Width=500,Height=410,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner};Ui.Window(w);
        var message=Ui.Label("",12,Ui.Muted);
        w.Content=new Border{Padding=new Thickness(20),Child=Ui.Column(Ui.Label("Shortcut (without trigger)"),name,Ui.Label("Expansion"),text,message,Ui.Button("Save expansion",()=>{
            var item=new Expansion(name.Text.Trim(),text.Text,edit?old!.Kind:kind);
            var trial=SettingsManager.Clone(draft);if(edit)trial.Expansions.Remove(old!);trial.Expansions.Add(item);
            try{trial.Validate();draft=trial;RefreshExpansions();w.Close();}catch(Exception e){message.Text=e.Message;}
        }))};w.ShowDialog();
    }
    void Delete() { if(expansions.SelectedItem is Expansion e){draft.Expansions.Remove(e);RefreshExpansions();} }
    void Save()
    {
        try
        {
            draft.Enabled=enabled.IsChecked==true;draft.StartAtLogin=startup.IsChecked==true;draft.Fuzzy=fuzzy.IsChecked==true;draft.TabConfirms=tab.IsChecked==true;draft.ClosingTriggerConfirms=closing.IsChecked==true;draft.DoubleTrigger=doubleTrigger.IsChecked==true;draft.LeaveOnClipboard=clipboard.IsChecked==true;draft.Trigger=trigger.Text;draft.MinQuery=int.Parse(minimum.Text);draft.MaxResults=int.Parse(maximum.Text);draft.Theme=theme.SelectedItem!.ToString()!;draft.PickerHotkey=hotkey.SelectedItem!.ToString()!;draft.Exclusions=exclusions.Text.Split(['\r','\n'],StringSplitOptions.RemoveEmptyEntries).ToList();
            // Preserve usage and GIF additions made while settings was open.
            draft.Recent=host.Settings.Recent;draft.Frequency=host.Settings.Frequency;draft.Gifs=host.Settings.Gifs;draft.SkinTone=host.Settings.SkinTone;
            draft.Validate();host.ApplySettings(draft);status.Text="Saved. Appearance applies to newly opened windows.";
        }
        catch(Exception e){status.Text=e.Message;}
    }
    void Export()
    {
        var d=new SaveFileDialog{Filter="JSON settings|*.json",FileName="Comet-settings.json"};if(d.ShowDialog()==true)try{File.WriteAllText(d.FileName,System.Text.Json.JsonSerializer.Serialize(host.Settings,SettingsManager.Json));status.Text="Exported saved settings.";}catch(Exception e){status.Text=e.Message;}
    }
    void Import()
    {
        var d=new OpenFileDialog{Filter="JSON settings|*.json"};if(d.ShowDialog()==true)try{if(new FileInfo(d.FileName).Length>4_000_000)throw new ArgumentException("Settings file exceeds 4 MB.");draft=SettingsManager.Read(File.ReadAllText(d.FileName));Populate();status.Text="Imported for review. Save to apply (including startup and hotkey).";}catch(Exception e){status.Text=e.Message;}
    }
}
