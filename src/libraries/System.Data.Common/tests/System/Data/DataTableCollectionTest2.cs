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
using System.Collections;
using System.ComponentModel;


namespace System.Data.Tests
{
    public class DataTableCollectionTest2
    {
        private int _counter = 0;

        [Fact]
        public void Add()
        {
            // Adding computed column to a data set
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("Table"));
            ds.Tables[0].Columns.Add(new DataColumn("EmployeeNo", typeof(string)));
            ds.Tables[0].Rows.Add(new object[] { "Maciek" });
            ds.Tables[0].Columns.Add("ComputedColumn", typeof(object), "EmployeeNo");

            Assert.Equal("EmployeeNo", ds.Tables[0].Columns["ComputedColumn"].Expression);
        }

        [Fact]
        public void AddTwoTables()
        {
            var ds = new DataSet();
            ds.Tables.Add();
            Assert.Equal("Table1", ds.Tables[0].TableName);
            //Assert.Equal(ds.Tables[0].TableName,"Table1");
            ds.Tables.Add();
            Assert.Equal("Table2", ds.Tables[1].TableName);
            //Assert.Equal(ds.Tables[1].TableName,"Table2");
        }

        [Fact]
        public void AddRange()
        {
            var ds = new DataSet();

            DataTable[] arr = new DataTable[2];

            arr[0] = new DataTable("NewTable1");
            arr[1] = new DataTable("NewTable2");

            ds.Tables.AddRange(arr);
            Assert.Equal("NewTable1", ds.Tables[0].TableName);
            Assert.Equal("NewTable2", ds.Tables[1].TableName);
        }
        [Fact]
        public void AddRange_NullValue()
        {
            var ds = new DataSet();
            ds.Tables.AddRange(null);
        }

        [Fact]
        public void AddRange_ArrayWithNull()
        {
            var ds = new DataSet();
            DataTable[] arr = new DataTable[2];
            arr[0] = new DataTable("NewTable1");
            arr[1] = null;
            ds.Tables.AddRange(arr);
            Assert.Equal("NewTable1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
        }

        [Fact]
        public void CanRemove()
        {
            var ds = new DataSet();
            ds.Tables.Add();
            Assert.Equal(true, ds.Tables.CanRemove(ds.Tables[0]));
        }

        [Fact]
        public void CanRemove_NullValue()
        {
            var ds = new DataSet();
            Assert.Equal(false, ds.Tables.CanRemove(null));
        }

        [Fact]
        public void CanRemove_TableDoesntBelong()
        {
            var ds = new DataSet();
            DataSet ds1 = new DataSet();
            ds1.Tables.Add();
            Assert.Equal(false, ds.Tables.CanRemove(ds1.Tables[0]));
        }

        [Fact]
        public void CanRemove_PartOfRelation()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());

