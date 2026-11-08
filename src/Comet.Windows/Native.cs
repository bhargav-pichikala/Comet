using System.Runtime.InteropServices;
using System.Text;
namespace Comet.Windows;
internal static class Native
{
    internal const nuint InjectionTag = 0x434F4D45;
    internal delegate nint HookProc(int code, nint w, nint l);
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct GuiInfo { public int Size, Flags; public nint Active, Focus, Capture, MenuOwner, MoveSize, Caret; public Rect CaretRect; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeyData { public uint Vk, Scan, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseData { public Point Point; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] public KeyInput Key; [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeyInput { public ushort Vk, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll", SetLastError=true)] internal static extern nint SetWindowsHookEx(int type, HookProc proc, nint module, uint thread);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint w, nint l);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] internal static extern nint GetModuleHandle(string? name);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] internal static extern bool GetGUIThreadInfo(uint thread, ref GuiInfo info);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] internal static extern short GetKeyState(int vk);
    [DllImport("user32.dll")] internal static extern nint GetKeyboardLayout(uint thread);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int ToUnicodeEx(uint vk,uint scan,byte[] state,StringBuilder buffer,int size,uint flags,nint layout);
    [DllImport("user32.dll", SetLastError=true)] internal static extern uint SendInput(uint count, Input[] input, int size);
    [DllImport("user32.dll")] internal static extern bool ClientToScreen(nint hwnd, ref Point point);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] internal static extern nint MonitorFromPoint(Point point, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] internal static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW")] internal static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int cx,int cy,uint flags);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("shcore.dll")] internal static extern int GetDpiForMonitor(nint monitor, int type, out uint x, out uint y);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(nint hwnd,int id,uint mods,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint hwnd,int id);
    [DllImport("imm32.dll")] internal static extern nint ImmGetContext(nint hwnd);
    [DllImport("imm32.dll")] internal static extern bool ImmGetOpenStatus(nint context);
    [DllImport("imm32.dll")] internal static extern bool ImmReleaseContext(nint hwnd,nint context);
    internal static GuiInfo FocusInfo()
    { var g = new GuiInfo { Size = Marshal.SizeOf<GuiInfo>() }; GetGUIThreadInfo(GetWindowThreadProcessId(GetForegroundWindow(),out _),ref g); return g; }
    internal static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    internal static bool Modifiers => Down(0x11) || Down(0x12) || Down(0x5b) || Down(0x5c);
    internal static bool Ime(nint hwnd)
    { var context = ImmGetContext(hwnd); if(context == 0) return false; try { return ImmGetOpenStatus(context); } finally { ImmReleaseContext(hwnd,context); } }
}
