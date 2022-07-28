// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;
using Xunit.Sdk;

namespace System.Data.Tests
{
    // !! Important !!
    // These tests manipulate global state, so they cannot be run in parallel with one another.
    // We rely on xunit's default behavior of not parallelizing unit tests declared on the same
    // test class: see https://xunit.net/docs/running-tests-in-parallel.html.
    public class RestrictedTypeHandlingTests
    {
        private const string AppDomainDataSetDefaultAllowedTypesKey = "System.Data.DataSetDefaultAllowedTypes";

        private static readonly Type[] _alwaysAllowedTypes = new Type[]
        {
            /* primitives */
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(string),
            typeof(Guid),
            typeof(SqlBinary),
            typeof(SqlBoolean),
            typeof(SqlByte),
            typeof(SqlBytes),
            typeof(SqlChars),
            typeof(SqlDateTime),
            typeof(SqlDecimal),
            typeof(SqlDouble),
            typeof(SqlGuid),
            typeof(SqlInt16),
            typeof(SqlInt32),
            typeof(SqlInt64),
            typeof(SqlMoney),
            typeof(SqlSingle),
            typeof(SqlString),

            /* non-primitives, but common */
            typeof(object),
            typeof(Type),
            typeof(BigInteger),
            typeof(Uri),

            /* frequently used System.Drawing types */
            typeof(Color),
            typeof(Point),
            typeof(PointF),
            typeof(Rectangle),
            typeof(RectangleF),
            typeof(Size),
            typeof(SizeF),

            /* to test that enums are allowed */
            typeof(StringComparison),
        };

        public static IEnumerable<object[]> AllowedTypes()
        {
            foreach (Type type in _alwaysAllowedTypes)
            {
                yield return new object[] { type }; // T
                yield return new object[] { type.MakeArrayType() }; // T[] (SZArray)
                yield return new object[] { type.MakeArrayType().MakeArrayType() }; // T[][] (jagged array)
                yield return new object[] { typeof(List<>).MakeGenericType(type) }; // List<T>
            }
        }

        public static IEnumerable<object[]> ForbiddenTypes()
        {
            // StringBuilder isn't in the allow list

            yield return new object[] { typeof(StringBuilder) };
            yield return new object[] { typeof(StringBuilder[]) };

            // multi-dim arrays and non-sz arrays are forbidden

            yield return new object[] { typeof(int[,]) };
            yield return new object[] { Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 1 }).GetType() };

            // HashSet<T> isn't in the allow list

            yield return new object[] { typeof(HashSet<int>) };

            // DataSet / DataTable / SqlXml aren't in the allow list

            yield return new object[] { typeof(DataSet) };
            yield return new object[] { typeof(DataTable) };
            yield return new object[] { typeof(SqlXml) };

            // Enum, Array, and other base types aren't allowed

