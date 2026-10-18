using MediBillingPro.Application.Interfaces;
using MediBillingPro.Domain.Entities.Contacts;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediBillingPro.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _repository;

        public SupplierService(ISupplierRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public List<Supplier> GetAll() => _repository.GetAll();

        public List<Supplier> Search(string searchText) => _repository.Search(searchText);

        public int Add(Supplier supplier)
        {
            Validate(supplier);
            supplier.SupplierName = supplier.SupplierName.Trim();
            supplier.IsActive = true;
            return _repository.Add(supplier);
        }

        public bool Update(Supplier supplier)
        {
            Validate(supplier);
            if (supplier.SupplierId <= 0)
                throw new ArgumentException("A valid Supplier ID is required.");

            supplier.SupplierName = supplier.SupplierName.Trim();
            return _repository.Update(supplier);
        }

        public bool Delete(int supplierId)
        {
            if (supplierId <= 0)
                throw new ArgumentException("A valid Supplier ID is required.");

            return _repository.Deactivate(supplierId);
        }

        private static void Validate(Supplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                throw new ArgumentException("Supplier name is required.");

            if (supplier.SupplierName.Trim().Length > 150)
                throw new ArgumentException("Supplier name cannot exceed 150 characters.");

            if (supplier.ContactPerson?.Length > 100)
                throw new ArgumentException("Contact person cannot exceed 100 characters.");
            if (supplier.MobileNumber?.Length > 20)
                throw new ArgumentException("Mobile number cannot exceed 20 characters.");
            if (supplier.Email?.Length > 150)
                throw new ArgumentException("Email cannot exceed 150 characters.");
            if (supplier.Address?.Length > 500)
                throw new ArgumentException("Address cannot exceed 500 characters.");
            if (supplier.GSTIN?.Length > 20)
                throw new ArgumentException("GSTIN cannot exceed 20 characters.");
            if (supplier.LicenseNumber?.Length > 100)
                throw new ArgumentException("License number cannot exceed 100 characters.");
        }
    }
}
