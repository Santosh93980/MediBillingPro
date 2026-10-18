using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
