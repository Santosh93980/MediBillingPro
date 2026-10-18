using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class PharmacyLicense
    {
        public int LicenseId { get; set; }
        public int PharmacyId { get; set; }
        public int LicensePlanId { get; set; }

        // Store the hash, never the original license key.
        public string LicenseKeyHash { get; set; }
        public string LicenseStatus { get; set; }

        public DateTime PurchaseDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public bool IsLifetime { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDevices { get; set; }

        public DateTime? LockedAt { get; set; }
        public string LockReason { get; set; }

        public DateTime? SuspendedAt { get; set; }
        public string SuspensionReason { get; set; }

        public DateTime? LastValidatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Pharmacy Pharmacy { get; set; }
        public LicensePlan LicensePlan { get; set; }
    }
}
