// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) Copyright 2002 Franklin Wise
// (C) Copyright 2003 Martin Willemoes Hansen
//

//
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
//


using Xunit;

namespace System.Data.Tests
{
    public class DataRowCollectionTest
    {
        private readonly DataTable _tbl = new DataTable();

        [Fact]
        public void AutoIncrement()
        {
            DataColumn col = new DataColumn("Auto");
            col.AutoIncrement = true;
            col.AutoIncrementSeed = 0;
            col.AutoIncrementStep = 1;

            _tbl.Columns.Add(col);
            _tbl.Rows.Add(_tbl.NewRow());

            Assert.Equal(0, Convert.ToInt32(_tbl.Rows[0]["Auto"]));

            _tbl.Rows.Add(_tbl.NewRow());
            Assert.Equal(1, Convert.ToInt32(_tbl.Rows[1]["Auto"]));

            col.AutoIncrement = false;
            Assert.Equal(1, Convert.ToInt32(_tbl.Rows[1]["Auto"]));

            _tbl.Rows.Add(_tbl.NewRow());
            Assert.Equal(DBNull.Value, _tbl.Rows[2]["Auto"]);

            col.AutoIncrement = true;
            col.AutoIncrementSeed = 10;
            col.AutoIncrementStep = 2;

            _tbl.Rows.Add(_tbl.NewRow());
            Assert.Equal(10, Convert.ToInt32(_tbl.Rows[3]["Auto"]));
            _tbl.Rows.Add(_tbl.NewRow());
            Assert.Equal(12, Convert.ToInt32(_tbl.Rows[4]["Auto"]));

            col = new DataColumn("Auto2");
            col.DataType = typeof(string);
            col.AutoIncrement = true;
            col.AutoIncrementSeed = 0;
            col.AutoIncrementStep = 1;
            _tbl.Columns.Add(col);

            _tbl.Rows.Add(_tbl.NewRow());
            Assert.Equal(typeof(int), _tbl.Columns[1].DataType);
            Assert.Equal(typeof(int), _tbl.Rows[5]["Auto2"].GetType());

            col = new DataColumn("Auto3");
            col.AutoIncrement = true;
            col.AutoIncrementSeed = 0;
            col.AutoIncrementStep = 1;
            col.DataType = typeof(string);
            Assert.Equal(typeof(string), col.DataType);
            Assert.False(col.AutoIncrement);
        }

        [Fact]
        public void Add()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            DataRow row = _tbl.NewRow();
            DataRowCollection rows = _tbl.Rows;

            rows.Add(row);
            Assert.Equal(1, rows.Count);
            Assert.False(rows.IsReadOnly);
            Assert.False(rows.IsSynchronized);
            Assert.Equal("System.Data.DataRowCollection", rows.ToString());

            string[] cols = new string[2] { "first", "second" };

            rows.Add(cols);
            cols[0] = "something";
            cols[1] = "else";
            rows.Add(cols);

            Assert.Equal(3, rows.Count);
            Assert.Equal("System.Data.DataRow", rows[0].ToString());
            Assert.Equal(DBNull.Value, rows[0][0]);
            Assert.Equal(DBNull.Value, rows[0][1]);
            Assert.Equal("first", rows[1][0]);
            Assert.Equal("something", rows[2][0]);
            Assert.Equal("second", rows[1][1]);
            Assert.Equal("else", rows[2][1]);

            // This row already belongs to this table.
            Assert.Throws<ArgumentException>(() => rows.Add(row));

            // 'row' argument cannot be null.\r\nParameter name: row
            Assert.Throws<ArgumentNullException>(() => rows.Add((DataRow)null));

            DataColumn column = new DataColumn("not_null");
            column.AllowDBNull = false;
            _tbl.Columns.Add(column);

            cols = new string[3];
            cols[0] = "first";
            cols[1] = "second";
            cols[2] = null;

            // Column 'not_null' does not allow nulls.
            Assert.Throws<NoNullAllowedException>(() => rows.Add(cols));

            column = _tbl.Columns[0];
            column.Unique = true;

            cols = new string[3];
            cols[0] = "first";
            cols[1] = "second";
            cols[2] = "blabal";

            // Column 'Column1' is constrained to be unique.  Value 'first' is already present.
            Assert.Throws<ConstraintException>(() => rows.Add(cols));

            column = new DataColumn("integer");
            column.DataType = typeof(short);
            _tbl.Columns.Add(column);

