// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
            DataTable Table = new DataTable("test_table");
            DataTable Table2 = new DataTable("test_table2");
            DataColumnCollection Cols = Table.Columns;
            DataColumn C = null;

            try
            {
                Cols.Add(C);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            C = new DataColumn("test");
            Cols.Add(C);

            try
            {
                Cols.Add(C);
                Assert.False(true);
            }
            catch (ArgumentException e)
            {
                //				Assert.Equal (typeof (ArgumentException), e.GetType ());
                //				Assert.Equal ("Column 'test' already belongs to this or another DataTable.", e.Message);
            }

            try
            {
                Table2.Columns.Add(C);
                Assert.False(true);
            }
            catch (ArgumentException e)
            {
                //				Assert.Equal (typeof (ArgumentException), e.GetType ());
                //				Assert.Equal ("Column 'test' already belongs to this or another DataTable.", e.Message);
            }

            DataColumn C2 = new DataColumn("test");

            try
            {
                Cols.Add(C2);
                Assert.False(true);
            }
            catch (DuplicateNameException e)
            {
                //				Assert.Equal (typeof (DuplicateNameException), e.GetType ());
                //				Assert.Equal ("A DataColumn named 'test' already belongs to this DataTable.", e.Message);
            }

            try
            {
                Cols.Add("test2", typeof(string), "substring ('fdsafewq', 2)");
                Assert.False(true);
            }
            catch (InvalidExpressionException e)
            {
                //				Assert.True (e is InvalidExpressionException);
                //				Assert.Equal ("Expression 'substring ('fdsafewq', 2)' is invalid.", e.Message);
            }
        }

        [Fact]
        public void AddRange()
        {
            DataTable Table = new DataTable("test_table");
            DataTable Table2 = new DataTable("test_table2");
            DataColumnCollection Cols = Table.Columns;
            DataColumn C = null;
            DataColumn[] ColArray = new DataColumn[2];

            C = new DataColumn("test1");
            ColArray[0] = C;

            C = new DataColumn("test2");
            C.AllowDBNull = false;
            C.Caption = "Test_caption";
            C.DataType = typeof(XmlReader);
            ColArray[1] = C;

            Cols.AddRange(ColArray);

            C = Cols[0];
            Assert.True(C.AllowDBNull);
            Assert.False(C.AutoIncrement);
            Assert.Equal(0L, C.AutoIncrementSeed);
            Assert.Equal(1L, C.AutoIncrementStep);
            Assert.Equal("test1", C.Caption);
            Assert.Equal("Element", C.ColumnMapping.ToString());
            Assert.Equal("test1", C.ColumnName);
            Assert.Null(C.Container);
            Assert.Equal(typeof(string), C.DataType);
            Assert.Equal(DBNull.Value, C.DefaultValue);
            Assert.False(C.DesignMode);
            Assert.Equal("", C.Expression);
            Assert.Equal(0, C.ExtendedProperties.Count);
            Assert.Equal(-1, C.MaxLength);
            Assert.Equal("", C.Namespace);
            Assert.Equal(0, C.Ordinal);
            Assert.Equal("", C.Prefix);
            Assert.False(C.ReadOnly);
            Assert.Null(C.Site);
            Assert.Equal("test_table", C.Table.TableName);
            Assert.Equal("test1", C.ToString());
            Assert.False(C.Unique);

            C = Cols[1];
            Assert.False(C.AllowDBNull);
            Assert.False(C.AutoIncrement);
            Assert.Equal(0L, C.AutoIncrementSeed);
            Assert.Equal(1L, C.AutoIncrementStep);
            Assert.Equal("Test_caption", C.Caption);
            Assert.Equal("Element", C.ColumnMapping.ToString());
            Assert.Equal("test2", C.ColumnName);
            Assert.Null(C.Container);
            Assert.Equal(typeof(XmlReader), C.DataType);
            Assert.Equal(DBNull.Value, C.DefaultValue);
            Assert.False(C.DesignMode);
            Assert.Equal("", C.Expression);
            Assert.Equal(0, C.ExtendedProperties.Count);
            Assert.Equal(-1, C.MaxLength);
            Assert.Equal("", C.Namespace);
            Assert.Equal(1, C.Ordinal);
            Assert.Equal("", C.Prefix);
            Assert.False(C.ReadOnly);
            Assert.Null(C.Site);
            Assert.Equal("test_table", C.Table.TableName);
            Assert.Equal("test2", C.ToString());
            Assert.False(C.Unique);
        }

        [Fact]
        public void CanRemove()
        {
            DataTable Table = new DataTable("test_table");
            DataTable Table2 = new DataTable("test_table_2");
            DataColumnCollection Cols = Table.Columns;
            DataColumn C = new DataColumn("test1");
            Cols.Add();

            // LAMESPEC: MSDN says that if C doesn't belong to Cols
            // Exception is thrown.
            Assert.False(Cols.CanRemove(C));

            Cols.Add(C);
            Assert.True(Cols.CanRemove(C));

            C = new DataColumn();
            C.Expression = "test1 + 2";
            Cols.Add(C);

            C = Cols["test2"];
            Assert.False(Cols.CanRemove(C));

            C = new DataColumn("t");
            Table2.Columns.Add(C);
            DataColumnCollection Cols2 = Table2.Columns;
            Assert.True(Cols2.CanRemove(C));

            DataSet Set = new DataSet();
            Set.Tables.Add(Table);
            Set.Tables.Add(Table2);
            DataRelation Rel = new DataRelation("Rel", Table.Columns[0], Table2.Columns[0]);
            Set.Relations.Add(Rel);

            Assert.False(Cols2.CanRemove(C));
            Assert.False(Cols.CanRemove(null));
        }

        [Fact]
        public void Clear()
        {
            DataTable Table = new DataTable("test_table");
            DataTable Table2 = new DataTable("test_table2");
            DataSet Set = new DataSet();
            Set.Tables.Add(Table);
            Set.Tables.Add(Table2);
            DataColumnCollection Cols = Table.Columns;
            DataColumnCollection Cols2 = Table2.Columns;

            Cols.Add();
            Cols.Add("testi");

            Cols.Clear();
            Assert.Equal(0, Cols.Count);

            Cols.Add();
            Cols.Add("testi");
            Cols2.Add();
            Cols2.Add();

            DataRelation Rel = new DataRelation("Rel", Cols[0], Cols2[0]);
            Set.Relations.Add(Rel);
            try
            {
                Cols.Clear();
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Cannot remove this column, because it is part of the parent key for relationship Rel.", e.Message);
            }
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
            DataTable Table = new DataTable("test_table");
            DataColumnCollection Cols = Table.Columns;

            Cols.Add("test");
            Cols.Add("tesT2");

            Assert.True(Cols.Contains("test"));
            Assert.False(Cols.Contains("_test"));
            Assert.True(Cols.Contains("TEST"));
            Table.CaseSensitive = true;
            Assert.True(Cols.Contains("TEST"));
            Assert.True(Cols.Contains("test2"));
            Assert.False(Cols.Contains("_test2"));
            Assert.True(Cols.Contains("TEST2"));
        }

        [Fact]
        public void CopyTo()
        {
            DataTable Table = new DataTable("test_table");
            DataColumnCollection Cols = Table.Columns;

            Cols.Add("test");
            Cols.Add("test2");
            Cols.Add("test3");
            Cols.Add("test4");

            DataColumn[] array = new DataColumn[4];
            Cols.CopyTo(array, 0);
            Assert.Equal(4, array.Length);
            Assert.Equal("test", array[0].ColumnName);
            Assert.Equal("test2", array[1].ColumnName);
            Assert.Equal("test3", array[2].ColumnName);
            Assert.Equal("test4", array[3].ColumnName);

            array = new DataColumn[6];
            Cols.CopyTo(array, 2);
            Assert.Equal(6, array.Length);
            Assert.Equal("test", array[2].ColumnName);
            Assert.Equal("test2", array[3].ColumnName);
            Assert.Equal("test3", array[4].ColumnName);
            Assert.Equal("test4", array[5].ColumnName);
            Assert.Null(array[0]);
            Assert.Null(array[1]);
        }

        [Fact]
        public void Equals()
        {
            DataTable Table = new DataTable("test_table");
            DataTable Table2 = new DataTable("test_table");
            DataColumnCollection Cols = Table.Columns;
            DataColumnCollection Cols2 = Table2.Columns;

            Assert.False(Cols.Equals(Cols2));
            Assert.False(Cols2.Equals(Cols));
            Assert.False(object.Equals(Cols, Cols2));
            Assert.True(Cols.Equals(Cols));
            Assert.True(Cols2.Equals(Cols2));
            Assert.True(object.Equals(Cols2, Cols2));
        }

        [Fact]
        public void IndexOf()
        {
            DataTable Table = new DataTable("test_table");
            DataColumnCollection Cols = Table.Columns;

            Cols.Add("test");
            Cols.Add("test2");
            Cols.Add("test3");
            Cols.Add("test4");

            Assert.Equal(0, Cols.IndexOf("test"));
            Assert.Equal(1, Cols.IndexOf("TEST2"));
            Table.CaseSensitive = true;
            Assert.Equal(1, Cols.IndexOf("TEST2"));

            Assert.Equal(3, Cols.IndexOf(Cols[3]));
            DataColumn C = new DataColumn("error");
            Assert.Equal(-1, Cols.IndexOf(C));
            Assert.Equal(-1, Cols.IndexOf("_error_"));
        }

        [Fact]
        public void Remove()
        {
            DataTable Table = new DataTable("test_table");
            DataColumnCollection Cols = Table.Columns;

            Cols.Add("test");
            Cols.Add("test2");
            Cols.Add("test3");
            Cols.Add("test4");

            Assert.Equal(4, Cols.Count);
            Cols.Remove("test2");
            Assert.Equal(3, Cols.Count);
            Cols.Remove("TEST3");
            Assert.Equal(2, Cols.Count);

            try
            {
                Cols.Remove("_test_");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Column '_test_' does not belong to table test_table.", e.Message);
            }

            Cols.Add();
            Cols.Add();
            Cols.Add();
            Cols.Add();

            Assert.Equal(6, Cols.Count);
            Cols.Remove(Cols[0]);
            Cols.Remove(Cols[0]);
            Assert.Equal(4, Cols.Count);
            Assert.Equal("Column1", Cols[0].ColumnName);

            try
            {
                Cols.Remove(new DataColumn("Column10"));
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Cannot remove a column that doesn't belong to this table.", e.Message);
            }

            Cols.Add();
            Cols.Add();
            Cols.Add();
            Cols.Add();

            Assert.Equal(8, Cols.Count);
            Cols.RemoveAt(7);
            Cols.RemoveAt(1);
            Cols.RemoveAt(0);
            Cols.RemoveAt(0);
            Assert.Equal(4, Cols.Count);
            Assert.Equal("Column4", Cols[0].ColumnName);
            Assert.Equal("Column5", Cols[1].ColumnName);

            try
            {
                Cols.RemoveAt(10);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Cannot find column 10.", e.Message);
            }
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
        public void ToStringTest()
        {
            DataTable Table = new DataTable("test_table");
            DataColumnCollection Cols = Table.Columns;

            Cols.Add("test");
            Cols.Add("test2");
            Cols.Add("test3");
            Assert.Equal("System.Data.DataColumnCollection", Cols.ToString());
        }

        [Fact]
        public void CaseSensitiveIndexOfTest()
        {
            DataTable dt = new DataTable("TestCaseSensitiveIndexOf");
            dt.Columns.Add("nom_colonne1", typeof(string));
            dt.Columns.Add("NOM_COLONNE1", typeof(string));
            dt.Columns.Remove("nom_colonne1");
            int i = dt.Columns.IndexOf("nom_colonne1");
            Assert.Equal(0, dt.Columns.IndexOf("nom_colonne1"));
        }
    }
}