            ds.Relations.Add("rel", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"], false);

            Assert.Equal(false, ds.Tables.CanRemove(ds.Tables[0]));
            Assert.Equal(false, ds.Tables.CanRemove(ds.Tables[1]));
        }
        [Fact]
        public void CanRemove_PartOfConstraint()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            Assert.Equal(false, ds.Tables.CanRemove(ds.Tables[0]));
            Assert.Equal(false, ds.Tables.CanRemove(ds.Tables[1]));
        }

        [Fact]
        public void CollectionChanged()
        {
            _counter = 0;
            var ds = new DataSet();
            ds.Tables.CollectionChanged += new CollectionChangeEventHandler(Tables_CollectionChanged);
            ds.Tables.Add();
            ds.Tables.Add();
            Assert.Equal(2, _counter);

            ds.Tables.Remove(ds.Tables[0]);
            ds.Tables.Remove(ds.Tables[0]);
            Assert.Equal(4, _counter);
        }

        private void Tables_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            _counter++;
        }

        [Fact]
        public void CollectionChanging()
        {
            _counter = 0;
            var ds = new DataSet();
            ds.Tables.CollectionChanging += new CollectionChangeEventHandler(Tables_CollectionChanging);
            ds.Tables.Add();
            ds.Tables.Add();
            Assert.Equal(2, _counter);

            ds.Tables.Remove(ds.Tables[0]);
            ds.Tables.Remove(ds.Tables[0]);
            Assert.Equal(4, _counter);
        }

        private void Tables_CollectionChanging(object sender, CollectionChangeEventArgs e)
        {
            _counter++;
        }

        [Fact]
        public void Contains()
        {
            var ds = new DataSet();
            ds.Tables.Add("NewTable1");
            ds.Tables.Add("NewTable2");

            Assert.Equal(true, ds.Tables.Contains("NewTable1"));
            Assert.Equal(true, ds.Tables.Contains("NewTable2"));
            Assert.Equal(false, ds.Tables.Contains("NewTable3"));

            ds.Tables["NewTable1"].TableName = "Tbl1";
            Assert.Equal(false, ds.Tables.Contains("NewTable1"));
            Assert.Equal(true, ds.Tables.Contains("Tbl1"));
        }

        [Fact]
        public void CopyTo()
        {
            var ds = new DataSet();
            ds.Tables.Add();
            ds.Tables.Add();
            DataTable[] arr = new DataTable[2];
            ds.Tables.CopyTo(arr, 0);
            Assert.Equal("Table1", arr[0].TableName);
            Assert.Equal("Table2", arr[1].TableName);
        }

        [Fact]
        public void Count()
        {
            var ds = new DataSet();
            Assert.Equal(0, ds.Tables.Count);

            ds.Tables.Add();
            Assert.Equal(1, ds.Tables.Count);

            ds.Tables.Add();
            Assert.Equal(2, ds.Tables.Count);

            ds.Tables.Remove("Table1");
            Assert.Equal(1, ds.Tables.Count);

            ds.Tables.Remove("Table2");
            Assert.Equal(0, ds.Tables.Count);
        }

        [Fact]
        public void GetEnumerator()
        {
            var ds = new DataSet();
            ds.Tables.Add();
            ds.Tables.Add();
            int count = 0;

            IEnumerator myEnumerator = ds.Tables.GetEnumerator();

            while (myEnumerator.MoveNext())
            {
                Assert.Equal("Table", ((DataTable)myEnumerator.Current).TableName.Substring(0, 5));
                count++;
            }
            Assert.Equal(2, count);
        }
        public void IndexOf_ByDataTable()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            DataTable dt1 = new DataTable("NewTable2");
            ds.Tables.AddRange(new DataTable[] { dt, dt1 });

            Assert.Equal(0, ds.Tables.IndexOf(dt));
            Assert.Equal(1, ds.Tables.IndexOf(dt1));

            ds.Tables.IndexOf((DataTable)null);

            DataTable dt2 = new DataTable("NewTable2");

            Assert.Equal(-1, ds.Tables.IndexOf(dt2));
        }

        public void IndexOf_ByName()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            DataTable dt1 = new DataTable("NewTable2");
            ds.Tables.AddRange(new DataTable[] { dt, dt1 });

            Assert.Equal(0, ds.Tables.IndexOf("NewTable1"));
            Assert.Equal(1, ds.Tables.IndexOf("NewTable2"));

            ds.Tables.IndexOf((string)null);

            Assert.Equal(-1, ds.Tables.IndexOf("NewTable3"));
        }

        [Fact]
        public void Item()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            DataTable dt1 = new DataTable("NewTable2");
            ds.Tables.AddRange(new DataTable[] { dt, dt1 });

            Assert.Equal(dt, ds.Tables[0]);
            Assert.Equal(dt1, ds.Tables[1]);
            Assert.Equal(dt, ds.Tables["NewTable1"]);
            Assert.Equal(dt1, ds.Tables["NewTable2"]);
        }

        [Fact]
        public void DataTableCollection_Add_D1()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            ds.Tables.Add(dt);
            Assert.Equal("NewTable1", ds.Tables[0].TableName);
        }

        [Fact]
        public void DataTableCollection_Add_D2()
        {
            var ds = new DataSet();
            Assert.Throws<ArgumentNullException>(() =>
           {
               ds.Tables.Add((DataTable)null);
           });
        }

        [Fact]
        public void DataTableCollection_Add_D3()
        {
            var ds = new DataSet();
            DataSet ds1 = new DataSet();
            ds1.Tables.Add();

            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables.Add(ds1.Tables[0]));
        }

        [Fact]
        public void DataTableCollection_Add_D4()
        {
            var ds = new DataSet();
            ds.Tables.Add();

            DataTable dt = new DataTable("Table1");
            Assert.Throws<DuplicateNameException>(() => ds.Tables.Add(dt));
        }

        [Fact]
        public void DataTableCollection_Add_S1()
        {
            var ds = new DataSet();
            ds.Tables.Add("NewTable1");
            Assert.Equal("NewTable1", ds.Tables[0].TableName);
            ds.Tables.Add("NewTable2");
            Assert.Equal("NewTable2", ds.Tables[1].TableName);
        }

        [Fact]
        public void DataTableCollection_Add_S2()
        {
            var ds = new DataSet();
            ds.Tables.Add("NewTable1");

            Assert.Throws<DuplicateNameException>(() => ds.Tables.Add("NewTable1"));
        }

        [Fact]
        public void DataTableCollection_Clear1()
        {
            var ds = new DataSet();
            ds.Tables.Add();
            ds.Tables.Add();
            ds.Tables.Clear();
            Assert.Equal(0, ds.Tables.Count);
        }

        [Fact]
        public void DataTableCollection_Clear2()
        {
            var ds = new DataSet();
            ds.Tables.Add();
            ds.Tables.Add();
            ds.Tables.Clear();

            Assert.Throws<IndexOutOfRangeException>(() => ds.Tables[0].TableName = "Error");
        }

        [Fact]
        public void DataTableCollection_Remove_D1()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            DataTable dt1 = new DataTable("NewTable2");
            ds.Tables.AddRange(new DataTable[] { dt, dt1 });

            ds.Tables.Remove(dt);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal(dt1, ds.Tables[0]);
            ds.Tables.Remove(dt1);
            Assert.Equal(0, ds.Tables.Count);
        }

        [Fact]
        public void DataTableCollection_Remove_D2()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");

            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables.Remove(dt));
        }

        [Fact]
        public void DataTableCollection_Remove_D3()
        {
            var ds = new DataSet();

            Assert.Throws<ArgumentNullException>(() => ds.Tables.Remove((DataTable)null));
        }

        [Fact]
        public void DataTableCollection_Remove_S1()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            DataTable dt1 = new DataTable("NewTable2");
            ds.Tables.AddRange(new DataTable[] { dt, dt1 });

            ds.Tables.Remove("NewTable1");
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal(dt1, ds.Tables[0]);
            ds.Tables.Remove("NewTable2");
            Assert.Equal(0, ds.Tables.Count);
        }

        [Fact]
        public void DataTableCollection_Remove_S2()
        {
            var ds = new DataSet();

            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables.Remove("NewTable2"));
        }

        [Fact]
        public void DataTableCollection_Remove_S3()
        {
            var ds = new DataSet();

            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables.Remove((string)null));
        }

        [Fact]
        public void DataTableCollection_RemoveAt_I1()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("NewTable1");
            DataTable dt1 = new DataTable("NewTable2");
            ds.Tables.AddRange(new DataTable[] { dt, dt1 });

            ds.Tables.RemoveAt(1);
            Assert.Equal(dt, ds.Tables[0]);
            ds.Tables.RemoveAt(0);
            Assert.Equal(0, ds.Tables.Count);
        }

        [Fact]
        public void DataTableCollection_RemoveAt_I2()
        {
            var ds = new DataSet();

            Assert.Throws<IndexOutOfRangeException>(() => ds.Tables.RemoveAt(-1));
        }

        [Fact]
        public void DataTableCollection_RemoveAt_I3()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();

            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables.RemoveAt(0)); //Parent table
        }

        [Fact]
        public void AddTable_DiffNamespaceTest()
        {
            var ds = new DataSet();
            ds.Tables.Add("table", "namespace1");
            ds.Tables.Add("table", "namespace2");
            Assert.Equal(2, ds.Tables.Count);

            try
            {
                ds.Tables.Add("table", "namespace1");
                Assert.False(true);
            }
            catch (DuplicateNameException e) { }

            ds.Tables.Add("table");
            try
            {
                ds.Tables.Add("table", null);
                Assert.False(true);
            }
            catch (DuplicateNameException e) { }
        }

        [Fact]
        public void Contains_DiffNamespaceTest()
        {
            var ds = new DataSet();
            ds.Tables.Add("table");
            Assert.True(ds.Tables.Contains("table"));

            ds.Tables.Add("table", "namespace1");
            ds.Tables.Add("table", "namespace2");

            // Should fail if it cannot be resolved to a single table
            Assert.False(ds.Tables.Contains("table"));

            try
            {
                ds.Tables.Contains("table", null);
                Assert.False(true);
            }
            catch (ArgumentNullException e) { }

            Assert.True(ds.Tables.Contains("table", "namespace1"));
            Assert.False(ds.Tables.Contains("table", "namespace3"));
        }

        [Fact]
        public void IndexOf_DiffNamespaceTest()
        {
            var ds = new DataSet();
            ds.Tables.Add("table");
            Assert.Equal(0, ds.Tables.IndexOf("table"));
            ds.Tables.Add("table", "namespace1");
            ds.Tables.Add("table", "namespace2");
            Assert.Equal(-1, ds.Tables.IndexOf("table"));
            Assert.Equal(2, ds.Tables.IndexOf("table", "namespace2"));
            Assert.Equal(1, ds.Tables.IndexOf("table", "namespace1"));
        }

        [Fact]
        public void Remove_DiffNamespaceTest()
        {
            var ds = new DataSet();
            ds.Tables.Add("table");
            ds.Tables.Add("table", "namespace1");
            ds.Tables.Add("table", "namespace2");

            try
            {
                ds.Tables.Remove("table");
                Assert.False(true);
            }
            catch (ArgumentException e) { }

            ds.Tables.Remove("table", "namespace2");
            Assert.Equal(2, ds.Tables.Count);
            Assert.Equal("namespace1", ds.Tables[1].Namespace);

            try
            {
                ds.Tables.Remove("table", "namespace2");
                Assert.False(true);
            }
            catch (ArgumentException e) { }
        }
    }
}
