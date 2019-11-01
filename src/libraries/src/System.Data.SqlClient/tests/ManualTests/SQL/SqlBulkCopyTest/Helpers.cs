// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class Helpers
    {
        internal static void ProcessCommandBatch(Type connType, string constr, string[] batch)
        {
            if (batch.Length > 0)
            {
                object[] activatorArgs = new object[1];
                activatorArgs[0] = constr;
                using (DbConnection conn = (DbConnection)Activator.CreateInstance(connType, activatorArgs))
                {
                    conn.Open();
                    DbCommand cmd = conn.CreateCommand();

                    ProcessCommandBatch(cmd, batch);
                }
            }
        }

        internal static void ProcessCommandBatch(DbCommand cmd, string[] batch)
        {
            foreach (string cmdtext in batch)
            {
                Helpers.TryExecute(cmd, cmdtext);
            }
        }

        public static int TryDropTable(string dstConstr, string tableName)
        {
            using (SqlConnection dropConn = new SqlConnection(dstConstr))
            using (SqlCommand dropCmd = dropConn.CreateCommand())
            {
                dropConn.Open();
                return Helpers.TryExecute(dropCmd, "drop table " + tableName);
            }
        }

        public static int TryExecute(DbCommand cmd, string strText)
        {
            cmd.CommandText = strText;
            return cmd.ExecuteNonQuery();
        }

        public static int ExecuteNonQueryAzure(string strConnectionString, string strCommand, int commandTimeout = 60)
        {
            using (SqlConnection connection = new SqlConnection(strConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                // We need to increase CommandTimeout else you might see the following error:
                // "Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding."
                command.CommandTimeout = commandTimeout;
                return Helpers.TryExecute(command, strCommand);
            }
        }

        public static bool VerifyResults(DbConnection conn, string dstTable, int expectedColumns, int expectedRows)
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from " + dstTable + "; select count(*) from " + dstTable;
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    int numColumns = reader.FieldCount;
                    reader.NextResult();
                    reader.Read();
                    int numRows = (int)reader[0];
                    reader.Close();

                    DataTestUtility.AssertEqualsWithDescription(expectedColumns, numColumns, "Unexpected number of columns.");
                    DataTestUtility.AssertEqualsWithDescription(expectedRows, numRows, "Unexpected number of columns.");
                }
            }
            return false;
        }

        public static bool CheckTableRows(DbConnection conn, string table, bool shouldHaveRows)
        {
            string query = "select * from " + table;
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    DataTestUtility.AssertEqualsWithDescription(shouldHaveRows, reader.HasRows, "Unexpected value for HasRows.");
                }
            }
            return false;
        }
    }
}
