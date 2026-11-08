using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Drawing=System.Drawing;
namespace Comet.Windows;
internal sealed class GifWindow : Window
{
    readonly AppHost host;
    readonly TextBox search=new();
    readonly ListBox list=new(){Height=160};
    readonly Image preview=new(){Height=230,Stretch=Stretch.Uniform};
    readonly TextBlock status=Ui.Label("Add GIF files you own or are licensed to share.",12,Ui.Muted);
    Drawing.Image? animation;
    EventHandler? frameHandler;
    internal GifWindow(AppHost host)
    {
        this.host=host;Title="Comet · GIF library";Width=640;Height=650;Ui.Window(this);
        Content=new Border{Padding=new Thickness(22),Child=Ui.Column(Ui.Label("A little motion",25),Ui.Label("Search your local GIF collection",12,Ui.Muted),search,list,preview,status,Ui.Row(Ui.Button("Add GIFs",Add),Ui.Button("Copy GIF file",Copy),Ui.Button("Remove",Remove)))};
        search.TextChanged+=(_,_)=>Refresh();list.SelectionChanged+=(_,_)=>Preview();Closed+=(_,_)=>Stop();Refresh();
    }
    void Refresh(){list.ItemsSource=host.Settings.Gifs.Where(g=>g.Name.Contains(search.Text,StringComparison.OrdinalIgnoreCase)).ToArray();list.DisplayMemberPath="Name";}
    void Add()
    {
        var d=new OpenFileDialog{Filter="GIF images|*.gif",Multiselect=true};if(d.ShowDialog()!=true)return;
        try
        {
            var dir=Path.Combine(AppHost.DataDirectory,"Gifs");Directory.CreateDirectory(dir);
            foreach(var path in d.FileNames)
            {
                if(new FileInfo(path).Length>25_000_000)throw new ArgumentException("GIFs must be smaller than 25 MB.");
                using var image=Drawing.Image.FromFile(path);if(image.RawFormat.Guid!=Drawing.Imaging.ImageFormat.Gif.Guid)throw new ArgumentException("Not a GIF file.");
                var dest=Path.Combine(dir,Guid.NewGuid()+".gif");File.Copy(path,dest);host.Settings.Gifs.Add(new(Path.GetFileNameWithoutExtension(path),dest));
            }
            host.Save();Refresh();
        }catch(Exception e){status.Text=e.Message;}
    }
    void Stop(){if(animation!=null){if(frameHandler!=null)Drawing.ImageAnimator.StopAnimate(animation,frameHandler);animation.Dispose();animation=null;}preview.Source=null;}
    void Preview()
    {
        Stop();if(list.SelectedItem is not GifItem g)return;
        try
        {
            animation=Drawing.Image.FromFile(g.Path);if(animation.Width>4096 || animation.Height>4096){Stop();throw new ArgumentException("Preview dimensions exceed 4096 pixels.");}
            frameHandler=(_,_)=>Dispatcher.BeginInvoke(()=>Render());Drawing.ImageAnimator.Animate(animation,frameHandler);Render();status.Text=g.Name;
        }catch(Exception e){status.Text=e.Message;}
    }
    void Render()
    {
        if(animation==null)return;
        try{Drawing.ImageAnimator.UpdateFrames(animation);using var stream=new MemoryStream();animation.Save(stream,Drawing.Imaging.ImageFormat.Png);stream.Position=0;var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();preview.Source=bitmap;}catch{Stop();status.Text="Could not render this GIF.";}
    }
    void Copy()
    {
        if(list.SelectedItem is not GifItem g)return;
        try{var files=new System.Collections.Specialized.StringCollection{g.Path};Clipboard.SetFileDropList(files);status.Text="GIF file copied. Paste into an app that accepts file attachments.";}catch(Exception e){status.Text=e.Message;}
    }
    void Remove()
    {
        if(list.SelectedItem is not GifItem g)return;Stop();host.Settings.Gifs.Remove(g);host.Save();Refresh();
        // Do not delete a file referenced by exported settings or an active clipboard.
    }
}
