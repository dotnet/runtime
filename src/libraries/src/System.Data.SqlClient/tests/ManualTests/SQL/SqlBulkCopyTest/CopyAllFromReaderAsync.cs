// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CopyAllFromReaderAsync
    {
        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
            // Use this semaphore to ensure that results are written to the log in the correct order
            SemaphoreSlim outputSemaphore = new SemaphoreSlim(0, 1);

            Task t = TestAsync(srcConstr, dstConstr, dstTable, outputSemaphore);
            outputSemaphore.Release();
            t.Wait();
            Assert.True(t.IsCompleted, "Task did not complete! Status: " + t.Status);
        }

        private static async Task TestAsync(string srcConstr, string dstConstr, string dstTable, SemaphoreSlim outputSemaphore)
        {
            string initialQueryTemplate = "create table {0} (col1 int, col2 nvarchar(20), col3 nvarchar(10))";
            string sourceQueryTemplate = "select top 5 EmployeeID, LastName, FirstName from {0}";
            string srcTable = "employees";

            string sourceQuery = string.Format(sourceQueryTemplate, srcTable);
            string initialQuery = string.Format(initialQueryTemplate, dstTable);

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();
                try
                {
                    Helpers.TryExecute(dstCmd, initialQuery);
                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    using (SqlCommand srcCmd = new SqlCommand(sourceQuery, srcConn))
                    {
                        srcConn.Open();
                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = dstTable;

                                await bulkcopy.WriteToServerAsync(reader);
                            }
                            await outputSemaphore.WaitAsync();
                            Helpers.VerifyResults(dstConn, dstTable, 3, 5);
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
