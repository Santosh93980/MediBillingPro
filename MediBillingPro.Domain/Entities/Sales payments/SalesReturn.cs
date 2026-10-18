using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Sales_payments
{
    public class SalesReturn
    {
        public int SalesReturnId { get; set; }
        public string ReturnNumber { get; set; }
        public int InvoiceId { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string RefundMethod { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public int? ProcessedBy { get; set; }
    }
}
