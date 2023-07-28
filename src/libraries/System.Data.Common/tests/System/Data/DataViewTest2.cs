// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Collections.Generic;

namespace System.Data.Tests
{
    public class DataViewTest2
    {
        private EventProperties _evProp = null;

        private class EventProperties  //hold the event properties to be checked
        {
            public ListChangedType lstType;
            public int NewIndex;
            public int OldIndex;
        }

        [Fact]
        public void AddNew()
        {
            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            int CountView = dv.Count;
            int CountTable = dt.Rows.Count;

            DataRowView drv = null;

            // AddNew - DataView Row Count
            drv = dv.AddNew();
            Assert.Equal(dv.Count, CountView + 1);

            // AddNew - Table Row Count
            Assert.Equal(dt.Rows.Count, CountTable);

            // AddNew - new row in DataTable
            drv.EndEdit();
            Assert.Equal(dt.Rows.Count, CountTable + 1);

            // AddNew - new row != null
            Assert.NotNull(drv);

            // AddNew - check table
            Assert.Equal(dt, drv.Row.Table);
        }

        [Fact]
        public void AllowDelete()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            // AllowDelete - default value
            Assert.True(dv.AllowDelete);

            // AllowDelete - true
            dv.AllowDelete = true;
            Assert.True(dv.AllowDelete);

            // AllowDelete - false
            dv.AllowDelete = false;
            Assert.False(dv.AllowDelete);

            dv.AllowDelete = false;
            // AllowDelete false- Exception
            Assert.Throws<DataException>(() =>
            {
                dv.Delete(0);
            });

            dv.AllowDelete = true;
            int RowsCount = dv.Count;
            // AllowDelete true- Exception
            dv.Delete(0);
            Assert.Equal(RowsCount - 1, dv.Count);
        }

        [Fact]
        public void AllowEdit()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            // AllowEdit - default value
            Assert.True(dv.AllowEdit);

            // AllowEdit - true
            dv.AllowEdit = true;
            Assert.True(dv.AllowEdit);

            // AllowEdit - false
            dv.AllowEdit = false;
            Assert.False(dv.AllowEdit);

            dv.AllowEdit = false;

            // AllowEdit false - exception
            Assert.Throws<DataException>(() =>
            {
                dv[0][2] = "aaa";
            });

            dv.AllowEdit = true;

