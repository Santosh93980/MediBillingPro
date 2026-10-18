# Role-based pharmacy counter workflow

## Demo sign-in accounts
The first sign-in creates the `dbo.AppUsers` table when it does not exist and seeds these accounts without overwriting existing passwords or roles:

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@123` | Admin |
| `pharmacist` | `Pharma@123` | Pharmacist |
| `cashier` | `Cashier@123` | Cashier |
| `inventory` | `Stock@123` | Inventory Manager |

Change demo passwords and disable test accounts before using real pharmacy data. Passwords are stored as SHA-256 hashes in this demo implementation; for production, upgrade to a salted password KDF (PBKDF2/Argon2/bcrypt), add lockout/audit logging, and enforce authorization in every data/service operation.

## Roles
- **Admin:** all current modules, plus User Management (create accounts and enable/disable accounts).
- **Pharmacist:** Counter Billing, Customers and Stock & Batches.
- **Cashier:** Counter Billing and Customers.
- **Inventory Manager:** Medicine Inventory, Suppliers, Purchases, Stock & Batches and Reports.

## Counter Billing
- Scanner-ready barcode input (most USB scanners act like a keyboard and send Enter after the barcode).
- Searches medicine name, generic name, manufacturer/brand, barcode and batch number.
- Only active, non-expired batches with stock are shown; sale rechecks stock and expiry in the SQL transaction.
- Cart quantity validation, GST total, amount received, balance due and cash change calculation.
- Payment method is recorded on the invoice (Cash, UPI, Card, Bank transfer or Mixed).

## UPI limitations / configuration
The current UPI action prepares a UPI intent string for the bill amount and shows the configured VPA placeholder `YOUR-PHARMACY-UPI-ID`. Replace it with the pharmacy's verified UPI ID before testing. This is **not** a payment gateway integration and it cannot verify a UPI payment automatically. Staff must verify payment in the merchant app and enter the amount actually received. A real gateway needs a provider account, merchant credentials, server-side order creation, signed webhook/payment verification and a reconciliation flow; never place secret API keys in the desktop client.

## Database
The app creates `dbo.AppUsers` automatically at sign-in and adds `Invoices.PaymentMethod` when `dbo.Invoices` already exists. You can also run `Database/Scripts/003_UserAccessAndPaymentModes.sql` after the existing operational schema scripts. Ensure `001_PurchaseBatchSchema.sql` and `002_OperationalModules.sql` have been run before testing billing.

## Visual Studio
Pull the latest `main` branch (Git > Pull in Visual Studio) or close the solution and clone/open the repository again. Build the solution, verify the `MediBillingProDB` connection string in App.config, then run the Desktop project. No successful local build or live SQL transaction has been verified by this GitHub update.
