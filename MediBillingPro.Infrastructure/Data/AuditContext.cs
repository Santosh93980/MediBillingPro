using System;
using System.Data;
using System.Data.SqlClient;
using MediBillingPro.Domain.Security;

namespace MediBillingPro.Infrastructure.Data
{
    public static class AuditContext
    {
        public static void Apply(SqlConnection connection, string screenName)
        {
            if (connection == null || connection.State != ConnectionState.Open) return;
            using (var command = new SqlCommand(@"
EXEC sys.sp_set_session_context @key=N'UserId', @value=@uid;
EXEC sys.sp_set_session_context @key=N'UserName', @value=@uname;
EXEC sys.sp_set_session_context @key=N'ScreenName', @value=@screen;", connection))
            {
                command.Parameters.Add("@uid", SqlDbType.Int).Value = (object)CurrentUserContext.UserId ?? DBNull.Value;
                command.Parameters.Add("@uname", SqlDbType.NVarChar, 150).Value = CurrentUserContext.UserName;
                command.Parameters.Add("@screen", SqlDbType.NVarChar, 150).Value = screenName ?? "Unknown";
                command.ExecuteNonQuery();
            }
        }
    }
}
