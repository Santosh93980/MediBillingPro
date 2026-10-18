using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Purchasing
{
    public class PurchaseReturnItem
    {
        public int PurchaseReturnItemId { get; set; }
        public int PurchaseReturnId { get; set; }
        public int PurchaseItemId { get; set; }
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }
    }
}
