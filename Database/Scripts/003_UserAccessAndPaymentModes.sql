/* Run after 002_OperationalModules.sql. The app also creates AppUsers and adds PaymentMethod on sign-in. */
IF OBJECT_ID(N'dbo.AppUsers',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AppUsers(
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
IF OBJECT_ID(N'dbo.Invoices',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Invoices',N'PaymentMethod') IS NULL
 ALTER TABLE dbo.Invoices ADD PaymentMethod NVARCHAR(40) NULL;
GO