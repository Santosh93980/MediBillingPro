using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Sales_payments
{
    public class Payment
    {
        public long PaymentId { get; set; }
        public int? InvoiceId { get; set; }
        public int? PurchaseId { get; set; }
        public string PaymentType { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; }
        public int? ReceivedBy { get; set; }
    }
}
