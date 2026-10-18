using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Sales_payments
{
    public class SalesReturnItem
    {
        public int SalesReturnItemId { get; set; }
        public int SalesReturnId { get; set; }
        public int InvoiceItemId { get; set; }
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool RestockApproved { get; set; }
    }
}
