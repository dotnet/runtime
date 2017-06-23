// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) 2005 Novell Inc,
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
    public class DataRowViewTest
    {
        private DataView CreateTestView()
        {
            DataTable dt1 = new DataTable("table1");
            DataColumn c1 = new DataColumn("col");
            dt1.Columns.Add(c1);
            dt1.Rows.Add(new object[] { "1" });
            return new DataView(dt1);
        }

        [Fact]
        public void CreateChildViewNullStringArg()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               DataView dv = CreateTestView();
               DataRowView dvr = dv[0];
               dvr.CreateChildView((string)null);
           });
        }

        [Fact]
        public void CreateChildViewNullDataRelationArg()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataView dv = CreateTestView();
                DataRowView dvr = dv[0];
                dvr.CreateChildView((DataRelation)null);
            });
        }

        [Fact]
        public void CreateChildViewNonExistentName()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataView dv = CreateTestView();
                DataRowView dvr = dv[0];
                dvr.CreateChildView("nothing");
            });
        }

        [Fact]
        public void CreateChildViewSimple()
        {
            DataSet ds = new DataSet();

            DataTable dt1 = new DataTable("table1");
            ds.Tables.Add(dt1);
            DataColumn c1 = new DataColumn("col");
            dt1.Columns.Add(c1);
            dt1.Rows.Add(new object[] { "1" });

            DataTable dt2 = new DataTable("table2");
            ds.Tables.Add(dt2);
            DataColumn c2 = new DataColumn("col");
            dt2.Columns.Add(c2);
            dt2.Rows.Add(new object[] { "1" });

            DataRelation dr = new DataRelation("dr", c1, c2);

            DataView dv = new DataView(dt1);
            DataRowView dvr = dv[0];
            DataView v = dvr.CreateChildView(dr);
            Assert.Equal("", v.RowFilter);
            Assert.Equal("", v.Sort);
        }

        [Fact]
        public void IsEdit()
        {
            DataTable dt = new DataTable("table");
            dt.Columns.Add("col");
            dt.Rows.Add((new object[] { "val" }));

            DataView dv = new DataView(dt);
            DataRowView drv = dv[0];
            dt.Rows[0].BeginEdit();
            Assert.Equal(true, drv.IsEdit);

            drv = dv.AddNew();
            drv.Row["col"] = "test";
            drv.Row.CancelEdit();
            Assert.Equal(false, drv.IsEdit);
        }

        [Fact]
        public void Item()
        {
            DataTable dt = new DataTable("table");
            dt.Columns.Add("col");
            dt.Rows.Add((new object[] { "val" }));
            DataView dv = new DataView(dt);
            DataRowView drv = dv[0];
            dt.Rows[0].BeginEdit();
            Assert.Equal("val", drv["col"]);
        }

        [Fact]
        public void ItemException()
        {
            Assert.Throws<RowNotInTableException>(() =>
           {
               DataTable dt = new DataTable("table");
               dt.Columns.Add("col");
               dt.Rows.Add((new object[] { "val" }));
               DataView dv = new DataView(dt);
               DataRowView drv = dv.AddNew();
               drv.Row["col"] = "test";
               drv.Row.CancelEdit();
               object o = drv["col"];
           });
        }

        [Fact]
        public void RowVersion1()
        {
            // I guess we could write better tests.
            DataTable dt = new DataTable("table");
            dt.Columns.Add("col");
            dt.Rows.Add(new object[] { 1 });
            DataView dv = new DataView(dt);
            DataRowView drv = dv.AddNew();
            Assert.Equal(DataRowVersion.Current, drv.RowVersion);
            Assert.Equal(DataRowVersion.Current, dv[0].RowVersion);
            drv["col"] = "mod";
            Assert.Equal(DataRowVersion.Current, drv.RowVersion);
            Assert.Equal(DataRowVersion.Current, dv[0].RowVersion);
            dt.AcceptChanges();
            Assert.Equal(DataRowVersion.Current, drv.RowVersion);
            Assert.Equal(DataRowVersion.Current, dv[0].RowVersion);
            drv.EndEdit();
            dv[0].EndEdit();
            Assert.Equal(DataRowVersion.Current, drv.RowVersion);
            Assert.Equal(DataRowVersion.Current, dv[0].RowVersion);
        }
    }
}
