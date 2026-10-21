/*
  MediBilling Pro - audit trail and record metadata.
  Run after scripts 001-004. Existing records are preserved.
  Application code should set SESSION_CONTEXT keys UserId, UserName and ScreenName
  on each opened SQL connection; triggers record database changes automatically.
*/
IF OBJECT_ID(N'dbo.AuditLogs',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AuditLogs(
  AuditLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
  ActionedBy NVARCHAR(150) NOT NULL CONSTRAINT DF_AuditLogs_ActionedBy DEFAULT(N'SYSTEM'),
  ActionedByUserId INT NULL,
  ActionedOn DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_ActionedOn DEFAULT(SYSUTCDATETIME()),
  ScreenName NVARCHAR(150) NOT NULL CONSTRAINT DF_AuditLogs_ScreenName DEFAULT(N'Unknown'),
  Activity NVARCHAR(500) NOT NULL,
  ActivityType NVARCHAR(40) NOT NULL,
  Details NVARCHAR(MAX) NULL,
  Category NVARCHAR(30) NOT NULL CONSTRAINT DF_AuditLogs_Category DEFAULT(N'Operations')
 );
 CREATE INDEX IX_AuditLogs_Category_ActionedOn ON dbo.AuditLogs(Category,ActionedOn DESC);
 CREATE INDEX IX_AuditLogs_ActionedBy ON dbo.AuditLogs(ActionedBy);
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLogs') AND name=N'IX_AuditLogs_Category_ActionedOn') CREATE INDEX IX_AuditLogs_Category_ActionedOn ON dbo.AuditLogs(Category,ActionedOn DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLogs') AND name=N'IX_AuditLogs_ActionedBy') CREATE INDEX IX_AuditLogs_ActionedBy ON dbo.AuditLogs(ActionedBy);
GO

