// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Data.OleDb.Tests
{
    [Collection("System.Data.OleDb")] // not let tests run in parallel
    public class OleDbDataReaderTests : OleDbTestBase
    {
        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void FieldCount_NoMetadata_ReturnsZero()
        {
            command.CommandText = @"CREATE TABLE sample.csv;";
            using (OleDbDataReader reader = command.ExecuteReader())
            {
                Assert.Equal(0, reader.FieldCount);
                Assert.Equal(0, reader.VisibleFieldCount);
            }
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void InvalidRowIndex()
        {
            RunTest((reader) => {
                reader.Read();
                Assert.True(reader.HasRows);
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(5, schema.Rows.Count);
                AssertExtensions.Throws<IndexOutOfRangeException>(
                    () => reader.GetString(6), 
                    "Index was outside the bounds of the array.");
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void NonExistentColumn()
        {
            RunTest((reader) => {
                reader.Read();
                Assert.True(reader.HasRows);
                object obj;
                AssertExtensions.Throws<IndexOutOfRangeException>(
                    () => obj = reader["NonExistentColumn"], "NonExistentColumn");
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
                Assert.Equal(new DateTime(2015, 1, 11, 12, 54, 1), values[4]);
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void EmptyReader_SchemaOnly_EmptyReader()
        {
            const string expectedMessage = "No data exists for the row/column.";
            RunTest((reader) => {
                reader.Read();
                Assert.False(reader.HasRows);
                AssertExtensions.Throws<InvalidOperationException>(() => reader.GetString(1), expectedMessage);
                AssertExtensions.Throws<InvalidOperationException>(() => reader.GetValues(new object[1]), expectedMessage);
                AssertExtensions.Throws<InvalidOperationException>(() => reader.GetData(0), expectedMessage);
            }, schemaOnly: true);
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchemaTable_SchemaOnly_GetsColumnInfo()
        {
            RunTest((reader) => {
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(5, schema.Rows.Count);  
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
                Assert.Equal(5, schema.Rows.Count);  
                Assert.Equal("CustomerID", schema.Rows[0].Field<String>("ColumnName"));
                Assert.Equal("CustomerName", schema.Rows[1].Field<String>("ColumnName"));
                Assert.Equal("SingleAmount", schema.Rows[2].Field<String>("ColumnName"));
                Assert.Equal("RealAmount", schema.Rows[3].Field<String>("ColumnName"));
                Assert.Equal("DateChecked", schema.Rows[4].Field<String>("ColumnName"));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchemaTable_DataType_Success()
        {
            RunTest((reader) => {
                DataTable schema = reader.GetSchemaTable();
                Assert.Equal(5, schema.Rows.Count);
                Assert.Equal(typeof(int), schema.Rows[0].Field<Type>("DataType"));
                Assert.Equal(typeof(string), schema.Rows[1].Field<Type>("DataType"));
                Assert.Equal(typeof(double), schema.Rows[2].Field<Type>("DataType"));
                Assert.Equal(typeof(float), schema.Rows[3].Field<Type>("DataType"));
                Assert.Equal(typeof(DateTime), schema.Rows[4].Field<Type>("DataType"));
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
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Read_GetDateTime_Success()
        {
            RunTest((reader) => {
                Assert.True(reader.HasRows);
                reader.Read();
                Assert.Throws<InvalidCastException>(() => reader.GetFloat(2));
                Assert.Equal(new DateTime(2015, 1, 11, 12, 54, 1), reader.GetDateTime(4));
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
        public void IsClosed_CallReaderApis_Throws()
        {
            RunTest((reader) => {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => reader.Depth);
                Assert.Throws<InvalidOperationException>(() => reader.FieldCount);
                Assert.Throws<InvalidOperationException>(() => reader.VisibleFieldCount);
                Assert.Throws<InvalidOperationException>(() => reader.HasRows);
                Assert.Throws<InvalidOperationException>(() => reader.GetSchemaTable());
                Assert.Throws<InvalidOperationException>(() => reader.NextResult());
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void InnerReader_OpenReaderExists_Throws()
        {
            RunTest((reader) => {
                AssertExtensions.Throws<InvalidOperationException>(
                    () => command.ExecuteReader(),
                    "There is already an open DataReader associated with this Command which must be closed first."
                );
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
                    RealAmount REAL,
                    DateChecked DATETIME);";
            command.ExecuteNonQuery();
            Assert.True(File.Exists(Path.Combine(TestDirectory, tableName)));

            if (!schemaOnly)
            {
                command.CommandText =
                    @"INSERT INTO " + tableName + @" ( 
                        CustomerID,
                        CustomerName, 
                        SingleAmount, 
                        RealAmount,
                        DateChecked)
                    VALUES ( 123, 'XYZ', @value, @realValue, '01/11/2015 12:54:01' );";
#pragma warning disable 612,618
                command.Parameters.Add("@value", 42.2);
                command.Parameters.Add("@realValue", 48.3);
#pragma warning restore 612,618
                command.ExecuteNonQuery();
            }

            command.CommandText = "SELECT CustomerID, CustomerName, SingleAmount, RealAmount, DateChecked FROM " + tableName;
            using (OleDbDataReader reader = schemaOnly ? command.ExecuteReader(CommandBehavior.SchemaOnly) : command.ExecuteReader())
            {
                testAction(reader);
            }
            command.CommandText = @"DROP TABLE " + tableName;
            command.ExecuteNonQuery();
        }
    }
}
