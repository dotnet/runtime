// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Punit Todi

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
    public class DataTableCollectionTest : IDisposable
    {
        // common variables here
        private DataSet[] _dataset;
        private DataTable[] _tables;

        public DataTableCollectionTest()
        {
            // setting up dataset && tables
            _dataset = new DataSet[2];
            _tables = new DataTable[2];
            _dataset[0] = new DataSet();
            _dataset[1] = new DataSet();
            _tables[0] = new DataTable("Books");
            _tables[0].Columns.Add("id", typeof(int));
            _tables[0].Columns.Add("name", typeof(string));
            _tables[0].Columns.Add("author", typeof(string));

            _tables[1] = new DataTable("Category");
            _tables[1].Columns.Add("id", typeof(int));
            _tables[1].Columns.Add("desc", typeof(string));
        }
        // clean up code here
        public void Dispose()
        {
            _dataset[0].Tables.Clear();
            _dataset[1].Tables.Clear();
        }
        [Fact]
        public void Add()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add(_tables[0]);
            int i, j;
            i = 0;

            foreach (DataTable table in tbcol)
            {
                Assert.Equal(_tables[i].TableName, table.TableName);
                j = 0;
                foreach (DataColumn column in table.Columns)
                {
                    Assert.Equal(_tables[i].Columns[j].ColumnName, column.ColumnName);
                    j++;
                }
                i++;
            }

            tbcol.Add(_tables[1]);
            i = 0;
            foreach (DataTable table in tbcol)
            {
                Assert.Equal(_tables[i].TableName, table.TableName);
                j = 0;
                foreach (DataColumn column in table.Columns)
                {
                    Assert.Equal(_tables[i].Columns[j].ColumnName, column.ColumnName);
                    j++;
                }
                i++;
            }
        }

        [Fact]
        public void AddException1()
        {
            Assert.Throws<ArgumentNullException>(() =>
           {
               DataTableCollection tbcol = _dataset[0].Tables;
               DataTable tb = null;
               tbcol.Add(tb);
           });
        }

        [Fact]
        public void AddException2()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               /* table already exist in the collection */
               DataTableCollection tbcol = _dataset[0].Tables;
               tbcol.Add(_tables[0]);
               tbcol.Add(_tables[0]);
           });
        }

        [Fact]
        public void AddException3()
        {
            Assert.Throws<DuplicateNameException>(() =>
           {
               DataTableCollection tbcol = _dataset[0].Tables;
               tbcol.Add(new DataTable("SameTableName"));
               tbcol.Add(new DataTable("SameTableName"));
           });
        }

        [Fact]
        public void AddException4()
        {
            Assert.Throws<DuplicateNameException>(() =>
           {
               DataTableCollection tbcol = _dataset[0].Tables;
               tbcol.Add("SameTableName");
               tbcol.Add("SameTableName");
           });
        }

        [Fact]
        public void Count()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add(_tables[0]);
            Assert.Equal(1, tbcol.Count);
            tbcol.Add(_tables[1]);
            Assert.Equal(2, tbcol.Count);
        }

        [Fact]
        public void AddRange()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Clear();
            /* _tables is array of type DataTable defined in Setup */
            tbcol.AddRange(_tables);
            int i, j;
            i = 0;
            foreach (DataTable table in tbcol)
            {
                Assert.Equal(_tables[i].TableName, table.TableName);
                j = 0;
                foreach (DataColumn column in table.Columns)
                {
                    Assert.Equal(_tables[i].Columns[j].ColumnName, column.ColumnName);
                    j++;
                }
                i++;
            }
        }

        [Fact]
        public void CanRemove()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Clear();
            /* _tables is array of DataTables defined in Setup */
            tbcol.AddRange(_tables);
            DataTable tbl = null;
            /* checking for a recently input table, expecting true */
            Assert.Equal(true, tbcol.CanRemove(_tables[0]));
            /* trying to check with a null reference, expecting false */
            Assert.Equal(false, tbcol.CanRemove(tbl));
            /* trying to check with a table that does not exist in collection, expecting false */
            Assert.Equal(false, tbcol.CanRemove(new DataTable("newTable")));
        }

        [Fact]
        public void Remove()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Clear();
            /* _tables is array of DataTables defined in Setup */
            tbcol.AddRange(_tables);

            /* removing a recently added table */
            int count = tbcol.Count;
            tbcol.Remove(_tables[0]);
            Assert.Equal(count - 1, tbcol.Count);
            DataTable tbl = null;
            /* removing a null reference. must generate an Exception */
            try
            {
                tbcol.Remove(tbl);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }
            /* removing a table that is not there in collection */
            try
            {
                tbcol.Remove(new DataTable("newTable"));
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
        }
        [Fact]
        public void Clear()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add(_tables[0]);
            tbcol.Clear();
            Assert.Equal(0, tbcol.Count);

            tbcol.AddRange(new DataTable[] { _tables[0], _tables[1] });
            tbcol.Clear();
            Assert.Equal(0, tbcol.Count);
        }
        [Fact]
        public void Contains()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Clear();
            /* _tables is array of DataTables defined in Setup */
            tbcol.AddRange(_tables);
            string tblname = "";
            /* checking for a recently input table, expecting true */
            Assert.Equal(true, tbcol.Contains(_tables[0].TableName));
            /* trying to check with an empty string, expecting false */
            Assert.Equal(false, tbcol.Contains(tblname));
            /* trying to check for a table that donot exist, expecting false */
            Assert.Equal(false, tbcol.Contains("InvalidTableName"));
        }

        [Fact]
        public void CopyTo()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add("Table1");
            tbcol.Add("Table2");
            tbcol.Add("Table3");
            tbcol.Add("Table4");

            DataTable[] array = new DataTable[4];
            /* copying to the beginning of the array */
            tbcol.CopyTo(array, 0);
            Assert.Equal(4, array.Length);
            Assert.Equal("Table1", array[0].TableName);
            Assert.Equal("Table2", array[1].TableName);
            Assert.Equal("Table3", array[2].TableName);
            Assert.Equal("Table4", array[3].TableName);

            /* copying with in an array */
            DataTable[] array1 = new DataTable[6];
            tbcol.CopyTo(array1, 2);
            Assert.Equal(null, array1[0]);
            Assert.Equal(null, array1[1]);
            Assert.Equal("Table1", array1[2].TableName);
            Assert.Equal("Table2", array1[3].TableName);
            Assert.Equal("Table3", array1[4].TableName);
            Assert.Equal("Table4", array1[5].TableName);
        }
        [Fact]
        public void Equals()
        {
            DataTableCollection tbcol1 = _dataset[0].Tables;
            DataTableCollection tbcol2 = _dataset[1].Tables;
            DataTableCollection tbcol3;
            tbcol1.Add(_tables[0]);
            tbcol2.Add(_tables[1]);
            tbcol3 = tbcol1;

            Assert.Equal(true, tbcol1.Equals(tbcol1));
            Assert.Equal(true, tbcol1.Equals(tbcol3));
            Assert.Equal(true, tbcol3.Equals(tbcol1));

            Assert.Equal(false, tbcol1.Equals(tbcol2));
            Assert.Equal(false, tbcol2.Equals(tbcol1));

            Assert.Equal(true, object.Equals(tbcol1, tbcol3));
            Assert.Equal(true, object.Equals(tbcol1, tbcol1));
            Assert.Equal(false, object.Equals(tbcol1, tbcol2));
        }
        [Fact]
        public void IndexOf()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add(_tables[0]);
            tbcol.Add("table1");
            tbcol.Add("table2");

            Assert.Equal(0, tbcol.IndexOf(_tables[0]));
            Assert.Equal(-1, tbcol.IndexOf(_tables[1]));
            Assert.Equal(1, tbcol.IndexOf("table1"));
            Assert.Equal(2, tbcol.IndexOf("table2"));

            Assert.Equal(0, tbcol.IndexOf(tbcol[0]));
            Assert.Equal(1, tbcol.IndexOf(tbcol[1]));
            Assert.Equal(-1, tbcol.IndexOf("_noTable_"));
            DataTable tb = new DataTable("new_table");
            Assert.Equal(-1, tbcol.IndexOf(tb));
        }
        [Fact]
        public void RemoveAt()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add(_tables[0]);
            tbcol.Add("table1");

            try
            {
                tbcol.RemoveAt(-1);
                Assert.False(true);
            }
            catch (IndexOutOfRangeException e)
            {
            }
            try
            {
                tbcol.RemoveAt(101);
                Assert.False(true);
            }
            catch (IndexOutOfRangeException e)
            {
            }
            tbcol.RemoveAt(1);
            Assert.Equal(1, tbcol.Count);
            tbcol.RemoveAt(0);
            Assert.Equal(0, tbcol.Count);
        }

        [Fact]
        public void ToStringTest()
        {
            DataTableCollection tbcol = _dataset[0].Tables;
            tbcol.Add("Table1");
            tbcol.Add("Table2");
            tbcol.Add("Table3");
            Assert.Equal("System.Data.DataTableCollection", tbcol.ToString());
        }

        [Fact]
        public void TableDataSetNamespaces()
        {
            DataTable dt = new DataTable("dt1");
            Assert.Equal(string.Empty, dt.Namespace);
            Assert.Null(dt.DataSet);

            DataSet ds1 = new DataSet("ds1");
            ds1.Tables.Add(dt);
            Assert.Equal(string.Empty, dt.Namespace);
            Assert.Equal(ds1, dt.DataSet);

            ds1.Namespace = "ns1";
            Assert.Equal("ns1", dt.Namespace);

            // back to null again
            ds1.Tables.Remove(dt);
            Assert.Equal(string.Empty, dt.Namespace);
            Assert.Null(dt.DataSet);

            // This table is being added to _already namespaced_
            // dataset.
            dt = new DataTable("dt2");

            ds1.Tables.Add(dt);
            Assert.Equal("ns1", dt.Namespace);
            Assert.Equal(ds1, dt.DataSet);

            ds1.Tables.Remove(dt);
            Assert.Equal(string.Empty, dt.Namespace);
            Assert.Null(dt.DataSet);

            DataSet ds2 = new DataSet("ds2");
            ds2.Namespace = "ns2";
            ds2.Tables.Add(dt);
            Assert.Equal("ns2", dt.Namespace);
            Assert.Equal(ds2, dt.DataSet);
        }
    }
}
