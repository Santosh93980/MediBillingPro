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
    public partial class PriceDetailsForm : Form
    {
        public PriceDetailsForm(Medicine medicine)
        {
            if (medicine == null)
                throw new ArgumentNullException(nameof(medicine));

            Text = "Price Details";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(400, 430);
            Size = new Size(480, 500);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = MediBillingTheme.Page;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20)
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

            var title = new Label
            {
                Text = "Price Details",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = MediBillingTheme.Secondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                BackColor = Color.White,
                Padding = new Padding(12),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            details.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 55));
            details.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 45));

            string[] labels =
            {
                "Product Type",
                "Purchase Price",
                "Margin / Deduction (%)",
                "GST (%)",
                "Price After Margin",
                "GST Deduction",
                "Final Calculated Price"
            };

            string[] values =
            {
                medicine.ProductType ?? "Other",
                FormatMoney(medicine.PurchasePrice),
                medicine.MarginPercent.ToString("0.##") + "%",
                medicine.GstPercent.ToString("0.##") + "%",
                FormatMoney(medicine.SellingPriceExcludingGst),
                FormatMoney(medicine.GstAmount),
                FormatMoney(medicine.SellingPrice)
            };

            for (int i = 0; i < labels.Length; i++)
            {
                details.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100f / labels.Length));

                var key = new Label
                {
                    Text = labels[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Padding = new Padding(4)
                };

                var value = new Label
                {
                    Text = values[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(31, 41, 55),
                    Font = new Font(
                        "Segoe UI",
                        10F,
                        i == 6 ? FontStyle.Bold : FontStyle.Regular),
                    Padding = new Padding(4)
                };

                if (i == 6)
                    value.ForeColor = MediBillingTheme.Primary;

                details.Controls.Add(key, 0, i);
                details.Controls.Add(value, 1, i);
            }

            var footer = new Panel { Dock = DockStyle.Fill };

            var close = new Button
            {
                Text = "Close",
                Size = new Size(100, 36),
                BackColor = MediBillingTheme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            close.FlatAppearance.BorderSize = 0;
            close.Click += (s, e) => Close();

            footer.Controls.Add(close);
            footer.Resize += (s, e) =>
            {
                close.Location = new Point(
                    footer.ClientSize.Width - close.Width, 5);
            };

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(details, 0, 1);
            root.Controls.Add(footer, 0, 2);
            Controls.Add(root);
        }

        private static string FormatMoney(decimal amount)
        {
            return "\u20B9" + amount.ToString("N2");
        }
    }
}
