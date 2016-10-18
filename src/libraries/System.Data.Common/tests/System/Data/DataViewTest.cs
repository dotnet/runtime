// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) 2003 Patrick Kalkman
//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
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
using System.IO;

namespace System.Data.Tests
{
    public class DataViewTest : IDisposable
    {
        private DataTable _dataTable;
        private DataView _dataView;
        private Random _rndm;
        private int _seed,_rowCount;
        private ListChangedEventArgs _listChangedArgs;
        private TextWriter _eventWriter;

        private DataColumn _dc1;
        private DataColumn _dc2;
        private DataColumn _dc3;
        private DataColumn _dc4;

        public DataViewTest()
        {
            _dataTable = new DataTable("itemTable");
            _dc1 = new DataColumn("itemId");
            _dc2 = new DataColumn("itemName");
            _dc3 = new DataColumn("itemPrice");
            _dc4 = new DataColumn("itemCategory");

            _dataTable.Columns.Add(_dc1);
            _dataTable.Columns.Add(_dc2);
            _dataTable.Columns.Add(_dc3);
            _dataTable.Columns.Add(_dc4);
            DataRow dr;
            _seed = 123;
            _rowCount = 5;
            _rndm = new Random(_seed);
            for (int i = 1; i <= _rowCount; i++)
            {
                dr = _dataTable.NewRow();
                dr["itemId"] = "item " + i;
                dr["itemName"] = "name " + _rndm.Next();
                dr["itemPrice"] = "Rs. " + (_rndm.Next() % 1000);
                dr["itemCategory"] = "Cat " + ((_rndm.Next() % 10) + 1);
                _dataTable.Rows.Add(dr);
            }
            _dataTable.AcceptChanges();
            _dataView = new DataView(_dataTable);
            _dataView.ListChanged += new ListChangedEventHandler(OnListChanged);
            _listChangedArgs = null;
        }

        protected void OnListChanged(object sender, ListChangedEventArgs args)
        {
            _listChangedArgs = args;
        }

        private void PrintTableOrView(DataTable t, string label)
        {
            Console.WriteLine("\n" + label);
            for (int i = 0; i < t.Rows.Count; i++)
            {
                foreach (DataColumn dc in t.Columns)
                    Console.Write(t.Rows[i][dc] + "\t");
                Console.WriteLine("");
            }
            Console.WriteLine();
        }

        private void PrintTableOrView(DataView dv, string label)
        {
            Console.WriteLine("\n" + label);
            Console.WriteLine("Sort Key :: " + dv.Sort);
            for (int i = 0; i < dv.Count; i++)
            {
                foreach (DataColumn dc in dv.Table.Columns)
                    Console.Write(dv[i].Row[dc] + "\t");
                Console.WriteLine("");
            }
            Console.WriteLine();
        }

        public void Dispose()
        {
            _dataTable = null;
            _dataView = null;
        }

        [Fact]
        public void TestSortWithoutTable()
        {
            DataView dv = new DataView();
            Assert.Throws<DataException>(() => dv.Sort = "abc");
        }

        [Fact]
        public void TestSort()
        {
            DataView dv = new DataView();
            dv.Table = new DataTable("dummy");
            dv.Table.Columns.Add("abc");
            dv.Sort = "abc";
            dv.Sort = string.Empty;
            dv.Sort = "abc";
            Assert.Equal("abc", dv.Sort);
        }

