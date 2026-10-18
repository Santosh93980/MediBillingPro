/* MediBilling Pro operational modules: customers, billing, and settings. Run after 001_PurchaseBatchSchema.sql. */
IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.Customers(
  CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
  CustomerName NVARCHAR(150) NOT NULL,
  MobileNumber NVARCHAR(20) NULL,
  Email NVARCHAR(150) NULL,
  Address NVARCHAR(500) NULL,
  GSTIN NVARCHAR(20) NULL,
  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT(SYSDATETIME())
 );
 CREATE INDEX IX_Customers_Name_Mobile ON dbo.Customers(CustomerName,MobileNumber);
END;
GO
IF OBJECT_ID(N'dbo.Invoices',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.Invoices(
  InvoiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
  InvoiceNumber NVARCHAR(50) NOT NULL,
  CustomerId INT NULL,
  InvoiceDate DATETIME2 NOT NULL CONSTRAINT DF_Invoices_Date DEFAULT(SYSDATETIME()),
  SubTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_SubTotal DEFAULT(0),
  DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Discount DEFAULT(0),
  TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Tax DEFAULT(0),
  RoundOff DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_RoundOff DEFAULT(0),
  TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Total DEFAULT(0),
  AmountPaid DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Paid DEFAULT(0),
  BalanceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Balance DEFAULT(0),
  PaymentStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Invoices_PaymentStatus DEFAULT('Unpaid'),
  InvoiceStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT('Completed'),
  PrescriptionReference NVARCHAR(100) NULL,
  Notes NVARCHAR(1000) NULL,
  CreatedBy INT NULL,
  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT(SYSDATETIME()),
  CONSTRAINT UQ_Invoices_Number UNIQUE(InvoiceNumber),
  CONSTRAINT FK_Invoices_Customers FOREIGN KEY(CustomerId) REFERENCES dbo.Customers(CustomerId)
 );
 CREATE INDEX IX_Invoices_Date ON dbo.Invoices(InvoiceDate DESC);
END;
GO
IF OBJECT_ID(N'dbo.InvoiceItems',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.InvoiceItems(
  InvoiceItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceItems PRIMARY KEY,
  InvoiceId INT NOT NULL,
  MedicineId INT NOT NULL,
  BatchId INT NOT NULL,
  Quantity INT NOT NULL,
  UnitPrice DECIMAL(18,2) NOT NULL,
  DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_Discount DEFAULT(0),
  GSTPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_InvoiceItems_GST DEFAULT(0),
  TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_Tax DEFAULT(0),
  LineTotal DECIMAL(18,2) NOT NULL,
  CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY(InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
  CONSTRAINT FK_InvoiceItems_Medicines FOREIGN KEY(MedicineId) REFERENCES dbo.Medicines(MedicineId),
  CONSTRAINT FK_InvoiceItems_Batches FOREIGN KEY(BatchId) REFERENCES dbo.MedicineBatches(BatchId),
  CONSTRAINT CK_InvoiceItems_Quantity CHECK(Quantity>0)
 );
 CREATE INDEX IX_InvoiceItems_InvoiceId ON dbo.InvoiceItems(InvoiceId);
END;
GO
IF OBJECT_ID(N'dbo.AppSettings',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AppSettings(
  SettingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY,
  SettingKey NVARCHAR(100) NOT NULL,
  SettingValue NVARCHAR(1000) NULL,
  Description NVARCHAR(500) NULL,
  UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_AppSettings_UpdatedAt DEFAULT(SYSDATETIME()),
  CONSTRAINT UQ_AppSettings_Key UNIQUE(SettingKey)
 );
END;
GO