// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
using System.Security.Principal;
using System.Windows;

namespace Argon.Host;

public partial class App : Application
{
    private Mutex? instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instanceMutex = new Mutex(true, "Local\\ArgonHost-" + WindowsIdentity.GetCurrent().User!.Value, out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Argon is already open. Switch workspaces in the existing window.", "Argon");
            Shutdown();
            return;
        }
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Argon could not start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
