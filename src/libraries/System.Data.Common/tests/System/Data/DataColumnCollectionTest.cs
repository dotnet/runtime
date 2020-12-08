// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) Copyright 2002 Franklin Wise
// (C) Copyright 2003 Ville Palo
// (C) Copyright 2003 Martin Willemoes Hansen

// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

using System.Xml;
using Xunit;

namespace System.Data.Tests
{
    public class DataColumnCollectionTest
    {
        [Fact]
        public void Add()
        {
            var Table = new DataTable("test_table");
            DataColumnCollection cols = Table.Columns;
            DataColumn c = null;
            cols.Add();
            cols.Add();

            for (int i = 0; i < 2; i++)
            {
                c = cols[i];
                Assert.True(c.AllowDBNull);
                Assert.False(c.AutoIncrement);
                Assert.Equal(0L, c.AutoIncrementSeed);
                Assert.Equal(1L, c.AutoIncrementStep);
                Assert.Equal($"Column{i + 1}", c.Caption);
                Assert.Equal("Element", c.ColumnMapping.ToString());
                Assert.Equal($"Column{i + 1}", c.ColumnName);
                Assert.Null(c.Container);
                Assert.Equal(typeof(string), c.DataType);
                Assert.Equal(DBNull.Value, c.DefaultValue);
                Assert.False(c.DesignMode);
                Assert.Equal("", c.Expression);
                Assert.Equal(0, c.ExtendedProperties.Count);
                Assert.Equal(-1, c.MaxLength);
                Assert.Equal("", c.Namespace);
                Assert.Equal(i, c.Ordinal);
                Assert.Equal("", c.Prefix);
                Assert.False(c.ReadOnly);
                Assert.Null(c.Site);
                Assert.Equal("test_table", c.Table.TableName);
                Assert.Equal($"Column{i + 1}", c.ToString());
                Assert.False(c.Unique);
            }

            cols.Add("test1", typeof(int), "");
            cols.Add("test2", typeof(string), "Column1 + Column2");

            c = cols[2];
            Assert.True(c.AllowDBNull);
            Assert.False(c.AutoIncrement);
            Assert.Equal(0L, c.AutoIncrementSeed);
            Assert.Equal(1L, c.AutoIncrementStep);
            Assert.Equal("test1", c.Caption);
            Assert.Equal("Element", c.ColumnMapping.ToString());
            Assert.Equal("test1", c.ColumnName);
            Assert.Null(c.Container);
            Assert.Equal(typeof(int), c.DataType);
            Assert.Equal(DBNull.Value, c.DefaultValue);
            Assert.False(c.DesignMode);
            Assert.Equal("", c.Expression);
            Assert.Equal(0, c.ExtendedProperties.Count);
            Assert.Equal(-1, c.MaxLength);
            Assert.Equal("", c.Namespace);
            Assert.Equal(2, c.Ordinal);
            Assert.Equal("", c.Prefix);
            Assert.False(c.ReadOnly);
            Assert.Null(c.Site);
            Assert.Equal("test_table", c.Table.TableName);
            Assert.Equal("test1", c.ToString());
            Assert.False(c.Unique);

            c = cols[3];
            Assert.True(c.AllowDBNull);
            Assert.False(c.AutoIncrement);
            Assert.Equal(0L, c.AutoIncrementSeed);
            Assert.Equal(1L, c.AutoIncrementStep);
            Assert.Equal("test2", c.Caption);
            Assert.Equal("Element", c.ColumnMapping.ToString());
            Assert.Equal("test2", c.ColumnName);
            Assert.Null(c.Container);
            Assert.Equal(typeof(string), c.DataType);
            Assert.Equal(DBNull.Value, c.DefaultValue);
            Assert.False(c.DesignMode);
            Assert.Equal("Column1 + Column2", c.Expression);
            Assert.Equal(0, c.ExtendedProperties.Count);
            Assert.Equal(-1, c.MaxLength);
            Assert.Equal("", c.Namespace);
            Assert.Equal(3, c.Ordinal);
            Assert.Equal("", c.Prefix);
            Assert.True(c.ReadOnly);
            Assert.Null(c.Site);
            Assert.Equal("test_table", c.Table.TableName);
            Assert.Equal("test2 + Column1 + Column2", c.ToString());
            Assert.False(c.Unique);

            c = new DataColumn("test3", typeof(int));
            cols.Add(c);

            c = cols[4];
            Assert.True(c.AllowDBNull);
            Assert.False(c.AutoIncrement);
            Assert.Equal(0L, c.AutoIncrementSeed);
            Assert.Equal(1L, c.AutoIncrementStep);
            Assert.Equal("test3", c.Caption);
            Assert.Equal("Element", c.ColumnMapping.ToString());
            Assert.Equal("test3", c.ColumnName);
            Assert.Null(c.Container);
            Assert.Equal(typeof(int), c.DataType);
            Assert.Equal(DBNull.Value, c.DefaultValue);
            Assert.False(c.DesignMode);
            Assert.Equal("", c.Expression);
            Assert.Equal(0, c.ExtendedProperties.Count);
            Assert.Equal(-1, c.MaxLength);
            Assert.Equal("", c.Namespace);
            Assert.Equal(4, c.Ordinal);
            Assert.Equal("", c.Prefix);
            Assert.False(c.ReadOnly);
            Assert.Null(c.Site);
            Assert.Equal("test_table", c.Table.TableName);
            Assert.Equal("test3", c.ToString());
            Assert.False(c.Unique);
        }

