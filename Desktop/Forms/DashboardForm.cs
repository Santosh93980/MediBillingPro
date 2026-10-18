using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    /// <summary>
    /// Modern navigation dashboard for MediBilling Pro.
    /// Existing WinForms screens are discovered by class name at runtime.
    /// </summary>
    public sealed class DashboardForm : Form
    {
        private readonly Color nav = Color.FromArgb(21, 60, 49);
        private readonly Color hover = Color.FromArgb(34, 83, 68);
        private readonly Color accent = Color.FromArgb(8, 127, 91);
        private readonly Color page = Color.FromArgb(244, 248, 246);
        private readonly Color ink = Color.FromArgb(35, 52, 45);
        private readonly Color muted = Color.FromArgb(91, 111, 101);
        private readonly Panel sidebar;
        private readonly Panel main;
        private Panel navItems;
        private bool collapsed;
        private const int ExpandedWidth = 244;
        private const int CollapsedWidth = 76;

        public DashboardForm()
        {
            Text = "MediBilling Pro | Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 680);
            Size = new Size(1440, 900);
            BackColor = page;
            Font = new Font("Segoe UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;

            sidebar = new Panel { Dock = DockStyle.Left, Width = ExpandedWidth, BackColor = nav };
            main = new Panel { Dock = DockStyle.Fill, BackColor = page, AutoScroll = true };
            Controls.Add(main);
            Controls.Add(sidebar);
            BuildSidebar();
            BuildDashboard();
        }

        private void BuildSidebar()
        {
            var brand = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = nav };
            var logo = new Label { Text = "✚", Dock = DockStyle.Left, Width = 58, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Font = new Font("Segoe UI", 26F, FontStyle.Bold) };
            var brandText = new Label { Text = "MediBilling\nPRO", Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            brand.Controls.Add(brandText);
            brand.Controls.Add(logo);
            sidebar.Controls.Add(brand);

            navItems = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = nav };
            var toggle = new Button { Text = "☰   Collapse menu", Dock = DockStyle.Top, Height = 44, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(220, 237, 229), BackColor = nav, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), Cursor = Cursors.Hand };
            toggle.FlatAppearance.BorderSize = 0;
            toggle.Click += (s, e) =>
            {
                collapsed = !collapsed;
                sidebar.Width = collapsed ? CollapsedWidth : ExpandedWidth;
                toggle.Text = collapsed ? "☰" : "☰   Collapse menu";
                brandText.Visible = !collapsed;
                foreach (Control control in navItems.Controls)
                {
                    var button = control as Button;
                    if (button == null) continue;
                    string label = button.Tag as string ?? "";
                    button.Text = collapsed ? IconPart(label) : label;
                    button.TextAlign = collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
                    button.Padding = collapsed ? new Padding(0) : new Padding(18, 0, 0, 0);
                }
            };

            sidebar.Controls.Add(navItems);
            sidebar.Controls.Add(toggle);
            navItems.BringToFront();
            toggle.BringToFront();

            AddNav("⌂   Dashboard", null, true);
            AddNav("▦   Medicine Inventory", "MedicineForm");
            AddNav("♙   Suppliers", "SupplierForm");
            AddNav("▤   Supplier Medicines", "SupplierMedicineViewForm");
            AddNav("＋   Purchase Entry", "PurchaseEntryForm");
            AddNav("▣   Sales / Billing", "BillingForm");
            AddNav("♧   Customers", "CustomerForm");
            AddNav("▥   Stock & Batches", "BatchStockForm");
            AddNav("▤   Reports", "ReportsForm");
            AddNav("⚙   Settings", "SettingsForm");
        }

        private static string IconPart(string label)
        {
            int split = label.IndexOf("   ", StringComparison.Ordinal);
            return split > 0 ? label.Substring(0, split) : label;
        }

        private void AddNav(string label, string formType, bool active = false)
        {
            var button = new Button { Text = label, Tag = label, Dock = DockStyle.Top, Height = 47, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(231, 241, 236), BackColor = active ? accent : nav, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0), Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, active ? FontStyle.Bold : FontStyle.Regular) };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hover;
            button.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(formType)) BuildDashboard();
                else OpenModule(formType);
            };
            navItems.Controls.Add(button);
            navItems.Controls.SetChildIndex(button, 0);
        }

        private void BuildDashboard()
        {
            main.SuspendLayout();
            main.Controls.Clear();
            var content = new Panel { Dock = DockStyle.Top, Height = 690, Padding = new Padding(30, 24, 30, 20), BackColor = page };
            main.Controls.Add(content);

            var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = page };
            header.Controls.Add(new Label { Text = "Good day! Welcome back", Dock = DockStyle.Top, Height = 42, Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = ink });
            header.Controls.Add(new Label { Text = "Your pharmacy workspace, all in one place.", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F), ForeColor = muted });
            content.Controls.Add(header);

            content.Controls.Add(SectionLabel("OVERVIEW", 32));
            var metrics = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 122, WrapContents = false, AutoScroll = true, BackColor = page, Padding = new Padding(0, 5, 0, 5), Margin = new Padding(0) };
            metrics.Controls.Add(MetricCard("Medicine Items", "Inventory", "▦", Color.FromArgb(8, 127, 91)));
            metrics.Controls.Add(MetricCard("Suppliers", "Supplier directory", "♙", Color.FromArgb(49, 109, 177)));
            metrics.Controls.Add(MetricCard("Purchases", "Stock receiving", "＋", Color.FromArgb(198, 126, 37)));
            metrics.Controls.Add(MetricCard("Reports", "Business insights", "▥", Color.FromArgb(126, 87, 194)));
            content.Controls.Add(metrics);

            content.Controls.Add(SectionLabel("QUICK ACTIONS", 38));
            var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 174, WrapContents = true, AutoScroll = true, BackColor = page, Padding = new Padding(0, 4, 0, 4), Margin = new Padding(0) };
            actions.Controls.Add(ActionCard("Medicine Inventory", "Search and manage medicines", "MedicineForm"));
            actions.Controls.Add(ActionCard("Supplier Management", "Maintain supplier details", "SupplierForm"));
            actions.Controls.Add(ActionCard("Supplier Medicines", "Browse by supplier", "SupplierMedicineViewForm"));
            actions.Controls.Add(ActionCard("Purchase Entry", "Record invoices and receive stock", "PurchaseEntryForm"));
            actions.Controls.Add(ActionCard("Sales / Billing", "Open billing workflow", "BillingForm"));
            actions.Controls.Add(ActionCard("Reports", "Review business reports", "ReportsForm"));
            content.Controls.Add(actions);

            var note = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.White, Padding = new Padding(17) };
            note.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(222, 233, 227))) e.Graphics.DrawRectangle(pen, 0, 0, note.Width - 1, note.Height - 1); };
            note.Controls.Add(new Label { Text = "Choose a module from the left menu to get started. Live stock, sales and revenue totals can be connected when the corresponding dashboard queries are available.", Dock = DockStyle.Fill, ForeColor = muted, Font = new Font("Segoe UI", 9F) });
            note.Controls.Add(new Label { Text = "YOUR WORKSPACE", Dock = DockStyle.Top, Height = 24, ForeColor = ink, Font = new Font("Segoe UI", 10F, FontStyle.Bold) });
            content.Controls.Add(note);

            // Dock order is reverse of the intended top-to-bottom layout.
            content.Controls.SetChildIndex(note, 0);
            content.Controls.SetChildIndex(actions, 1);
            content.Controls.SetChildIndex(SectionControl(content, "QUICK ACTIONS"), 2);
            content.Controls.SetChildIndex(metrics, 3);
            content.Controls.SetChildIndex(SectionControl(content, "OVERVIEW"), 4);
            content.Controls.SetChildIndex(header, 5);
            main.ResumeLayout(true);
        }

        private static Control SectionControl(Control parent, string text)
        {
            return parent.Controls.Cast<Control>().FirstOrDefault(c => c is Label && ((Label)c).Text == text) ?? new Panel();
        }

        private Control SectionLabel(string text, int height)
        {
            return new Label { Text = text, Dock = DockStyle.Top, Height = height, Padding = new Padding(0, 10, 0, 0), ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        }

        private Control MetricCard(string heading, string caption, string symbol, Color symbolColor)
        {
            var card = new Panel { Size = new Size(220, 104), BackColor = Color.White, Margin = new Padding(0, 0, 14, 8), Padding = new Padding(12) };
            card.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(222, 233, 227))) e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = symbol, Dock = DockStyle.Left, Width = 42, ForeColor = symbolColor, Font = new Font("Segoe UI", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter });
            var textPanel = new Panel { Dock = DockStyle.Fill };
            textPanel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top, Height = 24, ForeColor = muted, Font = new Font("Segoe UI", 8.5F) });
            textPanel.Controls.Add(new Label { Text = heading, Dock = DockStyle.Top, Height = 30, ForeColor = ink, Font = new Font("Segoe UI", 10F, FontStyle.Bold) });
            card.Controls.Add(textPanel);
            return card;
        }

        private Control ActionCard(string heading, string description, string formType)
        {
            var button = new Button { Size = new Size(286, 68), Margin = new Padding(0, 0, 14, 10), BackColor = Color.White, ForeColor = ink, FlatStyle = FlatStyle.Flat, Text = heading + Environment.NewLine + description, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 8, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderColor = Color.FromArgb(222, 233, 227);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 243, 237);
            button.Click += (s, e) => OpenModule(formType);
            return button;
        }

        private void OpenModule(string formTypeName)
        {
            try
            {
                Type type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes)
                    .FirstOrDefault(t => t != null && t.Name.Equals(formTypeName, StringComparison.OrdinalIgnoreCase)
                        && typeof(Form).IsAssignableFrom(t) && t != typeof(DashboardForm));
                if (type == null)
                {
                    MessageBox.Show("The '" + formTypeName + "' screen is not included in the current desktop build yet.\n\nWhen that WinForms module is added, this menu item will open it.", "Module not available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var form = Activator.CreateInstance(type) as Form;
                if (form == null)
                {
                    MessageBox.Show("The screen was found but does not support a parameterless constructor: " + type.FullName, "Unable to open screen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog(this);
                form.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open " + formTypeName + ".\n" + ex.Message, "Navigation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Type[] SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).ToArray(); }
            catch { return new Type[0]; }
        }
    }
}
