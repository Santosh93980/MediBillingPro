using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class LicenseAuditLog
    {
        public long LicenseAuditLogId { get; set; }
        public int? LicenseId { get; set; }
        public int? PharmacyId { get; set; }
        public int? PerformedBy { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
