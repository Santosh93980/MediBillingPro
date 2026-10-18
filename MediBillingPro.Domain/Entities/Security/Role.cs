using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Domain.Entities.Security
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
