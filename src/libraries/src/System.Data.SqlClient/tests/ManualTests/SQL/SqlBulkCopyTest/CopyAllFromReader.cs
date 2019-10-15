// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CopyAllFromReader
    {
        private static readonly string destinationTable = null;
        private static readonly string sourceTable = "employees";
        private static readonly string initialQueryTemplate = "create table {0} (col1 int, col2 nvarchar(20), col3 nvarchar(10))";
        private static readonly string sourceQueryTemplate = "select top 5 EmployeeID, LastName, FirstName from {0}";

        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
            Debug.Assert((int)SqlBulkCopyOptions.UseInternalTransaction == 1 << 5, "Compiler screwed up the options");

            dstTable = destinationTable != null ? destinationTable : dstTable;

            string sourceQuery = string.Format(sourceQueryTemplate, sourceTable);
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
                            IDictionary stats;
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = dstTable;
                                dstConn.StatisticsEnabled = true;
                                bulkcopy.WriteToServer(reader);
                                dstConn.StatisticsEnabled = false;
                                stats = dstConn.RetrieveStatistics();
                            }
                            Helpers.VerifyResults(dstConn, dstTable, 3, 5);

                            Assert.True(0 < (long)stats["BytesReceived"], "BytesReceived is non-positive.");
                            Assert.True(0 < (long)stats["BytesSent"], "BytesSent is non-positive.");
                            Assert.True((long)stats["ConnectionTime"] >= (long)stats["ExecutionTime"], "Connection Time is less than Execution Time.");
                            Assert.True((long)stats["ExecutionTime"] >= (long)stats["NetworkServerTime"], "Execution Time is less than Network Server Time.");
                            DataTestUtility.AssertEqualsWithDescription((long)0, (long)stats["UnpreparedExecs"], "Non-zero UnpreparedExecs value: " + (long)stats["UnpreparedExecs"]);
                            DataTestUtility.AssertEqualsWithDescription((long)0, (long)stats["PreparedExecs"], "Non-zero PreparedExecs value: " + (long)stats["PreparedExecs"]);
                            DataTestUtility.AssertEqualsWithDescription((long)0, (long)stats["Prepares"], "Non-zero Prepares value: " + (long)stats["Prepares"]);
                            DataTestUtility.AssertEqualsWithDescription((long)0, (long)stats["CursorOpens"], "Non-zero CursorOpens value: " + (long)stats["CursorOpens"]);
                            DataTestUtility.AssertEqualsWithDescription((long)0, (long)stats["IduRows"], "Non-zero IduRows value: " + (long)stats["IduRows"]);

                            DataTestUtility.AssertEqualsWithDescription((long)4, stats["BuffersReceived"], "Unexpected BuffersReceived value.");
                            DataTestUtility.AssertEqualsWithDescription((long)3, stats["BuffersSent"], "Unexpected BuffersSent value.");
                            DataTestUtility.AssertEqualsWithDescription((long)0, stats["IduCount"], "Unexpected IduCount value.");
                            DataTestUtility.AssertEqualsWithDescription((long)3, stats["SelectCount"], "Unexpected SelectCount value.");
                            DataTestUtility.AssertEqualsWithDescription((long)3, stats["ServerRoundtrips"], "Unexpected ServerRoundtrips value.");
                            DataTestUtility.AssertEqualsWithDescription((long)4, stats["SelectRows"], "Unexpected SelectRows value.");
                            DataTestUtility.AssertEqualsWithDescription((long)2, stats["SumResultSets"], "Unexpected SumResultSets value.");
                            DataTestUtility.AssertEqualsWithDescription((long)0, stats["Transactions"], "Unexpected Transactions value.");
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
