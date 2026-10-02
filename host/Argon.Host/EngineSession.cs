// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.Diagnostics;
using System.IO;

namespace Argon.Host;

public sealed class EngineSession : IDisposable
{
    public Process Process { get; }
    public nint Window { get; private set; }
    public Workspace Workspace { get; }
    private readonly string executable;

    private EngineSession(Process process, Workspace workspace, string executable) { Process = process; Workspace = workspace; this.executable = executable; }

    public static EngineSession Start(Workspace workspace, WorkspaceStore store)
    {
        string engine = Path.Combine(AppContext.BaseDirectory, "engine", "argon-engine.exe");
        if (!File.Exists(engine)) throw new FileNotFoundException("The Argon Gecko engine is missing. Extract the complete Windows package, including its engine folder.", engine);
        store.EnsureProfile(workspace);
        var start = new ProcessStartInfo(engine) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(engine)!, WindowStyle = ProcessWindowStyle.Hidden };
        // ArgumentList prevents workspace names or paths from becoming switches.
        // Keep Firefox's security/DLL-blocklist launcher alive for lifecycle
        // tracking. Its child process can own the actual browser HWND.
        start.ArgumentList.Add("-wait-for-browser");
        start.ArgumentList.Add("-no-remote");
        start.ArgumentList.Add("-profile");
        start.ArgumentList.Add(store.ProfilePath(workspace));
        var process = Process.Start(start) ?? throw new InvalidOperationException("Gecko did not start.");
        return new EngineSession(process, workspace, engine);
    }

    public async Task WaitForWindowAsync()
    {
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(60))
        {
            if (Process.HasExited) throw new InvalidOperationException($"Gecko exited with code {Process.ExitCode}. The profile may already be in use.");
            Window = Native.FindBrowserWindow(Process.Id);
            if (Window == 0) Window = Native.FindBrowserDescendantWindow(Process.Id, executable);
            if (Window != 0) return;
            await Task.Delay(40);
        }
        // Never kill a process that may hold unsaved forms or profile writes.
        throw new TimeoutException("Gecko has not created its browser window after 60 seconds. Check for a profile-lock or startup dialog.");
    }

    public async Task<bool> CloseAsync()
    {
        if (Process.HasExited) return true;
        if (Native.IsWindow(Window)) Native.PostMessage(Window, 0x0010, 0, 0); // WM_CLOSE, normal Gecko shutdown
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try { await Process.WaitForExitAsync(timeout.Token); return true; }
        catch (OperationCanceledException) { return false; }
    }

    public void Dispose() => Process.Dispose();
}
