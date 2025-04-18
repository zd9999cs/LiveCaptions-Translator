using System;
using System.IO; // Added for file logging
using System.Threading.Tasks; // Keep existing using
using System.Windows;
using System.Windows.Threading; // Added for DispatcherUnhandledExceptionEventArgs

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        App()
        {
            // Add this line to register the handler
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            Task.Run(() => Translator.SyncLoop());
            Task.Run(() => Translator.TranslateLoop());
        }

        // Add this event handler method
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception details
            LogException(e.Exception);

            // Prevent default WPF crash behavior
            e.Handled = true;

            // Optionally: Show a user-friendly error message
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nThe application might become unstable. Please check the error log for details.",
                            "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Optionally: Decide if the application should shut down
            // Shutdown();
        }

        // Add this helper method to log exceptions
        private void LogException(Exception ex)
        {
            try
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                string errorMessage = $"[{DateTime.Now}] Unhandled Exception:\n" +
                                      $"Message: {ex.Message}\n" +
                                      $"StackTrace:\n{ex.StackTrace}\n\n";

                // Log inner exceptions if they exist
                if (ex.InnerException != null)
                {
                    errorMessage += $"Inner Exception:\n" +
                                    $"Message: {ex.InnerException.Message}\n" +
                                    $"StackTrace:\n{ex.InnerException.StackTrace}\n\n";
                }

                File.AppendAllText(logFilePath, errorMessage);
            }
            catch (Exception logEx)
            {
                // Fallback if logging fails
                Console.WriteLine($"Failed to write to error log: {logEx.Message}");
                Console.WriteLine($"Original Error: {ex.Message}");
            }
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (Translator.Window != null)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                LiveCaptionsHandler.KillLiveCaptions(Translator.Window);
            }
        }
    }
}
