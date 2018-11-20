// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
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
using System.ComponentModel;

using System.Collections;

namespace System.Data.Tests
{
    public class DataColumnCollectionTest2
    {
        private int _counter = 0;

        [Fact]
        public void Add()
        {
            DataColumn dc = null;
            DataTable dt = new DataTable();

            //----------------------------- check default --------------------
            dc = dt.Columns.Add();
            // Add column 1
            Assert.Equal("Column1", dc.ColumnName);

            // Add column 2
            dc = dt.Columns.Add();
            Assert.Equal("Column2", dc.ColumnName);

            dc = dt.Columns.Add();
            // Add column 3
            Assert.Equal("Column3", dc.ColumnName);

            dc = dt.Columns.Add();
            // Add column 4
            Assert.Equal("Column4", dc.ColumnName);

            dc = dt.Columns.Add();
            // Add column 5
            Assert.Equal("Column5", dc.ColumnName);
            Assert.Equal(5, dt.Columns.Count);

            //----------------------------- check Add/Remove from begining --------------------
            dt = initTable();

            dt.Columns.Remove(dt.Columns[0]);
            dt.Columns.Remove(dt.Columns[0]);
            dt.Columns.Remove(dt.Columns[0]);

            // check column 4 - remove - from begining
            Assert.Equal("Column4", dt.Columns[0].ColumnName);

            // check column 5 - remove - from begining
            Assert.Equal("Column5", dt.Columns[1].ColumnName);
            Assert.Equal(2, dt.Columns.Count);

            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();

            // check column 0 - Add new  - from begining
            Assert.Equal("Column4", dt.Columns[0].ColumnName);

            // check column 1 - Add new - from begining
            Assert.Equal("Column5", dt.Columns[1].ColumnName);

            // check column 2 - Add new - from begining
            Assert.Equal("Column6", dt.Columns[2].ColumnName);

            // check column 3 - Add new - from begining
            Assert.Equal("Column7", dt.Columns[3].ColumnName);

            // check column 4 - Add new - from begining
            Assert.Equal("Column8", dt.Columns[4].ColumnName);

            // check column 5 - Add new - from begining
            Assert.Equal("Column9", dt.Columns[5].ColumnName);

            //----------------------------- check Add/Remove from middle --------------------

            dt = initTable();

            dt.Columns.Remove(dt.Columns[2]);
            dt.Columns.Remove(dt.Columns[2]);
            dt.Columns.Remove(dt.Columns[2]);

            // check column 0 - remove - from Middle
            Assert.Equal("Column1", dt.Columns[0].ColumnName);

            // check column 1 - remove - from Middle
            Assert.Equal("Column2", dt.Columns[1].ColumnName);

            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();

            // check column 0 - Add new  - from Middle
            Assert.Equal("Column1", dt.Columns[0].ColumnName);

            // check column 1 - Add new - from Middle
            Assert.Equal("Column2", dt.Columns[1].ColumnName);

            // check column 2 - Add new - from Middle
            Assert.Equal("Column3", dt.Columns[2].ColumnName);

            // check column 3 - Add new - from Middle
            Assert.Equal("Column4", dt.Columns[3].ColumnName);

            // check column 4 - Add new - from Middle
            Assert.Equal("Column5", dt.Columns[4].ColumnName);

            // check column 5 - Add new - from Middle
            Assert.Equal("Column6", dt.Columns[5].ColumnName);

            //----------------------------- check Add/Remove from end --------------------

            dt = initTable();

            dt.Columns.Remove(dt.Columns[4]);
            dt.Columns.Remove(dt.Columns[3]);
            dt.Columns.Remove(dt.Columns[2]);

            // check column 0 - remove - from end
            Assert.Equal("Column1", dt.Columns[0].ColumnName);

            // check column 1 - remove - from end
            Assert.Equal("Column2", dt.Columns[1].ColumnName);

            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();

            // check column 0 - Add new  - from end
            Assert.Equal("Column1", dt.Columns[0].ColumnName);

            // check column 1 - Add new - from end
            Assert.Equal("Column2", dt.Columns[1].ColumnName);

            // check column 2 - Add new - from end
            Assert.Equal("Column3", dt.Columns[2].ColumnName);

            // check column 3 - Add new - from end
            Assert.Equal("Column4", dt.Columns[3].ColumnName);

            // check column 4 - Add new - from end
            Assert.Equal("Column5", dt.Columns[4].ColumnName);

            // check column 5 - Add new - from end
            Assert.Equal("Column6", dt.Columns[5].ColumnName);
        }

