// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class Bug85007
    {
        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
            string targetCustomerTable = dstTable + "_customer";

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                try
                {
                    Helpers.TryExecute(dstCmd, "CREATE TABLE [" + targetCustomerTable + "] ([CustomerID] [nchar] (5) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL, CONSTRAINT [PK_" + targetCustomerTable + "] PRIMARY KEY CLUSTERED (CustomerID) ON [PRIMARY]) ON [PRIMARY]");

                    Helpers.TryExecute(dstCmd,
                        "CREATE TABLE [" + dstTable + "] (" +
                        "    [OrderID] [int] IDENTITY (1, 1) NOT NULL ," +
                        "    [CustomerID] [nchar] (5) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    [EmployeeID] [int] NULL ," +
                        "    [OrderDate] [datetime] NULL ," +
                        "    [RequiredDate] [datetime] NULL ," +
                        "    [ShippedDate] [datetime] NULL ," +
                        "    [ShipVia] [int] NULL ," +
                        "    [Freight] [money] NULL CONSTRAINT [DF_" + dstTable + "_Freight] DEFAULT (0)," +
                        "    [ShipName] [nvarchar] (40) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    [ShipAddress] [nvarchar] (60) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    [ShipCity] [nvarchar] (15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    [ShipRegion] [nvarchar] (15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    [ShipPostalCode] [nvarchar] (10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    [ShipCountry] [nvarchar] (15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ," +
                        "    CONSTRAINT [PK_" + dstTable + "] PRIMARY KEY  CLUSTERED " +
                        "    (" +
                        "        [OrderID]" +
                        "    )  ON [PRIMARY] ," +
                        "    CONSTRAINT [FK_" + dstTable + "_Customers] FOREIGN KEY " +
                        "    (" +
                        "        [CustomerID]" +
                        "    ) REFERENCES [" + targetCustomerTable + "] (" +
                        "        [CustomerID]" +
                        "    )" +
                        ") ON [PRIMARY]");

                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    using (SqlCommand customerCmd = new SqlCommand("SELECT CustomerID from Northwind..Customers", srcConn))
                    {
                        srcConn.Open();

                        // First copy the customer ID list across
                        using (DbDataReader reader = customerCmd.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = targetCustomerTable;
                                bulkcopy.WriteToServer(reader);
                            }
                        }

                        SqlCommand srcCmd = new SqlCommand("select * from orders", srcConn);
                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        {

                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = dstTable;
                                bulkcopy.BatchSize = 6;

                                SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                                ColumnMappings.Add("OrderID", "OrderID");
                                ColumnMappings.Add("CustomerID", "CustomerID");
                                ColumnMappings.Add("EmployeeID", "EmployeeID");
                                ColumnMappings.Add("RequiredDate", "RequiredDate");
                                ColumnMappings.Add("ShippedDate", "ShippedDate");
                                ColumnMappings.Add("ShipVia", "ShipVia");
                                ColumnMappings.Add("Freight", "Freight");
                                ColumnMappings.Add("ShipName", "ShipName");
                                ColumnMappings.Add("ShipAddress", "ShipAddress");
                                ColumnMappings.Add("ShipCity", "ShipCity");
                                ColumnMappings.Add("ShipRegion", "ShipRegion");
                                ColumnMappings.Add("ShipPostalCode", "ShipPostalCode");
                                ColumnMappings.Add("ShipCountry", "ShipCountry");

                                bulkcopy.WriteToServer(reader);
                            }
                            Helpers.VerifyResults(dstConn, dstTable, 14, 830);
                        }
                    }
                }
                finally
                {
                    Helpers.TryExecute(dstCmd, "drop table " + dstTable);
                    Helpers.TryExecute(dstCmd, "drop table " + targetCustomerTable);
                }
            }
        }
    }
}
