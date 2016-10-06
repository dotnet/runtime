// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
        private DataTable _tbl;

        public DataRowCollectionTest()
        {
            _tbl = new DataTable();
        }

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
            DataRow Row = _tbl.NewRow();
            DataRowCollection Rows = _tbl.Rows;

            Rows.Add(Row);
            Assert.Equal(1, Rows.Count);
            Assert.False(Rows.IsReadOnly);
            Assert.False(Rows.IsSynchronized);
            Assert.Equal("System.Data.DataRowCollection", Rows.ToString());

            string[] cols = new string[2];
            cols[0] = "first";
            cols[1] = "second";

            Rows.Add(cols);
            cols[0] = "something";
            cols[1] = "else";
            Rows.Add(cols);

            Assert.Equal(3, Rows.Count);
            Assert.Equal("System.Data.DataRow", Rows[0].ToString());
            Assert.Equal(DBNull.Value, Rows[0][0]);
            Assert.Equal(DBNull.Value, Rows[0][1]);
            Assert.Equal("first", Rows[1][0]);
            Assert.Equal("something", Rows[2][0]);
            Assert.Equal("second", Rows[1][1]);
            Assert.Equal("else", Rows[2][1]);

            try
            {
                Rows.Add(Row);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("This row already belongs to this table.", e.Message);
            }

            try
            {
                Row = null;
                Rows.Add(Row);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
                //Assert.Equal ("'row' argument cannot be null.\r\nParameter name: row", e.Message);
            }

            DataColumn Column = new DataColumn("not_null");
            Column.AllowDBNull = false;
            _tbl.Columns.Add(Column);

            cols = new string[3];
            cols[0] = "first";
            cols[1] = "second";
            cols[2] = null;

            try
            {
                Rows.Add(cols);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(NoNullAllowedException), e.GetType());
                //Assert.Equal ("Column 'not_null' does not allow nulls.", e.Message);
            }

            Column = _tbl.Columns[0];
            Column.Unique = true;

            cols = new string[3];
            cols[0] = "first";
            cols[1] = "second";
            cols[2] = "blabal";

            try
            {
                Rows.Add(cols);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ConstraintException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Column 'Column1' is constrained to be unique.  Value 'first' is already present.", e.Message);
            }

            Column = new DataColumn("integer");
            Column.DataType = typeof(short);
            _tbl.Columns.Add(Column);

            object[] obs = new object[4];
            obs[0] = "_first";
            obs[1] = "second";
            obs[2] = "blabal";
            obs[3] = "ads";

            try
            {
                Rows.Add(obs);
                Assert.False(true);
            }
            catch (ArgumentException e)
            {
                // LAMESPEC: MSDN says this exception is InvalidCastException
                //				Assert.Equal (typeof (ArgumentException), e.GetType ());
            }

            object[] obs1 = new object[5];
            obs1[0] = "A";
            obs1[1] = "B";
            obs1[2] = "C";
            obs1[3] = 38;
            obs1[4] = "Extra";
            try
            {
                Rows.Add(obs1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
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
            DataRowCollection Rows = _tbl.Rows;
            DataTable Table = new DataTable("child");
            Table.Columns.Add("first", typeof(int));
            Table.Columns.Add("second", typeof(string));

            _tbl.Columns.Add("first", typeof(int));
            _tbl.Columns.Add("second", typeof(float));

            string[] cols = new string[2];
            cols[0] = "1";
            cols[1] = "1,1";
            Rows.Add(cols);

            cols[0] = "2";
            cols[1] = "2,1";
            Rows.Add(cols);

            cols[0] = "3";
            cols[1] = "3,1";
            Rows.Add(cols);

            Assert.Equal(3, Rows.Count);
            Rows.Clear();

            Assert.Equal(0, Rows.Count);

            cols[0] = "1";
            cols[1] = "1,1";
            Rows.Add(cols);

            cols[0] = "2";
            cols[1] = "2,1";
            Rows.Add(cols);

            cols[0] = "3";
            cols[1] = "3,1";
            Rows.Add(cols);

            cols[0] = "1";
            cols[1] = "test";
            Table.Rows.Add(cols);

            cols[0] = "2";
            cols[1] = "test2";
            Table.Rows.Add(cols);

            cols[0] = "3";
            cols[1] = "test3";
            Table.Rows.Add(cols);

            DataSet Set = new DataSet();
            Set.Tables.Add(_tbl);
            Set.Tables.Add(Table);
            DataRelation Rel = new DataRelation("REL", _tbl.Columns[0], Table.Columns[0]);
            Set.Relations.Add(Rel);

            try
            {
                Rows.Clear();
                Assert.False(true);
            }
            catch (InvalidConstraintException)
            {
            }

            Assert.Equal(3, Table.Rows.Count);
            Table.Rows.Clear();
            Assert.Equal(0, Table.Rows.Count);
        }

        [Fact]
        public void Contains()
        {
            DataColumn C = new DataColumn("key");
            C.Unique = true;
            C.DataType = typeof(int);
            C.AutoIncrement = true;
            C.AutoIncrementSeed = 0;
            C.AutoIncrementStep = 1;
            _tbl.Columns.Add(C);
            _tbl.Columns.Add("first", typeof(string));
            _tbl.Columns.Add("second", typeof(decimal));

            DataRowCollection Rows = _tbl.Rows;

            DataRow Row = _tbl.NewRow();
            _tbl.Rows.Add(Row);
            Row = _tbl.NewRow();
            _tbl.Rows.Add(Row);
            Row = _tbl.NewRow();
            _tbl.Rows.Add(Row);
            Row = _tbl.NewRow();
            _tbl.Rows.Add(Row);

            Rows[0][1] = "test0";
            Rows[0][2] = 0;
            Rows[1][1] = "test1";
            Rows[1][2] = 1;
            Rows[2][1] = "test2";
            Rows[2][2] = 2;
            Rows[3][1] = "test3";
            Rows[3][2] = 3;

            Assert.Equal(3, _tbl.Columns.Count);
            Assert.Equal(4, _tbl.Rows.Count);
            Assert.Equal(0, _tbl.Rows[0][0]);
            Assert.Equal(1, _tbl.Rows[1][0]);
            Assert.Equal(2, _tbl.Rows[2][0]);
            Assert.Equal(3, _tbl.Rows[3][0]);

            try
            {
                Rows.Contains(1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(MissingPrimaryKeyException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Table doesn't have a primary key.", e.Message);
            }

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0] };
            Assert.True(Rows.Contains(1));
            Assert.True(Rows.Contains(2));
            Assert.False(Rows.Contains(4));

            try
            {
                Rows.Contains(new object[] { 64, "test0" });
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Expecting 1 value(s) for the key being indexed, but received 2 value(s).", e.Message);
            }

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0], _tbl.Columns[1] };
            Assert.False(Rows.Contains(new object[] { 64, "test0" }));
            Assert.False(Rows.Contains(new object[] { 0, "test1" }));
            Assert.True(Rows.Contains(new object[] { 1, "test1" }));
            Assert.True(Rows.Contains(new object[] { 2, "test2" }));

            try
            {
                Rows.Contains(new object[] { 2 });
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Expecting 2 value(s) for the key being indexed, but received 1 value(s).", e.Message);
            }
        }

        [Fact]
        public void CopyTo()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();

            DataRowCollection Rows = _tbl.Rows;

            Rows.Add(new object[] { "1", "1", "1" });
            Rows.Add(new object[] { "2", "2", "2" });
            Rows.Add(new object[] { "3", "3", "3" });
            Rows.Add(new object[] { "4", "4", "4" });
            Rows.Add(new object[] { "5", "5", "5" });
            Rows.Add(new object[] { "6", "6", "6" });
            Rows.Add(new object[] { "7", "7", "7" });

            DataRow[] dr = new DataRow[10];

            try
            {
                Rows.CopyTo(dr, 4);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                //Assert.Equal ("Destination array was not long enough.  Check destIndex and length, and the array's lower bounds.", e.Message);
            }

            dr = new DataRow[11];
            Rows.CopyTo(dr, 4);

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
        public void Equals()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();

            DataRowCollection Rows1 = _tbl.Rows;

            Rows1.Add(new object[] { "1", "1", "1" });
            Rows1.Add(new object[] { "2", "2", "2" });
            Rows1.Add(new object[] { "3", "3", "3" });
            Rows1.Add(new object[] { "4", "4", "4" });
            Rows1.Add(new object[] { "5", "5", "5" });
            Rows1.Add(new object[] { "6", "6", "6" });
            Rows1.Add(new object[] { "7", "7", "7" });

            DataRowCollection Rows2 = _tbl.Rows;

            Assert.True(Rows2.Equals(Rows1));
            Assert.True(Rows1.Equals(Rows2));
            Assert.True(Rows1.Equals(Rows1));

            DataTable Table = new DataTable();
            Table.Columns.Add();
            Table.Columns.Add();
            Table.Columns.Add();
            DataRowCollection Rows3 = Table.Rows;

            Rows3.Add(new object[] { "1", "1", "1" });
            Rows3.Add(new object[] { "2", "2", "2" });
            Rows3.Add(new object[] { "3", "3", "3" });
            Rows3.Add(new object[] { "4", "4", "4" });
            Rows3.Add(new object[] { "5", "5", "5" });
            Rows3.Add(new object[] { "6", "6", "6" });
            Rows3.Add(new object[] { "7", "7", "7" });

            Assert.False(Rows3.Equals(Rows1));
            Assert.False(Rows3.Equals(Rows2));
            Assert.False(Rows1.Equals(Rows3));
            Assert.False(Rows2.Equals(Rows3));
        }

        [Fact]
        public void Find()
        {
            DataColumn Col = new DataColumn("test_1");
            Col.AllowDBNull = false;
            Col.Unique = true;
            Col.DataType = typeof(long);
            _tbl.Columns.Add(Col);

            Col = new DataColumn("test_2");
            Col.DataType = typeof(string);
            _tbl.Columns.Add(Col);

            DataRowCollection Rows = _tbl.Rows;

            Rows.Add(new object[] { 1, "first" });
            Rows.Add(new object[] { 2, "second" });
            Rows.Add(new object[] { 3, "third" });
            Rows.Add(new object[] { 4, "fourth" });
            Rows.Add(new object[] { 5, "fifth" });

            try
            {
                Rows.Find(1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(MissingPrimaryKeyException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Table doesn't have a primary key.", e.Message);
            }

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0] };
            DataRow row = Rows.Find(1);
            Assert.Equal(1L, row[0]);
            row = Rows.Find(2);
            Assert.Equal(2L, row[0]);
            row = Rows.Find("2");
            Assert.Equal(2L, row[0]);

            try
            {
                row = Rows.Find("test");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
                //Assert.Equal ("Input string was not in a correct format.", e.Message);
            }

            string tes = null;
            row = Rows.Find(tes);
            Assert.Null(row);
            _tbl.PrimaryKey = null;

            try
            {
                Rows.Find(new object[] { 1, "fir" });
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(MissingPrimaryKeyException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Table doesn't have a primary key.", e.Message);
            }

            _tbl.PrimaryKey = new DataColumn[] { _tbl.Columns[0], _tbl.Columns[1] };

            try
            {
                Rows.Find(1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Expecting 2 value(s) for the key being indexed, but received 1 value(s).", e.Message);
            }

            row = Rows.Find(new object[] { 1, "fir" });
            Assert.Null(row);
            row = Rows.Find(new object[] { 1, "first" });
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
            DataRowCollection Rows = _tbl.Rows;

            Rows.Add(new object[] { "a", "aa", "aaa" });
            Rows.Add(new object[] { "b", "bb", "bbb" });
            Rows.Add(new object[] { "c", "cc", "ccc" });
            Rows.Add(new object[] { "d", "dd", "ddd" });

            DataRow Row = _tbl.NewRow();
            Row[0] = "e";
            Row[1] = "ee";
            Row[2] = "eee";

            try
            {
                Rows.InsertAt(Row, -1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                // Never premise English.
                //Assert.Equal ("The row insert position -1 is invalid.", e.Message);
            }

            Rows.InsertAt(Row, 0);
            Assert.Equal("e", Rows[0][0]);
            Assert.Equal("a", Rows[1][0]);

            Row = _tbl.NewRow();
            Row[0] = "f";
            Row[1] = "ff";
            Row[2] = "fff";

            Rows.InsertAt(Row, 5);
            Assert.Equal("f", Rows[5][0]);

            Row = _tbl.NewRow();
            Row[0] = "g";
            Row[1] = "gg";
            Row[2] = "ggg";

            Rows.InsertAt(Row, 500);
            Assert.Equal("g", Rows[6][0]);

            try
            {
                Rows.InsertAt(Row, 6);  //Row already belongs to the table
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("This row already belongs to this table.", e.Message);
            }

            DataTable table = new DataTable();
            DataColumn col = new DataColumn("Name");
            table.Columns.Add(col);
            Row = table.NewRow();
            Row["Name"] = "Abc";
            table.Rows.Add(Row);
            try
            {
                Rows.InsertAt(Row, 6);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                // Never premise English.
                //Assert.Equal ("This row already belongs to another table.", e.Message);
            }

            table = new DataTable();
            col = new DataColumn("Name");
            col.DataType = typeof(string);
            table.Columns.Add(col);
            UniqueConstraint uk = new UniqueConstraint(col);
            table.Constraints.Add(uk);

            Row = table.NewRow();
            Row["Name"] = "aaa";
            table.Rows.InsertAt(Row, 0);

            Row = table.NewRow();
            Row["Name"] = "aaa";
            try
            {
                table.Rows.InsertAt(Row, 1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ConstraintException), e.GetType());
            }
            try
            {
                table.Rows.InsertAt(null, 1);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }
        }

        [Fact]
        public void Remove()
        {
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            _tbl.Columns.Add();
            DataRowCollection Rows = _tbl.Rows;

            Rows.Add(new object[] { "a", "aa", "aaa" });
            Rows.Add(new object[] { "b", "bb", "bbb" });
            Rows.Add(new object[] { "c", "cc", "ccc" });
            Rows.Add(new object[] { "d", "dd", "ddd" });

            Assert.Equal(4, _tbl.Rows.Count);

            Rows.Remove(_tbl.Rows[1]);
            Assert.Equal(3, _tbl.Rows.Count);
            Assert.Equal("a", _tbl.Rows[0][0]);
            Assert.Equal("c", _tbl.Rows[1][0]);
            Assert.Equal("d", _tbl.Rows[2][0]);

            try
            {
                Rows.Remove(null);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                // Never premise English.
                //Assert.Equal ("The given datarow is not in the current DataRowCollection.", e.Message);
            }

            DataRow Row = new DataTable().NewRow();

            try
            {
                Rows.Remove(Row);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                // Never premise English.
                //Assert.Equal ("The given datarow is not in the current DataRowCollection.", e.Message);
            }

            try
            {
                Rows.RemoveAt(-1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                // Never premise English.
                //Assert.Equal ("There is no row at position -1.", e.Message);
            }

            try
            {
                Rows.RemoveAt(64);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                // Never premise English.
                //Assert.Equal ("There is no row at position 64.", e.Message);
            }

            Rows.RemoveAt(0);
            Rows.RemoveAt(1);
            Assert.Equal(1, Rows.Count);
            Assert.Equal("c", Rows[0][0]);
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

            try
            {
                Assert.Equal(null, dt.Rows.Find(new object[] { null }));
            }
            catch (IndexOutOfRangeException)
            {
                Assert.False(true);
            }
        }

        [Fact]
        public void Find_DoesntThrowWithNullObject()
        {
            var dt = new DataTable("datatable");

            var column = new DataColumn();
            dt.Columns.Add(column);
            var columns = new DataColumn[] { column };
            dt.PrimaryKey = columns;

            try
            {
                Assert.Equal(null, dt.Rows.Find((object)null));
            }
            catch (IndexOutOfRangeException)
            {
                Assert.False(true);
            }
        }
    }
}
