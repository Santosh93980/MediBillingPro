using MediBillingPro.Domain.Entities.Medicine_inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Application.Interfaces
{
    public interface IMedicineRepository
    {
        List<Medicine> GetAllWithSupplierId(int? supplierId);
        List<Medicine> GetAll();
        List<Medicine> Search(string searchText, int supplierId = 0);
        int Add(Medicine medicine);
        bool Update(Medicine medicine);
        bool Delete(int medicineId);
    }
}