            yield return new object[] { typeof(Enum) };
            yield return new object[] { typeof(Array) };
            yield return new object[] { typeof(ValueType) };
            yield return new object[] { typeof(void) };
        }

        [Theory]
        [MemberData(nameof(AllowedTypes))]
        public void DataTable_ReadXml_AllowsKnownTypes(Type type)
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", type);

            string asXml = WriteXmlWithSchema(table.WriteXml);

            // Act

            table = ReadXml<DataTable>(asXml);

            // Assert

            Assert.Equal("MyTable", table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("MyColumn", table.Columns[0].ColumnName);
            Assert.Equal(type, table.Columns[0].DataType);
        }

        [Theory]
        [MemberData(nameof(ForbiddenTypes))]
        public void DataTable_ReadXml_ForbidsUnknownTypes(Type type)
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", type);

            string asXml = WriteXmlWithSchema(table.WriteXml);

            // Act & assert

            Assert.Throws<InvalidOperationException>(() => ReadXml<DataTable>(asXml));
        }

        [Fact]
        public void DataTable_ReadXml_HandlesXmlSerializableTypes()
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", typeof(object));
            table.Rows.Add(new MyXmlSerializableClass());

            string asXml = WriteXmlWithSchema(table.WriteXml, XmlWriteMode.IgnoreSchema);

            // Act & assert
            // MyXmlSerializableClass shouldn't be allowed as a member for a column
            // typed as 'object'.

            table.Rows.Clear();
            Assert.Throws<InvalidOperationException>(() => table.ReadXml(new StringReader(asXml)));
        }

        [Theory]
        [MemberData(nameof(ForbiddenTypes))]
        public void DataTable_ReadXmlSchema_AllowsUnknownTypes(Type type)
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", type);

            string asXml = WriteXmlWithSchema(table.WriteXml);

            // Act

            table = new DataTable();
            table.ReadXmlSchema(new StringReader(asXml));

            // Assert

            Assert.Equal("MyTable", table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("MyColumn", table.Columns[0].ColumnName);
            Assert.Equal(type, table.Columns[0].DataType);
        }

        [Fact]
        public void DataTable_HonorsGloballyDefinedAllowList()
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", typeof(MyCustomClass));

            string asXml = WriteXmlWithSchema(table.WriteXml);

            // Act & assert 1
            // First call should fail since MyCustomClass not allowed

            Assert.Throws<InvalidOperationException>(() => ReadXml<DataTable>(asXml));

            // Act & assert 2
            // Deserialization should succeed since it's now in the allow list

            try
            {
                AppDomain.CurrentDomain.SetData(AppDomainDataSetDefaultAllowedTypesKey, new Type[]
                {
                    typeof(MyCustomClass)
                });

                table = ReadXml<DataTable>(asXml);

                Assert.Equal("MyTable", table.TableName);
                Assert.Equal(1, table.Columns.Count);
                Assert.Equal("MyColumn", table.Columns[0].ColumnName);
                Assert.Equal(typeof(MyCustomClass), table.Columns[0].DataType);
            }
            finally
            {
                AppDomain.CurrentDomain.SetData(AppDomainDataSetDefaultAllowedTypesKey, null);
            }
        }

        [Fact]
        public void DataColumn_ConvertExpression_SubjectToAllowList_Success()
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", typeof(object), "CONVERT('42', 'System.Int32')");

            string asXml = WriteXmlWithSchema(table.WriteXml);

            // Act

            table = ReadXml<DataTable>(asXml);

            // Assert

            Assert.Equal("MyTable", table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("MyColumn", table.Columns[0].ColumnName);
            Assert.Equal(typeof(object), table.Columns[0].DataType);
            Assert.Equal("CONVERT('42', 'System.Int32')", table.Columns[0].Expression);
        }

        [Fact]
        public void DataColumn_ConvertExpression_SubjectToAllowList_Failure()
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("ColumnA", typeof(object));
            table.Columns.Add("ColumnB", typeof(object), "CONVERT(ColumnA, 'System.Text.StringBuilder')");

            string asXml = WriteXmlWithSchema(table.WriteXml);

            // Act
            // 'StringBuilder' isn't in the allow list, but we're not yet hydrating the Type
            // object so we won't check it just yet.

            table = ReadXml<DataTable>(asXml);

            // Assert - the CONVERT function node should have captured the active allow list
            // at construction and should apply it now.

            Assert.Throws<InvalidOperationException>(() => table.Rows.Add(new StringBuilder()));
        }

        [Theory]
        [MemberData(nameof(AllowedTypes))]
        public void DataSet_ReadXml_AllowsKnownTypes(Type type)
        {
            // Arrange

            DataSet set = new DataSet("MySet");
            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", type);
            set.Tables.Add(table);

            string asXml = WriteXmlWithSchema(set.WriteXml);

            // Act

            table = null;
            set = ReadXml<DataSet>(asXml);

            // Assert

            Assert.Equal("MySet", set.DataSetName);
            Assert.Equal(1, set.Tables.Count);

            table = set.Tables[0];
            Assert.Equal("MyTable", table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("MyColumn", table.Columns[0].ColumnName);
            Assert.Equal(type, table.Columns[0].DataType);
        }

        [Theory]
        [MemberData(nameof(ForbiddenTypes))]
        public void DataSet_ReadXml_ForbidsUnknownTypes(Type type)
        {
            // Arrange

            DataSet set = new DataSet("MySet");
            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", type);
            set.Tables.Add(table);

            string asXml = WriteXmlWithSchema(set.WriteXml);

            // Act & assert

            Assert.Throws<InvalidOperationException>(() => ReadXml<DataSet>(asXml));
        }

        [Theory]
        [MemberData(nameof(ForbiddenTypes))]
        public void DataSet_ReadXmlSchema_AllowsUnknownTypes(Type type)
        {
            // Arrange

            DataSet set = new DataSet("MySet");
            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", type);
            set.Tables.Add(table);

            string asXml = WriteXmlWithSchema(set.WriteXml);

            // Act

            set = new DataSet();
            set.ReadXmlSchema(new StringReader(asXml));

            // Assert

            Assert.Equal("MySet", set.DataSetName);
            Assert.Equal(1, set.Tables.Count);

            table = set.Tables[0];
            Assert.Equal("MyTable", table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("MyColumn", table.Columns[0].ColumnName);
            Assert.Equal(type, table.Columns[0].DataType);
        }

        [Fact]
        public void SerializationGuard_BlocksFileAccessOnDeserialize()
        {
            // Arrange

            DataTable table = new DataTable("MyTable");
            table.Columns.Add("MyColumn", typeof(MyCustomClassThatWritesToAFile));
            table.Rows.Add(new MyCustomClassThatWritesToAFile());

            string asXml = WriteXmlWithSchema(table.WriteXml);
            table.Rows.Clear();

            // Act & assert

            Assert.Throws<SerializationException>(() => table.ReadXml(new StringReader(asXml)));
        }

        private static string WriteXmlWithSchema(Action<TextWriter, XmlWriteMode> writeMethod, XmlWriteMode xmlWriteMode = XmlWriteMode.WriteSchema)
        {
            StringWriter writer = new StringWriter();
            writeMethod(writer, xmlWriteMode);
            return writer.ToString();
        }

        private static T ReadXml<T>(string xml) where T : IXmlSerializable, new()
        {
            T newObj = new T();
            newObj.ReadXml(new XmlTextReader(new StringReader(xml)) { XmlResolver = null }); // suppress DTDs, same as runtime code
            return newObj;
        }

        private sealed class MyCustomClass
        {
        }

        public sealed class MyXmlSerializableClass : IXmlSerializable
        {
            public XmlSchema GetSchema()
            {
                return null;
            }

            public void ReadXml(XmlReader reader)
            {
                return; // no-op
            }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("MyElement", "MyValue");
            }
        }

        private sealed class MyCustomClassThatWritesToAFile : IXmlSerializable
        {
            public XmlSchema GetSchema()
            {
                return null;
            }

            public void ReadXml(XmlReader reader)
            {
                // This should be called within a Serialization Guard scope, so the file write
                // should fail.

                string tempPath = Path.GetTempFileName();
                File.WriteAllText(tempPath, "This better not be written...");
                File.Delete(tempPath);
                throw new UnreachableException("Unreachable code (SerializationGuard should have kicked in)");
            }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("MyElement", "MyValue");
            }
        }
    }
}
