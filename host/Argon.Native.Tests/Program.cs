// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Argon.Host;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--fixture")) return Fixture.Run();
        var surface = new BrowserSurface();
        var window = new Window { Content = surface, Width = 640, Height = 480, ShowInTaskbar = false, ShowActivated = false };
        Process? first = null, second = null;
        try
        {
            window.Show();
            Native.ShowWindow(new WindowInteropHelper(window).Handle, 0);
            first = StartFixture(); second = StartFixture();
            nint a = WaitForWindow(first), b = WaitForWindow(second);
            surface.Activate(a);
            Check(Fixture.GetParent(a) == surface.Handle, "first process embedded in host");
            Check((Native.GetWindowLongPtr(a, -16).ToInt64() & 0x40000000) != 0, "embedded window is WS_CHILD");
            Native.GetClientRect(surface.Handle, out var parentBounds);
            Native.GetClientRect(a, out var childBounds);
            Check(parentBounds.Right == childBounds.Right && parentBounds.Bottom == childBounds.Bottom, "embedded viewport follows native surface size");
            surface.Activate(b);
            Check(Fixture.GetParent(b) == surface.Handle && Fixture.GetParent(a) == surface.Handle, "two independent processes share one shell surface");
            Check((Native.GetWindowLongPtr(a, -16).ToInt64() & 0x10000000) == 0, "inactive workspace is hidden");
            Check((Native.GetWindowLongPtr(b, -16).ToInt64() & 0x10000000) != 0, "active workspace is shown");
            surface.Activate(a);
            Check((Native.GetWindowLongPtr(b, -16).ToInt64() & 0x10000000) == 0, "switching back hides previous workspace");
            Native.PostMessage(a, 0x10, 0, 0); Native.PostMessage(b, 0x10, 0, 0);
            Check(first.WaitForExit(5000) && second.WaitForExit(5000), "normal WM_CLOSE preserves graceful process shutdown");
            Console.WriteLine("Native embedding smoke tests passed using fixture windows, not Gecko.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            foreach (var process in new[] { first, second })
            {
                if (process == null) continue;
                if (!process.HasExited) { var handle = Native.FindBrowserWindow(process.Id); if (handle != 0) Native.PostMessage(handle, 0x10, 0, 0); process.WaitForExit(5000); }
                process.Dispose();
            }
            window.Close();
        }
    }

    private static Process StartFixture()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
        start.ArgumentList.Add("--fixture");
        return Process.Start(start)!;
    }
    private static nint WaitForWindow(Process process)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < 10000)
        {
            nint window = Native.FindBrowserWindow(process.Id);
            if (window != 0) return window;
            if (process.HasExited) throw new Exception("Fixture exited before creating a window.");
            Thread.Sleep(20);
        }
        throw new TimeoutException("Fixture window missing.");
    }
    private static void Check(bool result, string name) { if (!result) throw new Exception(name); Console.WriteLine("PASS: " + name); }
}

internal static class Fixture
{
    private delegate nint WindowProc(nint window, uint message, nint wParam, nint lParam);
    private static readonly WindowProc Callback = Proc;
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size, Style;
        public nint Procedure;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background, Menu;
        public string ClassName;
        public nint SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Message { public nint Window; public uint Id; public nuint WParam; public nint LParam; public uint Time; public int X, Y; public uint Private; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern ushort RegisterClassEx(ref WindowClass cls);
    [DllImport("user32.dll")] internal static extern nint GetParent(nint window);
    [DllImport("user32.dll")] private static extern int GetMessage(out Message message, nint window, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern nint DispatchMessage(ref Message message);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] private static extern nint DefWindowProc(nint window, uint message, nint wParam, nint lParam);

    private static nint Proc(nint window, uint message, nint wParam, nint lParam)
    {
        if (message == 2) { PostQuitMessage(0); return 0; }
        return DefWindowProc(window, message, wParam, lParam);
    }
    internal static int Run()
    {
        var cls = new WindowClass { Size = (uint)Marshal.SizeOf<WindowClass>(), Procedure = Marshal.GetFunctionPointerForDelegate(Callback), ClassName = "MozillaWindowClass" };
        if (RegisterClassEx(ref cls) == 0) return 2;
        nint window = Native.CreateWindowEx(0, cls.ClassName, "Argon fixture", 0x00CF0000, 0, 0, 400, 300, 0, 0, 0, 0);
        if (window == 0) return 3;
        while (GetMessage(out var message, 0, 0, 0) > 0) { TranslateMessage(ref message); DispatchMessage(ref message); }
        return 0;
    }
}
