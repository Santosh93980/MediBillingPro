using MediBillingPro.Desktop.Forms;
using System;
using System.Windows.Forms;

namespace MediBillingPro.Desktop
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK || login.Session == null)
                    return;

                System.Windows.Forms.Application.Run(new DashboardForm(login.Session));
            }
        }
    }
}