            object[] obs = new object[4];
            obs[0] = "_first";
            obs[1] = "second";
            obs[2] = "blabal";
            obs[3] = "ads";
            Assert.Throws<ArgumentException>(() => rows.Add(obs));

            object[] obs1 = new object[5];
            obs1[0] = "A";
            obs1[1] = "B";
            obs1[2] = "C";
            obs1[3] = 38;
            obs1[4] = "Extra";
            Assert.Throws<ArgumentException>(() => rows.Add(obs1));
        }

        [Fact]
        public void Add_ByValuesNullTest()
        {
            DataTable t = new DataTable("test");
            t.Columns.Add("id", typeof(int));
            t.Columns.Add("name", typeof(string));
            t.Columns.Add("nullable", typeof(string));

            t.Columns[0].AutoIncrement = true;
            t.Columns[0].AutoIncrementSeed = 10;
            t.Columns[0].AutoIncrementStep = 5;

            t.Columns[1].DefaultValue = "testme";


            // null test & missing columns
            DataRow r = t.Rows.Add(new object[] { null, null });
            Assert.Equal(10, (int)r[0]);
            Assert.Equal("testme", (string)r[1]);
            Assert.Equal(DBNull.Value, r[2]);

            // dbNull test
            r = t.Rows.Add(new object[] { DBNull.Value, DBNull.Value, DBNull.Value });
            Assert.Equal(DBNull.Value, r[0]);
            Assert.Equal(DBNull.Value, r[1]);
            Assert.Equal(DBNull.Value, r[2]);

            // ai test & no default value test
            r = t.Rows.Add(new object[] { null, null, null });
            Assert.Equal(15, (int)r[0]);
            Assert.Equal("testme", (string)r[1]);
            Assert.Equal(DBNull.Value, r[2]);
        }

        [Fact]
        public void Clear()
        {
            DataRowCollection rows = _tbl.Rows;
            DataTable table = new DataTable("child");
            table.Columns.Add("first", typeof(int));
            table.Columns.Add("second", typeof(string));

            _tbl.Columns.Add("first", typeof(int));
            _tbl.Columns.Add("second", typeof(float));

            string[] cols = new string[2];
            cols[0] = "1";
            cols[1] = "1,1";
            rows.Add(cols);

            cols[0] = "2";
            cols[1] = "2,1";
            rows.Add(cols);

            cols[0] = "3";
            cols[1] = "3,1";
            rows.Add(cols);

            Assert.Equal(3, rows.Count);
            rows.Clear();

            Assert.Equal(0, rows.Count);

            cols[0] = "1";
            cols[1] = "1,1";
            rows.Add(cols);

            cols[0] = "2";
            cols[1] = "2,1";
            rows.Add(cols);

            cols[0] = "3";
            cols[1] = "3,1";
            rows.Add(cols);

            cols[0] = "1";
            cols[1] = "test";
            table.Rows.Add(cols);

            cols[0] = "2";
            cols[1] = "test2";
            table.Rows.Add(cols);

            cols[0] = "3";
            cols[1] = "test3";
            table.Rows.Add(cols);

            DataSet Set = new DataSet();
            Set.Tables.Add(_tbl);
            Set.Tables.Add(table);
            DataRelation Rel = new DataRelation("REL", _tbl.Columns[0], table.Columns[0]);
            Set.Relations.Add(Rel);

            Assert.Throws<InvalidConstraintException>(() => rows.Clear());

            Assert.Equal(3, table.Rows.Count);
            table.Rows.Clear();
            Assert.Equal(0, table.Rows.Count);
        }

