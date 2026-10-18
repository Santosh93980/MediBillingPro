using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace MediBillingPro.Infrastructure.Data
{
    public class DatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public DatabaseConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "Database connection string is missing.",
                    nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
