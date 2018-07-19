// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CheckConstraints
    {
        public static void Test(string constr, string srctable, string dstTable)
        {
            using (SqlConnection dstConn = new SqlConnection(constr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();
                try
                {
                    // create the source table
                    Helpers.TryExecute(dstCmd, "create table " + srctable + " (col1 int , col2 int, col3 text)");
                    Helpers.TryExecute(dstCmd, "insert into " + srctable + " values (33, 498, 'Michael')");
                    Helpers.TryExecute(dstCmd, "insert into " + srctable + " values (34, 499, 'Astrid')");
                    Helpers.TryExecute(dstCmd, "insert into " + srctable + " values (65, 500, 'alles Käse')");

                    Helpers.TryExecute(dstCmd, "create table " + dstTable + " (col1 int primary key, col2 int CONSTRAINT CK_" + dstTable + " CHECK (col2 < 500), col3 text)");

                    using (SqlConnection srcConn = new SqlConnection(constr))
                    using (SqlCommand srcCmd = new SqlCommand("select * from " + srctable, srcConn))
                    {
                        srcConn.Open();
                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        {
                            try
                            {
                                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn, SqlBulkCopyOptions.CheckConstraints, null))
                                {
                                    bulkcopy.DestinationTableName = dstTable;
                                    SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                                    ColumnMappings.Add("col1", "col1");
                                    ColumnMappings.Add("col2", "col2");
                                    ColumnMappings.Add("col3", "col3");
                                    bulkcopy.WriteToServer(reader);
                                }
                            }
                            catch (SqlException sqlEx)
                            {
                                // Error 547 == The %ls statement conflicted with the %ls constraint "%.*ls".
                                DataTestUtility.AssertEqualsWithDescription(547, sqlEx.Number, "Unexpected error number.");
                            }
                        }
                    }
                }
                finally
                {
                    Helpers.TryExecute(dstCmd, "drop table " + dstTable);
                    Helpers.TryExecute(dstCmd, "drop table " + srctable);
                }
            }
        }
    }
}
