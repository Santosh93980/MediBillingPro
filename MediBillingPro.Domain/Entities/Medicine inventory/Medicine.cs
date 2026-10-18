using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Medicine_inventory
{
    public class Medicine
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string GenericName { get; set; }
        public string Manufacturer { get; set; }
        public string Barcode { get; set; }
        public string Category { get; set; }
        public string Strength { get; set; }
        public string Unit { get; set; } = "Strip";
        public int ReorderLevel { get; set; } = 10;
        public bool IsPrescriptionRequired { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Physical medicine storage location
        public string RackCode { get; set; }
        public string ShelfCode { get; set; }
        public string TrayCode { get; set; }
        public string BoxCode { get; set; }
        public string StorageNotes { get; set; }


        public string ProductType { get; set; } = "Other";

        public decimal PurchasePrice { get; set; }

        public decimal MarginPercent { get; set; } = 15m;

        public decimal GstPercent { get; set; }

        public decimal SellingPriceExcludingGst { get; set; }

        public decimal GstAmount { get; set; }

        public decimal SellingPrice { get; set; }

        public int? SupplierId { get; set; }

    }
}
