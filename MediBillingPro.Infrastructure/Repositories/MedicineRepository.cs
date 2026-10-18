using MediBillingPro.Application.Interfaces;
using MediBillingPro.Domain.Entities.Medicine_inventory;
using MediBillingPro.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MediBillingPro.Infrastructure.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly DatabaseConnectionFactory _factory;

        public MedicineRepository(DatabaseConnectionFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factory = factory;
        }

        public List<Medicine> GetAllWithSupplierId(int? supplierId)
        {
            return ExecuteSearch(null, supplierId);
        }

        public List<Medicine> GetAll()
        {
            return ExecuteSearch(null);
        }

        public List<Medicine> Search(string searchText, int supplierId=0)
        {
            return ExecuteSearch(searchText, supplierId);
        }

        private List<Medicine> ExecuteSearch(string searchText, int? supplierId = 0)
        {
            var medicines = new List<Medicine>();

            const string sql = @"
        SELECT
            MedicineId,
            MedicineName,
            GenericName,
            Manufacturer,
            Barcode,
            Category,
            Strength,
            Unit,
            ReorderLevel,
            IsPrescriptionRequired,
            IsActive,
            CreatedAt,
            ProductType,
            PurchasePrice,
            MarginPercent,
            GstPercent,
            SellingPriceExcludingGst,
            GstAmount,
            SellingPrice,
            RackCode,
            ShelfCode,
            TrayCode,
            BoxCode,
            StorageNotes,
            SupplierId
        FROM dbo.Medicines
        WHERE IsActive = 1
          AND (@SupplierId IS NULL OR @SupplierId = 0 OR SupplierId = @SupplierId)
          AND (
              @SearchText IS NULL
              OR MedicineName LIKE '%' + @SearchText + '%'
              OR GenericName LIKE '%' + @SearchText + '%'
              OR Manufacturer LIKE '%' + @SearchText + '%'
              OR Barcode LIKE '%' + @SearchText + '%'
              OR Category LIKE '%' + @SearchText + '%'
              OR Strength LIKE '%' + @SearchText + '%'
              OR RackCode LIKE '%' + @SearchText + '%'
              OR ShelfCode LIKE '%' + @SearchText + '%'
              OR TrayCode LIKE '%' + @SearchText + '%'
              OR BoxCode LIKE '%' + @SearchText + '%'
          )
        ORDER BY MedicineName;";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                string search = string.IsNullOrWhiteSpace(searchText)
                    ? null
                    : searchText.Trim();

                command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 200)
                    .Value = (object)search ?? DBNull.Value;

                command.Parameters.Add("@SupplierId", SqlDbType.Int)
                    .Value = (object)supplierId ?? DBNull.Value;

                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        medicines.Add(MapMedicine(reader));
                    }
                }
            }

            return medicines;
        }



        private Medicine MapMedicine(SqlDataReader reader)
        {
            return new Medicine
            {
                MedicineId = Convert.ToInt32(reader["MedicineId"]),

                MedicineName = ReadString(reader, "MedicineName"),
                GenericName = ReadString(reader, "GenericName"),
                Manufacturer = ReadString(reader, "Manufacturer"),
                Barcode = ReadString(reader, "Barcode"),
                Category = ReadString(reader, "Category"),
                Strength = ReadString(reader, "Strength"),
                Unit = ReadString(reader, "Unit"),

                ReorderLevel = reader["ReorderLevel"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["ReorderLevel"]),

                IsPrescriptionRequired =
                    reader["IsPrescriptionRequired"] != DBNull.Value &&
                    Convert.ToBoolean(reader["IsPrescriptionRequired"]),

                IsActive = reader["IsActive"] != DBNull.Value &&
                    Convert.ToBoolean(reader["IsActive"]),

                CreatedAt = reader["CreatedAt"] == DBNull.Value
                    ? DateTime.MinValue
                    : Convert.ToDateTime(reader["CreatedAt"]),

                RackCode = ReadString(reader, "RackCode"),
                ShelfCode = ReadString(reader, "ShelfCode"),
                TrayCode = ReadString(reader, "TrayCode"),
                BoxCode = ReadString(reader, "BoxCode"),
                StorageNotes = ReadString(reader, "StorageNotes"),


                ProductType = ReadString(reader, "ProductType"),

                PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),

                MarginPercent = Convert.ToDecimal(reader["MarginPercent"]),

                GstPercent = Convert.ToDecimal(reader["GstPercent"]),

                SellingPriceExcludingGst =
                Convert.ToDecimal(reader["SellingPriceExcludingGst"]),

                GstAmount = Convert.ToDecimal(reader["GstAmount"]),

                SellingPrice = Convert.ToDecimal(reader["SellingPrice"]),

                SupplierId = reader["SupplierId"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["SupplierId"]),
            };
        }

        private string ReadString(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? null
                : Convert.ToString(reader[columnName]);
        }

        public int Add(Medicine medicine)
        {
            if (medicine == null)
                throw new ArgumentNullException(nameof(medicine));

            const string sql = @"
                INSERT INTO dbo.Medicines
                (
                    MedicineName,
                    GenericName,
                    Manufacturer,
                    Barcode,
                    Category,
                    Strength,
                    Unit,
                    ReorderLevel,
                    IsPrescriptionRequired,
                    IsActive,
                    CreatedAt,
                    RackCode,
                    ShelfCode,
                    TrayCode,
                    BoxCode,
                    StorageNotes,
                    ProductType,
                    PurchasePrice,
                    MarginPercent,
                    GstPercent,
                    SellingPriceExcludingGst,
                    GstAmount,
                    SellingPrice,
                    SupplierId
                )
                VALUES
                (
                    @MedicineName,
                    @GenericName,
                    @Manufacturer,
                    @Barcode,
                    @Category,
                    @Strength,
                    @Unit,
                    @ReorderLevel,
                    @IsPrescriptionRequired,
                    1,
                    GETDATE(),
                    @RackCode,
                    @ShelfCode,
                    @TrayCode,
                    @BoxCode,
                    @StorageNotes,
                    @ProductType,
                    @PurchasePrice,
                    @MarginPercent,
                    @GstPercent,
                    @SellingPriceExcludingGst,
                    @GstAmount,
                    @SellingPrice,
                    @SupplierId
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddMedicineParameters(command, medicine);

                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public bool Update(Medicine medicine)
        {
            if (medicine == null)
                throw new ArgumentNullException(nameof(medicine));

            const string sql = @"
                UPDATE dbo.Medicines
                SET
                    MedicineName = @MedicineName,
                    GenericName = @GenericName,
                    Manufacturer = @Manufacturer,
                    Barcode = @Barcode,
                    Category = @Category,
                    Strength = @Strength,
                    Unit = @Unit,
                    ReorderLevel = @ReorderLevel,
                    IsPrescriptionRequired = @IsPrescriptionRequired,
                    RackCode = @RackCode,
                    ShelfCode = @ShelfCode,
                    TrayCode = @TrayCode,
                    BoxCode = @BoxCode,
                    StorageNotes = @StorageNotes,

                    ProductType = @ProductType,
                    PurchasePrice = @PurchasePrice,
                    MarginPercent = @MarginPercent,
                    GstPercent = @GstPercent,
                    SellingPriceExcludingGst = @SellingPriceExcludingGst,
                    GstAmount = @GstAmount,
                    SellingPrice = @SellingPrice,
                    SupplierId = @SupplierId
                WHERE MedicineId = @MedicineId
                  AND IsActive = 1;";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                AddMedicineParameters(command, medicine);

                command.Parameters.Add(
                    "@MedicineId", SqlDbType.Int).Value =
                    medicine.MedicineId;

                connection.Open();

                return command.ExecuteNonQuery() == 1;
            }
        }

        public bool Delete(int medicineId)
        {
            if (medicineId <= 0)
                return false;

            const string sql = @"
            UPDATE dbo.Medicines
            SET IsActive = 0
            WHERE MedicineId = @MedicineId
              AND IsActive = 1;";

            using (var connection = _factory.CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@MedicineId", SqlDbType.Int).Value = medicineId;

                connection.Open();

                return command.ExecuteNonQuery() == 1;
            }
        }

        private void AddMedicineParameters(
            SqlCommand command,
            Medicine medicine)
        {
            command.Parameters.Add(
                "@MedicineName", SqlDbType.NVarChar, 200).Value =
                (object)medicine.MedicineName ?? DBNull.Value;

            command.Parameters.Add(
                "@GenericName", SqlDbType.NVarChar, 200).Value =
                (object)medicine.GenericName ?? DBNull.Value;

            command.Parameters.Add(
                "@Manufacturer", SqlDbType.NVarChar, 200).Value =
                (object)medicine.Manufacturer ?? DBNull.Value;

            command.Parameters.Add(
                "@Barcode", SqlDbType.NVarChar, 100).Value =
                (object)medicine.Barcode ?? DBNull.Value;

            command.Parameters.Add(
                "@Category", SqlDbType.NVarChar, 100).Value =
                (object)medicine.Category ?? DBNull.Value;

            command.Parameters.Add(
                "@Strength", SqlDbType.NVarChar, 100).Value =
                (object)medicine.Strength ?? DBNull.Value;

            command.Parameters.Add(
                "@Unit", SqlDbType.NVarChar, 50).Value =
                string.IsNullOrWhiteSpace(medicine.Unit)
                    ? "Strip"
                    : medicine.Unit;

            command.Parameters.Add(
                "@ReorderLevel", SqlDbType.Int).Value =
                medicine.ReorderLevel;

            command.Parameters.Add(
               "@SupplierId", SqlDbType.Int).Value =
               medicine.SupplierId;

            command.Parameters.Add(
                "@IsPrescriptionRequired", SqlDbType.Bit).Value =
                medicine.IsPrescriptionRequired;

            command.Parameters.Add(
                "@RackCode", SqlDbType.NVarChar, 30).Value =
                (object)medicine.RackCode ?? DBNull.Value;

            command.Parameters.Add(
                "@ShelfCode", SqlDbType.NVarChar, 30).Value =
                (object)medicine.ShelfCode ?? DBNull.Value;

            command.Parameters.Add(
                "@TrayCode", SqlDbType.NVarChar, 30).Value =
                (object)medicine.TrayCode ?? DBNull.Value;

            command.Parameters.Add(
                "@BoxCode", SqlDbType.NVarChar, 30).Value =
                (object)medicine.BoxCode ?? DBNull.Value;

            command.Parameters.Add(
                "@StorageNotes", SqlDbType.NVarChar, 200).Value =
                (object)medicine.StorageNotes ?? DBNull.Value;


            command.Parameters.Add(
                "@ProductType", SqlDbType.NVarChar, 50).Value =
                string.IsNullOrWhiteSpace(medicine.ProductType)
                    ? "Other"
                    : medicine.ProductType;

            AddDecimalParameter(
                command, "@PurchasePrice", medicine.PurchasePrice, 18, 2);

            AddDecimalParameter(
                command, "@MarginPercent", medicine.MarginPercent, 5, 2);

            AddDecimalParameter(
                command, "@GstPercent", medicine.GstPercent, 5, 2);

            AddDecimalParameter(
                command, "@SellingPriceExcludingGst",
                medicine.SellingPriceExcludingGst, 18, 2);

            AddDecimalParameter(
                command, "@GstAmount", medicine.GstAmount, 18, 2);

            AddDecimalParameter(
                command, "@SellingPrice", medicine.SellingPrice, 18, 2);

        }


        private void AddDecimalParameter(
            SqlCommand command,
            string name,
            decimal value,
            byte precision,
            byte scale)
        {
            SqlParameter parameter =
                command.Parameters.Add(name, SqlDbType.Decimal);

            parameter.Precision = precision;
            parameter.Scale = scale;
            parameter.Value = value;
        }

    }
}
