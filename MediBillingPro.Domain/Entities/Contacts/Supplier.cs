using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Contacts
{
    public class Supplier
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string GSTIN { get; set; }
        public string LicenseNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MedicineCount { get; set; }
    }
}