        [Fact] // Add (String)
        public void Add3_ColumnName_Empty()
        {
            DataTable table = new DataTable();
            DataColumnCollection cols = table.Columns;
            DataColumn col;

            col = cols.Add(string.Empty);
            Assert.Equal(1, cols.Count);
            Assert.Equal("Column1", col.ColumnName);
            Assert.Same(table, col.Table);

            col = cols.Add(string.Empty);
            Assert.Equal(2, cols.Count);
            Assert.Equal("Column2", col.ColumnName);
            Assert.Same(table, col.Table);

            cols.RemoveAt(1);

            col = cols.Add(string.Empty);
            Assert.Equal(2, cols.Count);
            Assert.Equal("Column2", col.ColumnName);
            Assert.Same(table, col.Table);

            cols.Clear();

            col = cols.Add(string.Empty);
            Assert.Equal(1, cols.Count);
            Assert.Equal("Column1", col.ColumnName);
            Assert.Same(table, col.Table);
        }

        [Fact] // Add (String)
        public void Add3_ColumnName_Null()
        {
            DataTable table = new DataTable();
            DataColumnCollection cols = table.Columns;
            DataColumn col;

            col = cols.Add((string)null);
            Assert.Equal(1, cols.Count);
            Assert.Equal("Column1", col.ColumnName);
            Assert.Same(table, col.Table);

            col = cols.Add((string)null);
            Assert.Equal(2, cols.Count);
            Assert.Equal("Column2", col.ColumnName);
            Assert.Same(table, col.Table);

            cols.RemoveAt(1);

            col = cols.Add((string)null);
            Assert.Equal(2, cols.Count);
            Assert.Equal("Column2", col.ColumnName);
            Assert.Same(table, col.Table);

            cols.Clear();

            col = cols.Add((string)null);
            Assert.Equal(1, cols.Count);
            Assert.Equal("Column1", col.ColumnName);
            Assert.Same(table, col.Table);
        }

        [Fact]
        public void AddExceptions()
        {
            DataTable table = new DataTable("test_table");
            DataTable table2 = new DataTable("test_table2");
            DataColumnCollection cols = table.Columns;
            DataColumn c = null;

            Assert.Throws<ArgumentNullException>(() => cols.Add(c));

            c = new DataColumn("test");
            cols.Add(c);

            // Column 'test' already belongs to this or another DataTable.
            Assert.Throws<ArgumentException>(() => cols.Add(c));

            // Column 'test' already belongs to this or another DataTable.
            Assert.Throws<ArgumentException>(() => table2.Columns.Add(c));

            DataColumn c2 = new DataColumn("test");

            // A DataColumn named 'test' already belongs to this DataTable.
            Assert.Throws<DuplicateNameException>(() => cols.Add(c2));

            // EvaluateException : Invalid number of arguments: function substring().
            Assert.ThrowsAny<InvalidExpressionException>(() => cols.Add("test2", typeof(string), "substring ('fdsafewq', 2)"));
        }

