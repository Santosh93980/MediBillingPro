using MediBillingPro.Domain.Entities.Medicine_inventory;
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
    public partial class StorageLocationForm : Form
    {
        private readonly Color green = Color.FromArgb(22, 163, 74);
        private readonly Color darkGreen = Color.FromArgb(20, 83, 45);
        private readonly Color page = Color.FromArgb(245, 250, 246);
        private readonly Color muted = Color.FromArgb(100, 116, 139);

        public StorageLocationForm(Medicine medicine)
        {
            if (medicine == null)
                throw new ArgumentNullException(nameof(medicine));

            Text = "Storage Location";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(440, 430);
            Size = new Size(520, 500);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = page;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;

            BuildUI(medicine);
        }

        private void BuildUI(Medicine medicine)
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(22),
                BackColor = page
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = page
            };

            var title = new Label
            {
                Text = "Storage Location",
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = darkGreen,
                AutoSize = true,
                Location = new Point(0, 2)
            };

            var subtitle = new Label
            {
                Text = medicine.MedicineName ?? "Medicine details",
                Font = new Font("Segoe UI", 10F),
                ForeColor = muted,
                AutoSize = true,
                Location = new Point(2, 39)
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            var details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(15),
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            details.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 38));
            details.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 62));

            string[,] items =
            {
                { "Rack", medicine.RackCode },
                { "Shelf", medicine.ShelfCode },
                { "Tray", medicine.TrayCode },
                { "Box", medicine.BoxCode },
                { "Notes", medicine.StorageNotes }
            };

            for (int i = 0; i < 5; i++)
            {
                details.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 20));

                var key = new Label
                {
                    Text = items[i, 0],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = muted,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Padding = new Padding(5, 0, 0, 0)
                };

                string value = string.IsNullOrWhiteSpace(items[i, 1])
                    ? "Not specified"
                    : items[i, 1];

                var val = new Label
                {
                    Text = value,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(31, 41, 55),
                    AutoEllipsis = false,
                    Padding = new Padding(8, 7, 5, 7)
                };

                details.Controls.Add(key, 0, i);
                details.Controls.Add(val, 1, i);
            }

            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = page
            };

            var close = new Button
            {
                Text = "Close",
                Width = 105,
                Height = 38,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(footer.Width - 105, 10)
            };

            close.FlatAppearance.BorderSize = 0;
            close.Click += (s, e) => Close();

            footer.Resize += (s, e) =>
            {
                close.Left = footer.ClientSize.Width - close.Width;
            };

            footer.Controls.Add(close);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(details, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);
        }
    }
}
