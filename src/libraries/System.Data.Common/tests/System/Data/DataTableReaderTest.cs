// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System.Data.Common;
using System.Collections;

using Xunit;

namespace System.Data.Tests
{
    public class DataTableReaderTest
    {
        private DataTable _dt;

        public DataTableReaderTest()
        {
            _dt = new DataTable("test");
            _dt.Columns.Add("id", typeof(int));
            _dt.Columns.Add("name", typeof(string));
            _dt.PrimaryKey = new DataColumn[] { _dt.Columns["id"] };

            _dt.Rows.Add(new object[] { 1, "mono 1" });
            _dt.Rows.Add(new object[] { 2, "mono 2" });
            _dt.Rows.Add(new object[] { 3, "mono 3" });

            _dt.AcceptChanges();
        }

        #region Positive Tests
        [Fact]
        public void CtorTest()
        {
            _dt.Rows[1].Delete();
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                int i = 0;
                while (reader.Read())
                    i++;
                reader.Close();

                Assert.Equal(2, i);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void RowInAccessibleTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
           {
               DataTableReader reader = new DataTableReader(_dt);
               try
               {
                   reader.Read();
                   reader.Read(); // 2nd row
                   _dt.Rows[1].Delete();
                   string value = reader[1].ToString();
               }
               finally
               {
                   if (reader != null && !reader.IsClosed)
                       reader.Close();
               }
           });
        }