        private DataTable initTable()
        {
            DataTable dt = new DataTable();
            for (int i = 0; i < 5; i++)
            {
                dt.Columns.Add();
            }
            return dt;
        }

        [Fact]
        public void TestAdd_ByTableName()
        {
            //this test is from boris

            var ds = new DataSet();
            DataTable dt = new DataTable();
            ds.Tables.Add(dt);

            // add one column
            dt.Columns.Add("id1", typeof(int));

            // DataColumnCollection add
            Assert.Equal(1, dt.Columns.Count);

            // add row
            DataRow dr = dt.NewRow();
            dt.Rows.Add(dr);

            // remove column
            dt.Columns.Remove("id1");

            // DataColumnCollection remove
            Assert.Equal(0, dt.Columns.Count);

            //row is still there

            // now add column
            dt.Columns.Add("id2", typeof(int));

            // DataColumnCollection add again
            Assert.Equal(1, dt.Columns.Count);
        }

        [Fact]
        public void TestCanRemove_ByDataColumn()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            DataColumn dummyCol = new DataColumn();
            Assert.Equal(false, dt.Columns.CanRemove(null));
            Assert.Equal(false, dt.Columns.CanRemove(dummyCol));
            Assert.Equal(false, dt.Columns.CanRemove(dt.Columns[0]));
            Assert.Equal(true, dt.Columns.CanRemove(dt.Columns[1]));
        }
        [Fact]
        public void TestCanRemove_ForigenConstraint()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();

