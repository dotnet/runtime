// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CopyVariants
    {
        public static void Test(string constr, string dstTable)
        {
            string[] prologue =
            {
                "create table " + dstTable + "_src (col_1 int primary key, col_2 sql_variant)",

                "insert into " + dstTable + "_src values (0, null)",
                "insert into " + dstTable + "_src values (1, convert(int, 0))",
                "insert into " + dstTable + "_src values (2, convert(smallint, -32768))",
                "insert into " + dstTable + "_src values (3, convert(real, 2.2))",
                "insert into " + dstTable + "_src values (4, convert(float, -3303.33303))",
                "insert into " + dstTable + "_src values (5, convert(decimal(28,4), 44404.4404))",
                "insert into " + dstTable + "_src values (6, convert(money, $555505.5505) )",
                "insert into " + dstTable + "_src values (7, convert(smallmoney, $-6.6606) )",
                "insert into " + dstTable + "_src values (8, convert(bit, 1) )",
                "insert into " + dstTable + "_src values (9, convert(tinyint, 8) )",
                "insert into " + dstTable + "_src values (10, convert(uniqueidentifier, '00000000-0000-0000-0000-000000000009') )",
                "insert into " + dstTable + "_src values (11, convert(varbinary(756), 0xAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA0A) )",
                "insert into " + dstTable + "_src values (12, convert(varchar(756), '111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111101') )",
                "insert into " + dstTable + "_src values (13, convert(nvarchar(756), N'???a???????????????????????????????üböuaäZßABCÄboÜOUÖvrhÃã??z?????????????????z?????????A?????a???????????????????????????????üböuaäZßABCÄboÜOUÖvrhÃã??z?????????????????z?????????A?????a???????????????????????????????üböuaäZßABCÄboÜOUÖvrhÃã??z?????????????????z?????????A?????a???????????????????????????????üböuaäZßABCÄboÜOUÖvrhÃã??z?????????????????z?????????A?????a?????') )",
                "insert into " + dstTable + "_src values (14, convert(datetime, {ts '2003-01-11 12:54:01.133'}) )",
                "insert into " + dstTable + "_src values (15, convert(bigint, 444444444444404) )",
                "insert into " + dstTable + "_src values (16, convert(int, -555505) )",
                "insert into " + dstTable + "_src values (17, convert(smallint, 16) )",
                "insert into " + dstTable + "_src values (18, convert(real, 777707.7) )",
                "insert into " + dstTable + "_src values (19, convert(float, -888888808.88018) )",
                "insert into " + dstTable + "_src values (20, convert(decimal(28,4), 99999999999999999909.9019) )",

                "create table " + dstTable + "_dst (col_1 int primary key, col_2 sql_variant)",
            };

            using (SqlConnection dstConn = new SqlConnection(constr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                try
                {
                    foreach (string cmdtext in prologue)
                    {
                        Helpers.TryExecute(dstCmd, cmdtext);
                    }
                    using (SqlConnection srcConn = new SqlConnection(constr))
                    using (SqlCommand srcCmd = new SqlCommand("select * from " + dstTable + "_src", srcConn))
                    {
                        srcConn.Open();

                        using (DbDataReader reader = srcCmd.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(dstConn))
                            {
                                bulkcopy.DestinationTableName = dstTable + "_dst";
                                bulkcopy.WriteToServer(reader);
                            }
                            Helpers.VerifyResults(dstConn, dstTable + "_dst", 2, 21);
                        }
                    }
                }
                finally
                {
                    Helpers.TryExecute(dstCmd, "drop table " + dstTable + "_src");
                    Helpers.TryExecute(dstCmd, "drop table " + dstTable + "_dst");
                }
            }
        }
    }
}
