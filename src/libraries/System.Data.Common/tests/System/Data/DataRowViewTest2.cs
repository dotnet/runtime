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

namespace System.Data.Tests
{
    public class DataRowViewTest2
    {
        [Fact]
        public void BeginEdit()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            drv.BeginEdit();
            drv["String1"] = "ChangeValue";

            // check Proposed value
            Assert.Equal("ChangeValue", dt.Rows[0]["String1", DataRowVersion.Proposed]);

            // check Original value
            Assert.Equal("1-String1", dt.Rows[0]["String1", DataRowVersion.Original]);

            // check IsEdit
            Assert.True(drv.IsEdit);

            // check IsEdit - change another row
            dv[1]["String1"] = "something";
            Assert.True(drv.IsEdit);
        }

        [Fact]
        public void CancelEdit()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            drv.BeginEdit();
            drv["String1"] = "ChangeValue";

            // check Proposed value
            Assert.Equal("ChangeValue", dt.Rows[0]["String1", DataRowVersion.Proposed]);

            // check IsEdit
            Assert.True(drv.IsEdit);

            // check Proposed value
            drv.CancelEdit();
            Assert.False(dt.Rows[0].HasVersion(DataRowVersion.Proposed));

            // check current value
            Assert.Equal("1-String1", dt.Rows[0]["String1", DataRowVersion.Current]);

            // check IsEdit after cancel edit
            Assert.False(drv.IsEdit);
        }

        [Fact]
        public void CreateChildView_ByDataRelation()
        {
            //create a dataset with two tables, with a DataRelation between them
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            var ds = new DataSet();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            DataRelation drel = new DataRelation("ParentChild", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(drel);

            //DataView dvChild = null;
            DataView dvParent = new DataView(dtParent);

            DataView dvTmp1 = dvParent[0].CreateChildView(drel);
            DataView dvTmp2 = dvParent[3].CreateChildView(drel);

            // ChildView != null
            Assert.NotNull(dvTmp1);

            // Child view table = ChildTable
            Assert.Equal(dtChild, dvTmp1.Table);

            // ChildView1.Table = ChildView2.Table
            Assert.Equal(dvTmp2.Table, dvTmp1.Table);

            //the child dataview are different
            // Child DataViews different
            Assert.False(dvTmp1.Equals(dvTmp2));
        }

        [Fact]
        public void CreateChildView_ByName()
        {
            //create a dataset with two tables, with a DataRelation between them
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            var ds = new DataSet();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            DataRelation drel = new DataRelation("ParentChild", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(drel);

            //DataView dvChild = null;
            DataView dvParent = new DataView(dtParent);

            DataView dvTmp1 = dvParent[0].CreateChildView("ParentChild");
            DataView dvTmp2 = dvParent[3].CreateChildView("ParentChild");

            // ChildView != null
            Assert.NotNull(dvTmp1);

            // Child view table = ChildTable
            Assert.Equal(dtChild, dvTmp1.Table);

            // ChildView1.Table = ChildView2.Table
            Assert.Equal(dvTmp2.Table, dvTmp1.Table);

            //the child dataview are different
            // Child DataViews different
            Assert.False(dvTmp1.Equals(dvTmp2));
        }

        [Fact]
        public void DataView()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv1 = dv[0];
            DataRowView drv2 = dv[4];

            // check DataRowView.DataView
            Assert.Equal(dv, drv1.DataView);

            // compare DataRowView.DataView
            Assert.Equal(drv2.DataView, drv1.DataView);

            //check that the DataRowView still has the same DataView even when the source table changed
            // check that the DataRowView still has the same DataView
            dv.Table = null;

            Assert.Same(drv1.DataView, dv);

            //check that the DataRowView has a new DataView
            // check that the DataRowView has a new DataView
            dv = new DataView();
            Assert.Equal(drv1.DataView, dv);
        }

        [Fact]
        public void Delete()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];
            int tableRowsCount = dt.Rows.Count;
            int viewRowCount = dv.Count;

            // DataView Count
            drv.Delete();
            Assert.Equal(viewRowCount - 1, dv.Count);

            //the table count should stay the same until EndEdit is invoked
            // Table Count
            Assert.Equal(tableRowsCount, dt.Rows.Count);

            // DataRowState deleted
            Assert.Equal(DataRowState.Deleted, drv.Row.RowState);
        }

        [Fact]
        public void EndEdit()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            drv.BeginEdit();
            drv["String1"] = "ChangeValue";

            //the row should be stay in edit mode event if changing other rows
            // check IsEdit - change another row
            dv[1]["String1"] = "something";
            Assert.True(drv.IsEdit);

            // check if has Proposed version
            drv.EndEdit();
            Assert.False(dt.Rows[0].HasVersion(DataRowVersion.Proposed));

            // check Current value
            Assert.Equal("ChangeValue", dt.Rows[0]["String1", DataRowVersion.Current]);

            // check IsEdit
            Assert.False(drv.IsEdit);
        }

        [Fact]
        public void EqualsTest()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView d1 = dv[0];
            DataRowView d2 = dv[0];

            // DataRowView.Equals
            Assert.Equal(d2, d1);
        }

        [Fact]
        public void IsEdit()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            // default value
            Assert.False(drv.IsEdit);

            // after BeginEdit
            drv.BeginEdit();
            Assert.True(drv.IsEdit);

            // after CancelEdit
            drv.CancelEdit();
            Assert.False(drv.IsEdit);

            // after BeginEdit again
            drv.BeginEdit();
            Assert.True(drv.IsEdit);

            // after EndEdit
            drv.EndEdit();
            Assert.False(drv.IsEdit);
        }

        [Fact]
        public void IsNew()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            // existing row
            Assert.False(drv.IsNew);

            // add new row
            drv = dv.AddNew();
            Assert.True(drv.IsNew);
        }

        [Fact]
        public void Item()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            // Item 0
            Assert.Equal(dt.Rows[0][0], drv[0]);

            // Item 4
            Assert.Equal(dt.Rows[0][4], drv[4]);

            // Item -1 - exception
            Assert.Throws<IndexOutOfRangeException>(() => drv[-1]);
        }

        [Fact]
        public void Item_Property()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);

            DataRowView drv = dv[0];

            // Item 'ParentId'
            Assert.Equal(dt.Rows[0]["ParentId"], drv["ParentId"]);

            // Item 'ParentDateTime'
            Assert.Equal(dt.Rows[0]["ParentDateTime"], drv["ParentDateTime"]);

            // Item invalid - exception
            AssertExtensions.Throws<ArgumentException>(null, () => drv["something"]);
        }

        [Fact]
        public void Row()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);
            DataRowView drv = null;

            // Compare DataRowView.Row to table row
            drv = dv[3];
            Assert.Equal(dt.Rows[3], drv.Row);
        }

        [Fact]
        public void RowVersion()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataView dv = new DataView(dt);
            dt.Columns[1].DefaultValue = "default";
            DataRowView drv = dv[0];

            dt.Rows.Add(new object[] { 99 });
            dt.Rows[1].Delete();
            dt.Rows[2].BeginEdit();
            dt.Rows[2][1] = "aaa";

            dv.RowStateFilter = DataViewRowState.CurrentRows;
            // check Current
            Assert.Equal(DataRowVersion.Current, drv.RowVersion);

            dv.RowStateFilter = DataViewRowState.Deleted;
            // check Original
            Assert.Equal(DataRowVersion.Current, drv.RowVersion);
        }
    }
}
