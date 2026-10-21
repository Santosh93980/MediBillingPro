using MediBillingPro.Application.Interfaces;
using MediBillingPro.Domain.Entities.Contacts;
using MediBillingPro.Domain.Entities.Purchasing;
using MediBillingPro.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace MediBillingPro.Infrastructure.Repositories
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly DatabaseConnectionFactory _factory;

        public PurchaseRepository(DatabaseConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public int Create(Purchase purchase, IList<PurchaseItem> items)
        {
            if (purchase == null) throw new ArgumentNullException(nameof(purchase));
            if (items == null || items.Count == 0) throw new ArgumentException("At least one item is required.");

            using (SqlConnection connection = _factory.CreateConnection())
            {
                connection.Open();
                AuditContext.Apply(connection, "Purchases");
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        EnsureActive(connection, transaction, "Suppliers", purchase.SupplierId, "supplier");

                        const string headerSql = @"
INSERT INTO dbo.Purchases
(PurchaseNumber, SupplierId, InvoiceNumber, PurchaseDate, SubTotal, DiscountAmount,
 TaxAmount, TotalAmount, AmountPaid, Status, Notes, CreatedBy, CreatedAt)
VALUES
(@PurchaseNumber, @SupplierId, @InvoiceNumber, @PurchaseDate, @SubTotal, @DiscountAmount,
 @TaxAmount, @TotalAmount, @AmountPaid, @Status, @Notes, @CreatedBy, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int purchaseId;
                        using (SqlCommand command = new SqlCommand(headerSql, connection, transaction))
                        {
                            command.Parameters.Add("@PurchaseNumber", SqlDbType.NVarChar, 50).Value = purchase.PurchaseNumber;
                            command.Parameters.Add("@SupplierId", SqlDbType.Int).Value = purchase.SupplierId;
                            command.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 100).Value = (object)purchase.InvoiceNumber ?? DBNull.Value;
                            command.Parameters.Add("@PurchaseDate", SqlDbType.DateTime2).Value = purchase.PurchaseDate;
                            AddMoney(command, "@SubTotal", purchase.SubTotal);
                            AddMoney(command, "@DiscountAmount", purchase.DiscountAmount);
                            AddMoney(command, "@TaxAmount", purchase.TaxAmount);
                            AddMoney(command, "@TotalAmount", purchase.TotalAmount);
                            AddMoney(command, "@AmountPaid", purchase.AmountPaid);
                            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = purchase.Status;
                            command.Parameters.Add("@Notes", SqlDbType.NVarChar, 1000).Value = (object)purchase.Notes ?? DBNull.Value;
                            command.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = (object)purchase.CreatedBy ?? DBNull.Value;
                            purchaseId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        foreach (PurchaseItem item in items)
                        {
                            EnsureActive(connection, transaction, "Medicines", item.MedicineId, "medicine");
                            int batchId = SaveBatch(connection, transaction, item);
                            item.PurchaseId = purchaseId;
                            item.BatchId = batchId;

                            const string itemSql = @"
INSERT INTO dbo.PurchaseItems
(PurchaseId, MedicineId, BatchId, BatchNumber, ExpiryDate, Quantity, FreeQuantity,
 PurchasePrice, SellingPrice, GSTPercent, TaxAmount, LineTotal)
VALUES
(@PurchaseId, @MedicineId, @BatchId, @BatchNumber, @ExpiryDate, @Quantity, @FreeQuantity,
 @PurchasePrice, @SellingPrice, @GSTPercent, @TaxAmount, @LineTotal);";

                            using (SqlCommand command = new SqlCommand(itemSql, connection, transaction))
                            {
                                command.Parameters.Add("@PurchaseId", SqlDbType.Int).Value = purchaseId;
                                command.Parameters.Add("@MedicineId", SqlDbType.Int).Value = item.MedicineId;
                                command.Parameters.Add("@BatchId", SqlDbType.Int).Value = batchId;
                                command.Parameters.Add("@BatchNumber", SqlDbType.NVarChar, 100).Value = item.BatchNumber.Trim();
                                command.Parameters.Add("@ExpiryDate", SqlDbType.Date).Value = item.ExpiryDate.Date;
                                command.Parameters.Add("@Quantity", SqlDbType.Int).Value = item.Quantity;
                                command.Parameters.Add("@FreeQuantity", SqlDbType.Int).Value = item.FreeQuantity;
                                AddMoney(command, "@PurchasePrice", item.PurchasePrice);
                                AddMoney(command, "@SellingPrice", item.SellingPrice);
                                AddMoney(command, "@GSTPercent", item.GSTPercent);
                                AddMoney(command, "@TaxAmount", item.TaxAmount);
                                AddMoney(command, "@LineTotal", item.LineTotal);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return purchaseId;
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public List<Purchase> GetAll()
        {
            var result = new List<Purchase>();
            const string sql = @"
SELECT p.PurchaseId, p.PurchaseNumber, p.SupplierId, p.InvoiceNumber, p.PurchaseDate,
       p.SubTotal, p.DiscountAmount, p.TaxAmount, p.TotalAmount, p.AmountPaid,
       p.Status, p.Notes, p.CreatedBy, p.CreatedAt, s.SupplierName
FROM dbo.Purchases p
INNER JOIN dbo.Suppliers s ON s.SupplierId = p.SupplierId
ORDER BY p.PurchaseDate DESC, p.PurchaseId DESC;";

            using (SqlConnection connection = _factory.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                AuditContext.Apply(connection, "Purchases");
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Purchase
                        {
                            PurchaseId = Convert.ToInt32(reader["PurchaseId"]),
                            PurchaseNumber = Convert.ToString(reader["PurchaseNumber"]),
                            SupplierId = Convert.ToInt32(reader["SupplierId"]),
                            InvoiceNumber = reader["InvoiceNumber"] == DBNull.Value ? null : Convert.ToString(reader["InvoiceNumber"]),
                            PurchaseDate = Convert.ToDateTime(reader["PurchaseDate"]),
                            SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                            DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                            TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            AmountPaid = Convert.ToDecimal(reader["AmountPaid"]),
                            Status = Convert.ToString(reader["Status"]),
                            Notes = reader["Notes"] == DBNull.Value ? null : Convert.ToString(reader["Notes"]),
                            CreatedBy = reader["CreatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CreatedBy"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            Supplier = new Supplier
                            {
                                SupplierId = Convert.ToInt32(reader["SupplierId"]),
                                SupplierName = Convert.ToString(reader["SupplierName"])
                            }
                        });
                    }
                }
            }
            return result;
        }

        private static void EnsureActive(SqlConnection connection, SqlTransaction transaction, string table, int id, string label)
        {
            string sql = "SELECT COUNT(1) FROM dbo." + table + " WITH (UPDLOCK, HOLDLOCK) WHERE " +
                (table == "Suppliers" ? "SupplierId" : "MedicineId") + " = @Id AND IsActive = 1;";
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                    throw new InvalidOperationException("The selected " + label + " does not exist or is inactive.");
            }
        }

        private static int SaveBatch(SqlConnection connection, SqlTransaction transaction, PurchaseItem item)
        {
            const string sql = @"
DECLARE @BatchId INT;
SELECT @BatchId = BatchId
FROM dbo.MedicineBatches WITH (UPDLOCK, HOLDLOCK)
WHERE MedicineId = @MedicineId AND BatchNumber = @BatchNumber AND ExpiryDate = @ExpiryDate;

IF @BatchId IS NULL
BEGIN
    INSERT INTO dbo.MedicineBatches
        (MedicineId, BatchNumber, ExpiryDate, PurchasePrice, SellingPrice, GSTPercent, QuantityOnHand, CreatedAt)
    VALUES
        (@MedicineId, @BatchNumber, @ExpiryDate, @PurchasePrice, @SellingPrice, @GSTPercent, @QuantityToAdd, SYSUTCDATETIME());
    SET @BatchId = CAST(SCOPE_IDENTITY() AS INT);
END
ELSE
BEGIN
    UPDATE dbo.MedicineBatches
    SET QuantityOnHand = QuantityOnHand + @QuantityToAdd,
        PurchasePrice = @PurchasePrice, SellingPrice = @SellingPrice, GSTPercent = @GSTPercent
    WHERE BatchId = @BatchId;
END;
SELECT @BatchId;";

            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@MedicineId", SqlDbType.Int).Value = item.MedicineId;
                command.Parameters.Add("@BatchNumber", SqlDbType.NVarChar, 100).Value = item.BatchNumber.Trim();
                command.Parameters.Add("@ExpiryDate", SqlDbType.Date).Value = item.ExpiryDate.Date;
                AddMoney(command, "@PurchasePrice", item.PurchasePrice);
                AddMoney(command, "@SellingPrice", item.SellingPrice);
                AddMoney(command, "@GSTPercent", item.GSTPercent);
                command.Parameters.Add("@QuantityToAdd", SqlDbType.Int).Value = checked(item.Quantity + item.FreeQuantity);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void AddMoney(SqlCommand command, string name, decimal value)
        {
            SqlParameter parameter = command.Parameters.Add(name, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = value;
        }
    }
}