using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Purchasing
{
    public class PurchaseReturn
    {
        public int PurchaseReturnId { get; set; }
        public string ReturnNumber { get; set; }
        public int PurchaseId { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public int? ProcessedBy { get; set; }
    }
}
