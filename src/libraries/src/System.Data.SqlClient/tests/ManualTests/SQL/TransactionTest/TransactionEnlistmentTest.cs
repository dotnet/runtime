// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Transactions;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class TransactionEnlistmentTest
    {
        [CheckConnStrSetupFact]
        public static void TestAmbientTransaction_TxScopeComplete()
        {
            const int inputCol1 = 1;
            const string inputCol2 = "one";

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            string connectionString = builder.ConnectionString;
            string testTableName = GenerateTableName();

            RunNonQuery(connectionString, $"create table {testTableName} (col1 int, col2 text)");

            try
            {
                using (TransactionScope txScope = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = $"INSERT INTO {testTableName} VALUES ({inputCol1}, '{inputCol2}')";
                            command.ExecuteNonQuery();
                        }
                    }
                    txScope.Complete();
                }

                DataTable result = RunQuery(connectionString, $"select col2 from {testTableName} where col1 = {inputCol1}");
                Assert.True(result.Rows.Count > 0);
                Assert.True(string.Equals(result.Rows[0][0], inputCol2));
            }
            finally
            {
                RunNonQuery(connectionString, $"drop table {testTableName}");
            }
        }

        [CheckConnStrSetupFact]
        public static void TestAmbientTransaction_TxScopeNonComplete()
        {
            const int inputCol1 = 2;
            const string inputCol2 = "two";

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            string connectionString = builder.ConnectionString;
            string testTableName = GenerateTableName();

            RunNonQuery(connectionString, $"create table {testTableName} (col1 int, col2 text)");

            try
            {
                using (TransactionScope txScope = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = $"INSERT INTO {testTableName} VALUES ({inputCol1}, '{inputCol2}')";
                            command.ExecuteNonQuery();
                        }
                    }
                }

                DataTable result = RunQuery(connectionString, $"select col2 from {testTableName} where col1 = {inputCol1}");
                Assert.True(result.Rows.Count == 0);
            }
            finally
            {
                RunNonQuery(connectionString, $"drop table {testTableName}");
            }
        }

        private static void RunNonQuery(string connectionString, string sql)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static DataTable RunQuery(string connectionString, string sql)
        {
            DataTable result = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        result = new DataTable();
                        result.Load(reader);
                    }
                }
            }
            return result;
        }

        private static string GenerateTableName()
        {
            return string.Format("TEST_{0}{1}{2}", Environment.GetEnvironmentVariable("ComputerName"), Environment.TickCount, Guid.NewGuid()).Replace('-', '_');
        }
    }
}
