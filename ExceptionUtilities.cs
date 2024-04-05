using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace VisualizeRoutingWpf
{
    internal static class ExceptionUtilities
    {
        public static IEnumerable<Exception> GetInnerExceptions(this Exception ex)
        {
            Exception innerException = ex ?? throw new ArgumentNullException(nameof(ex));
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            } while (innerException != null);
        }

        public static string GetInnerExceptionsAsString(this Exception ex)
        {
            return string.Join<Exception>(Environment.NewLine, ex.GetInnerExceptions());
        }

        public static string GetExceptionMessages(this Exception ex) =>
            ex.Message
            + Environment.NewLine
            + string.Join(Environment.NewLine, ex.GetInnerExceptions());

        public static void ShowException(this Exception ex)
        {
            Trace.WriteLine($"{ex.GetExceptionMessages()}");
        }

        public static void ShowExceptionMessageBox(this Exception ex, string context = "", string extensionName = "<general extension>")
        {
            using (var form = new Form { TopMost = true }) // required for messagebox to show up in front of VS splash screen
            {
                if (context == null) context = string.Empty;
                if (!string.IsNullOrEmpty(context)) context += ":";

                MessageBox.Show(form, 
                    $@"{context}{Environment.NewLine}{ex.GetExceptionMessages()}",
                    extensionName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