        [Fact]
        public void Contains()
        {
            DataColumn c = new DataColumn("key");
            c.Unique = true;
            c.DataType = typeof(int);
            c.AutoIncrement = true;
            c.AutoIncrementSeed = 0;
            c.AutoIncrementStep = 1;
            _tbl.Columns.Add(c);
            _tbl.Columns.Add("first", typeof(string));
            _tbl.Columns.Add("second", typeof(decimal));

            DataRowCollection rows = _tbl.Rows;

            DataRow row = _tbl.NewRow();
            _tbl.Rows.Add(row);
            row = _tbl.NewRow();
            _tbl.Rows.Add(row);
            row = _tbl.NewRow();
            _tbl.Rows.Add(row);
            row = _tbl.NewRow();
            _tbl.Rows.Add(row);

            rows[0][1] = "test0";
            rows[0][2] = 0;
            rows[1][1] = "test1";
            rows[1][2] = 1;
            rows[2][1] = "test2";
            rows[2][2] = 2;
            rows[3][1] = "test3";
            rows[3][2] = 3;

            Assert.Equal(3, _tbl.Columns.Count);
            Assert.Equal(4, _tbl.Rows.Count);
            Assert.Equal(0, _tbl.Rows[0][0]);
            Assert.Equal(1, _tbl.Rows[1][0]);
            Assert.Equal(2, _tbl.Rows[2][0]);
            Assert.Equal(3, _tbl.Rows[3][0]);

            // Table doesn't have a primary key.
            Assert.Throws<MissingPrimaryKeyException>(() => rows.Contains(1));

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0] };
            Assert.True(rows.Contains(1));
            Assert.True(rows.Contains(2));
            Assert.False(rows.Contains(4));

            // Expecting 1 value(s) for the key being indexed, but received 2 value(s).
            Assert.Throws<ArgumentException>(() => rows.Contains(new object[] { 64, "test0" }));

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0], _tbl.Columns[1] };
            Assert.False(rows.Contains(new object[] { 64, "test0" }));
            Assert.False(rows.Contains(new object[] { 0, "test1" }));
            Assert.True(rows.Contains(new object[] { 1, "test1" }));
            Assert.True(rows.Contains(new object[] { 2, "test2" }));

            // Expecting 2 value(s) for the key being indexed, but received 1 value(s).
            Assert.Throws<ArgumentException>(() => rows.Contains(new object[] { 2 }));
        }

        [Fact]
        public void CopyTo()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();

            DataRowCollection rows = _tbl.Rows;

            rows.Add(new object[] { "1", "1", "1" });
            rows.Add(new object[] { "2", "2", "2" });
            rows.Add(new object[] { "3", "3", "3" });
            rows.Add(new object[] { "4", "4", "4" });
            rows.Add(new object[] { "5", "5", "5" });
            rows.Add(new object[] { "6", "6", "6" });
            rows.Add(new object[] { "7", "7", "7" });

            DataRow[] dr = new DataRow[10];

            // Destination array was not long enough.  Check destIndex and length, and the array's lower bounds.
            Assert.Throws<ArgumentException>(() => rows.CopyTo(dr, 4));

            dr = new DataRow[11];
            rows.CopyTo(dr, 4);

            Assert.Null(dr[0]);
            Assert.Null(dr[1]);
            Assert.Null(dr[2]);
            Assert.Null(dr[3]);
            Assert.Equal("1", dr[4][0]);
            Assert.Equal("2", dr[5][0]);
            Assert.Equal("3", dr[6][0]);
            Assert.Equal("4", dr[7][0]);
            Assert.Equal("5", dr[8][0]);
            Assert.Equal("6", dr[9][0]);
        }

        [Fact]
        public void EqualsTest()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();

            DataRowCollection rows1 = _tbl.Rows;

            rows1.Add(new object[] { "1", "1", "1" });
            rows1.Add(new object[] { "2", "2", "2" });
            rows1.Add(new object[] { "3", "3", "3" });
            rows1.Add(new object[] { "4", "4", "4" });
            rows1.Add(new object[] { "5", "5", "5" });
            rows1.Add(new object[] { "6", "6", "6" });
            rows1.Add(new object[] { "7", "7", "7" });

            DataRowCollection rows2 = _tbl.Rows;

            Assert.Same(rows2, rows1);

            DataTable table = new DataTable();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            DataRowCollection rows3 = table.Rows;

            rows3.Add(new object[] { "1", "1", "1" });
            rows3.Add(new object[] { "2", "2", "2" });
            rows3.Add(new object[] { "3", "3", "3" });
            rows3.Add(new object[] { "4", "4", "4" });
            rows3.Add(new object[] { "5", "5", "5" });
            rows3.Add(new object[] { "6", "6", "6" });
            rows3.Add(new object[] { "7", "7", "7" });

            Assert.NotSame(rows3, rows2);
            Assert.NotSame(rows1, rows3);
        }

        [Fact]
        public void Find()
        {
            DataColumn col = new DataColumn("test_1");
            col.AllowDBNull = false;
            col.Unique = true;
            col.DataType = typeof(long);
            _tbl.Columns.Add(col);

            col = new DataColumn("test_2");
            col.DataType = typeof(string);
            _tbl.Columns.Add(col);

            DataRowCollection rows = _tbl.Rows;

            rows.Add(new object[] { 1, "first" });
            rows.Add(new object[] { 2, "second" });
            rows.Add(new object[] { 3, "third" });
            rows.Add(new object[] { 4, "fourth" });
            rows.Add(new object[] { 5, "fifth" });

            // Table doesn't have a primary key.
            Assert.Throws<MissingPrimaryKeyException>(() => rows.Find(1));

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0] };
            DataRow row = rows.Find(1);
            Assert.Equal(1L, row[0]);
            row = rows.Find(2);
            Assert.Equal(2L, row[0]);
            row = rows.Find("2");
            Assert.Equal(2L, row[0]);

            // Input string was not in a correct format.
            Assert.Throws<FormatException>(() => rows.Find("test"));

            string tes = null;
            row = rows.Find(tes);
            Assert.Null(row);
            _tbl.PrimaryKey = null;

            // Table doesn't have a primary key.
            Assert.Throws<MissingPrimaryKeyException>(() => rows.Find(new object[] { 1, "fir" }));

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0], _tbl.Columns[1] };

            // Expecting 2 value(s) for the key being indexed, but received 1 value(s).
            Assert.Throws<ArgumentException>(() => rows.Find(1));

            row = rows.Find(new object[] { 1, "fir" });
            Assert.Null(row);
            row = rows.Find(new object[] { 1, "first" });
            Assert.Equal(1L, row[0]);
        }

        [Fact]
        public void Find2()
        {
            DataSet ds = new DataSet();
            ds.EnforceConstraints = false;

            DataTable dt = new DataTable();
            ds.Tables.Add(dt);

            DataColumn dc = new DataColumn("Column A");
            dt.Columns.Add(dc);

            dt.PrimaryKey = new DataColumn[] { dc };

            DataRow dr = dt.NewRow();
            dr[0] = "a";
            dt.Rows.Add(dr);

            dr = dt.NewRow();
            dr[0] = "b";
            dt.Rows.Add(dr);

            dr = dt.NewRow();
            dr[0] = "c";
            dt.Rows.Add(dr);

            DataRow row = ds.Tables[0].Rows.Find(new object[] { "a" });

            Assert.NotNull(row);
        }

        [Fact]
        public void InsertAt()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            DataRowCollection rows = _tbl.Rows;

            rows.Add(new object[] { "a", "aa", "aaa" });
            rows.Add(new object[] { "b", "bb", "bbb" });
            rows.Add(new object[] { "c", "cc", "ccc" });
            rows.Add(new object[] { "d", "dd", "ddd" });

            DataRow row = _tbl.NewRow();
            row[0] = "e";
            row[1] = "ee";
            row[2] = "eee";

            // The row insert position -1 is invalid.
            Assert.Throws<IndexOutOfRangeException>(() => rows.InsertAt(row, -1));

            rows.InsertAt(row, 0);
            Assert.Equal("e", rows[0][0]);
            Assert.Equal("a", rows[1][0]);

            row = _tbl.NewRow();
            row[0] = "f";
            row[1] = "ff";
            row[2] = "fff";

            rows.InsertAt(row, 5);
            Assert.Equal("f", rows[5][0]);

            row = _tbl.NewRow();
            row[0] = "g";
            row[1] = "gg";
            row[2] = "ggg";

            rows.InsertAt(row, 500);
            Assert.Equal("g", rows[6][0]);

            // This row already belongs to this table.
            Assert.Throws<ArgumentException>(() => rows.InsertAt(row, 6));

            DataTable table = new DataTable();
            DataColumn col = new DataColumn("Name");
            table.Columns.Add(col);
            row = table.NewRow();
            row["Name"] = "Abc";
            table.Rows.Add(row);

            // This row already belongs to another table.
            Assert.Throws<ArgumentException>(() => rows.InsertAt(row, 6));

            table = new DataTable();
            col = new DataColumn("Name");
            col.DataType = typeof(string);
            table.Columns.Add(col);
            UniqueConstraint uk = new UniqueConstraint(col);
            table.Constraints.Add(uk);

            row = table.NewRow();
            row["Name"] = "aaa";
            table.Rows.InsertAt(row, 0);

            row = table.NewRow();
            row["Name"] = "aaa";

            Assert.Throws<ConstraintException>(() => table.Rows.InsertAt(row, 1));
            Assert.Throws<ArgumentNullException>(() => table.Rows.InsertAt(null, 1));
        }

        [Fact]
        public void Remove()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            DataRowCollection rows = _tbl.Rows;

            rows.Add(new object[] { "a", "aa", "aaa" });
            rows.Add(new object[] { "b", "bb", "bbb" });
            rows.Add(new object[] { "c", "cc", "ccc" });
            rows.Add(new object[] { "d", "dd", "ddd" });

            Assert.Equal(4, _tbl.Rows.Count);

            rows.Remove(_tbl.Rows[1]);
            Assert.Equal(3, _tbl.Rows.Count);
            Assert.Equal("a", _tbl.Rows[0][0]);
            Assert.Equal("c", _tbl.Rows[1][0]);
            Assert.Equal("d", _tbl.Rows[2][0]);

            // The given datarow is not in the current DataRowCollection.
            Assert.Throws<IndexOutOfRangeException>(() => rows.Remove(null));

            DataRow row = new DataTable().NewRow();

            // The given datarow is not in the current DataRowCollection.
            Assert.Throws<IndexOutOfRangeException>(() => rows.Remove(row));

            // There is no row at position -1.
            Assert.Throws<IndexOutOfRangeException>(() => rows.RemoveAt(-1));

            // There is no row at position 64.
            Assert.Throws<IndexOutOfRangeException>(() => rows.RemoveAt(64));

            rows.RemoveAt(0);
            rows.RemoveAt(1);
            Assert.Equal(1, rows.Count);
            Assert.Equal("c", rows[0][0]);
        }

        [Fact]
        public void IndexOf()
        {
            DataSet ds = new DataSet();

            DataTable dt = new DataTable();
            ds.Tables.Add(dt);

            DataColumn dc = new DataColumn("Column A");
            dt.Columns.Add(dc);

            dt.PrimaryKey = new DataColumn[] { dc };

            DataRow dr1 = dt.NewRow();
            dr1[0] = "a";
            dt.Rows.Add(dr1);

            DataRow dr2 = dt.NewRow();
            dr2[0] = "b";
            dt.Rows.Add(dr2);

            DataRow dr3 = dt.NewRow();
            dr3[0] = "c";
            dt.Rows.Add(dr3);

            DataRow dr4 = dt.NewRow();
            dr4[0] = "d";
            dt.Rows.Add(dr4);

            DataRow dr5 = dt.NewRow();
            dr5[0] = "e";

            int index = ds.Tables[0].Rows.IndexOf(dr3);
            Assert.Equal(2, index);

            index = ds.Tables[0].Rows.IndexOf(dr5);
            Assert.Equal(-1, index);
        }
        [Fact]
        public void IndexOfTest()
        {
            DataTable dt = new DataTable("TestWriteXmlSchema");
            dt.Columns.Add("Col1", typeof(int));
            dt.Columns.Add("Col2", typeof(int));
            DataRow dr = dt.NewRow();
            dr[0] = 10;
            dr[1] = 20;
            dt.Rows.Add(dr);
            DataRow dr1 = dt.NewRow();
            dr1[0] = 10;
            dr1[1] = 20;
            dt.Rows.Add(dr1);
            DataRow dr2 = dt.NewRow();
            dr2[0] = 10;
            dr2[1] = 20;
            dt.Rows.Add(dr2);
            Assert.Equal(1, dt.Rows.IndexOf(dr1));
            DataTable dt1 = new DataTable("HelloWorld");
            dt1.Columns.Add("T1", typeof(int));
            dt1.Columns.Add("T2", typeof(int));
            DataRow dr3 = dt1.NewRow();
            dr3[0] = 10;
            dr3[1] = 20;
            dt1.Rows.Add(dr3);
            Assert.Equal(-1, dt.Rows.IndexOf(dr3));
            Assert.Equal(-1, dt.Rows.IndexOf(null));
        }

        [Fact]
        public void Find_DoesntThrowWithNullObjectInArray()
        {
            var dt = new DataTable("datatable");

            var column = new DataColumn();
            dt.Columns.Add(column);
            var columns = new DataColumn[] { column };
            dt.PrimaryKey = columns;

            Assert.Null(dt.Rows.Find(new object[] { null }));
        }

        [Fact]
        public void Find_DoesntThrowWithNullObject()
        {
            var dt = new DataTable("datatable");

            var column = new DataColumn();
            dt.Columns.Add(column);
            var columns = new DataColumn[] { column };
            dt.PrimaryKey = columns;

            Assert.Null(dt.Rows.Find((object)null));
        }
    }
}