        [Fact]
        public void IgnoreDeletedRowsDynamicTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                _dt.Rows[1].Delete();
                reader.Read(); // it should be 3rd row
                string value = reader[0].ToString();
                Assert.Equal("3", value);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void SeeTheModifiedTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                _dt.Rows[1]["name"] = "mono changed";
                reader.Read();
                string value = reader[1].ToString();
                Assert.Equal("mono changed", value);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void SchemaTest()
        {
            DataTable another = new DataTable("another");
            another.Columns.Add("x", typeof(string));

            another.Rows.Add(new object[] { "test 1" });
            another.Rows.Add(new object[] { "test 2" });
            another.Rows.Add(new object[] { "test 3" });

            DataTableReader reader = new DataTableReader(new DataTable[] { _dt, another });
            try
            {
                DataTable schema = reader.GetSchemaTable();

                Assert.Equal(_dt.Columns.Count, schema.Rows.Count);
                Assert.Equal(_dt.Columns[1].DataType.ToString(), schema.Rows[1]["DataType"].ToString());

                reader.NextResult(); //schema should change here
                schema = reader.GetSchemaTable();

                Assert.Equal(another.Columns.Count, schema.Rows.Count);
                Assert.Equal(another.Columns[0].DataType.ToString(), schema.Rows[0]["DataType"].ToString());
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void MultipleResultSetsTest()
        {
            DataTable dt1 = new DataTable("test2");
            dt1.Columns.Add("x", typeof(string));
            dt1.Rows.Add(new object[] { "test" });
            dt1.Rows.Add(new object[] { "test1" });
            dt1.AcceptChanges();

            DataTable[] collection = new DataTable[] { _dt, dt1 };

            DataTableReader reader = new DataTableReader(collection);
            try
            {
                int i = 0;
                do
                {
                    while (reader.Read())
                        i++;
                } while (reader.NextResult());

                Assert.Equal(5, i);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void GetTest()
        {
            _dt.Columns.Add("nullint", typeof(int));
            _dt.Rows[0]["nullint"] = 333;

            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read();

                int ordinal = reader.GetOrdinal("nullint");
                // Get by name
                Assert.Equal(1, (int)reader["id"]);
                Assert.Equal(333, reader.GetInt32(ordinal));
                Assert.Equal("Int32", reader.GetDataTypeName(ordinal));
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void CloseTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataTableReader reader = new DataTableReader(_dt);
                try
                {
                    int i = 0;
                    while (reader.Read() && i < 1)
                        i++;
                    reader.Close();
                    reader.Read();
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void GetOrdinalTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                Assert.Equal(1, reader.GetOrdinal("name"));
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }
        #endregion // Positive Tests


        #region Negative Tests
        [Fact]
        public void NoRowsTest()
        {
            _dt.Rows.Clear();
            _dt.AcceptChanges();

            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                Assert.False(reader.Read());
                Assert.False(reader.NextResult());
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void NoTablesTest()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataTableReader reader = new DataTableReader(new DataTable[] { });
                try
                {
                    reader.Read();
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void ReadAfterClosedTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataTableReader reader = new DataTableReader(_dt);
                try
                {
                    reader.Read();
                    reader.Close();
                    reader.Read();
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void AccessAfterClosedTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataTableReader reader = new DataTableReader(_dt);
                try
                {
                    reader.Read();
                    reader.Close();
                    int i = (int)reader[0];
                    i++; // to suppress warning
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void AccessBeforeReadTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataTableReader reader = new DataTableReader(_dt);
                try
                {
                    int i = (int)reader[0];
                    i++; // to suppress warning
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void InvalidIndexTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                DataTableReader reader = new DataTableReader(_dt);
                try
                {
                    reader.Read();
                    int i = (int)reader[90]; // kidding, ;-)
                    i++; // to suppress warning
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void DontSeeTheEarlierRowsTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read(); // second row

                // insert a row at position 0
                DataRow r = _dt.NewRow();
                r[0] = 0;
                r[1] = "adhi bagavan";
                _dt.Rows.InsertAt(r, 0);

                Assert.Equal(2, reader.GetInt32(0));
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void AddBeforePointTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read(); // second row
                DataRow r = _dt.NewRow();
                r[0] = 0;
                r[1] = "adhi bagavan";
                _dt.Rows.InsertAt(r, 0);
                _dt.Rows.Add(new object[] { 4, "mono 4" }); // should not affect the counter
                Assert.Equal(2, (int)reader[0]);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void AddAtPointTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read(); // second row
                DataRow r = _dt.NewRow();
                r[0] = 0;
                r[1] = "same point";
                _dt.Rows.InsertAt(r, 1);
                _dt.Rows.Add(new object[] { 4, "mono 4" }); // should not affect the counter
                Assert.Equal(2, (int)reader[0]);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void DeletePreviousAndAcceptChangesTest()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read(); // second row
                _dt.Rows[0].Delete();
                _dt.AcceptChanges();
                Assert.Equal(2, (int)reader[0]);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void DeleteCurrentAndAcceptChangesTest2()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read(); // second row
                _dt.Rows[1].Delete(); // delete row, where reader points to
                _dt.AcceptChanges(); // accept the action
                Assert.Equal(1, (int)reader[0]);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void DeleteFirstCurrentAndAcceptChangesTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataTableReader reader = new DataTableReader(_dt);
                try
                {
                    reader.Read(); // first row
                    _dt.Rows[0].Delete(); // delete row, where reader points to
                    _dt.AcceptChanges(); // accept the action
                    Assert.Equal(2, (int)reader[0]);
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                }
            });
        }

        [Fact]
        public void DeleteLastAndAcceptChangesTest2()
        {
            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read(); // second row
                reader.Read(); // third row
                _dt.Rows[2].Delete(); // delete row, where reader points to
                _dt.AcceptChanges(); // accept the action
                Assert.Equal(2, (int)reader[0]);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void ClearTest()
        {
            DataTableReader reader = null;
            try
            {
                reader = new DataTableReader(_dt);
                reader.Read(); // first row
                reader.Read(); // second row
                _dt.Clear();
                try
                {
                    int i = (int)reader[0];
                    i++; // suppress warning
                    Assert.False(true);
                }
                catch (RowNotInTableException) { }

                // clear and add test
                reader.Close();
                reader = new DataTableReader(_dt);
                reader.Read(); // first row
                reader.Read(); // second row
                _dt.Clear();
                _dt.Rows.Add(new object[] { 8, "mono 8" });
                _dt.AcceptChanges();
                bool success = reader.Read();
                Assert.False(success);

                // clear when reader is not read yet
                reader.Close();
                reader = new DataTableReader(_dt);
                _dt.Clear();
                _dt.Rows.Add(new object[] { 8, "mono 8" });
                _dt.AcceptChanges();
                success = reader.Read();
                Assert.True(success);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }

        [Fact]
        public void MultipleDeleteTest()
        {
            _dt.Rows.Add(new object[] { 4, "mono 4" });
            _dt.Rows.Add(new object[] { 5, "mono 5" });
            _dt.Rows.Add(new object[] { 6, "mono 6" });
            _dt.Rows.Add(new object[] { 7, "mono 7" });
            _dt.Rows.Add(new object[] { 8, "mono 8" });
            _dt.AcceptChanges();

            DataTableReader reader = new DataTableReader(_dt);
            try
            {
                reader.Read(); // first row
                reader.Read();
                reader.Read();
                reader.Read();
                reader.Read();

                _dt.Rows[3].Delete();
                _dt.Rows[1].Delete();
                _dt.Rows[2].Delete();
                _dt.Rows[0].Delete();
                _dt.Rows[6].Delete();
                _dt.AcceptChanges();

                Assert.Equal(5, (int)reader[0]);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }
        }
        #endregion // Negative Tests

        [Fact]
        public void TestSchemaTable()
        {
            var ds = new DataSet();
            DataTable testTable = new DataTable("TestTable1");
            DataTable testTable1 = new DataTable();

            testTable.Namespace = "TableNamespace";

            testTable1.Columns.Add("col1", typeof(int));
            testTable1.Columns.Add("col2", typeof(int));
            ds.Tables.Add(testTable);
            ds.Tables.Add(testTable1);

            //create a col for standard datatype

            testTable.Columns.Add("col_string");
            testTable.Columns.Add("col_string_fixed");
            testTable.Columns["col_string_fixed"].MaxLength = 10;
            testTable.Columns.Add("col_int", typeof(int));
            testTable.Columns.Add("col_decimal", typeof(decimal));
            testTable.Columns.Add("col_datetime", typeof(DateTime));
            testTable.Columns.Add("col_float", typeof(float));

            // Check for col constraints/properties
            testTable.Columns.Add("col_readonly").ReadOnly = true;

            testTable.Columns.Add("col_autoincrement", typeof(long)).AutoIncrement = true;
            testTable.Columns["col_autoincrement"].AutoIncrementStep = 5;
            testTable.Columns["col_autoincrement"].AutoIncrementSeed = 10;

            testTable.Columns.Add("col_pk");
            testTable.PrimaryKey = new DataColumn[] { testTable.Columns["col_pk"] };

            testTable.Columns.Add("col_unique");
            testTable.Columns["col_unique"].Unique = true;

            testTable.Columns.Add("col_defaultvalue");
            testTable.Columns["col_defaultvalue"].DefaultValue = "DefaultValue";

            testTable.Columns.Add("col_expression_local", typeof(int));
            testTable.Columns["col_expression_local"].Expression = "col_int*5";

            ds.Relations.Add("rel", new DataColumn[] { testTable1.Columns["col1"] },
                    new DataColumn[] { testTable.Columns["col_int"] }, false);
            testTable.Columns.Add("col_expression_ext");
            testTable.Columns["col_expression_ext"].Expression = "parent.col2";

            testTable.Columns.Add("col_namespace");
            testTable.Columns["col_namespace"].Namespace = "ColumnNamespace";

            testTable.Columns.Add("col_mapping");
            testTable.Columns["col_mapping"].ColumnMapping = MappingType.Attribute;

            DataTable schemaTable = testTable.CreateDataReader().GetSchemaTable();

            Assert.Equal(25, schemaTable.Columns.Count);
            Assert.Equal(testTable.Columns.Count, schemaTable.Rows.Count);

            //True for all rows
            for (int i = 0; i < schemaTable.Rows.Count; ++i)
            {
                Assert.Equal(testTable.TableName, schemaTable.Rows[i]["BaseTableName"]);
                Assert.Equal(ds.DataSetName, schemaTable.Rows[i]["BaseCatalogName"]);
                Assert.Equal(DBNull.Value, schemaTable.Rows[i]["BaseSchemaName"]);
                Assert.Equal(schemaTable.Rows[i]["BaseColumnName"], schemaTable.Rows[i]["ColumnName"]);
                Assert.False((bool)schemaTable.Rows[i]["IsRowVersion"]);
            }

            Assert.Equal("col_string", schemaTable.Rows[0]["ColumnName"]);
            Assert.Equal(typeof(string), schemaTable.Rows[0]["DataType"]);
            Assert.Equal(-1, schemaTable.Rows[0]["ColumnSize"]);
            Assert.Equal(0, schemaTable.Rows[0]["ColumnOrdinal"]);
            // ms.net contradicts documented behavior
            Assert.False((bool)schemaTable.Rows[0]["IsLong"]);

            Assert.Equal("col_string_fixed", schemaTable.Rows[1]["ColumnName"]);
            Assert.Equal(typeof(string), schemaTable.Rows[1]["DataType"]);
            Assert.Equal(10, schemaTable.Rows[1]["ColumnSize"]);
            Assert.Equal(1, schemaTable.Rows[1]["ColumnOrdinal"]);
            Assert.False((bool)schemaTable.Rows[1]["IsLong"]);

            Assert.Equal("col_int", schemaTable.Rows[2]["ColumnName"]);
            Assert.Equal(typeof(int), schemaTable.Rows[2]["DataType"]);
            Assert.Equal(DBNull.Value, schemaTable.Rows[2]["NumericPrecision"]);
            Assert.Equal(DBNull.Value, schemaTable.Rows[2]["NumericScale"]);
            Assert.Equal(-1, schemaTable.Rows[2]["ColumnSize"]);
            Assert.Equal(2, schemaTable.Rows[2]["ColumnOrdinal"]);

            Assert.Equal("col_decimal", schemaTable.Rows[3]["ColumnName"]);
            Assert.Equal(typeof(decimal), schemaTable.Rows[3]["DataType"]);
            // When are the Precision and Scale Values set ? 
            Assert.Equal(DBNull.Value, schemaTable.Rows[3]["NumericPrecision"]);
            Assert.Equal(DBNull.Value, schemaTable.Rows[3]["NumericScale"]);
            Assert.Equal(-1, schemaTable.Rows[3]["ColumnSize"]);
            Assert.Equal(3, schemaTable.Rows[3]["ColumnOrdinal"]);

            Assert.Equal("col_datetime", schemaTable.Rows[4]["ColumnName"]);
            Assert.Equal(typeof(DateTime), schemaTable.Rows[4]["DataType"]);
            Assert.Equal(4, schemaTable.Rows[4]["ColumnOrdinal"]);

            Assert.Equal("col_float", schemaTable.Rows[5]["ColumnName"]);
            Assert.Equal(typeof(float), schemaTable.Rows[5]["DataType"]);
            Assert.Equal(5, schemaTable.Rows[5]["ColumnOrdinal"]);
            Assert.Equal(DBNull.Value, schemaTable.Rows[5]["NumericPrecision"]);
            Assert.Equal(DBNull.Value, schemaTable.Rows[5]["NumericScale"]);
            Assert.Equal(-1, schemaTable.Rows[5]["ColumnSize"]);

            Assert.Equal("col_readonly", schemaTable.Rows[6]["ColumnName"]);
            Assert.True((bool)schemaTable.Rows[6]["IsReadOnly"]);

            Assert.Equal("col_autoincrement", schemaTable.Rows[7]["ColumnName"]);
            Assert.True((bool)schemaTable.Rows[7]["IsAutoIncrement"]);
            Assert.Equal(10L, schemaTable.Rows[7]["AutoIncrementSeed"]);
            Assert.Equal(5L, schemaTable.Rows[7]["AutoIncrementStep"]);
            Assert.False((bool)schemaTable.Rows[7]["IsReadOnly"]);

            Assert.Equal("col_pk", schemaTable.Rows[8]["ColumnName"]);
            Assert.True((bool)schemaTable.Rows[8]["IsKey"]);
            Assert.True((bool)schemaTable.Rows[8]["IsUnique"]);

            Assert.Equal("col_unique", schemaTable.Rows[9]["ColumnName"]);
            Assert.True((bool)schemaTable.Rows[9]["IsUnique"]);

            Assert.Equal("col_defaultvalue", schemaTable.Rows[10]["ColumnName"]);
            Assert.Equal("DefaultValue", schemaTable.Rows[10]["DefaultValue"]);

            Assert.Equal("col_expression_local", schemaTable.Rows[11]["ColumnName"]);
            Assert.Equal("col_int*5", schemaTable.Rows[11]["Expression"]);
            Assert.True((bool)schemaTable.Rows[11]["IsReadOnly"]);

            // if expression depends on an external col, then set Expression as null..
            Assert.Equal("col_expression_ext", schemaTable.Rows[12]["ColumnName"]);
            Assert.Equal(DBNull.Value, schemaTable.Rows[12]["Expression"]);
            Assert.True((bool)schemaTable.Rows[12]["IsReadOnly"]);

            Assert.Equal("col_namespace", schemaTable.Rows[13]["ColumnName"]);
            Assert.Equal("TableNamespace", schemaTable.Rows[13]["BaseTableNamespace"]);
            Assert.Equal("TableNamespace", schemaTable.Rows[12]["BaseColumnNamespace"]);
            Assert.Equal("ColumnNamespace", schemaTable.Rows[13]["BaseColumnNamespace"]);

            Assert.Equal("col_mapping", schemaTable.Rows[14]["ColumnName"]);
            Assert.Equal(MappingType.Element, (MappingType)schemaTable.Rows[13]["ColumnMapping"]);
            Assert.Equal(MappingType.Attribute, (MappingType)schemaTable.Rows[14]["ColumnMapping"]);
        }

        [Fact]
        public void TestExceptionIfSchemaChanges()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1");
            DataTableReader rdr = table.CreateDataReader();
            Assert.Equal(1, rdr.GetSchemaTable().Rows.Count);

            table.Columns[0].ColumnName = "newcol1";
            try
            {
                rdr.GetSchemaTable();
                Assert.False(true);
            }
            catch (InvalidOperationException)
            {
                // Never premise English.
                //Assert.Equal ("Schema of current DataTable '" + table.TableName + 
                //		"' in DataTableReader has changed, DataTableReader is invalid.", e.Message, "#1");
            }

            rdr = table.CreateDataReader();
            rdr.GetSchemaTable(); //no exception
            table.Columns.Add("col2");
            try
            {
                rdr.GetSchemaTable();
                Assert.False(true);
            }
            catch (InvalidOperationException)
            {
                // Never premise English.
                //Assert.Equal ("Schema of current DataTable '" + table.TableName + 
                //		"' in DataTableReader has changed, DataTableReader is invalid.", e.Message, "#1");
            }
        }

        [Fact]
        public void EnumeratorTest()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Rows.Add(new object[] { 0 });
            table.Rows.Add(new object[] { 1 });

            DataTableReader rdr = table.CreateDataReader();
            IEnumerator enmr = rdr.GetEnumerator();

            table.Rows.Add(new object[] { 2 });
            table.Rows.RemoveAt(0);

            //Test if the Enumerator is stable
            int i = 1;
            while (enmr.MoveNext())
            {
                DbDataRecord rec = (DbDataRecord)enmr.Current;
                Assert.Equal(i, rec.GetInt32(0));
                i++;
            }
        }

        [Fact]
        public void GetCharsTest()
        {
            _dt.Columns.Add("col2", typeof(char[]));

            _dt.Rows.Clear();
            _dt.Rows.Add(new object[] { 1, "string", "string".ToCharArray() });
            _dt.Rows.Add(new object[] { 2, "string1", null });
            DataTableReader rdr = _dt.CreateDataReader();

            rdr.Read();

            try
            {
                rdr.GetChars(1, 0, null, 0, 10);
                Assert.False(true);
            }
            catch (InvalidCastException e)
            {
                // Never premise English.
                //Assert.Equal ("Unable to cast object of type 'System.String'" +
                //	" to type 'System.Char[]'.", e.Message, "#1");
            }
            char[] char_arr = null;
            long len = 0;

            len = rdr.GetChars(2, 0, null, 0, 0);
            Assert.Equal(6, len);

            char_arr = new char[len];
            len = rdr.GetChars(2, 0, char_arr, 0, 0);
            Assert.Equal(0, len);

            len = rdr.GetChars(2, 0, null, 0, 0);
            char_arr = new char[len + 2];
            len = rdr.GetChars(2, 0, char_arr, 2, 100);
            Assert.Equal(6, len);
            char[] val = (char[])rdr.GetValue(2);
            for (int i = 0; i < len; ++i)
                Assert.Equal(val[i], char_arr[i + 2]);
        }

        [Fact]
        public void GetProviderSpecificTests()
        {
            DataTableReader rdr = _dt.CreateDataReader();
            while (rdr.Read())
            {
                object[] values = new object[rdr.FieldCount];
                object[] pvalues = new object[rdr.FieldCount];
                rdr.GetValues(values);
                rdr.GetProviderSpecificValues(pvalues);

                for (int i = 0; i < rdr.FieldCount; ++i)
                {
                    Assert.Equal(values[i], pvalues[i]);
                    Assert.Equal(rdr.GetValue(i), rdr.GetProviderSpecificValue(i));
                    Assert.Equal(rdr.GetFieldType(i), rdr.GetProviderSpecificFieldType(i));
                }
            }
        }

        [Fact]
        public void GetNameTest()
        {
            DataTableReader rdr = _dt.CreateDataReader();
            for (int i = 0; i < _dt.Columns.Count; ++i)
                Assert.Equal(_dt.Columns[i].ColumnName, rdr.GetName(i));
        }
    }
}

