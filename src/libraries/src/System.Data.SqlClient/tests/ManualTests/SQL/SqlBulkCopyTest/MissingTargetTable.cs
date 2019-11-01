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
    public class MissingTargetTable
    {
        public static void Test(string srcConstr, string dstConstr, string targetTable)
        {
            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                using (SqlConnection srcConn = new SqlConnection(srcConstr))
                using (SqlCommand srcCmd = new SqlCommand("select top 5 EmployeeID, LastName, FirstName from employees", srcConn))
                {
                    srcConn.Open();

                    using (DbDataReader reader = srcCmd.ExecuteReader())
                    {
                        bool hitException = false;
                        try
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = targetTable;
                                SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                                ColumnMappings.Add("EmployeeID", "col1");
                                ColumnMappings.Add("LastName", "col2");
                                ColumnMappings.Add("FirstName", "col3");

                                bulkcopy.WriteToServer(reader);
                                bulkcopy.Close();
                            }
                        }
                        catch (InvalidOperationException ioe)
                        {
                            string expectedErrorMsg = string.Format(SystemDataResourceManager.Instance.SQL_BulkLoadInvalidDestinationTable, targetTable);
                            Assert.True(ioe.Message.Contains(expectedErrorMsg), "Unexpected error message: " + ioe.Message);
                            hitException = true;
                        }
                        Assert.True(hitException, "Did not get any exceptions!");
                    }
                }
            }
        }
    }
}