        [Fact]
        public void AddRange()
        {
            DataTable table = new DataTable("test_table");
            DataColumnCollection cols = table.Columns;
            DataColumn c = null;
            DataColumn[] colArray = new DataColumn[2];

            c = new DataColumn("test1");
            colArray[0] = c;

            c = new DataColumn("test2");
            c.AllowDBNull = false;
            c.Caption = "Test_caption";
            c.DataType = typeof(XmlReader);
            colArray[1] = c;

            cols.AddRange(colArray);

            c = cols[0];
            Assert.True(c.AllowDBNull);
            Assert.False(c.AutoIncrement);
            Assert.Equal(0L, c.AutoIncrementSeed);
            Assert.Equal(1L, c.AutoIncrementStep);
            Assert.Equal("test1", c.Caption);
            Assert.Equal("Element", c.ColumnMapping.ToString());
            Assert.Equal("test1", c.ColumnName);
            Assert.Null(c.Container);
            Assert.Equal(typeof(string), c.DataType);
            Assert.Equal(DBNull.Value, c.DefaultValue);
            Assert.False(c.DesignMode);
            Assert.Equal("", c.Expression);
            Assert.Equal(0, c.ExtendedProperties.Count);
            Assert.Equal(-1, c.MaxLength);
            Assert.Equal("", c.Namespace);
            Assert.Equal(0, c.Ordinal);
            Assert.Equal("", c.Prefix);
            Assert.False(c.ReadOnly);
            Assert.Null(c.Site);
            Assert.Equal("test_table", c.Table.TableName);
            Assert.Equal("test1", c.ToString());
            Assert.False(c.Unique);

            c = cols[1];
            Assert.False(c.AllowDBNull);
            Assert.False(c.AutoIncrement);
            Assert.Equal(0L, c.AutoIncrementSeed);
            Assert.Equal(1L, c.AutoIncrementStep);
            Assert.Equal("Test_caption", c.Caption);
            Assert.Equal("Element", c.ColumnMapping.ToString());
            Assert.Equal("test2", c.ColumnName);
            Assert.Null(c.Container);
            Assert.Equal(typeof(XmlReader), c.DataType);
            Assert.Equal(DBNull.Value, c.DefaultValue);
            Assert.False(c.DesignMode);
            Assert.Equal("", c.Expression);
            Assert.Equal(0, c.ExtendedProperties.Count);
            Assert.Equal(-1, c.MaxLength);
            Assert.Equal("", c.Namespace);
            Assert.Equal(1, c.Ordinal);
            Assert.Equal("", c.Prefix);
            Assert.False(c.ReadOnly);
            Assert.Null(c.Site);
            Assert.Equal("test_table", c.Table.TableName);
            Assert.Equal("test2", c.ToString());
            Assert.False(c.Unique);
        }

        [Fact]
        public void CanRemove()
        {
            DataTable table = new DataTable("test_table");
            DataTable table2 = new DataTable("test_table_2");
            DataColumnCollection cols = table.Columns;
            DataColumn c = new DataColumn("test1");
            cols.Add();

            // MSDN says that if C doesn't belong to Cols
            // Exception is thrown.
            Assert.False(cols.CanRemove(c));

            cols.Add(c);
            Assert.True(cols.CanRemove(c));

            c = new DataColumn();
            c.Expression = "test1 + 2";
            cols.Add(c);

            c = cols["test2"];
            Assert.False(cols.CanRemove(c));

            c = new DataColumn("t");
            table2.Columns.Add(c);
            DataColumnCollection Cols2 = table2.Columns;
            Assert.True(Cols2.CanRemove(c));

            DataSet Set = new DataSet();
            Set.Tables.Add(table);
            Set.Tables.Add(table2);
            DataRelation Rel = new DataRelation("Rel", table.Columns[0], table2.Columns[0]);
            Set.Relations.Add(Rel);

            Assert.False(Cols2.CanRemove(c));
            Assert.False(cols.CanRemove(null));
        }

        [Fact]
        public void Clear()
        {
            DataTable table = new DataTable("test_table");
            DataTable table2 = new DataTable("test_table2");
            DataSet set = new DataSet();
            set.Tables.Add(table);
            set.Tables.Add(table2);
            DataColumnCollection cols = table.Columns;
            DataColumnCollection cols2 = table2.Columns;

            cols.Add();
            cols.Add("testi");

            cols.Clear();
            Assert.Equal(0, cols.Count);

            cols.Add();
            cols.Add("testi");
            cols2.Add();
            cols2.Add();

            DataRelation rel = new DataRelation("Rel", cols[0], cols2[0]);
            set.Relations.Add(rel);
            // Cannot remove this column, because it is part of the parent key for relationship Rel.
            Assert.Throws<ArgumentException>(() => cols.Clear());
        }

        [Fact]
        public void Clear_ExpressionColumn()
        {
            DataTable table = new DataTable("test");
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int), "sum(col1)");

