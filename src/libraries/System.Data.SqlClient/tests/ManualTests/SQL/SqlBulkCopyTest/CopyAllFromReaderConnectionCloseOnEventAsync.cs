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

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CopyAllFromReaderConnectionClosedOnEventAsync
    {
        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
#if DEBUG
            string initialQueryTemplate = "create table {0} (col1 int, col2 nvarchar(20), col3 nvarchar(10), col4 varchar(8000))";
            string sourceQuery = "select EmployeeID, LastName, FirstName, REPLICATE('a', 8000) from employees";
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

                                // Close the bulk copy's connection when it notifies us
                                bulkcopy.NotifyAfter = 1;
                                bulkcopy.SqlRowsCopied += (sender, e) =>
                                {
                                    dstConn.Close();
                                };

                                using (AsyncDebugScope debugScope = new AsyncDebugScope())
                                {
                                    // Force all writes to pend, this will guarantee that we will go through the correct code path
                                    debugScope.ForceAsyncWriteDelay = 1;

                                    // Check that the copying fails
                                    string message = string.Format(SystemDataResourceManager.Instance.ADP_OpenConnectionRequired, "WriteToServer", SystemDataResourceManager.Instance.ADP_ConnectionStateMsg_Closed);
                                    DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => bulkcopy.WriteToServerAsync(reader).Wait(5000), innerExceptionMessage: message);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Helpers.TryDropTable(dstConstr, dstTable);
                }
            }
#endif
        }
    }
}
