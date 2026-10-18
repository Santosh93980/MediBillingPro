using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Contacts
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string GSTIN { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
