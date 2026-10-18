# MediBilling Pro Dashboard

## Added
- `Desktop/Forms/DashboardForm.cs`: modern WinForms dashboard with a collapsible left sidebar, overview cards, and quick-action cards.
- Navigation labels: Dashboard, Medicine Inventory, Suppliers, Supplier Medicines, Purchase Entry, Sales/Billing, Customers, Stock & Batches, Reports, and Settings.

## Add it to Visual Studio 2019
1. Pull the latest `main` branch.
2. In the existing **MediBillingPro.Desktop** WinForms project, add `Desktop/Forms/DashboardForm.cs` to the project (or copy it into the project's Forms folder and use **Add > Existing Item**).
3. Build the solution.
4. In the desktop project's `Program.cs`, change the startup form to `Application.Run(new MediBillingPro.Desktop.Forms.DashboardForm());` while keeping the existing initialization code.
5. Make sure each existing screen is included in the desktop assembly. Navigation finds WinForms classes by class name and opens parameterless forms. Screens requiring constructor arguments should be wired to their existing service/form factory.

## Important implementation notes
- Sidebar collapse/expand and dashboard layout are implemented.
- Existing screen names are looked up dynamically, so missing screens show a friendly message rather than crashing.
- Purchase Entry currently has backend/database services only; the Purchase Entry WinForms screen has not yet been added.
- Sales/Billing, Customers, Stock & Batches, Reports and Settings are navigation placeholders unless those forms already exist in your local project.
- Overview cards are visual placeholders, not live database counts. Live metrics need service/query methods.
- The repository currently has a root item named `MediBillingPro.Desktop` that prevents placing files under that exact path through GitHub's file API, so this source is committed under `Desktop/Forms` for you to include in the existing Visual Studio project.
