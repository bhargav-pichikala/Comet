using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Text;
using Comet.Windows;
// Opt-in interactive Windows integration harness. Never runs during a normal Comet launch.
static class Program
{
    [STAThread] static void Main()
    {
        var app=new Application();var log=new TextBox{IsReadOnly=true,AcceptsReturn=true,Height=200,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        var input=new TextBox{AcceptsReturn=true,Height=100,FontSize=20};var password=new PasswordBox{Height=35};
        var button=new Button{Content="Run smoke tests (do not type or move the mouse while running)",Padding=new Thickness(10)};
        var panel=new StackPanel{Margin=new Thickness(20)};panel.Children.Add(new TextBlock{Text="First quit Comet, then start Comet.exe --test-input.\nClose Settings, leave default trigger :, minimum 2, and ensure autocomplete is enabled.\nThis harness uses synthetic VK input on a US keyboard layout.",TextWrapping=TextWrapping.Wrap});panel.Children.Add(button);panel.Children.Add(input);panel.Children.Add(new TextBlock{Text="Password-field test:"});panel.Children.Add(password);panel.Children.Add(log);
        var window=new Window{Title="Comet · Windows integration smoke tests",Width=780,Height=560,Content=panel};
        button.Click+=async(_,_)=>
        {
            button.IsEnabled=false;log.Clear();int passed=0,failed=0;
            void Result(string name,bool good){log.AppendText((good?"PASS ":"FAIL ")+name+Environment.NewLine);if(good)passed++;else failed++;}
            void Send(ushort vk,bool up=false){var data=new[]{new Native.Input{Type=1,Data=new(){Key=new(){Vk=vk,Flags=up?2u:0u}}}};if(Native.SendInput(1,data,Marshal.SizeOf<Native.Input>())!=1)throw new Exception("SendInput rejected");}
            void Press(ushort vk){Send(vk);Send(vk,true);}
            async Task Type(string text)
            {
                foreach(char c in text)
                {
                    if(c==':'){Send(0x10);Press(0xba);Send(0x10,true);}
                    else if(c==' ')Press(0x20);
                    else Press((ushort)char.ToUpperInvariant(c));
                    await Task.Delay(55);
                }
            }
            async Task Prepare(string text="") { input.Text=text;input.Focus();Keyboard.Focus(input);input.CaretIndex=input.Text.Length;await Task.Delay(900); }
            try
            {
                var layout=Native.GetKeyboardLayout(0).ToInt64()&0xffff;if(layout!=0x409)throw new Exception("Select English (United States) keyboard layout before this test.");
                await Prepare();await Type(":rocket");await Task.Delay(350);Press(13);await Task.Delay(800);Result("Shortcut replaced with rocket",input.Text=="🚀");
                await Prepare("before ");await Type(":rocket");await Task.Delay(350);Press(13);await Task.Delay(800);Result("Surrounding text preserved",input.Text=="before 🚀");
                await Prepare();await Type(":rocket");await Task.Delay(350);Press(27);await Task.Delay(100);await Type("x");Result("Escape dismisses and preserves candidate",input.Text==":rocketx");
                await Prepare();await Type(":rocket");await Task.Delay(350);Press(8);await Task.Delay(200);Press(13);await Task.Delay(800);Result("Backspace updates and replacement remains correct",input.Text=="🚀");
                password.Clear();password.Focus();Keyboard.Focus(password);await Task.Delay(900);await Type(":rocket");Press(13);await Task.Delay(300);Result("Password field is untouched",password.Password==":rocket");password.Clear();
                await Prepare();await Type(":rocket");await Task.Delay(350);Press(0x25);Press(13);await Task.Delay(300);Result("Cursor movement cancels replacement",input.Text.Contains(":rocke"));
            }
            catch(Exception e){log.AppendText("BLOCKED "+e.Message+Environment.NewLine);failed++;}
            log.AppendText($"RESULT {passed} passed, {failed} failed.\nNot a test of physical typing, other applications, elevation, IME, or monitor positioning.\n");
            var path=Path.Combine(Path.GetTempPath(),"Comet-Windows-smoke-results.txt");File.WriteAllText(path,log.Text);log.AppendText("Saved "+path+Environment.NewLine);button.IsEnabled=true;
        };
        app.Run(window);
    }
}
