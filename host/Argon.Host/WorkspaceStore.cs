// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.IO;
using System.Text.Json;

namespace Argon.Host;

public sealed record Workspace(string Id, string Name);

// The only shared persisted information is the workspace id/name registry.
// No cookies, keys, browser prefs, tab sessions or extension data are copied.
public sealed class WorkspaceStore
{
    public string Root { get; }
    public List<Workspace> Workspaces { get; }
    private string RegistryPath => Path.Combine(Root, "workspaces.json");

    public WorkspaceStore(string root)
    {
        Root = Path.GetFullPath(root);
        Directory.CreateDirectory(Root);
        Workspaces = File.Exists(RegistryPath)
            ? JsonSerializer.Deserialize<List<Workspace>>(File.ReadAllText(RegistryPath)) ?? throw new InvalidDataException("Workspace registry is empty.")
            : [];
        if (Workspaces.Count > 100 || Workspaces.Select(w => w.Id).Distinct().Count() != Workspaces.Count)
            throw new InvalidDataException("Invalid workspace registry.");
        foreach (var workspace in Workspaces)
        {
            ValidateId(workspace.Id);
            ValidateName(workspace.Name);
        }
    }

    public Workspace Create(string name)
    {
        name = name.Trim();
        ValidateName(name);
        if (Workspaces.Count >= 100) throw new InvalidOperationException("Maximum workspace count reached.");
        var workspace = new Workspace(Guid.NewGuid().ToString("D"), name);
        // Create the independent profile before adding it to the registry.
        EnsureProfile(workspace);
        Workspaces.Add(workspace);
        try { Save(); }
        catch { Workspaces.Remove(workspace); throw; }
        return workspace;
    }

    public string ProfilePath(Workspace workspace)
    {
        ValidateId(workspace.Id);
        return Path.Combine(Root, workspace.Id, "profile");
    }

    public void EnsureProfile(Workspace workspace)
    {
        string path = ProfilePath(workspace);
        Directory.CreateDirectory(path);
        // Never rewrite an existing user.js: user choices belong to that profile.
        string prefs = Path.Combine(path, "user.js");
        if (File.Exists(prefs)) return;
        using var stream = new FileStream(prefs, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream);
        writer.Write("""
            // Argon hosted workspace. This profile belongs only to this workspace.
            user_pref("argon.hosted-workspace", true);
            user_pref("browser.profiles.enabled", false);
            user_pref("browser.startup.page", 3);
            user_pref("zen.view.use-single-toolbar", false);
            user_pref("zen.view.compact.enable-at-startup", false);
            user_pref("zen.view.hide-window-controls", false);
            user_pref("zen.welcome-screen.seen", true);
            user_pref("zen.urlbar.open-on-startup", false);
            user_pref("browser.shell.checkDefaultBrowser", false);
            user_pref("browser.search.suggest.enabled", false);
            user_pref("browser.urlbar.suggest.searches", false);
            user_pref("fission.autostart", true);
            user_pref("app.update.auto", false);
            user_pref("services.sync.engine.passwords", false);
            """);
    }

    public void Rename(Workspace workspace, string name)
    {
        name = name.Trim();
        ValidateName(name);
        int index = Workspaces.FindIndex(w => w.Id == workspace.Id);
        if (index < 0) throw new InvalidOperationException("Workspace is not registered.");
        var original = Workspaces[index];
        Workspaces[index] = workspace with { Name = name };
        try { Save(); }
        catch { Workspaces[index] = original; throw; }
    }

    private void Save()
    {
        string temporary = RegistryPath + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, Workspaces);
            stream.Flush(true);
        }
        File.Move(temporary, RegistryPath, true);
    }

    private static void ValidateId(string id)
    {
        if (!Guid.TryParseExact(id, "D", out var guid) || guid.ToString("D") != id)
            throw new InvalidDataException("Invalid workspace identifier.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || name.Any(char.IsControl))
            throw new InvalidDataException("Use a workspace name with 1–64 visible characters.");
    }
}
