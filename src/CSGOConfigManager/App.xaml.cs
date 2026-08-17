using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace CSGOConfigManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var exception = e.Exception;
        var errorDetails = $"An unexpected error occurred:\n\n{exception.Message}\n\n" +
                          $"Stack Trace:\n{exception.StackTrace}\n\n" +
                          $"Source: {exception.Source}\n" +
                          $"Target Site: {exception.TargetSite?.Name}";

        MessageBox.Show(
            errorDetails,
            "CS:GO Config Manager - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        // Log the error for debugging
        Debug.WriteLine($"Unhandled Exception: {exception}");
        
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            var errorDetails = $"Fatal error occurred:\n\n{exception.Message}\n\n" +
                              $"Stack Trace:\n{exception.StackTrace}\n\n" +
                              $"The application will now exit.";

            MessageBox.Show(
                errorDetails,
                "CS:GO Config Manager - Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            Debug.WriteLine($"Fatal Exception: {exception}");
        }
    }
}
