// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlServerTypesTest
    {
        [CheckConnStrSetupFact]
        public static void GetSchemaTableTest()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand cmd = new SqlCommand("select hierarchyid::Parse('/1/') as col0", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                {
                    DataTable schemaTable = reader.GetSchemaTable();
                    DataTestUtility.AssertEqualsWithDescription(1, schemaTable.Rows.Count, "Unexpected schema table row count.");

                    string columnName = (string)(string)schemaTable.Rows[0][schemaTable.Columns["ColumnName"]];
                    DataTestUtility.AssertEqualsWithDescription("col0", columnName, "Unexpected column name.");

                    string dataTypeName = (string)schemaTable.Rows[0][schemaTable.Columns["DataTypeName"]];
                    DataTestUtility.AssertEqualsWithDescription("Northwind.sys.hierarchyid", dataTypeName, "Unexpected data type name.");

                    string udtAssemblyName = (string)schemaTable.Rows[0][schemaTable.Columns["UdtAssemblyQualifiedName"]];
                    Assert.True(udtAssemblyName?.StartsWith("Microsoft.SqlServer.Types.SqlHierarchyId"), "Unexpected UDT assembly name: " + udtAssemblyName);
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void GetValueTest()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand cmd = new SqlCommand("select hierarchyid::Parse('/1/') as col0", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Assert.True(reader.Read());

                    // SqlHierarchyId is part of Microsoft.SqlServer.Types, which is not supported in Core
                    Assert.Throws<FileNotFoundException>(() => reader.GetValue(0));
                    Assert.Throws<FileNotFoundException>(() => reader.GetSqlValue(0));
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtZeroByte()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/') as col0";
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.False(reader.IsDBNull(0));
                    SqlBytes sqlBytes = reader.GetSqlBytes(0);
                    Assert.False(sqlBytes.IsNull, "Expected a zero length byte array");
                    Assert.True(sqlBytes.Length == 0, "Expected a zero length byte array");
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetSqlBytesSequentialAccess()
        {
            TestUdtSqlDataReaderGetSqlBytes(CommandBehavior.SequentialAccess);
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetSqlBytes()
        {
            TestUdtSqlDataReaderGetSqlBytes(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetSqlBytes(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    SqlBytes sqlBytes = null;

                    sqlBytes = reader.GetSqlBytes(0);
                    Assert.Equal("5ade", ToHexString(sqlBytes.Value));

                    sqlBytes = reader.GetSqlBytes(1);
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(sqlBytes.Value));

                    sqlBytes = reader.GetSqlBytes(2);
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(sqlBytes.Value));

                    if (behavior == CommandBehavior.Default)
                    {
                        sqlBytes = reader.GetSqlBytes(0);
                        Assert.Equal("5ade", ToHexString(sqlBytes.Value));
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetBytesSequentialAccess()
        {
            TestUdtSqlDataReaderGetBytes(CommandBehavior.SequentialAccess);
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetBytes()
        {
            TestUdtSqlDataReaderGetBytes(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetBytes(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    int byteCount = 0;
                    byte[] bytes = null;

                    byteCount = (int)reader.GetBytes(0, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(0, 0, bytes, 0, bytes.Length);
                    Assert.Equal("5ade", ToHexString(bytes));

                    byteCount = (int)reader.GetBytes(1, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(1, 0, bytes, 0, bytes.Length);
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(bytes));

                    byteCount = (int)reader.GetBytes(2, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(2, 0, bytes, 0, bytes.Length);
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(bytes));

                    if (behavior == CommandBehavior.Default)
                    {
                        byteCount = (int)reader.GetBytes(0, 0, null, 0, 0);
                        Assert.True(byteCount > 0);
                        bytes = new byte[byteCount];
                        reader.GetBytes(0, 0, bytes, 0, bytes.Length);
                        Assert.Equal("5ade", ToHexString(bytes));
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetStreamSequentialAccess()
        {
            TestUdtSqlDataReaderGetStream(CommandBehavior.SequentialAccess);
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetStream()
        {
            TestUdtSqlDataReaderGetStream(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetStream(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    MemoryStream buffer = null;
                    byte[] bytes = null;

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(0))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("5ade", ToHexString(bytes));

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(1))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(bytes));

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(2))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(bytes));

                    if (behavior == CommandBehavior.Default)
                    {
                        buffer = new MemoryStream();
                        using (Stream stream = reader.GetStream(0))
                        {
                            stream.CopyTo(buffer);
                        }
                        bytes = buffer.ToArray();
                        Assert.Equal("5ade", ToHexString(bytes));
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSchemaMetadata()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    ReadOnlyCollection<DbColumn> columns = reader.GetColumnSchema();

                    DbColumn column = null;

                    // Validate Microsoft.SqlServer.Types.SqlHierarchyId, Microsoft.SqlServer.Types, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
                    column = columns[0];
                    Assert.Equal(column.ColumnName, "col0");
                    Assert.True(column.DataTypeName.EndsWith(".hierarchyid"), $"Unexpected DataTypeName \"{column.DataTypeName}\"");
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlHierarchyId");

                    // Validate Microsoft.SqlServer.Types.SqlGeometry, Microsoft.SqlServer.Types, Version = 11.0.0.0, Culture = neutral, PublicKeyToken = 89845dcd8080cc91
                    column = columns[1];
                    Assert.Equal(column.ColumnName, "col1");
                    Assert.True(column.DataTypeName.EndsWith(".geometry"), $"Unexpected DataTypeName \"{column.DataTypeName}\"");
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlGeometry");

                    // Validate Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version = 11.0.0.0, Culture = neutral, PublicKeyToken = 89845dcd8080cc91
                    column = columns[2];
                    Assert.Equal(column.ColumnName, "col2");
                    Assert.True(column.DataTypeName.EndsWith(".geography"), $"Unexpected DataTypeName \"{column.DataTypeName}\"");
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlGeography");
                }
            }
        }

        private static void AssertSqlUdtAssemblyQualifiedName(string assemblyQualifiedName, string expectedType)
        {
            List<string> parts = assemblyQualifiedName.Split(',').Select(x => x.Trim()).ToList();

            string type = parts[0];
            string assembly = parts.Count < 2 ? string.Empty : parts[1];
            string version = parts.Count < 3 ? string.Empty : parts[2];
            string culture = parts.Count < 4 ? string.Empty : parts[3];
            string token = parts.Count < 5 ? string.Empty : parts[4];

            Assert.Equal(expectedType, type);
            Assert.Equal("Microsoft.SqlServer.Types", assembly);
            Assert.True(version.StartsWith("Version"));
            Assert.True(culture.StartsWith("Culture"));
            Assert.True(token.StartsWith("PublicKeyToken"));
        }

        private static string ToHexString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }
    }
}