/* Apply the standard four metadata fields to existing business tables. */
DECLARE @tables TABLE(TableName SYSNAME);
INSERT INTO @tables(TableName) VALUES
(N'AppUsers'),(N'Users'),(N'Roles'),(N'Medicines'),(N'Suppliers'),(N'MedicineBatches'),
(N'Customers'),(N'Invoices'),(N'InvoiceItems'),(N'Purchases'),(N'PurchaseItems'),(N'AppSettings');
DECLARE @t SYSNAME, @obj INT, @pk SYSNAME, @sql NVARCHAR(MAX), @trigger SYSNAME, @createdByType INT, @createdBySet NVARCHAR(300);
DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM @tables;
OPEN table_cursor; FETCH NEXT FROM table_cursor INTO @t;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @obj=OBJECT_ID(N'dbo.'+@t,N'U');
 IF @obj IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.'+@t,N'CreatedBy') IS NULL
   EXEC(N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD CreatedBy NVARCHAR(150) NULL;');
  IF COL_LENGTH(N'dbo.'+@t,N'CreatedByName') IS NULL
   EXEC(N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD CreatedByName NVARCHAR(150) NULL;');
  IF COL_LENGTH(N'dbo.'+@t,N'CreatedDate') IS NULL
   EXEC(N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD CreatedDate DATETIME2 NULL;');
  IF COL_LENGTH(N'dbo.'+@t,N'UpdatedBy') IS NULL
   EXEC(N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD UpdatedBy NVARCHAR(150) NULL;');
  IF COL_LENGTH(N'dbo.'+@t,N'UpdatedDate') IS NULL
   EXEC(N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD UpdatedDate DATETIME2 NULL;');

  SELECT TOP(1) @pk=c.name
  FROM sys.indexes ix
  JOIN sys.index_columns ic ON ic.object_id=ix.object_id AND ic.index_id=ix.index_id AND ic.key_ordinal=1
  JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
  WHERE ix.object_id=@obj AND ix.is_primary_key=1
  ORDER BY ic.key_ordinal;

  IF @pk IS NOT NULL
  BEGIN
   SELECT @createdByType=system_type_id FROM sys.columns WHERE object_id=@obj AND name=N'CreatedBy';
   SET @createdBySet=CASE WHEN @createdByType IN (48,52,56,127) THEN N'' ELSE N'CreatedBy=CASE WHEN EXISTS(SELECT 1 FROM deleted) THEN target.CreatedBy ELSE COALESCE(target.CreatedBy,@uname) END,' END;
   SET @trigger=N'trg_Audit_'+@t;
   SET @sql=N'CREATE OR ALTER TRIGGER dbo.'+QUOTENAME(@trigger)+N' ON dbo.'+QUOTENAME(@t)+N'
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @uid INT=TRY_CONVERT(INT,SESSION_CONTEXT(N''UserId''));
 DECLARE @uname NVARCHAR(150)=COALESCE(CONVERT(NVARCHAR(150),SESSION_CONTEXT(N''UserName'')),N''SYSTEM'');
 DECLARE @screen NVARCHAR(150)=COALESCE(CONVERT(NVARCHAR(150),SESSION_CONTEXT(N''ScreenName'')),N''Unattributed database operation'');
 DECLARE @now DATETIME2=SYSUTCDATETIME();
 UPDATE target SET
  CreatedByName=CASE WHEN EXISTS(SELECT 1 FROM deleted) THEN target.CreatedByName ELSE COALESCE(target.CreatedByName,@uname) END,
  '+@createdBySet+N'
  CreatedDate=CASE WHEN EXISTS(SELECT 1 FROM deleted) THEN target.CreatedDate ELSE COALESCE(target.CreatedDate,@now) END,
  UpdatedBy=@uname, UpdatedDate=@now
 FROM dbo.'+QUOTENAME(@t)+N' target
 JOIN inserted i ON target.'+QUOTENAME(@pk)+N'=i.'+QUOTENAME(@pk)+N';
 INSERT dbo.AuditLogs(ActionedBy,ActionedByUserId,ActionedOn,ScreenName,Activity,ActivityType,Details,Category)
 SELECT @uname,@uid,@now,@screen,
  N'''+REPLACE(@t,'''','''''')+N' ''+CASE WHEN EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted) THEN N''updated''
    WHEN EXISTS(SELECT 1 FROM inserted) THEN N''created'' ELSE N''deleted'' END,
  CASE WHEN EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted) THEN N''Update''
    WHEN EXISTS(SELECT 1 FROM inserted) THEN N''Create'' ELSE N''Delete'' END,
  CASE WHEN EXISTS(SELECT 1 FROM inserted) THEN (SELECT * FROM inserted FOR JSON PATH) ELSE (SELECT * FROM deleted FOR JSON PATH) END,
  CASE WHEN N'''+@t+N''' IN (N''AppUsers'',N''Users'',N''Roles'',N''AppSettings'') THEN N''Administration'' ELSE N''Operations'' END;
END;';
   EXEC sys.sp_executesql @sql;
  END
 END
 SET @pk=NULL;
 FETCH NEXT FROM table_cursor INTO @t;
END
CLOSE table_cursor; DEALLOCATE table_cursor;
GO

/* Backfill creation metadata for existing records without inventing a user. */
DECLARE @backfill TABLE(TableName SYSNAME);
INSERT INTO @backfill VALUES(N'AppUsers'),(N'Users'),(N'Roles'),(N'Medicines'),(N'Suppliers'),(N'MedicineBatches'),(N'Customers'),(N'Invoices'),(N'InvoiceItems'),(N'Purchases'),(N'PurchaseItems'),(N'AppSettings');
DECLARE @bt SYSNAME, @bsql NVARCHAR(MAX);
DECLARE backfill_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM @backfill;
OPEN backfill_cursor; FETCH NEXT FROM backfill_cursor INTO @bt;
WHILE @@FETCH_STATUS=0
BEGIN
 IF OBJECT_ID(N'dbo.'+@bt,N'U') IS NOT NULL
 BEGIN
  SET @bsql=N'UPDATE dbo.'+QUOTENAME(@bt)+N' SET CreatedDate=COALESCE(CreatedDate,CreatedAt) WHERE CreatedDate IS NULL AND COL_LENGTH(N''dbo.'+@bt+N''',N''CreatedAt'') IS NOT NULL;';
  /* Avoid dynamic references to a CreatedAt column when a table does not have one. */
  IF COL_LENGTH(N'dbo.'+@bt,N'CreatedAt') IS NOT NULL EXEC sys.sp_executesql @bsql;
  SET @bsql=N'UPDATE dbo.'+QUOTENAME(@bt)+N' SET CreatedDate=COALESCE(CreatedDate,SYSUTCDATETIME()) WHERE CreatedDate IS NULL;';
  EXEC sys.sp_executesql @bsql;
 END
 FETCH NEXT FROM backfill_cursor INTO @bt;
END
CLOSE backfill_cursor; DEALLOCATE backfill_cursor;
GO
