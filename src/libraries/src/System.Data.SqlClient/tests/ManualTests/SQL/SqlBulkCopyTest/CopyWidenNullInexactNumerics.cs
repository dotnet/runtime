// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CopyWidenNullInexactNumerics
    {
        public static void Test(string sourceDatabaseConnectionString, string destinationDatabaseConnectionString)
        {
            // this test copies float and real inexact numeric types into decimal targets using bulk copy to check that the widening of the type succeeds.

            using (var sourceConnection = new SqlConnection(sourceDatabaseConnectionString))
            using (var destinationConnection = new SqlConnection(destinationDatabaseConnectionString))
            {
                sourceConnection.Open();
                destinationConnection.Open();

                RunCommands(sourceConnection,
                    new[]
                    {
                        "drop table if exists dbo.__SqlBulkCopyBug_Source",
                        "create table dbo.__SqlBulkCopyBug_Source (floatVal float null, realVal real null)",
                        "insert dbo.__SqlBulkCopyBug_Source(floatVal,realVal) values(1,1),(2,2),(null,null),(0.00000000000001,0.00000000000001)"
                    }
                );

                RunCommands(destinationConnection,
                    new[]
                    {
                        "drop table if exists dbo.__SqlBulkCopyBug_Destination",
                        "create table dbo.__SqlBulkCopyBug_Destination (floatVal decimal(18,10) null,realVal decimal(18,10) null)"
                    }
                );

                Exception error = null;
                try
                {
                    var bulkCopy = new SqlBulkCopy(destinationConnection, SqlBulkCopyOptions.Default, null);
                    bulkCopy.DestinationTableName = "dbo.__SqlBulkCopyBug_Destination";
                    using (var sourceCommand = new SqlCommand("select * from dbo.__SqlBulkCopyBug_Source", sourceConnection, null))
                    using (var sourceReader = sourceCommand.ExecuteReader())
                    {
                        bulkCopy.WriteToServer(sourceReader);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    try
                    {
                        RunCommands(sourceConnection,
                            new[]
                            {
                                "drop table if exists dbo.__SqlBulkCopyBug_Source",
                                "drop table if exists dbo.__SqlBulkCopyBug_Destination",
                            }
                        );
                    }
                    catch
                    {
                    }
                }

                Assert.Null(error);
            }
        }

        public static void RunCommands(SqlConnection connection, IEnumerable<string> commands)
        {
            using (var sqlCommand = connection.CreateCommand())
            {
                foreach (var command in commands)
                {
                    Helpers.TryExecute(sqlCommand, command);
                }
            }
        }
    }
}
