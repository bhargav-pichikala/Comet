using Microsoft.Win32;
namespace Comet.Windows;
internal static class Ui
{
    internal static Brush Bg=Brushes.White, Fg=Brushes.Black, Muted=Brushes.Gray, Panel=Brushes.WhiteSmoke, Accent=new SolidColorBrush(Color.FromRgb(99,82,220));
    internal static void Theme(string theme)
    {
        bool dark=theme=="Dark";
        if(theme=="System") { using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");dark=key?.GetValue("AppsUseLightTheme") is int i && i==0; }
        Bg=new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark?"#171923":"#FAFAFD"));
        Fg=new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark?"#F2F2FA":"#242333"));
        Muted=new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark?"#B2B5C9":"#626578"));
        Panel=new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark?"#282B3B":"#EEEEF7"));
    }
    internal static void Window(Window w)
    {
        w.Background=Bg;w.Foreground=Fg;w.FontFamily=new FontFamily("Segoe UI");w.FontSize=14;
        var button=new Style(typeof(Button));button.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(13,7,13,7)));button.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(3)));button.Setters.Add(new Setter(Control.BackgroundProperty,Panel));button.Setters.Add(new Setter(Control.ForegroundProperty,Fg));w.Resources.Add(typeof(Button),button);
        var text=new Style(typeof(TextBox));text.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(8)));text.Setters.Add(new Setter(Control.BackgroundProperty,Panel));text.Setters.Add(new Setter(Control.ForegroundProperty,Fg));text.Setters.Add(new Setter(Control.BorderBrushProperty,Muted));w.Resources.Add(typeof(TextBox),text);
        var combo=new Style(typeof(ComboBox));combo.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(6)));w.Resources.Add(typeof(ComboBox),combo);
        w.Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/comet.ico"));
    }
    internal static TextBlock Label(string text,double size=14,Brush? color=null) => new(){Text=text,FontSize=size,Foreground=color??Fg,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,5)};
    internal static Button Button(string label,Action click) { var b=new Button{Content=label};b.Click+=(_,_)=>click();return b; }
    internal static StackPanel Row(params UIElement[] items) { var p=new StackPanel{Orientation=Orientation.Horizontal};foreach(var i in items)p.Children.Add(i);return p; }
    internal static StackPanel Column(params UIElement[] items) { var p=new StackPanel();foreach(var i in items)p.Children.Add(i);return p; }
    internal static void Error(string message) => MessageBox.Show(message,"Comet",MessageBoxButton.OK,MessageBoxImage.Information);
}