            Assert.Equal(false, ds.Tables["child"].Columns.CanRemove(ds.Tables["child"].Columns["parentId"]));
            Assert.Equal(false, ds.Tables["parent"].Columns.CanRemove(ds.Tables["child"].Columns["parentId"]));
        }
        [Fact]
        public void TestCanRemove_ParentRelations()
        {
            var ds = new DataSet();

            ds.Tables.Add("table1");
            ds.Tables.Add("table2");
            ds.Tables["table1"].Columns.Add("col1");
            ds.Tables["table2"].Columns.Add("col1");

            ds.Tables[1].ParentRelations.Add("name1", ds.Tables[0].Columns["col1"], ds.Tables[1].Columns["col1"], false);

            Assert.Equal(false, ds.Tables[1].Columns.CanRemove(ds.Tables[1].Columns["col1"]));
            Assert.Equal(false, ds.Tables[0].Columns.CanRemove(ds.Tables[0].Columns["col1"]));
        }

        [Fact]
        public void TestCanRemove_Expression()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("col1", typeof(string));
            dt.Columns.Add("col2", typeof(string), "sum(col1)");

            Assert.Equal(false, dt.Columns.CanRemove(dt.Columns["col1"]));
        }

        [Fact]
        public void TestAdd_CollectionChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.Columns.CollectionChanged += new CollectionChangeEventHandler(Columns_CollectionChanged);
            _counter = 0;
            DataColumn c = dt.Columns.Add("tempCol");

            Assert.Equal(1, _counter);
            Assert.Equal(c, _change_element);
        }

        [Fact]
        public void TestRemove_CollectionChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.Columns.CollectionChanged += new CollectionChangeEventHandler(Columns_CollectionChanged);
            DataColumn c = dt.Columns.Add("tempCol");
            _counter = 0;
            dt.Columns.Remove("tempCol");

            Assert.Equal(1, _counter);
            Assert.Equal(c, _change_element);
        }

        [Fact]
        public void TestSetName_CollectionChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.Columns.CollectionChanged += new CollectionChangeEventHandler(Columns_CollectionChanged);
            dt.Columns.Add("tempCol");
            _counter = 0;
            dt.Columns[0].ColumnName = "tempCol2";

            Assert.Equal(0, _counter);
        }

        private object _change_element;
        private void Columns_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            _counter++;
            _change_element = e.Element;
        }

        [Fact]
        public void TestContains_ByColumnName()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            Assert.Equal(true, dt.Columns.Contains("ParentId"));
            Assert.Equal(true, dt.Columns.Contains("String1"));
            Assert.Equal(true, dt.Columns.Contains("ParentBool"));

            Assert.Equal(false, dt.Columns.Contains("ParentId1"));
            dt.Columns.Remove("ParentId");
            Assert.Equal(false, dt.Columns.Contains("ParentId"));

            dt.Columns["String1"].ColumnName = "Temp1";

            Assert.Equal(false, dt.Columns.Contains("String1"));
            Assert.Equal(true, dt.Columns.Contains("Temp1"));
        }
        public void NotReadyTestContains_S2() // FIXME: fails in MS
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            Assert.Equal(false, dt.Columns.Contains(null));
        }


        [Fact]
        public void Count()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            Assert.Equal(6, dt.Columns.Count);

            dt.Columns.Add("temp1");
            Assert.Equal(7, dt.Columns.Count);

            dt.Columns.Remove("temp1");
            Assert.Equal(6, dt.Columns.Count);

            dt.Columns.Remove("ParentId");
            Assert.Equal(5, dt.Columns.Count);
        }

        [Fact]
        public void TestIndexOf_ByDataColumn()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                Assert.Equal(i, dt.Columns.IndexOf(dt.Columns[i]));
            }

            DataColumn col = new DataColumn();

            Assert.Equal(-1, dt.Columns.IndexOf(col));

            Assert.Equal(-1, dt.Columns.IndexOf((DataColumn)null));
        }

        [Fact]
        public void TestIndexOf_ByColumnName()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                Assert.Equal(i, dt.Columns.IndexOf(dt.Columns[i].ColumnName));
            }

            DataColumn col = new DataColumn();

            Assert.Equal(-1, dt.Columns.IndexOf("temp1"));

            Assert.Equal(-1, dt.Columns.IndexOf((string)null));
        }

        [Fact]
        public void TestRemove_ByDataColumn()
        {
            //prepare a DataSet with DataTable to be checked
            DataTable dtSource = new DataTable();
            dtSource.Columns.Add("Col_0", typeof(int));
            dtSource.Columns.Add("Col_1", typeof(int));
            dtSource.Columns.Add("Col_2", typeof(int));
            dtSource.Rows.Add(new object[] { 0, 1, 2 });

            DataTable dt = null;

            //------Check Remove first column---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            dt.Columns.Remove(dt.Columns[0]);
            // Remove first column - check column count
            Assert.Equal(2, dt.Columns.Count);

            // Remove first column - check column removed
            Assert.Equal(false, dt.Columns.Contains("Col_0"));

            // Remove first column - check column 0 data
            Assert.Equal(1, dt.Rows[0][0]);

            // Remove first column - check column 1 data
            Assert.Equal(2, dt.Rows[0][1]);

            //------Check Remove middle column---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            dt.Columns.Remove(dt.Columns[1]);
            // Remove middle column - check column count
            Assert.Equal(2, dt.Columns.Count);

            // Remove middle column - check column removed
            Assert.Equal(false, dt.Columns.Contains("Col_1"));

            // Remove middle column - check column 0 data
            Assert.Equal(0, dt.Rows[0][0]);

            // Remove middle column - check column 1 data
            Assert.Equal(2, dt.Rows[0][1]);

            //------Check Remove last column---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            dt.Columns.Remove(dt.Columns[2]);
            // Remove last column - check column count
            Assert.Equal(2, dt.Columns.Count);

            // Remove last column - check column removed
            Assert.Equal(false, dt.Columns.Contains("Col_2"));

            // Remove last column - check column 0 data
            Assert.Equal(0, dt.Rows[0][0]);

            // Remove last column - check column 1 data
            Assert.Equal(1, dt.Rows[0][1]);

            //------Check Remove column exception---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataColumn dc = new DataColumn();
                dt.Columns.Remove(dc);
            });
        }

        [Fact]
        public void Add_DataColumn1()
        {
            DataTable dt = new DataTable();
            DataColumn col = new DataColumn("col1", Type.GetType("System.String"));
            dt.Columns.Add(col);
            Assert.Equal(1, dt.Columns.Count);
            Assert.Equal("col1", dt.Columns[0].ColumnName);
            Assert.Equal("System.String", dt.Columns[0].DataType.ToString());
        }

        [Fact]
        public void Add_DataColumn2()
        {
            DataTable dt = new DataTable();
            DataColumn col = new DataColumn("col1", Type.GetType("System.String"));
            dt.Columns.Add(col);
            AssertExtensions.Throws<ArgumentException>(null, () => dt.Columns.Add(col));
        }

        [Fact]
        public void Add_DataColumn3()
        {
            DataTable dt = new DataTable();
            DataColumn col = new DataColumn("col1", Type.GetType("System.String"));
            dt.Columns.Add(col);
            Assert.Throws<DuplicateNameException>(() =>
            {
                DataColumn col1 = new DataColumn("col1", Type.GetType("System.String"));
                dt.Columns.Add(col1);
            });
        }

        [Fact]
        public void Add_String1()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("col1");
            Assert.Equal(1, dt.Columns.Count);
            Assert.Equal("col1", dt.Columns[0].ColumnName);
        }

        [Fact]
        public void Add_String2()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("col1");
            Assert.Throws<DuplicateNameException>(() =>
            {
                dt.Columns.Add("col1");
            });
        }

        [Fact]
        public void AddRange_DataColumn1()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(GetDataColumArray());
            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal("col1", dt.Columns[0].ColumnName);
            Assert.Equal("col2", dt.Columns[1].ColumnName);
            Assert.Equal(typeof(int), dt.Columns[0].DataType);
            Assert.Equal(typeof(string), dt.Columns[1].DataType);
        }

        [Fact]
        public void AddRange_DataColumn2()
        {
            DataTable dt = new DataTable();
            Assert.Throws<DuplicateNameException>(() =>
            {
                dt.Columns.AddRange(GetBadDataColumArray());
            });
        }

        [Fact]
        public void DataColumnCollection_AddRange_DataColumn3()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(null);
        }

        private DataColumn[] GetDataColumArray()
        {
            DataColumn[] arr = new DataColumn[2];

            arr[0] = new DataColumn("col1", typeof(int));
            arr[1] = new DataColumn("col2", typeof(string));

            return arr;
        }

        private DataColumn[] GetBadDataColumArray()
        {
            DataColumn[] arr = new DataColumn[2];

            arr[0] = new DataColumn("col1", typeof(int));
            arr[1] = new DataColumn("col1", typeof(string));

            return arr;
        }

        [Fact]
        public void Clear1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Columns.Clear();
            Assert.Equal(0, dt.Columns.Count);
        }

        [Fact]
        public void Clear2()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                ds.Tables[0].Columns.Clear();
            });
        }

        [Fact]
        public void Clear3()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            ds.Tables[1].Constraints.RemoveAt(0);
            ds.Tables[0].Constraints.RemoveAt(0);
            ds.Tables[0].Columns.Clear();
            ds.Tables[1].Columns.Clear();
            Assert.Equal(0, ds.Tables[0].Columns.Count);
            Assert.Equal(0, ds.Tables[1].Columns.Count);
        }

        [Fact]
        public void GetEnumerator()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();

            int counter = 0;
            IEnumerator myEnumerator = dt.Columns.GetEnumerator();
            while (myEnumerator.MoveNext())
            {
                counter++;
            }
            Assert.Equal(6, counter);

            Assert.Throws<InvalidOperationException>(() =>
            {
                DataColumn col = (DataColumn)myEnumerator.Current;
            });
        }

        [Fact] // this [Int32]
        public void Indexer1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataColumn col;

            col = dt.Columns[5];
            Assert.NotNull(col);
            Assert.Equal("ParentBool", col.ColumnName);

            col = dt.Columns[0];
            Assert.NotNull(col);
            Assert.Equal("ParentId", col.ColumnName);

            col = dt.Columns[3];
            Assert.NotNull(col);
            Assert.Equal("ParentDateTime", col.ColumnName);
        }

        [Fact] // this [Int32]
        public void Indexer1_Index_Negative()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            try
            {
                DataColumn column = dt.Columns[-1];
                Assert.False(true);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Cannot find column -1
                Assert.Equal(typeof(IndexOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact] // this [Int32]
        public void Indexer1_Index_Overflow()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            try
            {
                DataColumn column = dt.Columns[6];
                Assert.False(true);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Cannot find column 6
                Assert.Equal(typeof(IndexOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact] // this [String]
        public void Indexer2()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataColumnCollection cols = dt.Columns;
            DataColumn col;

            col = cols["ParentId"];
            Assert.NotNull(col);
            Assert.Equal("ParentId", col.ColumnName);

            col = cols["parentiD"];
            Assert.NotNull(col);
            Assert.Equal("ParentId", col.ColumnName);

            col = cols["DoesNotExist"];
            Assert.Null(col);
        }

        [Fact] // this [String]
        public void Indexer2_Name_Empty()
        {
            DataTable dt = new DataTable();
            DataColumnCollection cols = dt.Columns;

            cols.Add(string.Empty, typeof(int));
            cols.Add(null, typeof(bool));

            DataColumn column = cols[string.Empty];
            Assert.Null(column);
        }

        [Fact] // this [String]
        public void Indexer2_Name_Null()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            try
            {
                DataColumn column = dt.Columns[null];
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Equal("name", ex.ParamName);
            }
        }

        [Fact]
        public void Remove()
        {
            //prepare a DataSet with DataTable to be checked
            DataTable dtSource = new DataTable();
            dtSource.Columns.Add("Col_0", typeof(int));
            dtSource.Columns.Add("Col_1", typeof(int));
            dtSource.Columns.Add("Col_2", typeof(int));
            dtSource.Rows.Add(new object[] { 0, 1, 2 });

            DataTable dt = null;

            //------Check Remove first column---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            dt.Columns.Remove(dt.Columns[0].ColumnName);
            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal(false, dt.Columns.Contains("Col_0"));
            Assert.Equal(1, dt.Rows[0][0]);
            Assert.Equal(2, dt.Rows[0][1]);



            //------Check Remove middle column---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            dt.Columns.Remove(dt.Columns[1].ColumnName);
            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal(false, dt.Columns.Contains("Col_1"));
            Assert.Equal(0, dt.Rows[0][0]);
            Assert.Equal(2, dt.Rows[0][1]);


            //------Check Remove last column---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            dt.Columns.Remove(dt.Columns[2].ColumnName);

            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal(false, dt.Columns.Contains("Col_2"));
            Assert.Equal(0, dt.Rows[0][0]);
            Assert.Equal(1, dt.Rows[0][1]);


            //------Check Remove column exception---------
            dt = dtSource.Clone();
            dt.ImportRow(dtSource.Rows[0]);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dt.Columns.Remove("NotExist");
            });

            dt.Columns.Clear();

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dt.Columns.Remove("Col_0");
            });
        }

        private bool _eventOccurred = false;

        [Fact]
        public void RemoveAt_Integer()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Columns.CollectionChanged += new CollectionChangeEventHandler(Columns_CollectionChanged1);
            int originalColumnCount = dt.Columns.Count;
            dt.Columns.RemoveAt(0);
            Assert.Equal(originalColumnCount - 1, dt.Columns.Count);
            Assert.Equal(true, _eventOccurred);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                dt.Columns.RemoveAt(-1);
            });
        }

        [Fact]
        public void Test_Indexes()
        {
            DataTable dt = new DataTable();
            DataColumn dc = new DataColumn("A");
            dt.Columns.Add(dc);

            dc = new DataColumn("B");
            dt.Columns.Add(dc);

            dc = new DataColumn("C");
            dt.Columns.Add(dc);

            for (int i = 0; i < 10; i++)
            {
                DataRow dr = dt.NewRow();
                dr["A"] = i;
                dr["B"] = i + 1;
                dr["C"] = i + 2;
                dt.Rows.Add(dr);
            }

            DataRow[] rows = dt.Select("A=5");
            Assert.Equal(1, rows.Length);

            dt.Columns.Remove("A");

            dc = new DataColumn("A");
            dc.DefaultValue = 5;

            dt.Columns.Add(dc);

            rows = dt.Select("A=5");
            Assert.Equal(10, rows.Length);
        }

        private void Columns_CollectionChanged1(object sender, CollectionChangeEventArgs e)
        {
            _eventOccurred = true;
        }
    }
}
