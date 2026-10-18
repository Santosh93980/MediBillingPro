using MediBillingPro.Domain.Entities.Medicine_inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Purchasing
{
    public class PurchaseItem
    {
        public int PurchaseItemId { get; set; }
        public int PurchaseId { get; set; }
        public int MedicineId { get; set; }
        public int? BatchId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public int FreeQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }

        public Purchase Purchase { get; set; }
        public Medicine Medicine { get; set; }
        public MedicineBatch Batch { get; set; }
    }
}
