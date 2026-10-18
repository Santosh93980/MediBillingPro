using MediBillingPro.Domain.Entities.Contacts;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Sales_payments
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public int? CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string InvoiceStatus { get; set; }
        public string PrescriptionReference { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public Customer Customer { get; set; }
    }

}
