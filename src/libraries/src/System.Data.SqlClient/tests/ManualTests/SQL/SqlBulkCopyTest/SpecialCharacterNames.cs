// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SpecialCharacterNames
    {
        private static string EscapeIdentifier(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        public static void Test(string srcConstr, string dstConstr, string dstPrefix)
        {
            // create schema and table names with special characters, with ] character escaped.
            string dstschema = dstPrefix + "_Schema'-]['']";
            dstschema = EscapeIdentifier(dstschema);

            string dstTable = dstPrefix + "_Table'-]['']";
            dstTable = EscapeIdentifier(dstTable);

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                try
                {
                    Helpers.TryExecute(dstCmd, "create schema " + dstschema);
                    Helpers.TryExecute(dstCmd, "create table " + dstTable + " (orderid int, customerid nchar(5))");

                    using (SqlConnection srcConn = new SqlConnection(srcConstr))
                    using (SqlCommand srcCmd = new SqlCommand("select top 2 orderid, customerid from orders", srcConn))
                    {
                        srcConn.Open();

                        using (SqlDataReader srcreader = srcCmd.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = dstTable;

                                bulkcopy.WriteToServer(srcreader);
                            }
                        }
                        Helpers.VerifyResults(dstConn, dstTable, 2, 2);
                    }
                }
                finally
                {
                    Helpers.TryExecute(dstCmd, "drop table " + dstTable);
                    Helpers.TryExecute(dstCmd, "drop schema " + dstschema);
                }
            }
        }
    }
}
