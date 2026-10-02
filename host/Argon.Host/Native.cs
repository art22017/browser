// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace Argon.Host;

internal static class Native
{
    internal delegate bool EnumWindowProc(nint window, nint state);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern nint CreateWindowEx(int exStyle, string cls, string title, int style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] internal static extern bool DestroyWindow(nint window);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetParent(nint child, nint parent);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] internal static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] internal static extern nint SetWindowLongPtr(nint window, int index, nint value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(nint window, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowProc callback, nint state);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll")] internal static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern nint SetFocus(nint window);
    [DllImport("user32.dll")] internal static extern bool AttachThreadInput(uint from, uint to, bool attach);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(nint window, System.Text.StringBuilder buffer, int length);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size, Usage, Id;
        public nuint Heap;
        public uint Module, Threads, Parent;
        public int Priority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Executable;
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern SafeFileHandle CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode)] private static extern bool Process32First(SafeFileHandle snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode)] private static extern bool Process32Next(SafeFileHandle snapshot, ref ProcessEntry entry);

    internal static nint FindBrowserDescendantWindow(int launcher, string executable)
    {
        // Firefox's security launcher may own no window. Only consider its
        // descendants, and require the same distribution executable path.
        var parents = new Dictionary<uint, uint>();
        using var snapshot = CreateToolhelp32Snapshot(2, 0);
        if (snapshot.IsInvalid) return 0;
        var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>(), Executable = "" };
        if (!Process32First(snapshot, ref entry)) return 0;
        do { parents[entry.Id] = entry.Parent; } while (Process32Next(snapshot, ref entry));
        foreach (var id in parents.Keys)
        {
            uint ancestor = id;
            bool belongs = false;
            for (int depth = 0; depth < 32 && parents.TryGetValue(ancestor, out var parent); depth++)
            {
                if (parent == launcher) { belongs = true; break; }
                if (parent == ancestor) break;
                ancestor = parent;
            }
            if (!belongs) continue;
            var window = FindBrowserWindow((int)id);
            if (window == 0) continue;
            try
            {
                using var process = Process.GetProcessById((int)id);
                if (string.Equals(process.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase)) return window;
            }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { }
        }
        return 0;
    }

    internal static nint FindBrowserWindow(int processId)
    {
        nint found = 0;
        EnumWindows((window, _) => {
            GetWindowThreadProcessId(window, out uint pid);
            if (pid != processId) return true;
            var cls = new System.Text.StringBuilder(256);
            GetClassName(window, cls, cls.Capacity);
            if (cls.ToString() != "MozillaWindowClass") return true;
            found = window;
            return false;
        }, 0);
        return found;
    }
}
