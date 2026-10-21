/* MediBilling Pro - first stage user access schema
   Safe to run against an existing database. AppUsers rows are preserved.
*/
IF OBJECT_ID(N'dbo.Roles',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        RoleName NVARCHAR(40) NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE,
        Description NVARCHAR(200) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT(1)
    );
END;
GO

MERGE dbo.Roles AS target
USING (VALUES
    (N'Super Admin', N'Full system administration and user management'),
    (N'Pharmacy Owner', N'Manages a pharmacy and its staff'),
    (N'Pharmacist', N'Pharmacy operations, inventory and counter billing'),
    (N'Cashier', N'Counter billing and customer payments'),
    (N'Inventory Manager', N'Medicines, suppliers and purchase stock'),
    (N'Admin', N'Legacy administrator role')
) AS source(RoleName, Description)
ON target.RoleName = source.RoleName
WHEN MATCHED THEN UPDATE SET Description = source.Description
WHEN NOT MATCHED THEN INSERT(RoleName, Description, IsActive) VALUES(source.RoleName, source.Description, 1);
GO

IF OBJECT_ID(N'dbo.AppUsers',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUsers
    (
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
        UserName NVARCHAR(80) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        PasswordHash NVARCHAR(100) NOT NULL,
        RoleName NVARCHAR(40) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_AppUsers_UserName UNIQUE(UserName)
    );
END;
GO

IF COL_LENGTH(N'dbo.AppUsers',N'FirstName') IS NULL ALTER TABLE dbo.AppUsers ADD FirstName NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'LastName') IS NULL ALTER TABLE dbo.AppUsers ADD LastName NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'MobileNumber') IS NULL ALTER TABLE dbo.AppUsers ADD MobileNumber NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'Email') IS NULL ALTER TABLE dbo.AppUsers ADD Email NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'Address') IS NULL ALTER TABLE dbo.AppUsers ADD [Address] NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyName') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyName NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyAddress') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyAddress NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyLicenseNumber') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyLicenseNumber NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyGSTIN') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyGSTIN NVARCHAR(20) NULL;
GO

/* Runtime initialization seeds demo users only when their usernames do not exist.
   Demo logins: admin / Admin@123, pharmacist / Pharma@123,
   cashier / Cashier@123, inventory / Stock@123.
   Change these demo passwords after the first login. */
