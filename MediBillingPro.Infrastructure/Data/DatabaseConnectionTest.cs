using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace MediBillingPro.Infrastructure.Data
{
    public class DatabaseConnectionTest
    {
        private readonly DatabaseConnectionFactory _factory;

        public DatabaseConnectionTest(
            DatabaseConnectionFactory factory)
        {
            _factory = factory;
        }

        public string TestConnection()
        {
            using (SqlConnection connection =
                   _factory.CreateConnection())
            {
                connection.Open();

                using (SqlCommand command =
                       new SqlCommand("SELECT DB_NAME()", connection))
                {
                    string databaseName =
                        Convert.ToString(command.ExecuteScalar());

                    return "Connected successfully to: " + databaseName;
                }
            }
        }
    }
}
