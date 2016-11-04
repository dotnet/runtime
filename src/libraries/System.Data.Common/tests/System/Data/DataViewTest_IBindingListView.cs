// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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


using System.ComponentModel;
using Xunit;

namespace System.Data.Tests
{
    public class DataViewTest_IBindingListView
    {
        private DataTable _table = null;

        public DataViewTest_IBindingListView()
        {
            _table = new DataTable("table");
            _table.Columns.Add("col1", typeof(int));
            _table.Columns.Add("col2", typeof(int));
            _table.Columns.Add("col3", typeof(int));

            _table.Rows.Add(new object[] { 1, 1, 1 });
            _table.Rows.Add(new object[] { 1, 1, 2 });
            _table.Rows.Add(new object[] { 1, 2, 1 });
            _table.Rows.Add(new object[] { 1, 2, 2 });
            _table.Rows.Add(new object[] { 2, 0, 0 });
            _table.Rows.Add(new object[] { 2, 1, 0 });
            _table.Rows.Add(new object[] { 2, 1, 1 });
            _table.Rows.Add(new object[] { 2, 1, 2 });
            _table.Rows.Add(new object[] { 2, 2, 1 });
            _table.Rows.Add(new object[] { 2, 2, 2 });
        }

        [Fact]
        public void FilterTest()
        {
            IBindingListView view = new DataView(_table);

            view.Filter = "";
            Assert.Equal(_table.Rows.Count, view.Count);

            view.Filter = null;
            Assert.Equal(_table.Rows.Count, view.Count);

            view.Filter = "col1 <> 1";
            Assert.Equal(view.Filter, ((DataView)view).RowFilter);
            Assert.Equal(6, view.Count);

            //RemoveFilter Test
            view.RemoveFilter();
            Assert.Equal("", view.Filter);
            Assert.Equal("", ((DataView)view).RowFilter);
            Assert.Equal(_table.Rows.Count, view.Count);
        }

        [Fact]
        public void SortDescriptionTest()
        {
            IBindingListView view = new DataView(_table);

            ListSortDescriptionCollection col = view.SortDescriptions;

            ((DataView)view).Sort = "";
            col = view.SortDescriptions;
            Assert.Equal(0, col.Count);

            ((DataView)view).Sort = null;
            col = view.SortDescriptions;
            Assert.Equal(0, col.Count);

            ((DataView)view).Sort = "col1 DESC, col2 ASC";
            col = view.SortDescriptions;
            Assert.Equal(2, col.Count);
            Assert.Equal("col1", col[0].PropertyDescriptor.Name);
            Assert.Equal(ListSortDirection.Descending, col[0].SortDirection);
            Assert.Equal("col2", col[1].PropertyDescriptor.Name);
            Assert.Equal(ListSortDirection.Ascending, col[1].SortDirection);

            //ApplySort Test
            IBindingListView view1 = new DataView(_table);

            Assert.False(view.Equals(view1));
            view1.ApplySort(col);
            Assert.Equal("[col1] DESC,[col2]", ((DataView)view1).Sort);
            for (int i = 0; i < view.Count; ++i)
                Assert.Equal(((DataView)view)[i].Row, ((DataView)view1)[i].Row);
        }

        [Fact]
        public void ReadOnlyPropTest()
        {
            IBindingListView view = new DataView(_table);
            Assert.True(view.SupportsAdvancedSorting);
            Assert.True(view.SupportsFiltering);
        }
    }
}
