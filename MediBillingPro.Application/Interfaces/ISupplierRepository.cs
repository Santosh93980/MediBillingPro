using MediBillingPro.Domain.Entities.Contacts;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Application.Interfaces
{
    public interface ISupplierRepository
    {
        List<Supplier> GetAll();
        List<Supplier> Search(string searchText);
        int Add(Supplier supplier);
        bool Update(Supplier supplier);
        bool Deactivate(int supplierId);
    }
}
