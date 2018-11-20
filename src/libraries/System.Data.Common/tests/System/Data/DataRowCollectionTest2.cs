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


namespace System.Data.Tests
{
    public class DataRowCollectionTest2
    {
        [Fact]
        public void CopyTo()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow[] arr = new DataRow[dt.Rows.Count];
            dt.Rows.CopyTo(arr, 0);
            Assert.Equal(dt.Rows.Count, arr.Length);

            int index = 0;
            foreach (DataRow dr in dt.Rows)
            {
                Assert.Equal(dr, arr[index]);
                index++;
            }
        }

        [Fact]
        public void Count()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            Assert.Equal(6, dt.Rows.Count);
            dt.Rows.Remove(dt.Rows[0]);
            Assert.Equal(5, dt.Rows.Count);
            dt.Rows.Add(new object[] { 1, "1-String1", "1-String2", new DateTime(2005, 1, 1, 0, 0, 0, 0), 1.534, true });
            Assert.Equal(6, dt.Rows.Count);
        }

        [Fact]
        public void GetEnumerator()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            IEnumerator myEnumerator = dt.Rows.GetEnumerator();
            int index = 0;
            while (myEnumerator.MoveNext())
            {
                Assert.Equal(dt.Rows[index], (DataRow)myEnumerator.Current);
                index++;
            }
            Assert.Equal(index, dt.Rows.Count);
        }

        [Fact]
        public void RemoveAt_ByIndex()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            int counter = dt.Rows.Count;
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };
            dt.Rows.RemoveAt(3);
            Assert.Equal(counter - 1, dt.Rows.Count);
            Assert.Equal(null, dt.Rows.Find(4));
        }

        [Fact]
        public void Remove_ByDataRow()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            int counter = dt.Rows.Count;
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };
            Assert.Equal(dt.Rows[0], dt.Rows.Find(1));
            dt.Rows.Remove(dt.Rows[0]);
            Assert.Equal(counter - 1, dt.Rows.Count);
            Assert.Equal(null, dt.Rows.Find(1));
        }

        [Fact]
        public void DataRowCollection_Add_D1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Rows.Clear();
            DataRow dr = dt.NewRow();
            dr["ParentId"] = 10;
            dr["String1"] = "string1";
            dr["String2"] = string.Empty;
            dr["ParentDateTime"] = new DateTime(2004, 12, 15);
            dr["ParentDouble"] = 3.14;
            dr["ParentBool"] = false;

            dt.Rows.Add(dr);

            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(dr, dt.Rows[0]);
        }

        [Fact]
        public void DataRowCollection_Add_D2()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();
               dt.Rows.Add(dt.Rows[0]);
           });
        }

        [Fact]
        public void DataRowCollection_Add_D3()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                DataTable dt = DataProvider.CreateParentDataTable();
                dt.Rows.Add((DataRow)null);
            });
        }

        [Fact]
        public void DataRowCollection_Add_D4()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataTable dt = DataProvider.CreateParentDataTable();
                DataTable dt1 = DataProvider.CreateParentDataTable();

                dt.Rows.Add(dt1.Rows[0]);
            });
        }

        [Fact]
        public void DataRowCollection_Add_O1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Rows.Clear();
            dt.Rows.Add(new object[] { 1, "1-String1", "1-String2", new DateTime(2005, 1, 1, 0, 0, 0, 0), 1.534, true });
            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(1, dt.Rows[0]["ParentId"]);
            Assert.Equal("1-String1", dt.Rows[0]["String1"]);
            Assert.Equal("1-String2", dt.Rows[0]["String2"]);
            Assert.Equal(new DateTime(2005, 1, 1, 0, 0, 0, 0), dt.Rows[0]["ParentDateTime"]);
            Assert.Equal(1.534, dt.Rows[0]["ParentDouble"]);
            Assert.Equal(true, dt.Rows[0]["ParentBool"]);
        }

        [Fact]
        public void DataRowCollection_Add_O2()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            int count = dt.Rows.Count;
            dt.Rows.Add(new object[] { 8, "1-String1", "1-String2", new DateTime(2005, 1, 1, 0, 0, 0, 0), 1.534 });
            Assert.Equal(count + 1, dt.Rows.Count);
        }

        [Fact]
        public void DataRowCollection_Add_O4()
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                DataTable dt = DataProvider.CreateParentDataTable();
                dt.Rows.Add((object[])null);
            });
        }

        [Fact]
        public void FindByKey()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.Rows.Add(new object[] { 1 });
            table.Rows.Add(new object[] { 2 });
            table.Rows.Add(new object[] { 3 });
            table.AcceptChanges();

            Assert.NotNull(table.Rows.Find(new object[] { 1 }));

            table.Rows[0].Delete();
            Assert.Null(table.Rows.Find(new object[] { 1 }));

            table.RejectChanges();
            Assert.NotNull(table.Rows.Find(new object[] { 1 }));
        }

        [Fact]
        public void FindByKey_VerifyOrder()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.Rows.Add(new object[] { 1 });
            table.Rows.Add(new object[] { 2 });
            table.Rows.Add(new object[] { 1000 });
            table.AcceptChanges();

            table.Rows[1][0] = 100;
            Assert.NotNull(table.Rows.Find(100));

            table.Rows[2][0] = 999;
            Assert.NotNull(table.Rows.Find(999));
            Assert.NotNull(table.Rows.Find(100));
        }

        [Fact]
        public void FindByKey_DuringDataLoad()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.Rows.Add(new object[] { 1 });
            table.Rows.Add(new object[] { 2 });
            table.AcceptChanges();

            table.BeginLoadData();
            table.LoadDataRow(new object[] { 1000 }, false);
            Assert.NotNull(table.Rows.Find(1));
            Assert.NotNull(table.Rows.Find(1000));
            table.EndLoadData();
            Assert.NotNull(table.Rows.Find(1000));
        }

        [Fact]
        public void DataRowCollection_Clear1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            int count = dt.Rows.Count;
            Assert.Equal(count != 0, true);
            dt.Rows.Clear();
            Assert.Equal(0, dt.Rows.Count);
        }

        [Fact]
        public void DataRowCollection_Clear2()
        {
            Assert.Throws<InvalidConstraintException>(() =>
            {
                DataSet ds = DataProvider.CreateForeignConstraint();

                ds.Tables[0].Rows.Clear(); //Try to clear the parent table		
            });
        }

        [Fact]
        public void DataRowCollection_Contains_O1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            Assert.Equal(true, dt.Rows.Contains(1));
            Assert.Equal(false, dt.Rows.Contains(10));
        }

        [Fact]
        public void DataRowCollection_Contains_O2()
        {
            Assert.Throws<MissingPrimaryKeyException>(() =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();
               Assert.Equal(false, dt.Rows.Contains(1));
           });
        }

        [Fact]
        public void DataRowCollection_Contains_O3()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0], dt.Columns[1] };

            //Prepare values array
            object[] arr = new object[2];
            arr[0] = 1;
            arr[1] = "1-String1";

            Assert.Equal(true, dt.Rows.Contains(arr));

            arr[0] = 8;

            Assert.Equal(false, dt.Rows.Contains(arr));
        }

        [Fact]
        public void DataRowCollection_Contains_O4()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();
               dt.PrimaryKey = new DataColumn[] { dt.Columns[0], dt.Columns[1] };

               //Prepare values array
               object[] arr = new object[1];
               arr[0] = 1;

               Assert.Equal(false, dt.Rows.Contains(arr));
           });
        }

        [Fact]
        public void DataRowCollection_Find_O1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            Assert.Equal(dt.Rows[0], dt.Rows.Find(1));
            Assert.Equal(null, dt.Rows.Find(10));
        }

        [Fact]
        public void DataRowCollection_Find_O2()
        {
            Assert.Throws<MissingPrimaryKeyException>(() =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();

               Assert.Equal(null, dt.Rows.Find(1));
           });
        }

        [Fact]
        public void DataRowCollection_Find_O3()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0], dt.Columns[1] };

            //Prepare values array
            object[] arr = new object[2];
            arr[0] = 2;
            arr[1] = "2-String1";

            Assert.Equal(dt.Rows[1], dt.Rows.Find(arr));

            arr[0] = 8;

            Assert.Equal(null, dt.Rows.Find(arr));
        }

        [Fact]
        public void DataRowCollection_Find_O4()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();
               dt.PrimaryKey = new DataColumn[] { dt.Columns[0], dt.Columns[1] };

               //Prepare values array
               object[] arr = new object[1];
               arr[0] = 1;

               Assert.Equal(null, dt.Rows.Find(arr));
           });
        }

        [Fact]
        public void DataRowCollection_InsertAt_DI1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow dr = GetNewDataRow(dt);
            dt.Rows.InsertAt(dr, 0);

            Assert.Equal(dr, dt.Rows[0]);
        }

        [Fact]
        public void DataRowCollection_InsertAt_DI2()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow dr = GetNewDataRow(dt);
            dt.Rows.InsertAt(dr, 3);

            Assert.Equal(dr, dt.Rows[3]);
        }

        [Fact]
        public void DataRowCollection_InsertAt_DI3()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow dr = GetNewDataRow(dt);
            dt.Rows.InsertAt(dr, 300);

            Assert.Equal(dr, dt.Rows[dt.Rows.Count - 1]);
        }

        [Fact]
        public void DataRowCollection_InsertAt_DI4()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();
               DataRow dr = GetNewDataRow(dt);

               dt.Rows.InsertAt(dr, -1);
           });
        }

        private DataRow GetNewDataRow(DataTable dt)
        {
            DataRow dr = dt.NewRow();
            dr["ParentId"] = 10;
            dr["String1"] = "string1";
            dr["String2"] = string.Empty;
            dr["ParentDateTime"] = new DateTime(2004, 12, 15);
            dr["ParentDouble"] = 3.14;
            dr["ParentBool"] = false;
            return dr;
        }

        [Fact]
        public void DataRowCollection_Item1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            int index = 0;

            foreach (DataRow dr in dt.Rows)
            {
                Assert.Equal(dr, dt.Rows[index]);
                index++;
            }
        }

        [Fact]
        public void DataRowCollection_Item2()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
           {
               DataTable dt = DataProvider.CreateParentDataTable();

               DataRow dr = dt.Rows[-1];
           });
        }
    }
}
