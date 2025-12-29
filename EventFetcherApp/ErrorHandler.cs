using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace EventFetcherApp
{
    internal class ErrorHandler
    {
        public ErrorHandler(string source, string message, Exception? ex = null)
        {
            Show(source, message, ex);
        }

        public static void Show(string source, string message, Exception? ex = null)
        {
            string text = $"[{source.ToUpper()}] {message}";
            if (ex != null) text += $"\nException: {ex.Message}";

            Trace.TraceError(text);
            MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
