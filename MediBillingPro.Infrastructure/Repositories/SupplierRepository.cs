using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MediBillingPro.Application.Interfaces;
using MediBillingPro.Domain.Entities;
using MediBillingPro.Domain.Entities.Contacts;
using MediBillingPro.Infrastructure.Data;

namespace MediBillingPro.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly DatabaseConnectionFactory _factory;

        public SupplierRepository(DatabaseConnectionFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factory = factory;
        }


        public List<Supplier> GetAll()
        {
            const string sql = @"
                SELECT
                    s.SupplierId,
                    s.SupplierName,
                    s.ContactPerson,
                    s.MobileNumber,
                    s.Email,
                    s.GSTIN,
                    s.LicenseNumber,
                    s.Address,
                    s.IsActive,
                    s.CreatedAt,
                    COUNT(m.MedicineId) AS MedicineCount
                FROM dbo.Suppliers s
                LEFT JOIN dbo.Medicines m
                    ON m.SupplierId = s.SupplierId
                    AND m.IsActive = 1
                WHERE s.IsActive = 1
                GROUP BY
                    s.SupplierId,
                    s.SupplierName,
                    s.ContactPerson,
                    s.MobileNumber,
                    s.Email,
                    s.GSTIN,
                    s.LicenseNumber,
                    s.Address,
                    s.IsActive,
                    s.CreatedAt
                ORDER BY s.SupplierName;";

            return QuerySuppliers(sql, null);
        }


        public List<Supplier> Search(string searchText)
        {
            const string sql = @"
            SELECT SupplierId, SupplierName, ContactPerson, MobileNumber, Email,
                   Address, GSTIN, LicenseNumber, IsActive, CreatedAt
            FROM dbo.Suppliers
            WHERE IsActive = 1
              AND (@Search = ''
                   OR SupplierName LIKE '%' + @Search + '%'
                   OR MobileNumber LIKE '%' + @Search + '%')
            ORDER BY SupplierName;";

            return QuerySuppliers(sql, cmd =>
                cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 150).Value =
                    (object)(searchText ?? string.Empty).Trim());
        }

        public int Add(Supplier supplier)
        {
            const string sql = @"
            INSERT INTO dbo.Suppliers
            (SupplierName, ContactPerson, MobileNumber, Email, Address, GSTIN,
             LicenseNumber, IsActive, CreatedAt)
            VALUES
            (@SupplierName, @ContactPerson, @MobileNumber, @Email, @Address, @GSTIN,
             @LicenseNumber, 1, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int);";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddSupplierParameters(command, supplier);
                connection.Open();
                AuditContext.Apply(connection, "Suppliers");
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public bool Update(Supplier supplier)
        {
            const string sql = @"
                UPDATE dbo.Suppliers
                SET SupplierName = @SupplierName,
                    ContactPerson = @ContactPerson,
                    MobileNumber = @MobileNumber,
                    Email = @Email,
                    Address = @Address,
                    GSTIN = @GSTIN,
                    LicenseNumber = @LicenseNumber
                WHERE SupplierId = @SupplierId AND IsActive = 1;";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddSupplierParameters(command, supplier);
                command.Parameters.Add("@SupplierId", SqlDbType.Int).Value = supplier.SupplierId;
                connection.Open();
                AuditContext.Apply(connection, "Suppliers");
                return command.ExecuteNonQuery() > 0;
            }
        }

        // Soft delete protects historical purchase/invoice records.

        public bool Deactivate(int supplierId)
        {
            const string sql = @"
                SET NOCOUNT ON;

                BEGIN TRY
                    BEGIN TRANSACTION;

                    -- Deactivate the supplier
                    UPDATE dbo.Suppliers
                    SET IsActive = 0
                    WHERE SupplierId = @SupplierId
                      AND IsActive = 1;

                    DECLARE @SupplierRows INT = @@ROWCOUNT;

                    IF @SupplierRows = 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(0 AS INT);
                        RETURN;
                    END;

                    -- Deactivate all medicines linked to this supplier
                    UPDATE dbo.Medicines
                    SET IsActive = 0
                    WHERE SupplierId = @SupplierId
                      AND IsActive = 1;

                    COMMIT TRANSACTION;

                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRANSACTION;

                    THROW;
                END CATCH;";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@SupplierId", SqlDbType.Int)
                    .Value = supplierId;

                connection.Open();
                AuditContext.Apply(connection, "Suppliers");

                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }


        private List<Supplier> QuerySuppliers(string sql, Action<SqlCommand> addParameters)
        {
            var suppliers = new List<Supplier>();

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                addParameters?.Invoke(command);
                connection.Open();
                AuditContext.Apply(connection, "Suppliers");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        suppliers.Add(new Supplier
                        {
                            SupplierId = Convert.ToInt32(reader["SupplierId"]),
                            SupplierName = Convert.ToString(reader["SupplierName"]),
                            ContactPerson = DbString(reader, "ContactPerson"),
                            MobileNumber = DbString(reader, "MobileNumber"),
                            Email = DbString(reader, "Email"),
                            Address = DbString(reader, "Address"),
                            GSTIN = DbString(reader, "GSTIN"),
                            LicenseNumber = DbString(reader, "LicenseNumber"),
                            IsActive = reader["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(reader["IsActive"]),

                            CreatedAt = reader["CreatedAt"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["CreatedAt"]),

                            MedicineCount = reader["MedicineCount"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["MedicineCount"])
                        });
                    }
                }
            }

            return suppliers;
        }

        private static string DbString(SqlDataReader reader, string column)
        {
            return reader[column] == DBNull.Value ? null : Convert.ToString(reader[column]);
        }

        private static void AddSupplierParameters(SqlCommand command, Supplier supplier)
        {
            command.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 150).Value = supplier.SupplierName.Trim();
            command.Parameters.Add("@ContactPerson", SqlDbType.NVarChar, 100).Value = DbValue(supplier.ContactPerson);
            command.Parameters.Add("@MobileNumber", SqlDbType.NVarChar, 20).Value = DbValue(supplier.MobileNumber);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = DbValue(supplier.Email);
            command.Parameters.Add("@Address", SqlDbType.NVarChar, 500).Value = DbValue(supplier.Address);
            command.Parameters.Add("@GSTIN", SqlDbType.NVarChar, 20).Value = DbValue(supplier.GSTIN);
            command.Parameters.Add("@LicenseNumber", SqlDbType.NVarChar, 100).Value = DbValue(supplier.LicenseNumber);
            //command.Parameters.Add("@PharmacyId", SqlDbType.Int).Value =
            //    supplier.PharmacyId.HasValue ? (object)supplier.PharmacyId.Value : DBNull.Value;
        }

        private static object DbValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }
    }
}
