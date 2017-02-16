// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.SqlClient;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class Bug903514
    {
        public static void Test(string constr, string dstTable)
        {
            using (SqlConnection dstConn = new SqlConnection(constr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                Helpers.Execute(dstCmd, "create table " + dstTable + " (col1 int, col2 varchar(7000))");
            }

            DoBulkCopy(constr, dstTable, 2);
            DoBulkCopy(constr, dstTable, 0);

            using (SqlConnection dstConn = new SqlConnection(constr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                Helpers.TryExecute(dstCmd, "drop table " + dstTable);
            }
        }

        private static void DoBulkCopy(string dstConstr, string dstTable, int timeout)
        {
            DataTable table = new DataTable();
            DataColumn column;
            DataRow row;

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "col1";
            table.Columns.Add(column);
            column = new DataColumn();
            column.DataType = Type.GetType("System.String");
            column.ColumnName = "col2";
            table.Columns.Add(column);

            if (0 == timeout)
            {
                for (int i = 0; i < 100; i++)
                {
                    row = table.NewRow();
                    row["col1"] = i;
                    row["col2"] = "item " + i;
                    table.Rows.Add(row);
                }
            }
            else
            {
                string s = new string('a', 4000);

                for (int i = 0; i < 750000; i++)
                {
                    row = table.NewRow();
                    row["col1"] = i;
                    row["col2"] = s;
                    table.Rows.Add(row);
                }
            }

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            {
                dstConn.Open();

                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                {
                    bulkcopy.DestinationTableName = dstTable;

                    bulkcopy.BulkCopyTimeout = timeout;

                    try
                    {
                        bulkcopy.WriteToServer(table);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.Message.Contains("Timeout expired") && 0 != timeout, "Unexpected exception: " + e);
                    }
                }
            }
        }
    }
}
