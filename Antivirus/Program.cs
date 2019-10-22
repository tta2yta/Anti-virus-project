using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Antivirus
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frm_splsh s = new frm_splsh();
            frmAntivirus a = new frmAntivirus();
            Application.Run(s);
            Application.Run(a);
            Application.Exit();
        }

    }
}
