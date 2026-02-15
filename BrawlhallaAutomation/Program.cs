using System;
using System.Windows.Forms;

namespace BrawlhallaAutomation
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Run the application
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                    "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
