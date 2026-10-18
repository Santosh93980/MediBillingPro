using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Medicine_inventory
{
    public class StockMovement
    {
        public long StockMovementId { get; set; }
        public int BatchId { get; set; }
        public string MovementType { get; set; }
        public int QuantityChange { get; set; }
        public string ReferenceNumber { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public MedicineBatch Batch { get; set; }
    }
}
