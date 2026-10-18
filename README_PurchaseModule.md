# MediBilling Pro — Purchase Entry & Batch Management

This commit adds the first backend/database layer for Purchase Entry.

## Files added
- `Database/Scripts/001_PurchaseBatchSchema.sql`
- `MediBillingPro.Application/Interfaces/IPurchaseRepository.cs`
- `MediBillingPro.Application/Services/PurchaseService.cs`
- `MediBillingPro.Infrastructure/Repositories/PurchaseRepository.cs`

## Prerequisites
The database must already contain `dbo.Medicines` with `MedicineId` and `IsActive`, and `dbo.Suppliers` with `SupplierId` and `IsActive`. Run the SQL script in SSMS against the same database used by the application.

## What the backend does
- Validates supplier, medicine, batch, expiry, quantities, and amounts.
- Saves the purchase header and item rows in one SQL transaction.
- Creates or increments stock in `MedicineBatches.QuantityOnHand`, including free units.
- Rolls back the full purchase if any insert/update fails.
- Returns purchase headers with supplier names through `GetAll()`.

## Register/use the repository
Construct `PurchaseRepository` with the existing `DatabaseConnectionFactory`, then pass it into `PurchaseService`:
```csharp
var repository = new PurchaseRepository(new DatabaseConnectionFactory());
var purchaseService = new PurchaseService(repository);
```
Adjust the factory construction if your current `DatabaseConnectionFactory` requires constructor arguments.

## Important
This is the database and application service foundation only; it does not yet add a WinForms Purchase Entry screen or wire a navigation button. Build the solution and confirm the actual SQL database schema before entering production purchases. The SQL script creates tables only when they do not already exist; it does not alter existing tables.
