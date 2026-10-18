using MediBillingPro.Domain.Entities.Contacts;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Purchasing
{
    public class Purchase
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; }
        public int SupplierId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public Supplier Supplier { get; set; }
    }
}
