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

            // Closing the dashboard through Logout returns to the sign-in screen.
            while (true)
            {
                UserSession session;
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK || login.Session == null)
                        return;
                    session = login.Session;
                    AuditService.SetSession(session);
                    AuditService.Write("Login", "Successful login", "Login", "Role=" + session.Role);
                }

                using (var dashboard = new DashboardForm(session))
                {
                    dashboard.ShowDialog();
                    if (!dashboard.WasLogoutRequested)
                        return;
                }
            }
        }
    }
}