        [Fact]
        public void DataView()
        {
            DataView dv1, dv2, dv3;
            dv1 = new DataView();
            // AssertEquals ("test#01",null,dv1.Table);
            Assert.Equal(true, dv1.AllowNew);
            Assert.Equal(true, dv1.AllowEdit);
            Assert.Equal(true, dv1.AllowDelete);
            Assert.Equal(false, dv1.ApplyDefaultSort);
            Assert.Equal(string.Empty, dv1.RowFilter);
            Assert.Equal(DataViewRowState.CurrentRows, dv1.RowStateFilter);
            Assert.Equal(string.Empty, dv1.Sort);

            dv2 = new DataView(_dataTable);
            Assert.Equal("itemTable", dv2.Table.TableName);
            Assert.Equal(string.Empty, dv2.Sort);
            Assert.Equal(false, dv2.ApplyDefaultSort);
            Assert.Equal(_dataTable.Rows[0], dv2[0].Row);

            dv3 = new DataView(_dataTable, "", "itemId DESC", DataViewRowState.CurrentRows);
            Assert.Equal("", dv3.RowFilter);
            Assert.Equal("itemId DESC", dv3.Sort);
            Assert.Equal(DataViewRowState.CurrentRows, dv3.RowStateFilter);
            //AssertEquals ("test#16",dataTable.Rows.[(dataTable.Rows.Count-1)],dv3[0]);
        }

        [Fact]
        public void TestValue()
        {
            DataView TestView = new DataView(_dataTable);
            Assert.Equal("item 1", TestView[0]["itemId"]);
        }

        [Fact]
        public void TestCount()
        {
            DataView TestView = new DataView(_dataTable);
            Assert.Equal(5, TestView.Count);
        }

        [Fact]
        public void AllowNew()
        {
            Assert.Equal(true, _dataView.AllowNew);
        }

        [Fact]
        public void ApplyDefaultSort()
        {
            UniqueConstraint uc = new UniqueConstraint(_dataTable.Columns["itemId"]);
            _dataTable.Constraints.Add(uc);
            _dataView.ApplyDefaultSort = true;
            // dataView.Sort = "itemName";
            // AssertEquals ("test#01","item 1",dataView[0]["itemId"]);
            Assert.Equal(ListChangedType.Reset, _listChangedArgs.ListChangedType);
            // UnComment the line below to see if dataView is sorted
            //   PrintTableOrView (dataView,"* OnApplyDefaultSort");
        }

        [Fact]
        public void RowStateFilter()
        {
            _dataView.RowStateFilter = DataViewRowState.Deleted;
            Assert.Equal(ListChangedType.Reset, _listChangedArgs.ListChangedType);
        }

        [Fact]
        public void RowStateFilter_2()
        {
            DataSet dataset = new DataSet("new");
            DataTable dt = new DataTable("table1");
            dataset.Tables.Add(dt);
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            dt.Rows.Add(new object[] { 1, 1 });
            dt.Rows.Add(new object[] { 1, 2 });
            dt.Rows.Add(new object[] { 1, 3 });
            dataset.AcceptChanges();

            DataView dataView = new DataView(dataset.Tables[0]);

            // 'new'  table in this sample contains 6 records
            dataView.AllowEdit = true;
            dataView.AllowDelete = true;
            string v;

            // Editing the row
            dataView[0]["col1"] = -1;
            dataView.RowStateFilter = DataViewRowState.ModifiedOriginal;
            v = dataView[0][0].ToString();
            Assert.Equal(1, dataView.Count);
            Assert.Equal("1", v);

            // Deleting the row
            dataView.Delete(0);
            dataView.RowStateFilter = DataViewRowState.Deleted;

            v = dataView[0][0].ToString();
            Assert.Equal(1, dataView.Count);
            Assert.Equal("1", v);
        }

        [Fact]
        public void Bug18898()
        {
            var table = new DataTable();
            table.Columns.Add("col1");
            table.Columns.Add("col2");

            table.Rows.Add("1", "2");
            table.Rows.Add("4", "3");

            table.AcceptChanges();

            table.Rows.Add("5", "6");

            DataView dv = new DataView(table, string.Empty, string.Empty, DataViewRowState.Added);
            dv.AllowNew = true;
            var new_row = dv.AddNew();
            new_row[0] = "7";
            new_row[1] = "8";

            var another_new_row = dv.AddNew();
            another_new_row[0] = "9";
            another_new_row[1] = "10";

            Assert.Equal(dv[2][0], "9");

            //This should not throw a System.Data.VersionNotFoundException: "There is no Proposed data to accces"
            Assert.Equal(dv[1][0], "7");
        }

