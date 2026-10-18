# MediBilling Pro — Operational Forms and Process Flow

## Files added
- `MediBillingPro.Desktop/Forms/OperationsForm.cs`: Customers, Purchase Entry, Sales & Billing, Stock & Batches, Reports, and Settings pages.
- `Database/Scripts/002_OperationalModules.sql`: Customers, Invoices, InvoiceItems, and AppSettings schema.
- `MediBillingPro.Desktop/Forms/DashboardForm.cs`: sidebar links wired to the operational pages.

## Database setup
Run scripts in SQL Server Management Studio against the same database configured by `MediBillingProDB`:
1. `Database/Scripts/001_PurchaseBatchSchema.sql` — purchase headers/items and medicine batches.
2. `Database/Scripts/002_OperationalModules.sql` — customer records, invoices, invoice items, and application settings.

The scripts create missing tables only; they do not migrate or alter existing tables. Back up the database before applying scripts to a database containing pre-existing tables.

## Available operations
- Customers: create, search, edit (double-click a row), and delete when not referenced by invoices.
- Purchases: create a supplier purchase with medicine, batch, expiry, quantities, purchase/selling price and GST; the existing purchase service saves the purchase transactionally and adds stock to the batch. Purchase list is available from the page.
- Sales & Billing: create a sale for a selected in-stock, unexpired batch; the invoice and invoice item are saved in a SQL transaction and stock is reduced only when sufficient quantity remains. Invoice list is available from the page.
- Stock & Batches: review batches, expiry dates, prices and quantities; use purchases and sales to change stock rather than editing stock counts directly.
- Reports: view purchase/sales totals and an inventory report.
- Settings: create, search, edit (double-click a row), and delete application key/value settings.

Financial documents are intentionally not hard-deleted. Corrections should be made through a proper return/void workflow so stock and accounting totals remain auditable.

## Process flow
Supplier setup → Medicine inventory setup → Purchase entry (supplier invoice + batch/expiry + quantities) → Batch stock increases → Sales & Billing (customer optional + batch + quantity + payment received) → Invoice saved and stock decreases → Stock/expiry review and reports. Settings are maintained separately.

## Notes
The dashboard displays live sales-today, purchases-today, stock-unit and low-stock cards when the database schema is available, with one-click Purchase Entry and Start Sales Billing actions. The WinForms pages use the configured SQL Server connection and expect the table/column names defined in scripts 001 and 002 plus the existing Medicine and Supplier schemas. Build the solution, run the scripts, then test with non-production sample data before live use. Invoice printing, returns, payment adjustments, role-based access, and a full pharmacy dashboard KPI query are not implemented by this commit.
