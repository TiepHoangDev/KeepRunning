using ClassLibraryHelper;
using StartupHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeepRunning
{
    static class Program
    {
        public const string VERSION = "2020.10.20.1";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            if (ControlHelper.NotYetStarted(AppName))
            {
                MethodHelper.UseTryCatch(() =>
                {
                    ControlHelper.appShortcutToDesktop(AppName);
                    ControlHelper.Startup(_StartupManager, true);
                });

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }

        const string AppName = "KeepRunning";
        public static StartupManager _StartupManager = new StartupManager(AppName, RegistrationScope.Local);
    }
}
