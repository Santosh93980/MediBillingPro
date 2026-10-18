using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class LicensePayment
    {
        public long LicensePaymentId { get; set; }
        public int LicenseId { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public string Notes { get; set; }
        public int? RecordedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
