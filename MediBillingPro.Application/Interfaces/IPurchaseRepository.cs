using MediBillingPro.Domain.Entities.Purchasing;
using System.Collections.Generic;

namespace MediBillingPro.Application.Interfaces
{
    public interface IPurchaseRepository
    {
        int Create(Purchase purchase, IList<PurchaseItem> items);
        List<Purchase> GetAll();
    }
}