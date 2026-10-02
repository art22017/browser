// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Argon.Host;

public partial class MainWindow : Window
{
    private readonly WorkspaceStore store;
    private readonly Dictionary<string, EngineSession> sessions = [];
    private readonly SemaphoreSlim switching = new(1);
    private Workspace? selected;
    private bool closing, allowClose;

    public MainWindow()
    {
        InitializeComponent();
        store = new WorkspaceStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Argon", "Workspaces"));
        Loaded += async (_, _) => {
            int dark = 1, backdrop = 3, rounded = 2;
            nint handle = new WindowInteropHelper(this).Handle;
            Native.DwmSetWindowAttribute(handle, 20, ref dark, 4);
            Native.DwmSetWindowAttribute(handle, 38, ref backdrop, 4);
            Native.DwmSetWindowAttribute(handle, 33, ref rounded, 4);
            if (store.Workspaces.Count == 0) store.Create("Personal");
            RenderWorkspaces();
            await SwitchAsync(store.Workspaces[0]);
        };
        Activated += (_, _) => Surface.FocusBrowser();
        Closing += OnClosing;
    }

    private void RenderWorkspaces()
    {
        WorkspaceCaption.Text = selected == null ? "Argon" : $"Argon � {selected.Name}";
        Title = WorkspaceCaption.Text;
        WorkspaceButtons.Children.Clear();
        foreach (var workspace in store.Workspaces)
        {
            var button = new Button { Content = workspace.Name, ToolTip = $"{workspace.Name} · independent browser profile", Background = new SolidColorBrush(workspace.Id == selected?.Id ? Color.FromArgb(70, 145, 166, 255) : Color.FromArgb(18, 255, 255, 255)) };
            button.Click += async (_, _) => await SwitchAsync(workspace);
            var menu = new ContextMenu();
            var rename = new MenuItem { Header = "Rename workspace…" };
            rename.Click += (_, _) => {
                string? name = AskName(workspace.Name);
                if (name == null) return;
                try {
                    store.Rename(workspace, name);
                    if (selected?.Id == workspace.Id) selected = store.Workspaces.Single(w => w.Id == workspace.Id);
                    RenderWorkspaces();
                }
                catch (Exception error) { MessageBox.Show(this, error.Message, "Argon", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            menu.Items.Add(rename);
            button.ContextMenu = menu;
            WorkspaceButtons.Children.Add(button);
        }
    }

    private async Task SwitchAsync(Workspace workspace)
    {
        if (closing || !await switching.WaitAsync(0)) return;
        selected = workspace;
        try
        {
            RetryButton.Visibility = Visibility.Collapsed;
            if (!sessions.TryGetValue(workspace.Id, out var session) || session.Process.HasExited)
            {
                if (session != null) { session.Dispose(); sessions.Remove(workspace.Id); }
                Surface.Visibility = Visibility.Collapsed;
                Status.Visibility = Visibility.Visible;
                StatusText.Text = $"Opening {workspace.Name}…";
                session = EngineSession.Start(workspace, store);
                sessions.Add(workspace.Id, session);
            }
            if (!Native.IsWindow(session.Window)) await session.WaitForWindowAsync();
            // Store session before embedding: a DPI/parenting failure must not
            // orphan a running browser or launch a duplicate against its profile.
            Surface.Visibility = Visibility.Visible;
            Surface.Activate(session.Window);
            Status.Visibility = Visibility.Collapsed;
            RenderWorkspaces();
        }
        catch (Exception error)
        {
            Surface.Visibility = Visibility.Collapsed;
            Status.Visibility = Visibility.Visible;
            StatusText.Text = error.Message;
            RetryButton.Visibility = Visibility.Visible;
        }
        finally { switching.Release(); }
    }

    private async void CreateWorkspaceClick(object sender, RoutedEventArgs e)
    {
        if (closing) return;
        string? name = AskName("");
        if (name == null) return;
        try { var workspace = store.Create(name); RenderWorkspaces(); await SwitchAsync(workspace); }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Argon", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private string? AskName(string current)
    {
        var dialog = new Window { Title = "Workspace name", Owner = this, Width = 360, Height = 180, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = new SolidColorBrush(Color.FromRgb(32, 33, 37)), Foreground = Brushes.White };
        var layout = new StackPanel { Margin = new Thickness(20) };
        var input = new TextBox { Text = current, MaxLength = 64, FontSize = 15, Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
        var save = new Button { Content = "Save", IsDefault = true };
        save.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(input.Text) && !input.Text.Any(char.IsControl)) dialog.DialogResult = true; };
        layout.Children.Add(input); layout.Children.Add(save); dialog.Content = layout;
        dialog.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (allowClose) return;
        e.Cancel = true;
        if (closing) return;
        closing = true;
        await switching.WaitAsync();
        try
        {
            foreach (var session in sessions.Values)
            {
                if (session.Process.HasExited) continue;
                Surface.Visibility = Visibility.Visible;
                Status.Visibility = Visibility.Collapsed;
                if (Native.IsWindow(session.Window)) Surface.Activate(session.Window);
                if (!await session.CloseAsync())
                {
                    MessageBox.Show(this, "This workspace is still open. Finish or cancel its browser dialog before closing Argon.", "Argon");
                    return;
                }
            }
            allowClose = true;
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Argon"); }
        finally { closing = false; switching.Release(); }
        if (allowClose) { foreach (var session in sessions.Values) session.Dispose(); Close(); }
    }

    private async void RetryClick(object sender, RoutedEventArgs e) { if (selected != null) await SwitchAsync(selected); }
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
