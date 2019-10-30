// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class FireTrigger
    {
        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
            string dstTable1 = dstTable + "_1"; // this table will receive a value if the trigger fires!
            string trigger = dstTable + "_2";
            string[] prologue =
            {
                "create table " + dstTable + "(col1 int, col2 nvarchar(20), col3 nvarchar(10))",
                "create table " + dstTable1 + "  (col1 int);",
                "create trigger " + trigger + "  on " + dstTable + " for INSERT as insert into " + dstTable1 + "  values (333)"
            };
            string[] epilogue =
            {
                "drop table " + dstTable1 + " ",
                "drop trigger " + trigger + " ",
                "drop table " + dstTable
            };

            string sourceTable = "employees";
            string sourceQueryTemplate = "select top 5 EmployeeID, LastName, FirstName from {0}";
            string sourceQuery = string.Format(sourceQueryTemplate, sourceTable);

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();
                Helpers.ProcessCommandBatch(dstCmd, prologue);

                try
                {
                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    using (SqlCommand srcCmd = new SqlCommand(sourceQuery, srcConn))
                    {
                        srcConn.Open();

                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        {
                            SqlBulkCopyOptions option = SqlBulkCopyOptions.FireTriggers;

                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn, option, null))
                            {
                                bulkcopy.DestinationTableName = dstTable;
                                bulkcopy.WriteToServer(reader);
                            }
                        }

                        dstCmd.CommandText = "select top 2 * from " + dstTable1 + " ";
                        using (DbDataReader reader2 = dstCmd.ExecuteReader())
                        {
                            Assert.True(reader2.Read(), "Failed to read!");

                            Assert.True(reader2[0] is int, "Unexpected Field(0) type: " + reader2[0].GetType());

                            Assert.True((int)(reader2[0]) == 333, "Unexpected Field(0) value: " + reader2[0]);
                        }
                    }
                }
                finally
                {
                    Helpers.ProcessCommandBatch(dstCmd, epilogue);
                }
            }
        }
    }
}
