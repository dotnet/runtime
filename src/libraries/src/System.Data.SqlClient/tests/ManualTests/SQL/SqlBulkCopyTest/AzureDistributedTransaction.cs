// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Transactions;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class AzureDistributedTransaction
    {
        private static readonly string s_connectionString = DataTestUtility.TcpConnStr;
        private static readonly string s_tableName        = "Azure_" + Guid.NewGuid().ToString().Replace('-', '_');
        private static readonly string s_createTableCmd   = $"CREATE TABLE {s_tableName} (NAME NVARCHAR(40), AGE INT)";
        private static readonly string s_sqlBulkCopyCmd   = "SELECT * FROM(VALUES ('Fuller', 33), ('Davon', 49)) AS q (FirstName, Age)";
        private static readonly int    s_commandTimeout   = 30;

        public static void Test()
        {
            try
            {
#if DEBUG
               Console.WriteLine($"Creating Table {s_tableName}");
#endif
                // Setup Azure Table
                Helpers.ExecuteNonQueryAzure(s_connectionString, s_createTableCmd, s_commandTimeout);
                using (var txScope = new TransactionScope())
                {
                    BulkCopy(s_connectionString);
                    txScope.Complete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Assert.Null(ex);
            }
            finally
            {
#if DEBUG
                Console.WriteLine($"Dropping Table {s_tableName}");
#endif
                // Drop Azure Table
                Helpers.ExecuteNonQueryAzure(s_connectionString, "DROP TABLE " + s_tableName, s_commandTimeout);
            }
        }

        static void BulkCopy(string connectionString)
        {
            using (SqlConnection connectionSrc = new SqlConnection(connectionString))
            using (SqlConnection connectionDst = new SqlConnection(connectionString))
            using (SqlCommand commandSrc = new SqlCommand(s_sqlBulkCopyCmd, connectionSrc))
            using (SqlCommand commandDst = connectionDst.CreateCommand())
            {
                connectionSrc.Open();
                connectionDst.Open();

                commandSrc.CommandTimeout = s_commandTimeout;
                using (SqlDataReader reader = commandSrc.ExecuteReader())
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connectionDst))
                    {
                        sqlBulkCopy.DestinationTableName = s_tableName;
                        sqlBulkCopy.WriteToServer(reader);
                    }
                    reader.Close();
                }
            }
        }
    }
}
