// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace System.Data.OleDb.Tests
{
    public class OleDbDataReaderTests : OleDbTestBase
    {
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ExecuteNonQuery_TableNameWithoutCsvExtension_Throws()
        {
            command.CommandText =
                @"CREATE TABLE TableNameWithoutCsvExtension (
                    SomeInt32 INT,
                    SomeString NVARCHAR(100))";
            Assert.Throws<OleDbException>(() => command.ExecuteNonQuery());
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void InvalidRowIndex()
        {
            RunTest((reader) => {
                reader.Read();
                Assert.True(reader.HasRows);
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(4, schema.Rows.Count);
                var exception = Record.Exception(() => reader.GetString(5));
                Assert.NotNull(exception);
                Assert.IsType<IndexOutOfRangeException>(exception);
                Assert.Equal(
                    "Index was outside the bounds of the array.",
                    exception.Message);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void NonExistentColumn()
        {
            RunTest((reader) => {
                reader.Read();
                Assert.True(reader.HasRows);
                var exception = Record.Exception(() => reader["NonExistentColumn"]);
                Assert.NotNull(exception);
                Assert.IsType<IndexOutOfRangeException>(exception);
                Assert.Equal(
                    "NonExistentColumn",
                    exception.Message);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetValues()
        {
            RunTest((reader) => {
                reader.Read();
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                Assert.Equal(123, values[0]);
                Assert.Equal("XYZ", values[1]);
                Assert.Equal(42.2, values[2]);
                Assert.Equal(48.3f, values[3]);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void EmptyReader_SchemaOnly_EmptyReader()
        {
            RunTest((reader) => {
                reader.Read();
                Assert.False(reader.HasRows);
                var exception = Record.Exception(() => reader.GetString(1));
                Assert.NotNull(exception);
                Assert.IsType<InvalidOperationException>(exception);
                Assert.Equal(
                    "No data exists for the row/column.",
                    exception.Message);

                var values = new object[1];
                exception = Record.Exception(() => reader.GetValues(values));
                Assert.NotNull(exception);
                Assert.IsType<InvalidOperationException>(exception);
                Assert.Equal(
                    "No data exists for the row/column.",
                    exception.Message);
            }, schemaOnly: true);
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchemaTable_SchemaOnly_GetsColumnInfo()
        {
            RunTest((reader) => {
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(4, schema.Rows.Count);  
                Assert.Equal("CustomerName", schema.Rows[1].Field<String>("ColumnName"));
                Assert.Equal(typeof(string), schema.Rows[1].Field<Type>("DataType"));
                Assert.Equal(40, schema.Rows[1].Field<int>("ColumnSize"));
            }, schemaOnly: true);
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchemaTable_ColumnName_Success()
        {
            RunTest((reader) => {
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(4, schema.Rows.Count);  
                Assert.Equal("CustomerID", schema.Rows[0].Field<String>("ColumnName"));
                Assert.Equal("CustomerName", schema.Rows[1].Field<String>("ColumnName"));
                Assert.Equal("SingleAmount", schema.Rows[2].Field<String>("ColumnName"));
                Assert.Equal("RealAmount", schema.Rows[3].Field<String>("ColumnName"));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchemaTable_DataType_Success()
        {
            RunTest((reader) => {
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(4, schema.Rows.Count);
                Assert.Equal(typeof(int), schema.Rows[0].Field<Type>("DataType"));
                Assert.Equal(typeof(string), schema.Rows[1].Field<Type>("DataType"));
                Assert.Equal(typeof(double), schema.Rows[2].Field<Type>("DataType"));
                Assert.Equal(typeof(float), schema.Rows[3].Field<Type>("DataType"));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Read_GetInt32_Success()
        {
            RunTest((reader) => {
                Assert.True(reader.HasRows);
                reader.Read();
                Assert.Throws<InvalidCastException>(() => reader.GetInt16(0));
                Assert.Equal(123, reader.GetInt32(0));
                Assert.Throws<InvalidCastException>(() => reader.GetInt64(0));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Read_GetString_Success()
        {
            RunTest((reader) => {
                Assert.True(reader.HasRows);
                reader.Read();
                Assert.Equal("XYZ", reader.GetString(1));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Read_GetDouble_Success()
        {
            RunTest((reader) => {
                Assert.True(reader.HasRows);
                reader.Read();
                Assert.Equal(42.2, reader.GetDouble(2));
                Assert.Throws<InvalidCastException>(() => reader.GetDouble(3));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Read_GetFloat_Success()
        {
            RunTest((reader) => {
                Assert.True(reader.HasRows);
                reader.Read();
                Assert.Throws<InvalidCastException>(() => reader.GetFloat(2));
                Assert.Equal(48.3f, reader.GetFloat(3));
            });
        }

        [OuterLoop]
        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [InlineData(0)]
        [InlineData(1)]
        public void GetChar_MethodNotSupported_Throws(int ordinal)
        {
            RunTest((reader) => {
                Assert.Throws<NotSupportedException>(() => reader.GetChar(ordinal));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetValues_Null_Throws()
        {
            RunTest((reader) => {
                Assert.Throws<ArgumentNullException>(() => reader.GetValues(null));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Depth_IsClosed_Throws()
        {
            RunTest((reader) => {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => reader.Depth);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void FieldCount_IsClosed_Throws()
        {
            RunTest((reader) => {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => reader.FieldCount);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void VisibleFieldCount_IsClosed_Throws()
        {
            RunTest((reader) => {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => reader.VisibleFieldCount);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void HasRows_IsClosed_Throws()
        {
            RunTest((reader) => {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => reader.HasRows);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchemaTable_IsClosed_Throws()
        {
            RunTest((reader) => {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => reader.GetSchemaTable());
            });
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetEnumerator_BadType_Throws()
        {
            Assert.Throws<ArgumentException>(() => OleDbEnumerator.GetEnumerator(typeof(Exception)));
        }

        private void RunTest(Action<OleDbDataReader> testAction, bool schemaOnly = false, [CallerMemberName] string memberName = null)
        {
            string tableName = Helpers.GetTableName(memberName);
            Assert.False(File.Exists(Path.Combine(TestDirectory, tableName)));
            command.CommandText =
                @"CREATE TABLE " + tableName + @" (
                    CustomerID INT,
                    CustomerName NVARCHAR(40), 
                    SingleAmount FLOAT, 
                    RealAmount REAL);";
            command.ExecuteNonQuery();
            Assert.True(File.Exists(Path.Combine(TestDirectory, tableName)));

            if (!schemaOnly)
            {
                command.CommandText =
                    @"INSERT INTO " + tableName + @" ( 
                        CustomerID,
                        CustomerName, 
                        SingleAmount, 
                        RealAmount)
                    VALUES ( 123, 'XYZ', @value, @realValue );";
#pragma warning disable 612,618
                command.Parameters.Add("@value", 42.2);
                command.Parameters.Add("@realValue", 48.3);
#pragma warning restore 612,618
                command.ExecuteNonQuery();
            }

            command.CommandText = "SELECT CustomerID, CustomerName, SingleAmount, RealAmount FROM " + tableName;
            using (OleDbDataReader reader = schemaOnly ? command.ExecuteReader(CommandBehavior.SchemaOnly) : command.ExecuteReader())
            {
                testAction(reader);
            }
            command.CommandText = @"DROP TABLE " + tableName;
            command.ExecuteNonQuery();
        }
    }
}