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

namespace System.Data.Tests
{
    using Xunit;


    public class DataViewTest_IBindingList
    {
        private DataTable _dt = new DataTable();

        public DataViewTest_IBindingList()
        {
            _dt.Columns.Add("id", typeof(int));
            _dt.Columns[0].AutoIncrement = true;
            _dt.Columns[0].AutoIncrementSeed = 5;
            _dt.Columns[0].AutoIncrementStep = 5;
            _dt.Columns.Add("name", typeof(string));

            _dt.Rows.Add(new object[] { null, "mono test 1" });
            _dt.Rows.Add(new object[] { null, "mono test 3" });
            _dt.Rows.Add(new object[] { null, "mono test 2" });
            _dt.Rows.Add(new object[] { null, "mono test 4" });
        }

        [Fact]
        public void PropertyTest()
        {
            DataView dv = new DataView(_dt);
            IBindingList ib = dv;
            Assert.True(ib.AllowEdit);
            Assert.True(ib.AllowNew);
            Assert.True(ib.AllowRemove);
            Assert.False(ib.IsSorted);
            Assert.Equal(ListSortDirection.Ascending, ib.SortDirection);
            Assert.True(ib.SupportsChangeNotification);
            Assert.True(ib.SupportsSearching);
            Assert.True(ib.SupportsSorting);
            Assert.Null(ib.SortProperty);
        }

        [Fact]
        public void AddNewTest()
        {
            DataView dv = new DataView(_dt);
            IBindingList ib = dv;
            ib.ListChanged += new ListChangedEventHandler(OnListChanged);

            try
            {
                _args = null;
                object o = ib.AddNew();
                Assert.Equal(typeof(DataRowView), o.GetType());
                Assert.Equal(ListChangedType.ItemAdded, _args.ListChangedType);
                Assert.Equal(4, _args.NewIndex);
                Assert.Equal(-1, _args.OldIndex);

                DataRowView r = (DataRowView)o;
                Assert.Equal(25, r["id"]);
                Assert.Equal(DBNull.Value, r["name"]);
                Assert.Equal(5, dv.Count);

                _args = null;
                r.CancelEdit();
                Assert.Equal(ListChangedType.ItemDeleted, _args.ListChangedType);
                Assert.Equal(4, _args.NewIndex);
                Assert.Equal(-1, _args.OldIndex);
                Assert.Equal(4, dv.Count);
            }
            finally
            {
                ib.ListChanged -= new ListChangedEventHandler(OnListChanged);
            }
        }

        private ListChangedEventArgs _args = null;

        public void OnListChanged(object sender, ListChangedEventArgs args)
        {
            _args = args;
        }

        [Fact]
        public void SortTest()
        {
            DataView dv = new DataView(_dt);
            IBindingList ib = dv;
            ib.ListChanged += new ListChangedEventHandler(OnListChanged);
            try
            {
                _args = null;
                dv.Sort = "[id] DESC";
                Assert.Equal(ListChangedType.Reset, _args.ListChangedType);
                Assert.Equal(-1, _args.NewIndex);
                Assert.Equal(-1, _args.OldIndex);
                Assert.True(ib.IsSorted);
                Assert.NotNull(ib.SortProperty);
                Assert.Equal(ListSortDirection.Descending, ib.SortDirection);


                _args = null;
                dv.Sort = null;
                Assert.Equal(ListChangedType.Reset, _args.ListChangedType);
                Assert.Equal(-1, _args.NewIndex);
                Assert.Equal(-1, _args.OldIndex);
                Assert.False(ib.IsSorted);
                Assert.Null(ib.SortProperty);

                PropertyDescriptorCollection pds = ((ITypedList)dv).GetItemProperties(null);
                PropertyDescriptor pd = pds.Find("id", false);
                _args = null;
                ib.ApplySort(pd, ListSortDirection.Ascending);
                Assert.Equal(ListChangedType.Reset, _args.ListChangedType);
                Assert.Equal(-1, _args.NewIndex);
                Assert.Equal(-1, _args.OldIndex);
                Assert.True(ib.IsSorted);
                Assert.NotNull(ib.SortProperty);
                Assert.Equal("[id]", dv.Sort);

                _args = null;
                ib.RemoveSort();
                Assert.Equal(ListChangedType.Reset, _args.ListChangedType);
                Assert.Equal(-1, _args.NewIndex);
                Assert.Equal(-1, _args.OldIndex);
                Assert.False(ib.IsSorted);
                Assert.Null(ib.SortProperty);
                Assert.Equal(string.Empty, dv.Sort);
                _args = null;

                // descending
                _args = null;
                ib.ApplySort(pd, ListSortDirection.Descending);
                Assert.Equal(20, dv[0][0]);
                Assert.Equal("[id] DESC", dv.Sort);
                _args = null;
            }
            finally
            {
                ib.ListChanged -= new ListChangedEventHandler(OnListChanged);
            }
        }

        [Fact]
        public void FindTest()
        {
            DataView dv = new DataView(_dt);

            IBindingList ib = dv;
            ib.ListChanged += new ListChangedEventHandler(OnListChanged);
            try
            {
                _args = null;
                dv.Sort = "id DESC";
                PropertyDescriptorCollection pds = ((ITypedList)dv).GetItemProperties(null);
                PropertyDescriptor pd = pds.Find("id", false);
                int index = ib.Find(pd, 15);
                Assert.Equal(1, index);

                // negative search
                index = ib.Find(pd, 44);
                Assert.Equal(-1, index);
            }
            finally
            {
                ib.ListChanged -= new ListChangedEventHandler(OnListChanged);
            }
        }

        [Fact]
        public void TestIfCorrectIndexIsUsed()
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(int));

            table.Rows.Add(new object[] { 1 });
            table.Rows.Add(new object[] { 2 });
            table.Rows.Add(new object[] { 3 });
            table.Rows.Add(new object[] { 4 });

            DataView dv = new DataView(table);

            dv.Sort = "[id] DESC";

            // for the new view, the index thats chosen, shud be different from the one
            // created for the older view.
            dv = new DataView(table);
            IBindingList ib = dv;
            PropertyDescriptorCollection pds = ((ITypedList)dv).GetItemProperties(null);
            PropertyDescriptor pd = pds.Find("id", false);
            int index = ib.Find(pd, 4);
            Assert.Equal(3, index);
        }
    }
}
