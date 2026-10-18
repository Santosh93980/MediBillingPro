using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class LicensePlan
    {
        public int LicensePlanId { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int? ValidityDays { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDevices { get; set; }
        public bool IsLifetime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