        [Fact]
        public void NullTableGetItemPropertiesTest()
        {
            DataView dataview = new DataView();
            PropertyDescriptorCollection col = ((ITypedList)dataview).GetItemProperties(null);
            Assert.Equal(0, col.Count);
        }

        #region Sort Tests
        [Fact]
        public void SortListChangedTest()
        {
            _dataView.Sort = "itemName DESC";
            Assert.Equal(ListChangedType.Reset, _listChangedArgs.ListChangedType);
            // UnComment the line below to see if dataView is sorted
            // PrintTableOrView (dataView);
        }


        [Fact]
        public void SortTestWeirdColumnName()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id]", typeof(int));
            dt.Columns.Add("[id", typeof(int));

            DataView dv = dt.DefaultView;
            dv.Sort = "id]";
            //dv.Sort = "[id"; // this is not allowed
            dv.Sort = "[id]]";
            dv.Sort = "[[id]";
            dv.Sort = "id] ASC";
            dv.Sort = "[id]] DESC";
            dv.Sort = "[[id] ASC";
        }


        [Fact]
        public void SortTests()
        {
            DataTable dataTable = new DataTable("itemTable");
            DataColumn dc1 = new DataColumn("itemId", typeof(int));
            DataColumn dc2 = new DataColumn("itemName", typeof(string));

            dataTable.Columns.Add(dc1);
            dataTable.Columns.Add(dc2);

            dataTable.Rows.Add(new object[2] { 1, "First entry" });
            dataTable.Rows.Add(new object[2] { 0, "Second entry" });
            dataTable.Rows.Add(new object[2] { 3, "Third entry" });
            dataTable.Rows.Add(new object[2] { 2, "Fourth entry" });

            DataView dataView = dataTable.DefaultView;

            string s = "Default sorting: ";
            Assert.Equal(1, dataView[0][0]);
            Assert.Equal(0, dataView[1][0]);
            Assert.Equal(3, dataView[2][0]);
            Assert.Equal(2, dataView[3][0]);

            s = "Ascending sorting 1: ";
            dataView.Sort = "itemId ASC";
            Assert.Equal(0, dataView[0][0]);
            Assert.Equal(1, dataView[1][0]);
            Assert.Equal(2, dataView[2][0]);
            Assert.Equal(3, dataView[3][0]);

            s = "Ascending sorting 2: ";
            dataView.Sort = "itemId     ASC";
            Assert.Equal(0, dataView[0][0]);
            Assert.Equal(1, dataView[1][0]);
            Assert.Equal(2, dataView[2][0]);
            Assert.Equal(3, dataView[3][0]);

            s = "Ascending sorting 3: ";
            dataView.Sort = "[itemId] ASC";
            Assert.Equal(0, dataView[0][0]);
            Assert.Equal(1, dataView[1][0]);
            Assert.Equal(2, dataView[2][0]);
            Assert.Equal(3, dataView[3][0]);

            s = "Ascending sorting 4: ";
            dataView.Sort = "[itemId]       ASC";
            Assert.Equal(0, dataView[0][0]);
            Assert.Equal(1, dataView[1][0]);
            Assert.Equal(2, dataView[2][0]);
            Assert.Equal(3, dataView[3][0]);

            s = "Ascending sorting 5: ";
            try
            {
                dataView.Sort = "itemId \tASC";
                Assert.Equal(true, false);
            }
            catch (IndexOutOfRangeException e)
            {
            }

            s = "Descending sorting : ";
            dataView.Sort = "itemId DESC";
            Assert.Equal(3, dataView[0][0]);
            Assert.Equal(2, dataView[1][0]);
            Assert.Equal(1, dataView[2][0]);
            Assert.Equal(0, dataView[3][0]);

            s = "Reverted to default sorting: ";
            dataView.Sort = null;
            Assert.Equal(1, dataView[0][0]);
            Assert.Equal(0, dataView[1][0]);
            Assert.Equal(3, dataView[2][0]);
            Assert.Equal(2, dataView[3][0]);
        }

        #endregion // Sort Tests

        [Fact]
        public void AddNew_1()
        {
            Assert.Throws<DataException>(() =>
            {
                _dataView.AllowNew = false;
                DataRowView drv = _dataView.AddNew();
            });
        }

        [Fact]
        public void AddNew_2()
        {
            _dataView.AllowNew = true;
            DataRowView drv = _dataView.AddNew();
            Assert.Equal(ListChangedType.ItemAdded, _listChangedArgs.ListChangedType);
            Assert.Equal(-1, _listChangedArgs.OldIndex);
            Assert.Equal(5, _listChangedArgs.NewIndex);
            Assert.Equal(drv["itemName"], _dataView[_dataView.Count - 1]["itemName"]);
            _listChangedArgs = null;
            drv["itemId"] = "item " + 1001;
            drv["itemName"] = "name " + _rndm.Next();
            drv["itemPrice"] = "Rs. " + (_rndm.Next() % 1000);
            drv["itemCategory"] = "Cat " + ((_rndm.Next() % 10) + 1);
            // Actually no events are arisen when items are set.
            Assert.Null(_listChangedArgs);
            drv.CancelEdit();
            Assert.Equal(ListChangedType.ItemDeleted, _listChangedArgs.ListChangedType);
            Assert.Equal(-1, _listChangedArgs.OldIndex);
            Assert.Equal(5, _listChangedArgs.NewIndex);
        }

        [Fact]
        public void BeginInit()
        {
            DataTable table = new DataTable("table");
            DataView dv = new DataView();
            DataColumn col1 = new DataColumn("col1");
            DataColumn col2 = new DataColumn("col2");

            dv.BeginInit();
            table.BeginInit();
            table.Columns.AddRange(new DataColumn[] { col1, col2 });

            dv.Table = table;
            Assert.Null(dv.Table);
            dv.EndInit();

            Assert.Null(dv.Table);
            Assert.Equal(0, table.Columns.Count);

            table.EndInit();
            Assert.Equal(table, dv.Table);
            Assert.Equal(2, table.Columns.Count);
        }

        private bool _dvInitialized;
        private void OnDataViewInitialized(object src, EventArgs args)
        {
            _dvInitialized = true;
        }
        [Fact]
        public void BeginInit2()
        {
            DataTable table = new DataTable("table");
            DataView dv = new DataView();
            DataColumn col1 = new DataColumn("col1");
            DataColumn col2 = new DataColumn("col2");

            _dvInitialized = false;

            dv.Initialized += new EventHandler(OnDataViewInitialized);

            dv.BeginInit();
            table.BeginInit();
            table.Columns.AddRange(new DataColumn[] { col1, col2 });

            dv.Table = table;
            Assert.Null(dv.Table);
            dv.EndInit();

            Assert.Null(dv.Table);
            Assert.Equal(0, table.Columns.Count);

            table.EndInit();

            dv.Initialized -= new EventHandler(OnDataViewInitialized); // this should not be unregistered before table.EndInit().

            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(table, dv.Table);
            Assert.Equal(true, _dvInitialized);
        }

        [Fact]
        public void Find_1()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                /* since the sort key is not specified. Must raise a ArgumentException */
                int sIndex = _dataView.Find("abc");
            });
        }

        [Fact]
        public void Find_2()
        {
            int randInt;
            DataRowView drv;
            randInt = _rndm.Next() % _rowCount;
            _dataView.Sort = "itemId";
            drv = _dataView[randInt];
            Assert.Equal(randInt, _dataView.Find(drv["itemId"]));

            _dataView.Sort = "itemId DESC";
            drv = _dataView[randInt];
            Assert.Equal(randInt, _dataView.Find(drv["itemId"]));

            _dataView.Sort = "itemId, itemName";
            drv = _dataView[randInt];
            object[] keys = new object[2];
            keys[0] = drv["itemId"];
            keys[1] = drv["itemName"];
            Assert.Equal(randInt, _dataView.Find(keys));

            _dataView.Sort = "itemId";
            Assert.Equal(-1, _dataView.Find("no item"));
        }

        [Fact]
        public void Find_3()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _dataView.Sort = "itemID, itemName";
                /* expecting order key count mismatch */
                _dataView.Find("itemValue");
            });
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("System.Data.DataView", _dataView.ToString());
        }

        [Fact]
        public void TestingEventHandling()
        {
            _dataView.Sort = "itemId";
            DataRow dr;
            dr = _dataTable.NewRow();
            dr["itemId"] = "item 0";
            dr["itemName"] = "name " + _rndm.Next();
            dr["itemPrice"] = "Rs. " + (_rndm.Next() % 1000);
            dr["itemCategory"] = "Cat " + ((_rndm.Next() % 10) + 1);
            _dataTable.Rows.Add(dr);

            //PrintTableOrView(dataView, "ItemAdded");
            Assert.Equal(ListChangedType.ItemAdded, _listChangedArgs.ListChangedType);
            _listChangedArgs = null;

            dr["itemId"] = "aitem 0";
            // PrintTableOrView(dataView, "ItemChanged");
            Assert.Equal(ListChangedType.ItemChanged, _listChangedArgs.ListChangedType);
            _listChangedArgs = null;

            dr["itemId"] = "zitem 0";
            // PrintTableOrView(dataView, "ItemMoved");
            Assert.Equal(ListChangedType.ItemMoved, _listChangedArgs.ListChangedType);
            _listChangedArgs = null;

            _dataTable.Rows.Remove(dr);
            // PrintTableOrView(dataView, "ItemDeleted");
            Assert.Equal(ListChangedType.ItemDeleted, _listChangedArgs.ListChangedType);

            _listChangedArgs = null;
            DataColumn dc5 = new DataColumn("itemDesc");
            _dataTable.Columns.Add(dc5);
            // PrintTableOrView(dataView, "PropertyDescriptorAdded");
            Assert.Equal(ListChangedType.PropertyDescriptorAdded, _listChangedArgs.ListChangedType);

            _listChangedArgs = null;
            dc5.ColumnName = "itemDescription";
            // PrintTableOrView(dataView, "PropertyDescriptorChanged");
            // Assert.Equal ("test#06",ListChangedType.PropertyDescriptorChanged);

            _listChangedArgs = null;
            _dataTable.Columns.Remove(dc5);
            // PrintTableOrView(dataView, "PropertyDescriptorDeleted");
            Assert.Equal(ListChangedType.PropertyDescriptorDeleted, _listChangedArgs.ListChangedType);
        }

        [Fact]
        public void TestFindRows()
        {
            DataView TestView = new DataView(_dataTable);
            TestView.Sort = "itemId";
            DataRowView[] Result = TestView.FindRows("item 3");
            Assert.Equal(1, Result.Length);
            Assert.Equal("item 3", Result[0]["itemId"]);
        }

        [Fact]
        public void FindRowsWithoutSort()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                DataTable dt = new DataTable("table");
                dt.Columns.Add("col1");
                dt.Columns.Add("col2");
                dt.Columns.Add("col3");
                dt.Rows.Add(new object[] { 1, 2, 3 });
                dt.Rows.Add(new object[] { 4, 5, 6 });
                dt.Rows.Add(new object[] { 4, 7, 8 });
                dt.Rows.Add(new object[] { 5, 7, 8 });
                dt.Rows.Add(new object[] { 4, 8, 9 });
                DataView dv = new DataView(dt);
                dv.Find(1);
            });
        }

        [Fact]
        public void FindRowsInconsistentKeyLength()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                DataTable dt = new DataTable("table");
                dt.Columns.Add("col1");
                dt.Columns.Add("col2");
                dt.Columns.Add("col3");
                dt.Rows.Add(new object[] { 1, 2, 3 });
                dt.Rows.Add(new object[] { 4, 5, 6 });
                dt.Rows.Add(new object[] { 4, 7, 8 });
                dt.Rows.Add(new object[] { 5, 7, 8 });
                dt.Rows.Add(new object[] { 4, 8, 9 });
                DataView dv = new DataView(dt, null, "col1",
                    DataViewRowState.CurrentRows);
                dv.FindRows(new object[] { 1, 2, 3 });
            });
        }

        [Fact]
        public void TestDelete()
        {
            Assert.Throws<DeletedRowInaccessibleException>(() =>
            {
                DataView TestView = new DataView(_dataTable);
                TestView.Delete(0);
                DataRow r = TestView.Table.Rows[0];
                Assert.True(!((string)r["itemId"] == "item 1"));
            });
        }

        [Fact]
        public void TestDeleteOutOfBounds()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                DataView TestView = new DataView(_dataTable);
                TestView.Delete(100);
            });
        }

        [Fact]
        public void TestDeleteNotAllowed()
        {
            Assert.Throws<DataException>(() =>
            {
                DataView TestView = new DataView(_dataTable);
                TestView.AllowDelete = false;
                TestView.Delete(0);
            });
        }

        [Fact]
        public void TestDeleteClosed()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                DataView TestView = new DataView(_dataTable);
                TestView.Dispose(); // Close the table
                TestView.Delete(0); // cannot access to item at 0.
            });
        }

        [Fact]
        public void TestDeleteAndCount()
        {
            DataSet dataset = new DataSet("new");
            DataTable dt = new DataTable("table1");
            dataset.Tables.Add(dt);
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            dt.Rows.Add(new object[] { 1, 1 });
            dt.Rows.Add(new object[] { 1, 2 });
            dt.Rows.Add(new object[] { 1, 3 });

            DataView dataView = new DataView(dataset.Tables[0]);

            Assert.Equal(3, dataView.Count);
            dataView.AllowDelete = true;

            // Deleting the first row
            dataView.Delete(0);

            Assert.Equal(2, dataView.Count);
        }

        [Fact]
        public void ListChangeOnSetItem()
        {
            DataTable dt = new DataTable("table");
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            dt.Columns.Add("col3");
            dt.Rows.Add(new object[] { 1, 2, 3 });
            dt.AcceptChanges();
            DataView dv = new DataView(dt);
            dv.ListChanged += new ListChangedEventHandler(OnChange);
            dv[0]["col1"] = 4;
        }

        private ListChangedEventArgs _listChangeArgOnSetItem;

        private void OnChange(object o, ListChangedEventArgs e)
        {
            if (_listChangeArgOnSetItem != null)
                throw new Exception("The event is already fired.");
            _listChangeArgOnSetItem = e;
        }

        [Fact]
        public void CancelEditAndEvents()
        {
            string reference = " =====ItemAdded:3 ------4 =====ItemAdded:3 =====ItemAdded:4 ------5 =====ItemAdded:4 =====ItemAdded:5 ------6 =====ItemDeleted:5 ------5 =====ItemAdded:5";

            _eventWriter = new StringWriter();

            DataTable dt = new DataTable();
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            dt.Columns.Add("col3");
            dt.Rows.Add(new object[] { 1, 2, 3 });
            dt.Rows.Add(new object[] { 1, 2, 3 });
            dt.Rows.Add(new object[] { 1, 2, 3 });

            DataView dv = new DataView(dt);
            dv.ListChanged += new ListChangedEventHandler(ListChanged);
            DataRowView a1 = dv.AddNew();
            _eventWriter.Write(" ------" + dv.Count);
            // I wonder why but MS fires another event here.
            a1 = dv.AddNew();
            _eventWriter.Write(" ------" + dv.Count);
            // I wonder why but MS fires another event here.
            DataRowView a2 = dv.AddNew();
            _eventWriter.Write(" ------" + dv.Count);
            a2.CancelEdit();
            _eventWriter.Write(" ------" + dv.Count);
            DataRowView a3 = dv.AddNew();

            Assert.Equal(reference, _eventWriter.ToString());
            GC.KeepAlive(dv);
        }

        [Fact]
        public void ColumnChangeName()
        {
            string result = @"setting table...
---- OnListChanged PropertyDescriptorChanged,0,0
---- OnListChanged Reset,-1,-1
table was set.
---- OnListChanged PropertyDescriptorChanged,0,0
";

            _eventWriter = new StringWriter();

            ComplexEventSequence1View dv =
                new ComplexEventSequence1View(_dataTable, _eventWriter);

            _dc2.ColumnName = "new_column_name";

            Assert.Equal(result.Replace("\r\n", "\n"), _eventWriter.ToString().Replace("\r\n", "\n"));
            GC.KeepAlive(dv);
        }

        private void ListChanged(object o, ListChangedEventArgs e)
        {
            _eventWriter.Write(" =====" + e.ListChangedType + ":" + e.NewIndex);
        }

        [Fact]
        public void DefaultColumnNameAddListChangedTest()
        {
            string result = @"setting table...
---- OnListChanged PropertyDescriptorChanged,0,0
---- OnListChanged Reset,-1,-1
table was set.
---- OnListChanged PropertyDescriptorAdded,0,0
 default named column added.
---- OnListChanged PropertyDescriptorAdded,0,0
 non-default named column added.
---- OnListChanged PropertyDescriptorAdded,0,0
 another default named column added (Column2).
---- OnListChanged PropertyDescriptorAdded,0,0
 add a column with the same name as the default columnnames.
---- OnListChanged PropertyDescriptorAdded,0,0
 add a column with a null name.
---- OnListChanged PropertyDescriptorAdded,0,0
 add a column with an empty name.
";
            _eventWriter = new StringWriter();
            DataTable dt = new DataTable("table");
            ComplexEventSequence1View dv =
                new ComplexEventSequence1View(dt, _eventWriter);
            dt.Columns.Add();
            _eventWriter.WriteLine(" default named column added.");
            dt.Columns.Add("non-defaultNamedColumn");
            _eventWriter.WriteLine(" non-default named column added.");
            DataColumn c = dt.Columns.Add();
            _eventWriter.WriteLine(" another default named column added ({0}).", c.ColumnName);
            dt.Columns.Add("Column3");
            _eventWriter.WriteLine(" add a column with the same name as the default columnnames.");
            dt.Columns.Add((string)null);
            _eventWriter.WriteLine(" add a column with a null name.");
            dt.Columns.Add("");
            _eventWriter.WriteLine(" add a column with an empty name.");

            Assert.Equal(result.Replace("\r\n", "\n"), _eventWriter.ToString().Replace("\r\n", "\n"));
            GC.KeepAlive(dv);
        }

        public class ComplexEventSequence1View : DataView
        {
            private TextWriter _w;

            public ComplexEventSequence1View(DataTable dt,
                TextWriter w) : base()
            {
                _w = w;
                w.WriteLine("setting table...");
                Table = dt;
                w.WriteLine("table was set.");
            }

            protected override void OnListChanged(ListChangedEventArgs e)
            {
                if (_w != null)
                    _w.WriteLine("---- OnListChanged " + e.ListChangedType + "," + e.NewIndex + "," + e.OldIndex);
                base.OnListChanged(e);
            }

            protected override void UpdateIndex(bool force)
            {
                if (_w != null)
                    _w.WriteLine("----- UpdateIndex : " + force);
                base.UpdateIndex(force);
            }
        }
    }
}
