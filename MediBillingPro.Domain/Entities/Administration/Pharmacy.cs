using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Administration
{
    public class Pharmacy
    {
        public int PharmacyId { get; set; }
        public string PharmacyCode { get; set; }
        public string PharmacyName { get; set; }
        public string OwnerName { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string GSTIN { get; set; }
        public string DrugLicenseNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
