// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class Bug84548
    {
        public static void Test(string srcConstr, string dstConstr, string targettable)
        {
            string targetCustomerTable = targettable + "_customer";

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();
                try
                {
                    Helpers.TryExecute(dstCmd, "CREATE TABLE [" + targetCustomerTable + "] ([CustomerID] [nchar] (5) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL, CONSTRAINT [PK_" + targetCustomerTable + "] PRIMARY KEY CLUSTERED (CustomerID) ON [PRIMARY]) ON [PRIMARY]");

                    Helpers.TryExecute(dstCmd,
                        "CREATE TABLE [" + targettable + "] ([OrderID] [int] NOT NULL , " +
                        " [CustomerID] [nchar] (5) COLLATE SQL_Latin1_General_CP1_CI_AS NULL , " +
                        " CONSTRAINT [PK_" + targettable + "] PRIMARY KEY  CLUSTERED " +
                        " (" +
                        "  [OrderID]" +
                        " )  ON [PRIMARY] ," +
                        " CONSTRAINT [FK_" + targettable + "_Customers] FOREIGN KEY " +
                        " (" +
                        "  [CustomerID]" +
                        " ) REFERENCES [" + targetCustomerTable + "] (" +
                        " [CustomerID]" +
                        " )" +
                        ") ON [PRIMARY]");

                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    {
                        srcConn.Open();

                        // First copy the customer ID list across
                        SqlCommand customerCommand = new SqlCommand("SELECT CustomerID from Northwind..Customers", srcConn);
                        using (DbDataReader reader = customerCommand.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = targetCustomerTable;
                                bulkcopy.WriteToServer(reader);
                            }
                        }

                        SqlCommand srcCmd = new SqlCommand("select OrderID, CustomerID from Northwind..Orders where OrderId = 10643", srcConn);
                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = targettable;
                                SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                                ColumnMappings.Add("OrderID", "OrderID");
                                ColumnMappings.Add("CustomerID", "CustomerID");

                                bulkcopy.WriteToServer(reader);
                                bulkcopy.Close();
                            }
                        }
                    }
                    Helpers.VerifyResults(dstConn, targettable, 2, 1);
                }
                finally
                {
                    Helpers.TryExecute(dstCmd, "drop table " + targettable);
                    Helpers.TryExecute(dstCmd, "drop table " + targetCustomerTable);
                }
            }
        }
    }
}