            //shudnt throw an exception.
            table.Columns.Clear();
            Assert.Equal(0, table.Columns.Count);
        }

        [Fact]
        public void Contains()
        {
            DataTable table = new DataTable("test_table");
            DataColumnCollection cols = table.Columns;

            cols.Add("test");
            cols.Add("tesT2");

            Assert.True(cols.Contains("test"));
            Assert.False(cols.Contains("_test"));
            Assert.True(cols.Contains("TEST"));
            table.CaseSensitive = true;
            Assert.True(cols.Contains("TEST"));
            Assert.True(cols.Contains("test2"));
            Assert.False(cols.Contains("_test2"));
            Assert.True(cols.Contains("TEST2"));
        }

        [Fact]
        public void CopyTo()
        {
            DataTable table = new DataTable("test_table");
            DataColumnCollection cols = table.Columns;

            cols.Add("test");
            cols.Add("test2");
            cols.Add("test3");
            cols.Add("test4");

            DataColumn[] array = new DataColumn[4];
            cols.CopyTo(array, 0);
            Assert.Equal(4, array.Length);
            Assert.Equal("test", array[0].ColumnName);
            Assert.Equal("test2", array[1].ColumnName);
            Assert.Equal("test3", array[2].ColumnName);
            Assert.Equal("test4", array[3].ColumnName);

            array = new DataColumn[6];
            cols.CopyTo(array, 2);
            Assert.Equal(6, array.Length);
            Assert.Equal("test", array[2].ColumnName);
            Assert.Equal("test2", array[3].ColumnName);
            Assert.Equal("test3", array[4].ColumnName);
            Assert.Equal("test4", array[5].ColumnName);
            Assert.Null(array[0]);
            Assert.Null(array[1]);
        }

        [Fact]
        public void EqualsTest()
        {
            DataTable table = new DataTable("test_table");
            DataTable table2 = new DataTable("test_table");
            DataColumnCollection cols = table.Columns;
            DataColumnCollection cols2 = table2.Columns;

            Assert.False(cols.Equals(cols2));
            Assert.False(cols2.Equals(cols));
        }

        [Fact]
        public void IndexOf()
        {
            DataTable table = new DataTable("test_table");
            DataColumnCollection cols = table.Columns;

            cols.Add("test");
            cols.Add("test2");
            cols.Add("test3");
            cols.Add("test4");

            Assert.Equal(0, cols.IndexOf("test"));
            Assert.Equal(1, cols.IndexOf("TEST2"));
            table.CaseSensitive = true;
            Assert.Equal(1, cols.IndexOf("TEST2"));

            Assert.Equal(3, cols.IndexOf(cols[3]));
            DataColumn C = new DataColumn("error");
            Assert.Equal(-1, cols.IndexOf(C));
            Assert.Equal(-1, cols.IndexOf("_error_"));
        }

        [Fact]
        public void Remove()
        {
            DataTable table = new DataTable("test_table");
            DataColumnCollection cols = table.Columns;

            cols.Add("test");
            cols.Add("test2");
            cols.Add("test3");
            cols.Add("test4");

            Assert.Equal(4, cols.Count);
            cols.Remove("test2");
            Assert.Equal(3, cols.Count);
            cols.Remove("TEST3");
            Assert.Equal(2, cols.Count);

            // Column '_test_' does not belong to table test_table.
            Assert.Throws<ArgumentException>(() => cols.Remove("_test_"));

            cols.Add();
            cols.Add();
            cols.Add();
            cols.Add();

            Assert.Equal(6, cols.Count);
            cols.Remove(cols[0]);
            cols.Remove(cols[0]);
            Assert.Equal(4, cols.Count);
            Assert.Equal("Column1", cols[0].ColumnName);

            // Cannot remove a column that doesn't belong to this table.
            Assert.Throws<ArgumentException>(() => cols.Remove(new DataColumn("Column10")));

            cols.Add();
            cols.Add();
            cols.Add();
            cols.Add();

            Assert.Equal(8, cols.Count);
            cols.RemoveAt(7);
            cols.RemoveAt(1);
            cols.RemoveAt(0);
            cols.RemoveAt(0);
            Assert.Equal(4, cols.Count);
            Assert.Equal("Column4", cols[0].ColumnName);
            Assert.Equal("Column5", cols[1].ColumnName);

            // Cannot find column 10.
            Assert.Throws<IndexOutOfRangeException>(() => cols.RemoveAt(10));
        }

        [Fact]
        public void Remove_Dep_Rel_Col()
        {
            var ds = new DataSet();
            ds.Tables.Add("test");
            ds.Tables.Add("test1");
            ds.Tables[0].Columns.Add("col1", typeof(int));
            ds.Tables[1].Columns.Add("col2", typeof(int));

            ds.Relations.Add("rel1", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables[0].Columns.RemoveAt(0));
        }

        [Fact]
        public void CaseSensitiveIndexOfTest()
        {
            DataTable dt = new DataTable("TestCaseSensitiveIndexOf");
            dt.Columns.Add("nom_colonne1", typeof(string));
            dt.Columns.Add("NOM_COLONNE1", typeof(string));
            dt.Columns.Remove("nom_colonne1");
            Assert.Equal(0, dt.Columns.IndexOf("nom_colonne1"));
        }
    }
}
