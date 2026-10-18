using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    public partial class DashboardForm : Form
    {
        private static readonly Color Green = Color.FromArgb(8, 127, 91);
        private static readonly Color DarkGreen = Color.FromArgb(21, 60, 49);
        private static readonly Color Page = Color.FromArgb(244, 248, 246);
        private static readonly Color Muted = Color.FromArgb(91, 111, 101);
        private static readonly Color LightGreen = Color.FromArgb(231, 246, 239);

        private readonly Dictionary<string, Button> _navigation = new Dictionary<string, Button>();
        private Panel _sidebar;
        private Panel _contentHost;
        private FlowLayoutPanel _navigationHost;
        private Button _collapseButton;
        private Label _brandTitle;
        private Label _brandSubtitle;
        private Label _sidebarFooter;
        private bool _sidebarCollapsed;
        private Form _activeChild;
        private string _activePage = "Dashboard";

        public DashboardForm()
        {
            BuildLayout();
            ShowDashboard();
            SetActiveNavigation("Dashboard");
        }

        private void BuildLayout()
        {
            Text = "MediBilling Pro | Pharmacy Management";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            BackColor = Page;
            Font = new Font("Segoe UI", 9.5F);
            AutoScaleMode = AutoScaleMode.Dpi;

            // Two columns keep navigation separate from the page body.
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Page,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                ColumnCount = 2,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248F));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Page,
                Padding = new Padding(24)
            };

            main.Controls.Add(_contentHost, 1, 0);

            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 248,
                BackColor = DarkGreen
            };

            var brand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 94,
                BackColor = DarkGreen
            };

            _brandTitle = new Label
            {
                Text = "✚  MediBilling Pro",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15.5F, FontStyle.Bold),
                Location = new Point(18, 18)
            };
            _brandSubtitle = new Label
            {
                Text = "PHARMACY MANAGEMENT",
                AutoSize = true,
                ForeColor = Color.FromArgb(184, 218, 202),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(22, 54)
            };
            brand.Controls.Add(_brandTitle);
            brand.Controls.Add(_brandSubtitle);

            _collapseButton = CreateNavigationButton("☰   Collapse menu");
            _collapseButton.Dock = DockStyle.Top;
            _collapseButton.Height = 44;
            _collapseButton.Click += (s, e) => ToggleSidebar();

            _navigationHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(7, 4, 7, 4),
                BackColor = DarkGreen
            };

            AddNavigation("Dashboard", "▦", ShowDashboard);
            AddNavigation("Medicine Inventory", "▤", () => OpenModule(new MedicineForm(), "Medicine Inventory"));
            AddNavigation("Suppliers", "♙", () => OpenModule(new SupplierForm(), "Suppliers"));
            AddNavigation("Purchases", "▣", () => OpenModule(new OperationsForm("Purchases"), "Purchases"));
            AddNavigation("Sales & Billing", "▤", () => OpenModule(new OperationsForm("Sales & Billing"), "Sales & Billing"));
            AddNavigation("Customers", "♙", () => OpenModule(new OperationsForm("Customers"), "Customers"));
            AddNavigation("Stock & Batches", "▦", () => OpenModule(new OperationsForm("Stock & Batches"), "Stock & Batches"));
            AddNavigation("Reports", "▥", () => OpenModule(new OperationsForm("Reports"), "Reports"));
            AddNavigation("Settings", "⚙", () => OpenModule(new OperationsForm("Settings"), "Settings"));

            _sidebarFooter = new Label
            {
                Text = "Secure • Simple • Reliable",
                Dock = DockStyle.Bottom,
                Height = 38,
                ForeColor = Color.FromArgb(184, 218, 202),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F)
            };

            _sidebar.Controls.Add(_navigationHost);
            _sidebar.Controls.Add(_collapseButton);
            _sidebar.Controls.Add(brand);
            _sidebar.Controls.Add(_sidebarFooter);

            // Reserve column 0 for the sidebar and column 1 for the page body.
            _sidebar.Dock = DockStyle.Fill;
            _sidebar.Margin = Padding.Empty;
            main.Controls.Add(_sidebar, 0, 0);
            Controls.Add(main);

            _sidebar.Resize += (s, e) => ResizeNavigationButtons();
            ResizeNavigationButtons();
        }

        private Button CreateNavigationButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Height = 44,
                Width = 228,
                Margin = new Padding(0, 2, 0, 3),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 4, 0),
                BackColor = DarkGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 83, 67);
            return button;
        }

        private void AddNavigation(string pageName, string glyph, Action action)
        {
            var button = CreateNavigationButton(glyph + "   " + pageName);
            button.Tag = glyph;
            button.Click += (s, e) =>
            {
                action();
                SetActiveNavigation(pageName);
            };
            _navigation[pageName] = button;
            _navigationHost.Controls.Add(button);
        }

        private void ResizeNavigationButtons()
        {
            if (_navigationHost == null) return;

            int width = Math.Max(52, _navigationHost.ClientSize.Width - 22);
            foreach (Control control in _navigationHost.Controls)
            {
                if (control is Button button)
                {
                    button.Width = width;
                    button.TextAlign = _sidebarCollapsed
                        ? ContentAlignment.MiddleCenter
                        : ContentAlignment.MiddleLeft;
                    button.Padding = _sidebarCollapsed
                        ? new Padding(0)
                        : new Padding(12, 0, 4, 0);
                }
            }

            if (_collapseButton != null)
            {
                _collapseButton.Width = Math.Max(52, _sidebar.ClientSize.Width - 14);
                _collapseButton.Text = _sidebarCollapsed ? "☰" : "☰   Collapse menu";
                _collapseButton.TextAlign = _sidebarCollapsed
                    ? ContentAlignment.MiddleCenter
                    : ContentAlignment.MiddleLeft;
                _collapseButton.Padding = _sidebarCollapsed
                    ? new Padding(0)
                    : new Padding(12, 0, 0, 0);
            }
        }

        private void ToggleSidebar()
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            if (Controls.Count > 0 && Controls[0] is TableLayoutPanel layout)
                layout.ColumnStyles[0].Width = _sidebarCollapsed ? 72F : 248F;
            _sidebar.Width = _sidebarCollapsed ? 72 : 248;

            foreach (var item in _navigation)
            {
                var button = item.Value;
                var glyph = Convert.ToString(button.Tag);
                button.Text = _sidebarCollapsed ? glyph : glyph + "   " + item.Key;
                button.TextAlign = _sidebarCollapsed
                    ? ContentAlignment.MiddleCenter
                    : ContentAlignment.MiddleLeft;
                button.Padding = _sidebarCollapsed
                    ? new Padding(0)
                    : new Padding(12, 0, 4, 0);
            }

            if (_brandTitle != null)
            {
                _brandTitle.Text = _sidebarCollapsed ? "✚" : "✚  MediBilling Pro";
                _brandTitle.Location = _sidebarCollapsed ? new Point(24, 18) : new Point(18, 18);
            }

            if (_brandSubtitle != null) _brandSubtitle.Visible = !_sidebarCollapsed;
            if (_sidebarFooter != null) _sidebarFooter.Visible = !_sidebarCollapsed;
            ResizeNavigationButtons();
        }

        private void SetActiveNavigation(string pageName)
        {
            _activePage = pageName;
            foreach (var item in _navigation)
            {
                bool selected = item.Key == pageName;
                item.Value.BackColor = selected ? Green : DarkGreen;
                item.Value.Font = new Font("Segoe UI", 9.5F,
                    selected ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private void ClearContent()
        {
            if (_activeChild != null)
            {
                _activeChild.FormClosed -= ActiveChild_FormClosed;
                _activeChild.Dispose();
                _activeChild = null;
            }

            _contentHost.SuspendLayout();
            _contentHost.Controls.Clear();
            _contentHost.Padding = Padding.Empty;
            _contentHost.ResumeLayout();
        }

        private void OpenModule(Form form, string pageName)
        {
            ClearContent();

            form.SuspendLayout();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.WindowState = FormWindowState.Normal;
            form.MinimumSize = Size.Empty;
            form.Dock = DockStyle.Fill;
            form.TopMost = false;
            form.ShowInTaskbar = false;
            form.Margin = Padding.Empty;
            form.ResumeLayout();

            _activeChild = form;
            _contentHost.Controls.Add(form);
            form.Show();
            form.BringToFront();
            _activePage = pageName;
            SetActiveNavigation(pageName);
        }

        private void ShowDashboard()
        {
            ClearContent();
            _contentHost.Padding = new Padding(28);

            var page = new Panel { Dock = DockStyle.Fill, BackColor = Page, AutoScroll = true };
            _contentHost.Controls.Add(page);

            var heading = new Label
            {
                Text = "Dashboard",
                AutoSize = true,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                ForeColor = DarkGreen,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Your pharmacy workspace at a glance.",
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Muted,
                Location = new Point(3, 48)
            };
            page.Controls.Add(heading);
            page.Controls.Add(subtitle);

            var cards = new TableLayoutPanel
            {
                Location = new Point(0, 96),
                Width = Math.Max(600, page.ClientSize.Width - 8),
                Height = 132,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Page,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (int i = 0; i < 4; i++)
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            cards.Controls.Add(CreateMetricCard("MEDICINE INVENTORY", "Manage products", "Search, edit and review medicines"), 0, 0);
            cards.Controls.Add(CreateMetricCard("SUPPLIERS", "Supplier directory", "Manage supplier contacts"), 1, 0);
            cards.Controls.Add(CreateMetricCard("PURCHASES", "Purchase entry", "Receive supplier stock"), 2, 0);
            cards.Controls.Add(CreateMetricCard("SALES & BILLING", "Invoice billing", "Sell and track payments"), 3, 0);
            page.Controls.Add(cards);

            var quickTitle = new Label
            {
                Text = "Quick actions",
                AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = DarkGreen,
                Location = new Point(2, 260)
            };
            page.Controls.Add(quickTitle);

            var quickActions = new FlowLayoutPanel
            {
                Location = new Point(0, 304),
                Width = Math.Max(600, page.ClientSize.Width - 8),
                Height = 62,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = true,
                BackColor = Page,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            quickActions.Controls.Add(CreateActionButton("Open Medicine Inventory", () =>
                OpenModule(new MedicineForm(), "Medicine Inventory")));
            quickActions.Controls.Add(CreateActionButton("Manage Suppliers", () =>
                OpenModule(new SupplierForm(), "Suppliers")));
            page.Controls.Add(quickActions);

            var info = new Panel
            {
                Location = new Point(0, 390),
                Width = Math.Max(600, page.ClientSize.Width - 8),
                Height = 94,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = LightGreen,
                Padding = new Padding(18)
            };
            var infoTitle = new Label
            {
                Text = "Getting started",
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = DarkGreen,
                Location = new Point(16, 13)
            };
            var infoText = new Label
            {
                Text = "Manage medicines and suppliers, record purchases, bill sales, maintain customers, review stock and open reports/settings.",
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 42,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            info.Controls.Add(infoText);
            info.Controls.Add(infoTitle);
            page.Controls.Add(info);

            page.Resize += (s, e) =>
            {
                cards.Width = Math.Max(600, page.ClientSize.Width - 8);
                quickActions.Width = Math.Max(600, page.ClientSize.Width - 8);
                info.Width = Math.Max(600, page.ClientSize.Width - 8);
            };
        }

        private Control CreateMetricCard(string title, string value, string description)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14)
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            var valueLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Top,
                Height = 34,
                ForeColor = Green,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold)
            };
            var descriptionLabel = new Label
            {
                Text = description,
                Dock = DockStyle.Fill,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8F)
            };

            card.Controls.Add(descriptionLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return card;
        }

        private Button CreateActionButton(string text, Action action)
        {
            var button = new Button
            {
                Text = text + "   →",
                Width = 240,
                Height = 44,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (s, e) => action();
            return button;
        }

        private void ShowPlaceholder(string pageName, string description)
        {
            ClearContent();
            _contentHost.Padding = new Padding(28);

            var page = new Panel { Dock = DockStyle.Fill, BackColor = Page };
            _contentHost.Controls.Add(page);

            var title = new Label
            {
                Text = pageName,
                AutoSize = true,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = DarkGreen,
                Location = new Point(0, 0)
            };
            var card = new Panel
            {
                Location = new Point(0, 74),
                Size = new Size(650, 210),
                MaximumSize = new Size(900, 210),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(24)
            };
            var badge = new Label
            {
                Text = "MODULE PREVIEW",
                AutoSize = true,
                ForeColor = Green,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(24, 22)
            };
            var detail = new Label
            {
                Text = description,
                AutoSize = false,
                Location = new Point(24, 58),
                Size = new Size(560, 58),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 11F)
            };
            var back = CreateActionButton("Back to Dashboard", () =>
            {
                ShowDashboard();
                SetActiveNavigation("Dashboard");
            });
            back.Location = new Point(24, 135);
            card.Controls.Add(badge);
            card.Controls.Add(detail);
            card.Controls.Add(back);
            page.Controls.Add(title);
            page.Controls.Add(card);
            _activePage = pageName;
            SetActiveNavigation(pageName);
        }

        private void ActiveChild_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (ReferenceEquals(sender, _activeChild))
                _activeChild = null;
        }
    }
}
