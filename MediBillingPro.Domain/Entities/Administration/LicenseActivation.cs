using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class LicenseActivation
    {
        public long ActivationId { get; set; }
        public int LicenseId { get; set; }
        public string DeviceCodeHash { get; set; }
        public string DeviceName { get; set; }
        public string AppVersion { get; set; }
        public DateTime ActivatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public bool IsActive { get; set; }
        public int? DeactivatedBy { get; set; }
        public string DeactivationReason { get; set; }
    }
}
