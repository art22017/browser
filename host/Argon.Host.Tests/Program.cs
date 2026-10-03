// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.Text.Json;
using Argon.Host;

string root = Path.Combine(Path.GetTempPath(), "argon-isolation-test-" + Guid.NewGuid().ToString("N"));
try
{
    var store = new WorkspaceStore(root);
    var first = store.Create("Personal");
    var second = store.Create("Work");
    string profileA = store.ProfilePath(first), profileB = store.ProfilePath(second);
    Check(profileA != profileB, "independent profile directories");
    // Firefox keeps these databases beneath ProfD. A new workspace must never
    // copy them, even when the names are identical or a registry is reloaded.
    foreach (string data in new[] { "cookies.sqlite", "places.sqlite", "logins.json", "key4.db", "extensions.json", "sessionstore.jsonlz4" })
    {
        File.WriteAllText(Path.Combine(profileA, data), "secret-from-personal");
        Check(!File.Exists(Path.Combine(profileB, data)), "no cross-workspace copy: " + data);
    }
    File.AppendAllText(Path.Combine(profileA, "user.js"), "\n// user customization");
    store.EnsureProfile(first);
    Check(File.ReadAllText(Path.Combine(profileA, "user.js")).Contains("user customization"), "existing preferences are not overwritten");
    store.Rename(first, "Home");
    var reopened = new WorkspaceStore(root);
    Check(reopened.Workspaces.Count == 2 && reopened.Workspaces[0].Name == "Home", "atomic registry reload");
    Reject(() => store.ProfilePath(new Workspace("../../outside", "Attack")), "path traversal");
    Reject(() => store.Create("\n"), "control character name");
    File.WriteAllText(Path.Combine(root, "workspaces.json"), JsonSerializer.Serialize(new[] { first, first }));
    Reject(() => new WorkspaceStore(root), "duplicate profile identifiers");
    File.WriteAllText(Path.Combine(root, "workspaces.json"), "invalid json");
    try { _ = new WorkspaceStore(root); throw new Exception("Corrupt registry accepted"); }
    catch (JsonException) { Console.WriteLine("PASS: corrupt registry fails without resetting profiles"); }
    Console.WriteLine("Workspace storage tests passed. This does not test Gecko runtime isolation or HWND embedding.");
}
finally { Directory.Delete(root, true); }

static void Check(bool result, string name)
{
    if (!result) throw new Exception(name);
    Console.WriteLine("PASS: " + name);
}
static void Reject(Action action, string name)
{
    try { action(); }
    catch (InvalidDataException) { Console.WriteLine("PASS: rejects " + name); return; }
    throw new Exception("Accepted " + name);
}
