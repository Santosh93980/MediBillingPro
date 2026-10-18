using MediBillingPro.Application.Interfaces;
using MediBillingPro.Domain.Entities.Purchasing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediBillingPro.Application.Services
{
    public class PurchaseService
    {
        private readonly IPurchaseRepository _repository;

        public PurchaseService(IPurchaseRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public int Create(Purchase purchase, IList<PurchaseItem> items)
        {
            if (purchase == null) throw new ArgumentNullException(nameof(purchase));
            if (purchase.SupplierId <= 0) throw new ArgumentException("Select a valid supplier.");
            if (items == null || items.Count == 0) throw new ArgumentException("Add at least one medicine.");

            foreach (PurchaseItem item in items)
            {
                if (item == null) throw new ArgumentException("Purchase contains an empty item.");
                if (item.MedicineId <= 0) throw new ArgumentException("Select a valid medicine.");
                if (string.IsNullOrWhiteSpace(item.BatchNumber)) throw new ArgumentException("Batch number is required.");
                if (item.ExpiryDate.Date <= DateTime.Today) throw new ArgumentException("Expiry date must be in the future.");
                if (item.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
                if (item.FreeQuantity < 0) throw new ArgumentException("Free quantity cannot be negative.");
                if (item.PurchasePrice < 0 || item.SellingPrice < 0 || item.GSTPercent < 0 ||
                    item.TaxAmount < 0 || item.LineTotal < 0)
                    throw new ArgumentException("Prices, GST, tax and line total cannot be negative.");
            }

            if (purchase.DiscountAmount < 0 || purchase.AmountPaid < 0)
                throw new ArgumentException("Discount and amount paid cannot be negative.");

            purchase.SubTotal = items.Sum(x => x.PurchasePrice * x.Quantity);
            purchase.TaxAmount = items.Sum(x => x.TaxAmount);
            purchase.TotalAmount = Math.Max(0m, purchase.SubTotal - purchase.DiscountAmount + purchase.TaxAmount);

            if (purchase.AmountPaid > purchase.TotalAmount)
                throw new ArgumentException("Amount paid cannot exceed the purchase total.");

            purchase.Status = purchase.AmountPaid == 0m ? "Credit" :
                purchase.AmountPaid >= purchase.TotalAmount ? "Paid" : "Partial";
            if (string.IsNullOrWhiteSpace(purchase.PurchaseNumber))
                purchase.PurchaseNumber = "PUR-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            if (purchase.PurchaseDate == DateTime.MinValue) purchase.PurchaseDate = DateTime.Today;

            return _repository.Create(purchase, items);
        }

        public List<Purchase> GetAll() => _repository.GetAll();
    }
}