using MediBillingPro.Domain.Entities.Medicine_inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Sales_payments
{
    public class InvoiceItem
    {
        public int InvoiceItemId { get; set; }
        public int InvoiceId { get; set; }
        public int MedicineId { get; set; }
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }

        public Invoice Invoice { get; set; }
        public Medicine Medicine { get; set; }
        public MedicineBatch Batch { get; set; }
    }
}
