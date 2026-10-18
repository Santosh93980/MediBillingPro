using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    public partial class DashboardForm : Form
    {
        private readonly Color _green = Color.FromArgb(8, 127, 91);
        private readonly Color _darkGreen = Color.FromArgb(21, 60, 49);
        private readonly Color _page = Color.FromArgb(244, 248, 246);
        private readonly Color _muted = Color.FromArgb(91, 111, 101);
        private Panel _sidebar;
        private Panel _main;
        private bool _sidebarCollapsed;

        public DashboardForm()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "MediBilling Pro";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 650);
            BackColor = _page;
            Font = new Font("Segoe UI", 9F);

            _sidebar = new Panel { Dock = DockStyle.Left, Width = 245, BackColor = _darkGreen };
            Controls.Add(_sidebar);

            var brand = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = _darkGreen };
            var brandTitle = new Label
            {
                Text = "MediBilling Pro",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            var brandSub = new Label
            {
                Text = "PHARMACY MANAGEMENT",
                ForeColor = Color.FromArgb(184, 218, 202),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(22, 51)
            };
            brand.Controls.Add(brandTitle);
            brand.Controls.Add(brandSub);
            _sidebar.Controls.Add(brand);

            var collapse = MakeNavButton("☰   Collapse menu", 0);
            collapse.Dock = DockStyle.Top;
            collapse.Height = 46;
            collapse.Click += (s, e) => ToggleSidebar();
            _sidebar.Controls.Add(collapse);

            AddNavButton("▦   Dashboard", () => ShowDashboard(), true);
            AddNavButton("▤   Medicine Inventory", () => OpenModule(new MedicineForm()));
            AddNavButton("♙   Suppliers", () => OpenModule(new SupplierForm()));
            AddNavButton("▣   Purchases", () => ShowNotConfigured("Purchases"));
            AddNavButton("▤   Sales & Billing", () => ShowNotConfigured("Sales & Billing"));
            AddNavButton("▥   Reports", () => ShowNotConfigured("Reports"));
            AddNavButton("⚙   Settings", () => ShowNotConfigured("Settings"));

            var bottom = new Label
            {
                Text = "Secure • Simple • Reliable",
                Dock = DockStyle.Bottom,
                Height = 44,
                ForeColor = Color.FromArgb(184, 218, 202),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _sidebar.Controls.Add(bottom);

            _main = new Panel { Dock = DockStyle.Fill, BackColor = _page, Padding = new Padding(28) };
            Controls.Add(_main);
            _main.BringToFront();
            _sidebar.BringToFront();
            ShowDashboard();
        }

        private void AddNavButton(string text, Action action, bool selected = false)
        {
            var button = MakeNavButton(text, 0);
            button.Dock = DockStyle.Top;
            button.Height = 48;
            button.BackColor = selected ? _green : _darkGreen;
            button.Click += (s, e) => action();
            _sidebar.Controls.Add(button);
            button.BringToFront();
        }

        private Button MakeNavButton(string text, int top)
        {
            return new Button
            {
                Text = text,
                Height = 46,
                Dock = DockStyle.None,
                Location = new Point(8, top),
                Width = 225,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                BackColor = _darkGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
        }

        private void ToggleSidebar()
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            _sidebar.Width = _sidebarCollapsed ? 72 : 245;
            foreach (Control control in _sidebar.Controls)
            {
                if (control is Button button)
                {
                    if (_sidebarCollapsed)
                    {
                        button.Text = button.Text.Contains("Dashboard") ? "▦" :
                                      button.Text.Contains("Medicine") ? "▤" :
                                      button.Text.Contains("Suppliers") ? "♙" :
                                      button.Text.Contains("Purchases") ? "▣" :
                                      button.Text.Contains("Sales") ? "▤" :
                                      button.Text.Contains("Reports") ? "▥" :
                                      button.Text.Contains("Settings") ? "⚙" : "☰";
                        button.TextAlign = ContentAlignment.MiddleCenter;
                        button.Padding = new Padding(0);
                    }
                    else
                    {
                        button.TextAlign = ContentAlignment.MiddleLeft;
                        button.Padding = new Padding(12, 0, 0, 0);
                    }
                }
            }
        }

        private void ShowDashboard()
        {
            _main.Controls.Clear();

            var header = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = _darkGreen,
                AutoSize = true,
                Location = new Point(28, 24)
            };
            _main.Controls.Add(header);

            var subtitle = new Label
            {
                Text = "Welcome back. Here's your pharmacy workspace.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = _muted,
                AutoSize = true,
                Location = new Point(31, 70)
            };
            _main.Controls.Add(subtitle);

            var cardY = 125;
            var cardWidth = Math.Max(180, (_main.ClientSize.Width - 100) / 4);
            AddMetricCard("MEDICINE ITEMS", "Open inventory", "Manage your medicine catalogue", 28, cardY, cardWidth);
            AddMetricCard("SUPPLIERS", "Open suppliers", "Manage supplier contacts", 38 + cardWidth, cardY, cardWidth);
            AddMetricCard("PURCHASES", "Coming next", "Purchase workflow not connected yet", 48 + cardWidth * 2, cardY, cardWidth);
            AddMetricCard("SALES TODAY", "Coming next", "Billing workflow not connected yet", 58 + cardWidth * 3, cardY, cardWidth);

            var section = new Label
            {
                Text = "Quick actions",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = _darkGreen,
                AutoSize = true,
                Location = new Point(31, 285)
            };
            _main.Controls.Add(section);

            AddActionButton("Open Medicine Inventory", 31, 330, () => OpenModule(new MedicineForm()));
            AddActionButton("Manage Suppliers", 31, 390, () => OpenModule(new SupplierForm()));

            var note = new Label
            {
                Text = "Live totals will appear here when dashboard data services are connected.",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = _muted,
                AutoSize = true,
                Location = new Point(31, 470)
            };
            _main.Controls.Add(note);
        }

        private void AddMetricCard(string title, string value, string description, int x, int y, int width)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width - 12, 125),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(16, 14),
                AutoSize = true,
                ForeColor = _muted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            var valueLabel = new Label
            {
                Text = value,
                Location = new Point(16, 40),
                AutoSize = true,
                ForeColor = _green,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold)
            };
            var descriptionLabel = new Label
            {
                Text = description,
                Location = new Point(16, 78),
                AutoSize = true,
                ForeColor = _muted,
                Font = new Font("Segoe UI", 8F)
            };
            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(descriptionLabel);
            _main.Controls.Add(card);
        }

        private void AddActionButton(string text, int x, int y, Action action)
        {
            var button = new Button
            {
                Text = text + "   →",
                Location = new Point(x, y),
                Size = new Size(260, 44),
                BackColor = _green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (s, e) => action();
            _main.Controls.Add(button);
        }

        private void OpenModule(Form form)
        {
            using (form)
            {
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog(this);
            }
        }

        private void ShowNotConfigured(string module)
        {
            MessageBox.Show(
                module + " page is not connected yet. The current dashboard links to the existing Medicine Inventory and Suppliers forms.",
                "Module not configured",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
