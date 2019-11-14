// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlVariantParam
    {
        private static string s_connStr;

        /// <summary>
        /// Tests all SqlTypes inside sql_variant to server using sql_variant parameter, SqlBulkCopy, and TVP parameter with sql_variant inside.
        /// </summary>
        public static void SendAllSqlTypesInsideVariant(string connStr)
        {
            s_connStr = connStr;
            Console.WriteLine("");
            Console.WriteLine("Starting test 'SqlVariantParam'");
            SendVariant(new SqlSingle((float)123.45), "System.Data.SqlTypes.SqlSingle", "real");
            SendVariant(new SqlSingle((double)123.45), "System.Data.SqlTypes.SqlSingle", "real");
            SendVariant(new SqlString("hello"), "System.Data.SqlTypes.SqlString", "nvarchar");
            SendVariant(new SqlDouble((double)123.45), "System.Data.SqlTypes.SqlDouble", "float");
            SendVariant(new SqlBinary(new byte[] { 0x00, 0x11, 0x22 }), "System.Data.SqlTypes.SqlBinary", "varbinary");
            SendVariant(new SqlGuid(Guid.NewGuid()), "System.Data.SqlTypes.SqlGuid", "uniqueidentifier");
            SendVariant(new SqlBoolean(true), "System.Data.SqlTypes.SqlBoolean", "bit");
            SendVariant(new SqlBoolean(1), "System.Data.SqlTypes.SqlBoolean", "bit");
            SendVariant(new SqlByte(1), "System.Data.SqlTypes.SqlByte", "tinyint");
            SendVariant(new SqlInt16(1), "System.Data.SqlTypes.SqlInt16", "smallint");
            SendVariant(new SqlInt32(1), "System.Data.SqlTypes.SqlInt32", "int");
            SendVariant(new SqlInt64(1), "System.Data.SqlTypes.SqlInt64", "bigint");
            SendVariant(new SqlDecimal(1234.123M), "System.Data.SqlTypes.SqlDecimal", "numeric");
            SendVariant(new SqlDateTime(DateTime.Now), "System.Data.SqlTypes.SqlDateTime", "datetime");
            SendVariant(new SqlMoney(123.123M), "System.Data.SqlTypes.SqlMoney", "money");
            Console.WriteLine("End test 'SqlVariantParam'");
        }
        /// <summary>
        /// Returns a SqlDataReader with embedded sql_variant column with paramValue inside.
        /// </summary>
        /// <param name="paramValue">object value to embed as sql_variant field value</param>
        /// <param name="includeBaseType">Set to true to return optional BaseType column which extracts base type of sql_variant column.</param>
        /// <returns></returns>
        private static SqlDataReader GetReaderForVariant(object paramValue, bool includeBaseType)
        {
            SqlConnection conn = new SqlConnection(s_connStr);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select @p1 as f1";
            if (includeBaseType)
                cmd.CommandText += ", sql_variant_property(@p1,'BaseType') as BaseType";
            cmd.Parameters.Add("@p1", SqlDbType.Variant);
            cmd.Parameters["@p1"].Value = paramValue;
            SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return dr;
        }
        /// <summary>
        /// Verifies if SqlDataReader returns expected SqlType and base type.
        /// </summary>
        /// <param name="tag">Test tag to identify caller</param>
        /// <param name="dr">SqlDataReader to verify</param>
        /// <param name="expectedTypeName">Expected type name (SqlType)</param>
        /// <param name="expectedBaseTypeName">Expected base type name (Sql Server type name)</param>
        private static void VerifyReader(string tag, SqlDataReader dr, string expectedTypeName, string expectedBaseTypeName)
        {
            // select sql_variant_property(cast(cast(123.45 as money) as sql_variant),'BaseType' ) as f1
            dr.Read();
            string actualTypeName = dr.GetSqlValue(0).GetType().ToString();
            string actualBaseTypeName = dr.GetString(1);
            Console.WriteLine("{0,-40} -> {1}:{2}", tag, actualTypeName, actualBaseTypeName);
            if (!actualTypeName.Equals(expectedTypeName))
            {
                Console.WriteLine("   --> ERROR: Expected type {0} does not match actual type {1}",
                    expectedTypeName, actualTypeName);
            }
            if (!actualBaseTypeName.Equals(expectedBaseTypeName))
            {
                Console.WriteLine("   --> ERROR: Expected base type {0} does not match actual base type {1}",
                    expectedBaseTypeName, actualBaseTypeName);
            }
        }
        /// <summary>
        /// Round trips a sql_variant to server and verifies result.
        /// </summary>
        /// <param name="paramValue">Value to send as sql_variant</param>
        /// <param name="expectedTypeName">Expected SqlType name to round trip</param>
        /// <param name="expectedBaseTypeName">Expected base type name (SQL Server base type inside sql_variant)</param>
        private static void SendVariant(object paramValue, string expectedTypeName, string expectedBaseTypeName)
        {
            // Round trip using Bulk Copy, normal param, and TVP param.
            SendVariantBulkCopy(paramValue, expectedTypeName, expectedBaseTypeName);
            SendVariantParam(paramValue, expectedTypeName, expectedBaseTypeName);
            SendVariantTvp(paramValue, expectedTypeName, expectedBaseTypeName);
        }
        /// <summary>
        /// Round trip sql_variant value as normal parameter.
        /// </summary>
        private static void SendVariantParam(object paramValue, string expectedTypeName, string expectedBaseTypeName)
        {
            using (SqlDataReader dr = GetReaderForVariant(paramValue, true))
            {
                VerifyReader("SendVariantParam", dr, expectedTypeName, expectedBaseTypeName);
            }
        }
        /// <summary>
        /// Round trip sql_variant value using SqlBulkCopy.
        /// </summary>
        private static void SendVariantBulkCopy(object paramValue, string expectedTypeName, string expectedBaseTypeName)
        {
            string bulkCopyTableName = DataTestUtility.GetUniqueNameForSqlServer("bulkDest");

            // Fetch reader using type.
            using (SqlDataReader dr = GetReaderForVariant(paramValue, false))
            {
                using (SqlConnection connBulk = new SqlConnection(s_connStr))
                {
                    connBulk.Open();

                    ExecuteSQL(connBulk, "create table dbo.{0} (f1 sql_variant)", bulkCopyTableName);
                    try
                    {
                        // Perform bulk copy to target.
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connBulk))
                        {
                            bulkCopy.BulkCopyTimeout = 60;
                            bulkCopy.BatchSize = 1;
                            bulkCopy.DestinationTableName = bulkCopyTableName;
                            bulkCopy.WriteToServer(dr);
                        }

                        // Verify target.
                        using (SqlCommand cmd = connBulk.CreateCommand())
                        {
                            cmd.CommandText = string.Format("select f1, sql_variant_property(f1,'BaseType') as BaseType from {0}", bulkCopyTableName);
                            using (SqlDataReader drVerify = cmd.ExecuteReader())
                            {
                                VerifyReader("SendVariantBulkCopy[SqlDataReader]", drVerify, expectedTypeName, expectedBaseTypeName);
                            }
                        }

                        // Truncate target table for next pass.
                        ExecuteSQL(connBulk, "truncate table {0}", bulkCopyTableName);

                        // Send using DataTable as source.
                        DataTable t = new DataTable();
                        t.Columns.Add("f1", typeof(object));
                        t.Rows.Add(new object[] { paramValue });

                        // Perform bulk copy to target.
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connBulk))
                        {
                            bulkCopy.BulkCopyTimeout = 60;
                            bulkCopy.BatchSize = 1;
                            bulkCopy.DestinationTableName = bulkCopyTableName;
                            bulkCopy.WriteToServer(t, DataRowState.Added);
                        }

                        // Verify target.
                        using (SqlCommand cmd = connBulk.CreateCommand())
                        {
                            cmd.CommandText = string.Format("select f1, sql_variant_property(f1,'BaseType') as BaseType from {0}", bulkCopyTableName);
                            using (SqlDataReader drVerify = cmd.ExecuteReader())
                            {
                                VerifyReader("SendVariantBulkCopy[DataTable]", drVerify, expectedTypeName, expectedBaseTypeName);
                            }
                        }

                        // Truncate target table for next pass.
                        ExecuteSQL(connBulk, "truncate table {0}", bulkCopyTableName);

                        // Send using DataRow as source.
                        DataRow[] rowToSend = t.Select();

                        // Perform bulk copy to target.
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connBulk))
                        {
                            bulkCopy.BulkCopyTimeout = 60;
                            bulkCopy.BatchSize = 1;
                            bulkCopy.DestinationTableName = bulkCopyTableName;
                            bulkCopy.WriteToServer(rowToSend);
                        }

                        // Verify target.
                        using (SqlCommand cmd = connBulk.CreateCommand())
                        {
                            cmd.CommandText = string.Format("select f1, sql_variant_property(f1,'BaseType') as BaseType from {0}", bulkCopyTableName);
                            using (SqlDataReader drVerify = cmd.ExecuteReader())
                            {
                                VerifyReader("SendVariantBulkCopy[DataRow]", drVerify, expectedTypeName, expectedBaseTypeName);
                            }
                        }
                    }
                    finally
                    {
                        // Cleanup target table.
                        ExecuteSQL(connBulk, "drop table {0}", bulkCopyTableName);
                    }
                }
            }
        }
        /// <summary>
        /// Round trip sql_variant value using TVP.
        /// </summary>
        private static void SendVariantTvp(object paramValue, string expectedTypeName, string expectedBaseTypeName)
        {
            string tvpTypeName = DataTestUtility.GetUniqueNameForSqlServer("tvpVariant");

            using (SqlConnection connTvp = new SqlConnection(s_connStr))
            {
                connTvp.Open();

                ExecuteSQL(connTvp, "create type dbo.{0} as table (f1 sql_variant)", tvpTypeName);
                try
                {
                    // Send TVP using SqlMetaData.
                    SqlMetaData[] metadata = new SqlMetaData[1];
                    metadata[0] = new SqlMetaData("f1", SqlDbType.Variant);
                    SqlDataRecord[] record = new SqlDataRecord[1];
                    record[0] = new SqlDataRecord(metadata);
                    record[0].SetValue(0, paramValue);

                    using (SqlCommand cmd = connTvp.CreateCommand())
                    {
                        cmd.CommandText = "select f1, sql_variant_property(f1,'BaseType') as BaseType from @tvpParam";
                        SqlParameter p = cmd.Parameters.AddWithValue("@tvpParam", record);
                        p.SqlDbType = SqlDbType.Structured;
                        p.TypeName = string.Format("dbo.{0}", tvpTypeName);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            VerifyReader("SendVariantTvp[SqlMetaData]", dr, expectedTypeName, expectedBaseTypeName);
                        }
                    }

                    // Send TVP using SqlDataReader.
                    using (SqlDataReader dr = GetReaderForVariant(paramValue, false))
                    {
                        using (SqlCommand cmd = connTvp.CreateCommand())
                        {
                            cmd.CommandText = "select f1, sql_variant_property(f1,'BaseType') as BaseType from @tvpParam";
                            SqlParameter p = cmd.Parameters.AddWithValue("@tvpParam", dr);
                            p.SqlDbType = SqlDbType.Structured;
                            p.TypeName = string.Format("dbo.{0}", tvpTypeName);
                            using (SqlDataReader dr2 = cmd.ExecuteReader())
                            {
                                VerifyReader("SendVariantTvp[SqlDataReader]", dr2, expectedTypeName, expectedBaseTypeName);
                            }
                        }
                    }
                }
                finally
                {
                    // Cleanup tvp type.
                    ExecuteSQL(connTvp, "drop type {0}", tvpTypeName);
                }
            }
        }
        /// <summary>
        /// Helper to execute t-sql with variable object name.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="formatSql">Format string using {0} to designate where to place objectName</param>
        /// <param name="objectName">Variable object name for t-sql</param>
        private static void ExecuteSQL(SqlConnection conn, string formatSql, string objectName)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format(formatSql, objectName);
                cmd.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Simple helper to execute t-sql.
        /// </summary>
        private static void ExecuteSQL(SqlConnection conn, string sql)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
}