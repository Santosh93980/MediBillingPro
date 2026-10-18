/*
  MediBilling Pro - Purchase Entry & Batch Management
  Run after dbo.Medicines and dbo.Suppliers exist.
*/
IF OBJECT_ID(N'dbo.MedicineBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineBatches
    (
        BatchId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicineBatches PRIMARY KEY,
        MedicineId INT NOT NULL,
        BatchNumber NVARCHAR(100) NOT NULL,
        ExpiryDate DATE NOT NULL,
        PurchasePrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_MedicineBatches_PurchasePrice DEFAULT (0),
        SellingPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_MedicineBatches_SellingPrice DEFAULT (0),
        GSTPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_MedicineBatches_GSTPercent DEFAULT (0),
        QuantityOnHand INT NOT NULL CONSTRAINT DF_MedicineBatches_QuantityOnHand DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MedicineBatches_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_MedicineBatches_Medicines FOREIGN KEY (MedicineId) REFERENCES dbo.Medicines(MedicineId),
        CONSTRAINT UQ_MedicineBatches_Medicine_Batch_Expiry UNIQUE (MedicineId, BatchNumber, ExpiryDate),
        CONSTRAINT CK_MedicineBatches_QuantityOnHand CHECK (QuantityOnHand >= 0)
    );
    CREATE INDEX IX_MedicineBatches_Medicine_Expiry ON dbo.MedicineBatches(MedicineId, ExpiryDate);
END;
GO
IF OBJECT_ID(N'dbo.Purchases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Purchases
    (
        PurchaseId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Purchases PRIMARY KEY,
        PurchaseNumber NVARCHAR(50) NOT NULL,
        SupplierId INT NOT NULL,
        InvoiceNumber NVARCHAR(100) NULL,
        PurchaseDate DATETIME2 NOT NULL CONSTRAINT DF_Purchases_PurchaseDate DEFAULT (SYSDATETIME()),
        SubTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchases_SubTotal DEFAULT (0),
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchases_DiscountAmount DEFAULT (0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchases_TaxAmount DEFAULT (0),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchases_TotalAmount DEFAULT (0),
        AmountPaid DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchases_AmountPaid DEFAULT (0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Purchases_Status DEFAULT ('Unpaid'),
        Notes NVARCHAR(1000) NULL,
        CreatedBy INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Purchases_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Purchases_PurchaseNumber UNIQUE (PurchaseNumber),
        CONSTRAINT FK_Purchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(SupplierId)
    );
    CREATE INDEX IX_Purchases_Supplier_Date ON dbo.Purchases(SupplierId, PurchaseDate DESC);
END;
GO
IF OBJECT_ID(N'dbo.PurchaseItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseItems
    (
        PurchaseItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseItems PRIMARY KEY,
        PurchaseId INT NOT NULL,
        MedicineId INT NOT NULL,
        BatchId INT NULL,
        BatchNumber NVARCHAR(100) NOT NULL,
        ExpiryDate DATE NOT NULL,
        Quantity INT NOT NULL,
        FreeQuantity INT NOT NULL CONSTRAINT DF_PurchaseItems_FreeQuantity DEFAULT (0),
        PurchasePrice DECIMAL(18,2) NOT NULL,
        SellingPrice DECIMAL(18,2) NOT NULL,
        GSTPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_PurchaseItems_GSTPercent DEFAULT (0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseItems_TaxAmount DEFAULT (0),
        LineTotal DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_PurchaseItems_Purchases FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchases(PurchaseId),
        CONSTRAINT FK_PurchaseItems_Medicines FOREIGN KEY (MedicineId) REFERENCES dbo.Medicines(MedicineId),
        CONSTRAINT FK_PurchaseItems_Batches FOREIGN KEY (BatchId) REFERENCES dbo.MedicineBatches(BatchId),
        CONSTRAINT CK_PurchaseItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_PurchaseItems_FreeQuantity CHECK (FreeQuantity >= 0)
    );
    CREATE INDEX IX_PurchaseItems_PurchaseId ON dbo.PurchaseItems(PurchaseId);
    CREATE INDEX IX_PurchaseItems_MedicineId ON dbo.PurchaseItems(MedicineId);
END;
GO