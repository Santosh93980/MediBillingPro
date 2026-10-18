using MediBillingPro.Application.Interfaces;
using MediBillingPro.Domain.Entities.Medicine_inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Application.Services
{
    public class MedicineService
    {
        private readonly IMedicineRepository _repository;

        public MedicineService(IMedicineRepository repository)
        {
            _repository = repository;
        }

        public List<Medicine> GetAll(int? supplierId)
        {
            return _repository.GetAllWithSupplierId(supplierId);
        }

        public List<Medicine> GetAll()
        {
            return _repository.GetAll();
        }

        public List<Medicine> Search(string searchText, int supplierId)
        {
            return _repository.Search(searchText, supplierId);
        }

        public int Add(Medicine medicine)
        {
            if (medicine == null)
                throw new ArgumentNullException(nameof(medicine));

            if (string.IsNullOrWhiteSpace(medicine.MedicineName))
                throw new ArgumentException("Medicine name is required.");

            if (medicine.ReorderLevel < 0)
                throw new ArgumentException(
                    "Reorder level cannot be negative.");

            return _repository.Add(medicine);
        }


        public bool Update(Medicine medicine)
        {
            if (medicine == null)
                throw new ArgumentNullException(nameof(medicine));

            if (medicine.MedicineId <= 0)
                throw new ArgumentException("Select a medicine to update.");

            if (string.IsNullOrWhiteSpace(medicine.MedicineName))
                throw new ArgumentException("Medicine name is required.");

            if (medicine.ReorderLevel < 0)
                throw new ArgumentException(
                    "Reorder level cannot be negative.");

            return _repository.Update(medicine);
        }

        public bool Delete(int medicineId)
        {
            if (medicineId <= 0)
                throw new ArgumentException("Invalid medicine ID.");

            return _repository.Delete(medicineId);
        }

    }
}
