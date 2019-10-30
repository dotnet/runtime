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
    public class CopyWithEvent1
    {
        private static SqlBulkCopy bulkcopy;

        private static readonly long[] ExpectedRowCopiedResults = { 50, 100 };
        private static int currentRowCopyResult = 0;

        private static void OnRowCopied(object sender, SqlRowsCopiedEventArgs e)
        {
            Assert.True(currentRowCopyResult < ExpectedRowCopiedResults.Length, "More row copies than expected!");
            DataTestUtility.AssertEqualsWithDescription(ExpectedRowCopiedResults[currentRowCopyResult++], e.RowsCopied, "Unexpected Rows Copied count.");

            if (e.RowsCopied > 50)
            {
                e.Abort = true; // Abort batch
            }
            else if (e.RowsCopied > 60)
            {
                bulkcopy.NotifyAfter = 0; // switch off notification
            }
            else if (e.RowsCopied > 3)
            {
                bulkcopy.NotifyAfter = 10; // decrease notification frequency
            }
        }

        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                try
                {
                    Helpers.TryExecute(dstCmd, "create table " + dstTable + " (orderid int, customerid nchar(5), rdate datetime, freight money, shipname nvarchar(40))");

                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    using (SqlCommand srcCmd = new SqlCommand("select top 100 * from orders", srcConn))
                    {
                        srcConn.Open();

                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        using (bulkcopy = new SqlBulkCopy(dstConn, SqlBulkCopyOptions.UseInternalTransaction, null))
                        {
                            bulkcopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnRowCopied);

                            bulkcopy.DestinationTableName = dstTable;
                            bulkcopy.NotifyAfter = 50;

                            SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                            ColumnMappings.Add("OrderID", "orderid");
                            ColumnMappings.Add("CustomerID", "customerid");
                            ColumnMappings.Add("RequiredDate", "rdate");
                            ColumnMappings.Add("Freight", "freight");
                            ColumnMappings.Add("ShipName", "shipname");

                            bulkcopy.NotifyAfter = 3;
                            DataTestUtility.AssertThrowsWrapper<OperationAbortedException>(() => bulkcopy.WriteToServer(reader));
                            bulkcopy.SqlRowsCopied -= new SqlRowsCopiedEventHandler(OnRowCopied);
                            bulkcopy.Close();
                        }
                    }
                }
                finally
                {
                    Helpers.TryExecute(dstCmd, "drop table " + dstTable);
                }
            }
        }
    }
}
