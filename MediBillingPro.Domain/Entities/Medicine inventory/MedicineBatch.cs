using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Medicine_inventory
{
    public class MedicineBatch
    {
        public int BatchId { get; set; }
        public int MedicineId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal GSTPercent { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime CreatedAt { get; set; }

        public Medicine Medicine { get; set; }
    }
}
