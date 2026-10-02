// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Argon.Host;

public sealed class BrowserSurface : HwndHost
{
    private readonly Dictionary<nint, (nint Style, nint ExtendedStyle)> attached = [];
    private nint active;

    protected override HandleRef BuildWindowCore(HandleRef parent)
    {
        nint handle = Native.CreateWindowEx(0, "STATIC", "Argon workspace surface", unchecked((int)0x56000000), 0, 0, 1, 1, parent.Handle, 0, 0, 0);
        if (handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new HandleRef(this, handle);
    }

    public void Activate(nint browser)
    {
        if (!Native.IsWindow(browser)) throw new InvalidOperationException("Browser window is unavailable.");
        if (!attached.ContainsKey(browser))
        {
            var style = Native.GetWindowLongPtr(browser, -16);
            var extended = Native.GetWindowLongPtr(browser, -20);
            Native.ShowWindow(browser, 0);
            // WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN, no separate caption.
            long childStyle = (style.ToInt64() & ~0x80CF0000L) | 0x46000000L;
            Native.SetWindowLongPtr(browser, -16, (nint)childStyle);
            Native.SetWindowLongPtr(browser, -20, (nint)(extended.ToInt64() & ~0x40008L));
            Marshal.SetLastPInvokeError(0);
            Native.SetParent(browser, Handle);
            int error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                Native.SetWindowLongPtr(browser, -16, style);
                Native.SetWindowLongPtr(browser, -20, extended);
                Native.ShowWindow(browser, 5);
                throw new Win32Exception(error, "Could not embed the Gecko window. Check process DPI compatibility.");
            }
            attached[browser] = (style, extended);
        }
        if (active != 0 && active != browser) Native.ShowWindow(active, 0);
        active = browser;
        ResizeBrowser();
        Native.ShowWindow(active, 5);
        FocusBrowser();
    }

    public void FocusBrowser()
    {
        if (!Native.IsWindow(active)) return;
        uint targetThread = Native.GetWindowThreadProcessId(active, out _);
        uint currentThread = Native.GetCurrentThreadId();
        bool joined = targetThread != currentThread && Native.AttachThreadInput(currentThread, targetThread, true);
        try { Native.SetFocus(active); }
        finally { if (joined) Native.AttachThreadInput(currentThread, targetThread, false); }
    }

    protected override void OnWindowPositionChanged(Rect rect)
    {
        base.OnWindowPositionChanged(rect);
        ResizeBrowser();
    }

    private void ResizeBrowser()
    {
        if (!Native.IsWindow(active) || Handle == 0) return;
        Native.GetClientRect(Handle, out var bounds);
        Native.SetWindowPos(active, 0, 0, 0, bounds.Right, bounds.Bottom, 0x0034);
    }

    protected override void DestroyWindowCore(HandleRef window)
    {
        // On abnormal shell teardown, restore surviving windows instead of
        // destroying someone else's HWND or terminating a browser process.
        foreach (var (browser, styles) in attached)
        {
            if (!Native.IsWindow(browser)) continue;
            Native.SetParent(browser, 0);
            Native.SetWindowLongPtr(browser, -16, styles.Style);
            Native.SetWindowLongPtr(browser, -20, styles.ExtendedStyle);
            Native.ShowWindow(browser, 5);
        }
        Native.DestroyWindow(window.Handle);
    }
}
