using System.Windows.Controls.Primitives;
namespace Comet.Windows;
internal sealed class PickerWindow : Window
{
    readonly AppHost host;
    readonly TextBox search=new();
    readonly ComboBox categories=new(), tones=new();
    readonly ListBox list=new(){BorderThickness=new Thickness(0),Background=Brushes.Transparent};
    readonly TextBlock count=Ui.Label("",12,Ui.Muted);
    readonly nint target;
    internal PickerWindow(AppHost host,nint target)
    {
        this.host=host;this.target=target;Title="Comet · Emoji picker";Width=760;Height=650;MinWidth=520;MinHeight=400;Ui.Window(this);list.Foreground=Ui.Fg;
        search.ToolTip="Search emoji names, shortcodes, and keywords";search.Margin=new Thickness(0,12,0,12);
        categories.ItemsSource=new[]{"All","Recent","Frequent","Favorites","Emoji","Snippet"}.Concat(host.Search.Emojis.Select(e=>e.Category).Distinct()).ToArray();categories.SelectedIndex=0;categories.Width=230;
        tones.ItemsSource=new[]{"Default skin tone","Light","Medium-light","Medium","Medium-dark","Dark"};tones.SelectedIndex=host.Settings.SkinTone;tones.Width=180;
        var factory=new FrameworkElementFactory(typeof(WrapPanel));list.ItemsPanel=new ItemsPanelTemplate(factory);
        var grid=new DockPanel{Margin=new Thickness(22)};
        var head=Ui.Column(Ui.Label("Find your next expression",25),search,Ui.Row(categories,tones),count);DockPanel.SetDock(head,Dock.Top);grid.Children.Add(head);
        var foot=Ui.Column(Ui.Label("Enter or double-click to insert · Right-click to favorite",12,Ui.Muted),Ui.Row(Ui.Button("Insert",Insert),Ui.Button("Copy",Copy),Ui.Button("Favorite / unfavorite",Favorite),Ui.Button("Settings",()=>host.OpenSettings()),Ui.Button("GIF library",()=>host.OpenGifs())));DockPanel.SetDock(foot,Dock.Bottom);grid.Children.Add(foot);grid.Children.Add(list);Content=grid;
        search.TextChanged+=(_,_)=>Refresh();categories.SelectionChanged+=(_,_)=>Refresh();tones.SelectionChanged+=(_,_)=>{host.Settings.SkinTone=tones.SelectedIndex;host.Search.Invalidate();host.Save();Refresh();};
        list.MouseDoubleClick+=(_,_)=>Insert();list.MouseRightButtonUp+=(_,_)=>Favorite();
        PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape)Close();if(e.Key==Key.Enter){Insert();e.Handled=true;}if(e.Key==Key.Down && search.IsKeyboardFocusWithin){list.Focus();if(list.SelectedIndex<0)list.SelectedIndex=0;e.Handled=true;}};
        Loaded+=(_,_)=>search.Focus();Refresh();
    }
    void Refresh()
    {
        var items=host.Search.Search(search.Text,host.Settings,400,categories.SelectedItem?.ToString()??"All");list.Items.Clear();
        foreach(var item in items)
        {
            string glyph=item.Text.Replace("\r","").Replace("\n"," ↵ ");if(glyph.Length>14)glyph=glyph[..14]+"…";
            var label=Ui.Label(glyph,item.Category=="Snippet"?14:29);label.TextAlignment=TextAlignment.Center;label.FontFamily=new FontFamily("Segoe UI Emoji");
            var cell=new ListBoxItem{Tag=item,Width=78,Height=74,ToolTip=$"{item.Name}\n{host.Settings.Prefix}{item.Shortcut}",Content=Ui.Column(label,Ui.Label(host.Settings.Favorites.Contains(item.Id)?"★":"",10,Ui.Accent))};
            list.Items.Add(cell);
        }
        if(list.Items.Count>0)list.SelectedIndex=0;count.Text=$"{items.Count} results"+(items.Count==400?" · refine search to see more":"");
    }
    Suggestion? Selected => (list.SelectedItem as ListBoxItem)?.Tag as Suggestion;
    void Insert() { if(Selected is {} item){Close();host.InsertFromPicker(item,target);} }
    void Copy() { if(Selected is {} item){host.Copy(item.Text);host.Used(item.Id);count.Text="Copied to clipboard";} }
    void Favorite() { if(Selected is {} item){if(!host.Settings.Favorites.Add(item.Id))host.Settings.Favorites.Remove(item.Id);host.Search.Invalidate();host.Save();Refresh();} }
}
