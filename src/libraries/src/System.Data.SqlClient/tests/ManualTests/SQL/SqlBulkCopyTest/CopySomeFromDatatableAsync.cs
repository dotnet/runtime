// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CopySomeFromDataTableAsync
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
            DataSet dataset;
            SqlDataAdapter adapter;
            DataTable datatable;

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                try
                {
                    Helpers.TryExecute(dstCmd, "create table " + dstTable + " (col1 int, col2 nvarchar(20), col3 nvarchar(10), col4 datetime)");
                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    using (SqlCommand srcCmd = new SqlCommand("select * from employees", srcConn))
                    {
                        srcConn.Open();

                        dataset = new DataSet("MyDataSet");
                        adapter = new SqlDataAdapter(srcCmd);
                        adapter.Fill(dataset);
                        datatable = dataset.Tables[0];

                        datatable.Rows[0].BeginEdit();
                        datatable.Rows[0][0] = 333;
                        datatable.Rows[0].EndEdit();

                        using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                        {
                            bulkcopy.DestinationTableName = dstTable;
                            bulkcopy.BatchSize = 7;

                            SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                            ColumnMappings.Add(0, "col1");
                            ColumnMappings.Add(1, "col2");
                            ColumnMappings.Add(2, "col3");
                            bulkcopy.WriteToServer(datatable, DataRowState.Unchanged);
                            datatable.Rows.GetEnumerator().Reset();
                            await bulkcopy.WriteToServerAsync(datatable, DataRowState.Modified);
                            datatable.Rows.GetEnumerator().Reset();
                            await bulkcopy.WriteToServerAsync(datatable, DataRowState.Deleted);
                            bulkcopy.Close();
                        }

                        await outputSemaphore.WaitAsync();
                        Helpers.VerifyResults(dstConn, dstTable, 4, 18);
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