            // AllowEdit true- exception
            dv[0][2] = "aaa";
            Assert.Equal("aaa", dv[0][2]);
        }

        [Fact]
        public void AllowNew()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            // AllowNew - default value
            Assert.True(dv.AllowNew);

            // AllowNew - true
            dv.AllowNew = true;
            Assert.True(dv.AllowNew);

            // AllowNew - false
            dv.AllowNew = false;
            Assert.False(dv.AllowNew);

            // AllowNew - exception
            Assert.Throws<DataException>(() =>
            {
                dv.AddNew();
            });

            dv.AllowNew = true;
            int RowsCount = dv.Count;

            // AllowNew - exception
            dv.AddNew();
            Assert.Equal(RowsCount + 1, dv.Count);
        }

        [Fact]
        public void ApplyDefaultSort()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            // ApplyDefaultSort - default value
            Assert.False(dv.ApplyDefaultSort);

            // ApplyDefaultSort - true
            dv.ApplyDefaultSort = true;
            Assert.True(dv.ApplyDefaultSort);

            // ApplyDefaultSort - false
            dv.ApplyDefaultSort = false;
            Assert.False(dv.ApplyDefaultSort);
        }

        [Fact]
        public void CopyTo()
        {
            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            DataRowView[] drvExpected = null;
            DataRowView[] drvResult = null;

            // ------- Copy from Index=0
            drvExpected = new DataRowView[dv.Count];
            for (int i = 0; i < dv.Count; i++)
            {
                drvExpected[i] = dv[i];
            }

            drvResult = new DataRowView[dv.Count];
            // CopyTo from index 0
            dv.CopyTo(drvResult, 0);
            Assert.Equal(drvResult, drvExpected);

            // ------- Copy from Index=3
            drvExpected = new DataRowView[dv.Count + 3];
            for (int i = 0; i < dv.Count; i++)
            {
                drvExpected[i + 3] = dv[i];
            }

            drvResult = new DataRowView[dv.Count + 3];
            // CopyTo from index 3
            dv.CopyTo(drvResult, 3);
            Assert.Equal(drvResult, drvExpected);

            // ------- Copy from Index=3,larger array
            drvExpected = new DataRowView[dv.Count + 9];
            for (int i = 0; i < dv.Count; i++)
            {
                drvExpected[i + 3] = dv[i];
            }

            drvResult = new DataRowView[dv.Count + 9];
            // CopyTo from index 3,larger array
            dv.CopyTo(drvResult, 3);
            Assert.Equal(drvResult, drvExpected);

            // ------- CopyTo smaller array, check exception
            drvResult = new DataRowView[dv.Count - 1];

            // CopyTo smaller array, check exception
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                dv.CopyTo(drvResult, 0);
            });
        }

        [Fact]
        public void Delete()
        {
            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            int CountView = dv.Count;
            int CountTable = dt.Rows.Count;

            DataRowView drv = dv[0];

            // Delete - DataView Row Count
            dv.Delete(0);
            Assert.Equal(dv.Count, CountView - 1);

            // Delete - Table Row Count
            Assert.Equal(dt.Rows.Count, CountTable);

            // Delete - check table
            Assert.Equal(dt, drv.Row.Table);
        }

        [Fact]
        public void FindRows_ByKey()
        {
            DataRowView[] dvArr = null;

            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            // FindRows ,no sort - exception
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dvArr = dv.FindRows(3);
            });

            dv.Sort = "String1";
            // Find = wrong sort, can not find
            dvArr = dv.FindRows(3);
            Assert.Equal(0, dvArr.Length);

            dv.Sort = "ChildId";

            //get expected results
            DataRow[] drExpected = dt.Select("ChildId=3");

            // FindRows - check count
            dvArr = dv.FindRows(3);
            Assert.Equal(drExpected.Length, dvArr.Length);

            // FindRows - check data

            //check that result is ok
            bool succeed = true;
            for (int i = 0; i < dvArr.Length; i++)
            {
                succeed = (int)dvArr[i]["ChildId"] == (int)drExpected[i]["ChildId"];
                if (!succeed)
                    break;
            }
            Assert.True(succeed);
        }

        [Fact]
        public void FindRows_ByKeys()
        {
            DataRowView[] dvArr = null;

            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            // FindRows ,no sort - exception
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dvArr = dv.FindRows(new object[] { "3", "3-String1" });
            });

            dv.Sort = "String1,ChildId";
            // Find = wrong sort, can not find
            Assert.Throws<FormatException>(() =>
            {
                dvArr = dv.FindRows(new object[] { "3", "3-String1" });
            });

            dv.Sort = "ChildId,String1";

            //get expected results
            DataRow[] drExpected = dt.Select("ChildId=3 and String1='3-String1'");

            // FindRows - check count
            dvArr = dv.FindRows(new object[] { "3", "3-String1" });
            Assert.Equal(drExpected.Length, dvArr.Length);

            // FindRows - check data

            //check that result is ok
            bool succeed = true;
            for (int i = 0; i < dvArr.Length; i++)
            {
                Assert.Equal((int)drExpected[i]["ChildId"], (int)dvArr[i]["ChildId"]);
            }
        }

        //Activate This Construntor to log All To Standard output
        //public TestClass():base(true){}

        //Activate this constructor to log Failures to a log file
        //public TestClass(System.IO.TextWriter tw):base(tw, false){}

        //Activate this constructor to log All to a log file
        //public TestClass(System.IO.TextWriter tw):base(tw, true){}

        //BY DEFAULT LOGGING IS DONE TO THE STANDARD OUTPUT ONLY FOR FAILURES

        [Fact]
        public void Find_ByObject()
        {
            int findResult, expectedResult = -1;

            //create the source datatable
            DataTable dt = DataProvider.CreateParentDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if ((int)dt.Rows[i]["ParentId"] == 3)
                {
                    expectedResult = i;
                    break;
                }
            }

            // Find ,no sort - exception
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                findResult = dv.Find("3");
            });

            dv.Sort = "String1";
            // Find = wrong sort, can not find
            findResult = dv.Find("3");
            Assert.Equal(-1, findResult);

            dv.Sort = "ParentId";
            // Find
            findResult = dv.Find("3");
            Assert.Equal(expectedResult, findResult);
        }

        [Fact]
        public void Find_ByArray()
        {
            int FindResult, ExpectedResult = -1;

            //create the source datatable
            DataTable dt = DataProvider.CreateParentDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if ((int)dt.Rows[i]["ParentId"] == 3 && dt.Rows[i]["String1"].ToString() == "3-String1")
                {
                    ExpectedResult = i;
                    break;
                }
            }

            // Find ,no sort - exception
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                FindResult = dv.Find(new object[] { "3", "3-String1" });
            });

            dv.Sort = "String1,ParentId";
            // Find = wrong sort, can not find
            Assert.Throws<FormatException>(() =>
            {
                FindResult = dv.Find(new object[] { "3", "3-String1" });
            });

            dv.Sort = "ParentId,String1";
            // Find
            FindResult = dv.Find(new object[] { "3", "3-String1" });
            Assert.Equal(ExpectedResult, FindResult);
        }

        //Activate This Construntor to log All To Standard output
        //public TestClass():base(true){}

        //Activate this constructor to log Failures to a log file
        //public TestClass(System.IO.TextWriter tw):base(tw, false){}

        //Activate this constructor to log All to a log file
        //public TestClass(System.IO.TextWriter tw):base(tw, true){}

        //BY DEFAULT LOGGING IS DONE TO THE STANDARD OUTPUT ONLY FOR FAILURES

        [Fact]
        public void GetEnumerator()
        {
            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            IEnumerator ienm = null;

            // GetEnumerator != null
            ienm = dv.GetEnumerator();
            Assert.NotNull(ienm);

            int i = 0;
            while (ienm.MoveNext())
            {
                // Check item i
                Assert.Equal(dv[i], (DataRowView)ienm.Current);
                i++;
            }
        }

        [Fact]
        public void Item()
        {
            //create the source datatable
            DataTable dt = DataProvider.CreateParentDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            // DataView Item 0
            Assert.Equal(dv[0].Row, dt.Rows[0]);

            // DataView Item 4
            Assert.Equal(dv[4].Row, dt.Rows[4]);

            dv.RowFilter = "ParentId in (1,3,6)";

            // DataView Item 0,DataTable with filter
            Assert.Equal(dv[1].Row, dt.Rows[2]);
        }

        [Fact]
        public void ListChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            //add event handler
            dv.ListChanged += new ListChangedEventHandler(dv_ListChanged);

            // ----- Change Value ---------
            _evProp = null;
            // change value - Event raised
            dv[1]["String1"] = "something";
            Assert.NotNull(_evProp);
            // change value - ListChangedType
            Assert.Equal(ListChangedType.ItemChanged, _evProp.lstType);
            // change value - NewIndex
            Assert.Equal(1, _evProp.NewIndex);
            // change value - OldIndex
            Assert.Equal(1, _evProp.OldIndex);

            // ----- Add New ---------
            _evProp = null;
            // Add New  - Event raised
            dv.AddNew();
            Assert.NotNull(_evProp);
            // Add New  - ListChangedType
            Assert.Equal(ListChangedType.ItemAdded, _evProp.lstType);
            // Add New  - NewIndex
            Assert.Equal(6, _evProp.NewIndex);
            // Add New  - OldIndex
            Assert.Equal(-1, _evProp.OldIndex);

            // ----- Sort ---------
            _evProp = null;
            // sort  - Event raised
            dv.Sort = "ParentId Desc";
            Assert.NotNull(_evProp);
            // sort - ListChangedType
            Assert.Equal(ListChangedType.Reset, _evProp.lstType);
            // sort - NewIndex
            Assert.Equal(-1, _evProp.NewIndex);
            // sort - OldIndex
            Assert.Equal(-1, _evProp.OldIndex);

            //ListChangedType - this was not checked
            //Move
            //PropertyDescriptorAdded - A PropertyDescriptor was added, which changed the schema.
            //PropertyDescriptorChanged - A PropertyDescriptor was changed, which changed the schema.
            //PropertyDescriptorDeleted
        }

        [Fact]
        public void AcceptChanges()
        {
            _evProp = null;
            DataTable dt = new DataTable();
            IBindingList list = dt.DefaultView;
            list.ListChanged += new ListChangedEventHandler(dv_ListChanged);
            dt.Columns.Add("test", typeof(int));
            dt.Rows.Add(new object[] { 10 });
            dt.Rows.Add(new object[] { 20 });
            // ListChangedType.Reset
            dt.AcceptChanges();

            Assert.NotNull(_evProp);
            // AcceptChanges - should emit ListChangedType.Reset
            Assert.Equal(ListChangedType.Reset, _evProp.lstType);
        }

        [Fact]
        public void ClearTable()
        {
            _evProp = null;
            DataTable dt = new DataTable();
            IBindingList list = dt.DefaultView;
            list.ListChanged += new ListChangedEventHandler(dv_ListChanged);
            dt.Columns.Add("test", typeof(int));
            dt.Rows.Add(new object[] { 10 });
            dt.Rows.Add(new object[] { 20 });
            // Clears DataTable
            dt.Clear();

            Assert.NotNull(_evProp);
            // Clear DataTable - should emit ListChangedType.Reset
            Assert.Equal(ListChangedType.Reset, _evProp.lstType);
            // Clear DataTable - should clear view count
            Assert.Equal(0, dt.DefaultView.Count);
        }

        private void dv_ListChanged(object sender, ListChangedEventArgs e)
        {
            _evProp = new EventProperties();
            _evProp.lstType = e.ListChangedType;
            _evProp.NewIndex = e.NewIndex;
            _evProp.OldIndex = e.OldIndex;
        }

        [Fact]
        public void RowFilter()
        {
            //note: this test does not check all the possible row filter expression. this is done in DataTable.Select method.
            // this test also check DataView.Count property

            DataRowView[] drvResult = null;
            List<DataRow> list = new List<DataRow>();

            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            //-------------------------------------------------------------
            //Get expected result
            foreach (DataRow dr in dt.Rows)
            {
                if ((int)dr["ChildId"] == 1)
                {
                    list.Add(dr);
                }
            }

            // RowFilter = 'ChildId=1', check count
            dv.RowFilter = "ChildId=1";
            Assert.Equal(list.Count, dv.Count);

            // RowFilter = 'ChildId=1', check rows
            drvResult = new DataRowView[dv.Count];
            dv.CopyTo(drvResult, 0);
            //check that the filterd rows exists
            bool succeed = true;
            for (int i = 0; i < drvResult.Length; i++)
            {
                succeed = list.Contains(drvResult[i].Row);
                if (!succeed)
                    break;
            }
            Assert.True(succeed);
            //-------------------------------------------------------------

            //-------------------------------------------------------------
            //Get expected result
            list.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((int)dr["ChildId"] == 1 && dr["String1"].ToString() == "1-String1")
                    list.Add(dr);

            // RowFilter - ChildId=1 and String1='1-String1'
            dv.RowFilter = "ChildId=1 and String1='1-String1'";
            Assert.Equal(list.Count, dv.Count);

            // RowFilter = ChildId=1 and String1='1-String1', check rows
            drvResult = new DataRowView[dv.Count];
            dv.CopyTo(drvResult, 0);
            //check that the filterd rows exists
            succeed = true;
            for (int i = 0; i < drvResult.Length; i++)
            {
                succeed = list.Contains(drvResult[i].Row);
                if (!succeed)
                    break;
            }
            Assert.True(succeed);
            //-------------------------------------------------------------

            //EvaluateException
            // RowFilter - check EvaluateException
            Assert.Throws<EvaluateException>(() =>
            {
                dv.RowFilter = "Col=1";
            });

            //SyntaxErrorException 1
            // RowFilter - check SyntaxErrorException 1
            Assert.Throws<SyntaxErrorException>(() =>
            {
                dv.RowFilter = "sum('something')";
            });

            //SyntaxErrorException 2
            // RowFilter - check SyntaxErrorException 2
            Assert.Throws<SyntaxErrorException>(() =>
            {
                dv.RowFilter = "HH**!";
            });
        }

        [Fact]
        public void RowStateFilter()
        {
            /*
                Added           A new row. 4
                CurrentRows     Current rows including unchanged, new, and modified rows. 22
                Deleted         A deleted row. 8
                ModifiedCurrent A current version, which is a modified version of original data (see ModifiedOriginal). 16
                ModifiedOriginal The original version (although it has since been modified and is available as ModifiedCurrent). 32
                None            None. 0
                OriginalRows    Original rows including unchanged and deleted rows. 42
                Unchanged       An unchanged row. 2
             */

            DataTable dt = DataProvider.CreateParentDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            DataRow[] drResult;

            dt.Rows[0].Delete();
            dt.Rows[1]["ParentId"] = 1;
            dt.Rows[2]["ParentId"] = 1;
            dt.Rows[3].Delete();
            dt.Rows.Add(new object[] { 1, "A", "B" });
            dt.Rows.Add(new object[] { 1, "C", "D" });
            dt.Rows.Add(new object[] { 1, "E", "F" });

            //---------- Added --------
            dv.RowStateFilter = DataViewRowState.Added;
            drResult = GetResultRows(dt, DataRowState.Added);
            // Added
            Assert.True(CompareSortedRowsByParentId(dv, drResult));

            //---------- CurrentRows --------
            dv.RowStateFilter = DataViewRowState.CurrentRows;
            drResult = GetResultRows(dt, DataRowState.Unchanged | DataRowState.Added | DataRowState.Modified);
            // CurrentRows
            Assert.True(CompareSortedRowsByParentId(dv, drResult));

            //---------- ModifiedCurrent --------
            dv.RowStateFilter = DataViewRowState.ModifiedCurrent;
            drResult = GetResultRows(dt, DataRowState.Modified);
            // ModifiedCurrent
            Assert.True(CompareSortedRowsByParentId(dv, drResult));

            //---------- ModifiedOriginal --------
            dv.RowStateFilter = DataViewRowState.ModifiedOriginal;
            drResult = GetResultRows(dt, DataRowState.Modified);
            // ModifiedOriginal
            Assert.True(CompareSortedRowsByParentId(dv, drResult));

            //---------- Deleted --------
            dv.RowStateFilter = DataViewRowState.Deleted;
            drResult = GetResultRows(dt, DataRowState.Deleted);
            // Deleted
            Assert.True(CompareSortedRowsByParentId(dv, drResult));
            /*
                    //---------- OriginalRows --------
                    dv.RowStateFilter = DataViewRowState.OriginalRows ;
                    drResult = GetResultRows(dt,DataRowState.Unchanged | DataRowState.Deleted );
                        // OriginalRows
                        Assert.Equal(true , CompareSortedRowsByParentId(dv,drResult));
            */
        }

        private DataRow[] GetResultRows(DataTable dt, DataRowState State)
        {
            //get expected rows
            List<DataRow> al = new List<DataRow>();
            DataRowVersion drVer = DataRowVersion.Current;

            //From MSDN -    The row the default version for the current DataRowState.
            //                For a DataRowState value of Added, Modified or Current,
            //                the default version is Current.
            //                For a DataRowState of Deleted, the version is Original.
            //                For a DataRowState value of Detached, the version is Proposed.

            if (((State & DataRowState.Added) > 0)
                | ((State & DataRowState.Modified) > 0)
                | ((State & DataRowState.Unchanged) > 0))
                drVer = DataRowVersion.Current;
            if ((State & DataRowState.Deleted) > 0
                | (State & DataRowState.Detached) > 0)
                drVer = DataRowVersion.Original;

            foreach (DataRow dr in dt.Rows)
            {
                if (dr.HasVersion(drVer)
                    //&& ((int)dr["ParentId", drVer] == 1)
                    && ((dr.RowState & State) > 0)
                    )
                    al.Add(dr);
            }
            DataRow[] result = al.ToArray();
            return result;
        }

        private bool CompareSortedRowsByParentId(DataView dv, DataRow[] drTable)
        {
            if (dv.Count != drTable.Length) throw new Exception("DataRows[] length are different");

            //comparing the rows by using columns ParentId and ChildId
            if ((dv.RowStateFilter & DataViewRowState.Deleted) > 0)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (dv[i].Row["ParentId", DataRowVersion.Original].ToString() != drTable[i]["ParentId", DataRowVersion.Original].ToString())
                        return false;
                }
            }
            else
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (dv[i].Row["ParentId"].ToString() != drTable[i]["ParentId"].ToString())
                        return false;
                }
            }
            return true;
        }

        [Fact]
        public void Sort()
        {
            DataRow[] drArrTable;

            //create the source datatable
            DataTable dt = DataProvider.CreateChildDataTable();

            //create the dataview for the table
            DataView dv = new DataView(dt);

            dv.Sort = "ParentId";
            drArrTable = dt.Select("", "ParentId");
            // sort = ParentId
            Assert.True(CompareSortedRowsByParentAndChildId(dv, drArrTable));

            dv.Sort = "ChildId";
            drArrTable = dt.Select("", "ChildId");
            // sort = ChildId
            Assert.True(CompareSortedRowsByParentAndChildId(dv, drArrTable));

            dv.Sort = "ParentId Desc, ChildId";
            drArrTable = dt.Select("", "ParentId Desc, ChildId");
            // sort = ParentId Desc, ChildId
            Assert.True(CompareSortedRowsByParentAndChildId(dv, drArrTable));

            dv.Sort = "ChildId Asc, ParentId";
            drArrTable = dt.Select("", "ChildId Asc, ParentId");
            // sort = ChildId Asc, ParentId
            Assert.True(CompareSortedRowsByParentAndChildId(dv, drArrTable));

            dv.Sort = "ChildId Asc, ChildId Desc";
            drArrTable = dt.Select("", "ChildId Asc, ChildId Desc");
            // sort = ChildId Asc, ChildId Desc
            Assert.True(CompareSortedRowsByParentAndChildId(dv, drArrTable));

            // IndexOutOfRangeException - 1
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                dv.Sort = "something";
            });

            // IndexOutOfRangeException - 2
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                dv.Sort = "ColumnId Desc Asc";
            });

            // IndexOutOfRangeException - 3
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                dv.Sort = "ColumnId blabla";
            });
        }

        private bool CompareSortedRowsByParentAndChildId(DataView dv, DataRow[] drTable)
        {
            if (dv.Count != drTable.Length) throw new Exception("DataRows[] length are different");

            //comparing the rows by using columns ParentId and ChildId
            for (int i = 0; i < dv.Count; i++)
            {
                if (dv[i].Row["ParentId"].ToString() != drTable[i]["ParentId"].ToString()
                    &&
                    dv[i].Row["ChildId"].ToString() != drTable[i]["ChildId"].ToString())
                    return false;
            }
            return true;
        }

        [Fact]
        public void Table()
        {
            DataTable dt = new DataTable();
            DataView dv = new DataView();

            // DataTable=null
            Assert.Null(dv.Table);

            // DataException - bind to table with no name
            Assert.Throws<DataException>(() =>
            {
                dv.Table = dt;
            });

            dt.TableName = "myTable";
            // DataTable!=null
            dv.Table = dt;
            Assert.Equal(dt, dv.Table);

            // assign null to DataTable
            dv.Table = null;
            Assert.Null(dv.Table);
        }

        [Fact]
        public void ctor_Empty()
        {
            DataView dv = new DataView();
            // ctor
        }

        [Fact]
        public void ctor_DataTable()
        {
            DataTable dt = new DataTable("myTable");
            // ctor
            DataView dv = new DataView(dt);

            // ctor - table
            Assert.Equal(dt, dv.Table);
        }

        [Fact]
        public void ctor_ExpectedExceptions()
        {
            DataView dv = null;
            DataTable dt = new DataTable("myTable");

            // ctor - missing column CutomerID Exception
            Assert.Throws<EvaluateException>(() => // also IndexOutOfRangeException?
            {
                //exception: System.Data.EvaluateException: Cannot find column [CustomerId]
                dv = new DataView(dt, "CustomerId > 100", "Age", DataViewRowState.Added);
            });

            dt.Columns.Add(new DataColumn("CustomerId"));

            // ctor - missing column Age Exception
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                //exception: System.Data.EvaluateException: Cannot find column [Age]
                dv = new DataView(dt, "CustomerId > 100", "Age", DataViewRowState.Added);
            });
        }

        [Fact]
        public void ctor_Complex()
        {
            DataView dv = null;
            DataTable dt = new DataTable("myTable");

            dt.Columns.Add(new DataColumn("CustomerId"));
            dt.Columns.Add(new DataColumn("Age"));

            // ctor
            dv = new DataView(dt, "CustomerId > 100", "Age", DataViewRowState.Added);

            // ctor - table
            Assert.Equal(dt, dv.Table);

            // ctor - RowFilter
            Assert.Equal("CustomerId > 100", dv.RowFilter);

            // ctor - Sort
            Assert.Equal("Age", dv.Sort);

            // ctor - RowStateFilter
            Assert.Equal(DataViewRowState.Added, dv.RowStateFilter);
        }

        [Fact]
        public void DataViewManager()
        {
            DataView dv = null;
            DataViewManager dvm = null;
            var ds = new DataSet();
            DataTable dt = new DataTable("myTable");
            ds.Tables.Add(dt);

            dv = dt.DefaultView;

            //    public DataViewManager DataViewManager {get;} -    The DataViewManager that created this view.
            //    If this is the default DataView for a DataTable, the DataViewManager property returns the default DataViewManager for the DataSet.
            //    Otherwise, if the DataView was created without a DataViewManager, this property is a null reference (Nothing in Visual Basic).

            dvm = dv.DataViewManager;
            Assert.Same(ds.DefaultViewManager, dvm);

            dv = new DataView(dt);
            dvm = dv.DataViewManager;
            Assert.Null(dvm);

            dv = ds.DefaultViewManager.CreateDataView(dt);
            Assert.Same(ds.DefaultViewManager, dv.DataViewManager);
        }

        [Fact]
        public void DataView_ListChangedEventTest()
        {
            // Test DataView generates events, when datatable is directly modified

            DataTable table = new DataTable("test");
            table.Columns.Add("col1", typeof(int));

            DataView view = new DataView(table);

            view.ListChanged += new ListChangedEventHandler(dv_ListChanged);

            _evProp = null;
            table.Rows.Add(new object[] { 1 });
            Assert.Equal(0, _evProp.NewIndex);
            Assert.Equal(-1, _evProp.OldIndex);
            Assert.Equal(ListChangedType.ItemAdded, _evProp.lstType);

            _evProp = null;
            table.Rows[0][0] = 5;
            Assert.Equal(0, _evProp.NewIndex);
            Assert.Equal(0, _evProp.OldIndex);
            Assert.Equal(ListChangedType.ItemChanged, _evProp.lstType);

            _evProp = null;
            table.Rows.RemoveAt(0);
            Assert.Equal(0, _evProp.NewIndex);
            Assert.Equal(-1, _evProp.OldIndex);
            Assert.Equal(ListChangedType.ItemDeleted, _evProp.lstType);

            table.Rows.Clear();
            Assert.Equal(-1, _evProp.NewIndex);
            Assert.Equal(-1, _evProp.OldIndex);
            Assert.Equal(ListChangedType.Reset, _evProp.lstType);

            // Keep the view alive otherwise we might miss events
            GC.KeepAlive(view);
        }

        [Fact]
        public void TestDefaultValues()
        {
            DataView view = new DataView();
            Assert.False(view.ApplyDefaultSort);
            Assert.Equal("", view.Sort);
            Assert.Equal("", view.RowFilter);
            Assert.Equal(DataViewRowState.CurrentRows, view.RowStateFilter);
            Assert.True(view.AllowDelete);
            Assert.True(view.AllowEdit);
            Assert.True(view.AllowNew);
        }

        [Fact]
        public void TestTableProperty()
        {
            DataTable table = new DataTable("table");
            DataView view = new DataView();
            view.Table = table;
            Assert.Equal("", view.Sort);
            Assert.Equal("", view.RowFilter);
            Assert.Equal(DataViewRowState.CurrentRows, view.RowStateFilter);
        }

        [Fact]
        public void TestEquals_SameTableDiffViewProp()
        {
            DataTable table = new DataTable("table");
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));
            for (int i = 0; i < 5; ++i)
                table.Rows.Add(new object[] { i, 100 + i });

            DataView view1 = new DataView(table);
            DataView view2 = new DataView(table);

            object obj2 = view2;
            Assert.False(view1.Equals(obj2));

            Assert.True(view1.Equals(view1));
            Assert.True(view2.Equals(view1));

            view1.Sort = "col1 ASC";
            Assert.False(view1.Equals(view2));
            view2.Sort = "col1 ASC";
            Assert.True(view1.Equals(view2));

            view1.RowFilter = "col1 > 100";
            Assert.False(view1.Equals(view2));
            view1.RowFilter = "";
            Assert.True(view1.Equals(view2));

            view1.RowStateFilter = DataViewRowState.Added;
            Assert.False(view1.Equals(view2));
            view1.RowStateFilter = DataViewRowState.CurrentRows;
            Assert.True(view1.Equals(view2));

            view1.AllowDelete = !view2.AllowDelete;
            Assert.False(view1.Equals(view2));
            view1.AllowDelete = view2.AllowDelete;
            Assert.True(view1.Equals(view2));

            view1.AllowEdit = !view2.AllowEdit;
            Assert.False(view1.Equals(view2));
            view1.AllowEdit = view2.AllowEdit;
            Assert.True(view1.Equals(view2));

            view1.AllowNew = !view2.AllowNew;
            Assert.False(view1.Equals(view2));
            view1.AllowNew = view2.AllowNew;
            Assert.True(view1.Equals(view2));

            //ApplyDefaultSort doesnet affect the comparison
            view1.ApplyDefaultSort = !view2.ApplyDefaultSort;
            Assert.True(view1.Equals(view2));

            DataTable table2 = table.Copy();
            view1.Table = table2;
            Assert.False(view1.Equals(view2));

            view1.Table = table;
            //well.. sort is set to null when Table is assigned..
            view1.Sort = view2.Sort;
            Assert.True(view1.Equals(view2));
        }

        [Fact]
        public void ToTable_SimpleTest()
        {
            var ds = new DataSet();
            ds.Tables.Add("table");
            ds.Tables[0].Columns.Add("col1", typeof(int));
            ds.Tables[0].Columns.Add("col2", typeof(int), "sum(col1)");
            ds.Tables[0].Columns.Add("col3", typeof(int));
            ds.Tables[0].Columns[2].AutoIncrement = true;

            ds.Tables[0].Rows.Add(new object[] { 1 });
            ds.Tables[0].Rows.Add(new object[] { 2 });

            ds.Tables[0].PrimaryKey = new DataColumn[] { ds.Tables[0].Columns[0] };

            DataView view = new DataView(ds.Tables[0]);
            DataTable table = view.ToTable();

            // The rule seems to be : Copy any col property that doesent
            // involve/depend on other columns..
            // Constraints and PrimaryKey info not copied over
            Assert.Equal(0, table.PrimaryKey.Length);
            Assert.Equal(0, table.Constraints.Count);
            // AllowDBNull state is maintained by ms.net
            Assert.False(table.Columns[0].AllowDBNull);
            Assert.True(table.Columns[2].AllowDBNull);
            // Expression is not copied over by ms.net
            Assert.Equal("", table.Columns[1].Expression);
            // AutoIncrement state is maintained by ms.net
            Assert.True(table.Columns[2].AutoIncrement);

            Assert.NotEqual(ds.Tables[0], table);

            Assert.Equal(ds.Tables[0].TableName, table.TableName);
            Assert.Equal(ds.Tables[0].Columns.Count, table.Columns.Count);
            Assert.Equal(ds.Tables[0].Rows.Count, table.Rows.Count);

            for (int i = 0; i < table.Columns.Count; ++i)
            {
                Assert.Equal(ds.Tables[0].Columns[i].ColumnName, table.Columns[i].ColumnName);
                Assert.Equal(ds.Tables[0].Columns[i].DataType, table.Columns[i].DataType);
                for (int j = 0; j < table.Rows.Count; ++j)
                    Assert.Equal(ds.Tables[0].Rows[j][i], table.Rows[j][i]);
            }

            DataTable table1 = view.ToTable("newtable");
            Assert.Equal("newtable", table1.TableName);
        }

        [Fact]
        public void ToTableTest_DataValidity()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));
            table.Columns.Add("col3", typeof(int));

            for (int i = 0; i < 5; ++i)
            {
                table.Rows.Add(new object[] { i, i + 1, i + 2 });
                table.Rows.Add(new object[] { i, i + 1, i + 2 });
            }

            table.AcceptChanges();
            DataView view = new DataView(table);

            // 'columnNames' argument cannot be null.\r\nParameter name: columnNames
            Assert.Throws<ArgumentNullException>(() => view.ToTable(false, null));
            DataTable newTable1 = view.ToTable(false, new string[] { });
            Assert.Equal(10, newTable1.Rows.Count);

            newTable1 = view.ToTable(true, new string[] { });
            Assert.Equal(3, newTable1.Columns.Count);
            Assert.Equal(5, newTable1.Rows.Count);

            table.Rows.Add(new object[] { 1, 100, 100 });

            newTable1 = view.ToTable(true, new string[] { });
            Assert.Equal(3, newTable1.Columns.Count);
            Assert.Equal(6, newTable1.Rows.Count);

            newTable1 = view.ToTable(true, new string[] { "col1" });
            Assert.Equal(1, newTable1.Columns.Count);
            Assert.Equal(5, newTable1.Rows.Count);

            newTable1 = view.ToTable(true, new string[] { "col2", "col3" });
            Assert.Equal(2, newTable1.Columns.Count);
            Assert.Equal(6, newTable1.Rows.Count);

            for (int i = 0; i < newTable1.Rows.Count; ++i)
                Assert.Equal(DataRowState.Added, newTable1.Rows[i].RowState);

            view = new DataView(table, "col1>1", "col1 asc, col2 desc", DataViewRowState.Added);
            Assert.Equal(0, view.Count);

            newTable1 = view.ToTable(false, new string[] { "col1", "col3" });
            Assert.Equal(0, newTable1.Rows.Count);

            table.Rows.Add(new object[] { 10, 1, 1 });
            table.Rows.Add(new object[] { 10, 1, 3 });
            table.Rows.Add(new object[] { 10, 1, 2 });

            Assert.Equal(3, view.Count);
            view.Sort = "col1 asc, col2 asc, col3 desc";
            newTable1 = view.ToTable(true, new string[] { "col1", "col3" });
            Assert.Equal(3, newTable1.Rows.Count);
            Assert.Equal(3, newTable1.Rows[0][1]);
            Assert.Equal(2, newTable1.Rows[1][1]);
            Assert.Equal(1, newTable1.Rows[2][1]);
        }
    }
}
