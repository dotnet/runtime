// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Novell Inc., and the individuals listed
// on the ChangeLog entries.
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
    public class DataTableLoadRowTest
    {
        private bool _rowChanging;
        private bool _rowChanged;
        private bool _rowDeleting;
        private bool _rowDeleted;

        private DataRow _rowInAction_Changing;
        private DataRowAction _rowAction_Changing;
        private DataRow _rowInAction_Changed;
        private DataRowAction _rowAction_Changed;
        private DataRow _rowInAction_Deleting;
        private DataRowAction _rowAction_Deleting;
        private DataRow _rowInAction_Deleted;
        private DataRowAction _rowAction_Deleted;

        [Fact]
        public void LoadRowTest()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));

            dt.Rows.Add(new object[] { 1, "mono 1" });
            dt.Rows.Add(new object[] { 2, "mono 2" });
            dt.Rows.Add(new object[] { 3, "mono 3" });

            dt.PrimaryKey = new DataColumn[] { dt.Columns["id"] };
            dt.AcceptChanges();

            dt.LoadDataRow(new object[] { 4, "mono 4" }, LoadOption.Upsert);
            Assert.Equal(4, dt.Rows.Count);
        }

        [Fact]
        public void LoadRowTestUpsert()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));

            dt.Rows.Add(new object[] { 1, "mono 1" });
            dt.Rows.Add(new object[] { 2, "mono 2" });
            dt.Rows.Add(new object[] { 3, "mono 3" });

            dt.PrimaryKey = new DataColumn[] { dt.Columns["id"] };

            dt.AcceptChanges();
            try
            {
                SubscribeEvents(dt);

                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test" }, LoadOption.Upsert);
                Assert.Equal(3, dt.Rows.Count);
                Assert.Equal("mono test", dt.Rows[1][1]);
                Assert.Equal("mono 2", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Modified, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Change, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Change, _rowAction_Changed);


                // Row State tests
                // current - modified ; result - modified
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test 2" }, LoadOption.Upsert);
                Assert.Equal("mono test 2", dt.Rows[1][1]);
                Assert.Equal("mono 2", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Modified, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Change, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Change, _rowAction_Changed);


                // current - Unchanged; result - Unchanged if no new value
                dt.AcceptChanges();
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test 2" }, LoadOption.Upsert);
                Assert.Equal("mono test 2", dt.Rows[1][1]);
                Assert.Equal("mono test 2", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Nothing, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Nothing, _rowAction_Changed);

                // not the same value again
                dt.RejectChanges();
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test 3" }, LoadOption.Upsert);
                Assert.Equal(DataRowState.Modified, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Change, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Change, _rowAction_Changed);


                // current - added; result - added
                dt.Rows.Add(new object[] { 4, "mono 4" });
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 4, "mono 4" }, LoadOption.Upsert);
                Assert.Equal("mono 4", dt.Rows[3][1]);
                try
                {
                    object o = dt.Rows[3][1, DataRowVersion.Original];
                    Assert.False(true);
                }
                catch (VersionNotFoundException) { }
                Assert.Equal(DataRowState.Added, dt.Rows[3].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[3], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Change, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[3], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Change, _rowAction_Changed);


                // current - none; result - added
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 5, "mono 5" }, LoadOption.Upsert);
                Assert.Equal("mono 5", dt.Rows[4][1]);
                try
                {
                    object o = dt.Rows[4][1, DataRowVersion.Original];
                    Assert.False(true);
                }
                catch (VersionNotFoundException) { }
                Assert.Equal(DataRowState.Added, dt.Rows[4].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[4], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Add, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[4], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Add, _rowAction_Changed);


                // current - deleted; result - added a new row
                ResetEventFlags();
                dt.AcceptChanges();
                dt.Rows[4].Delete();
                Assert.True(_rowDeleting);
                Assert.True(_rowDeleted);
                Assert.Equal(_rowInAction_Deleting, dt.Rows[4]);
                Assert.Equal(_rowInAction_Deleted, dt.Rows[4]);
                Assert.Equal(_rowAction_Deleting, DataRowAction.Delete);
                Assert.Equal(_rowAction_Deleted, DataRowAction.Delete);
                dt.LoadDataRow(new object[] { 5, "mono 5" }, LoadOption.Upsert);
                Assert.Equal(6, dt.Rows.Count);
                Assert.Equal("mono 5", dt.Rows[5][1]);
                try
                {
                    object o = dt.Rows[5][1, DataRowVersion.Original];
                    Assert.False(true);
                }
                catch (VersionNotFoundException) { }
                Assert.Equal(DataRowState.Added, dt.Rows[5].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[5], _rowInAction_Changing);
                Assert.Equal(DataRowAction.Add, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[5], _rowInAction_Changed);
                Assert.Equal(DataRowAction.Add, _rowAction_Changed);
            }
            finally
            {
                UnsubscribeEvents(dt);
            }
        }

        [Fact]
        public void LoadRowTestOverwriteChanges()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));

            dt.Rows.Add(new object[] { 1, "mono 1" });
            dt.Rows.Add(new object[] { 2, "mono 2" });
            dt.Rows.Add(new object[] { 3, "mono 3" });

            dt.PrimaryKey = new DataColumn[] { dt.Columns["id"] };
            dt.AcceptChanges();

            dt.Rows[1][1] = "overwrite";
            Assert.Equal(DataRowState.Modified, dt.Rows[1].RowState);

            try
            {
                SubscribeEvents(dt);
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test" }, LoadOption.OverwriteChanges);
                Assert.Equal(3, dt.Rows.Count);
                Assert.Equal("mono test", dt.Rows[1][1]);
                Assert.Equal("mono test", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);

                DataRow r = dt.Rows[1];
                r[1] = "test";
                Assert.Equal("test", dt.Rows[1][1]);
                Assert.Equal("mono test", dt.Rows[1][1, DataRowVersion.Original]);
                //Assert.Equal ("ramesh", dt.Rows [1] [1, DataRowVersion.Proposed]);

                // Row State tests
                // current - modified ; result - modified
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test 2" }, LoadOption.OverwriteChanges);
                Assert.Equal("mono test 2", dt.Rows[1][1]);
                Assert.Equal("mono test 2", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);


                // current - Unchanged; result - Unchanged
                dt.AcceptChanges();
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test 2" }, LoadOption.OverwriteChanges);
                Assert.Equal("mono test 2", dt.Rows[1][1]);
                Assert.Equal("mono test 2", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);

                // current - added; result - added
                dt.Rows.Add(new object[] { 4, "mono 4" });
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 4, "mono 4" }, LoadOption.OverwriteChanges);
                Assert.Equal("mono 4", dt.Rows[3][1]);
                Assert.Equal("mono 4", dt.Rows[3][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[3].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[3], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[3], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);


                // current - new; result - added
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 5, "mono 5" }, LoadOption.OverwriteChanges);
                Assert.Equal("mono 5", dt.Rows[4][1]);
                Assert.Equal("mono 5", dt.Rows[4][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[4].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[4], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[4], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);


                // current - deleted; result - added a new row
                ResetEventFlags();
                dt.AcceptChanges();
                dt.Rows[4].Delete();
                Assert.True(_rowDeleting);
                Assert.True(_rowDeleted);
                Assert.Equal(_rowInAction_Deleting, dt.Rows[4]);
                Assert.Equal(_rowInAction_Deleted, dt.Rows[4]);
                Assert.Equal(_rowAction_Deleting, DataRowAction.Delete);
                Assert.Equal(_rowAction_Deleted, DataRowAction.Delete);
                dt.LoadDataRow(new object[] { 5, "mono 51" }, LoadOption.OverwriteChanges);
                Assert.Equal(5, dt.Rows.Count);
                Assert.Equal("mono 51", dt.Rows[4][1]);
                Assert.Equal("mono 51", dt.Rows[4][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[4].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[4], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[4], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);
            }
            finally
            {
                UnsubscribeEvents(dt);
            }
        }

        [Fact]
        public void LoadRowTestPreserveChanges()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));

            dt.Rows.Add(new object[] { 1, "mono 1" });
            dt.Rows.Add(new object[] { 2, "mono 2" });
            dt.Rows.Add(new object[] { 3, "mono 3" });

            dt.PrimaryKey = new DataColumn[] { dt.Columns["id"] };
            dt.AcceptChanges();
            try
            {
                SubscribeEvents(dt);

                // current - modified; new - modified
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 2, "mono test" }, LoadOption.PreserveChanges);
                Assert.Equal(3, dt.Rows.Count);
                Assert.Equal("mono test", dt.Rows[1][1]);
                Assert.Equal("mono test", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);

                // current - none; new - unchanged
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 4, "mono 4" }, LoadOption.PreserveChanges);
                Assert.Equal(4, dt.Rows.Count);
                Assert.Equal("mono 4", dt.Rows[3][1]);
                Assert.Equal("mono 4", dt.Rows[3][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Unchanged, dt.Rows[3].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[3], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[3], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeCurrentAndOriginal, _rowAction_Changed);


                dt.RejectChanges();

                // current - added; new - modified
                dt.Rows.Add(new object[] { 5, "mono 5" });
                ResetEventFlags();
                dt.LoadDataRow(new object[] { 5, "mono test" }, LoadOption.PreserveChanges);
                Assert.Equal(5, dt.Rows.Count);
                Assert.Equal("mono 5", dt.Rows[4][1]);
                Assert.Equal("mono test", dt.Rows[4][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Modified, dt.Rows[4].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[4], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[4], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeOriginal, _rowAction_Changed);


                dt.RejectChanges();

                // current - deleted ; new - deleted ChangeOriginal
                ResetEventFlags();
                dt.Rows[1].Delete();
                Assert.True(_rowDeleting);
                Assert.True(_rowDeleted);
                Assert.Equal(_rowInAction_Deleting, dt.Rows[1]);
                Assert.Equal(_rowInAction_Deleted, dt.Rows[1]);
                Assert.Equal(_rowAction_Deleting, DataRowAction.Delete);
                Assert.Equal(_rowAction_Deleted, DataRowAction.Delete);
                dt.LoadDataRow(new object[] { 2, "mono deleted" }, LoadOption.PreserveChanges);
                Assert.Equal(5, dt.Rows.Count);
                Assert.Equal("mono deleted", dt.Rows[1][1, DataRowVersion.Original]);
                Assert.Equal(DataRowState.Deleted, dt.Rows[1].RowState);
                Assert.True(_rowChanging);
                Assert.Equal(dt.Rows[1], _rowInAction_Changing);
                Assert.Equal(DataRowAction.ChangeOriginal, _rowAction_Changing);
                Assert.True(_rowChanged);
                Assert.Equal(dt.Rows[1], _rowInAction_Changed);
                Assert.Equal(DataRowAction.ChangeOriginal, _rowAction_Changed);
            }
            finally
            {
                UnsubscribeEvents(dt);
            }
        }

        [Fact]
        public void LoadRowDefaultValueTest()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("age", typeof(int));
            dt.Columns.Add("name", typeof(string));

            dt.Columns[1].DefaultValue = 20;

            dt.Rows.Add(new object[] { 1, 15, "mono 1" });
            dt.Rows.Add(new object[] { 2, 25, "mono 2" });
            dt.Rows.Add(new object[] { 3, 35, "mono 3" });

            dt.PrimaryKey = new DataColumn[] { dt.Columns["id"] };

            dt.AcceptChanges();

            dt.LoadDataRow(new object[] { 2, null, "mono test" }, LoadOption.OverwriteChanges);
            Assert.Equal(3, dt.Rows.Count);
            Assert.Equal(25, dt.Rows[1][1]);
            Assert.Equal(25, dt.Rows[1][1, DataRowVersion.Original]);
        }

        [Fact]
        public void LoadRowAutoIncrementTest()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("age", typeof(int));
            dt.Columns.Add("name", typeof(string));

            dt.Columns[0].AutoIncrementSeed = 10;
            dt.Columns[0].AutoIncrementStep = 5;
            dt.Columns[0].AutoIncrement = true;

            dt.Rows.Add(new object[] { null, 15, "mono 1" });
            dt.Rows.Add(new object[] { null, 25, "mono 2" });
            dt.Rows.Add(new object[] { null, 35, "mono 3" });

            dt.PrimaryKey = new DataColumn[] { dt.Columns["id"] };

            dt.AcceptChanges();

            dt.LoadDataRow(new object[] { null, 20, "mono test" }, LoadOption.OverwriteChanges);
            Assert.Equal(4, dt.Rows.Count);
            Assert.Equal(25, dt.Rows[3][0]);
            Assert.Equal(25, dt.Rows[3][0, DataRowVersion.Original]);

            dt.LoadDataRow(new object[] { 25, 20, "mono test" }, LoadOption.Upsert);
            dt.LoadDataRow(new object[] { 25, 20, "mono test 2" }, LoadOption.Upsert);
            dt.LoadDataRow(new object[] { null, 20, "mono test aaa" }, LoadOption.Upsert);

            Assert.Equal(5, dt.Rows.Count);
            Assert.Equal(25, dt.Rows[3][0]);
            Assert.Equal(25, dt.Rows[3][0, DataRowVersion.Original]);

            Assert.Equal(30, dt.Rows[4][0]);
        }

        public void SubscribeEvents(DataTable dt)
        {
            dt.RowChanging += new DataRowChangeEventHandler(dt_RowChanging);
            dt.RowChanged += new DataRowChangeEventHandler(dt_RowChanged);
            dt.RowDeleted += new DataRowChangeEventHandler(dt_RowDeleted);
            dt.RowDeleting += new DataRowChangeEventHandler(dt_RowDeleting);
            //dt.TableNewRow += new DataTableNewRowEventHandler (dt_TableNewRow);
        }


        public void UnsubscribeEvents(DataTable dt)
        {
            dt.RowChanging -= new DataRowChangeEventHandler(dt_RowChanging);
            dt.RowChanged -= new DataRowChangeEventHandler(dt_RowChanged);
            dt.RowDeleted -= new DataRowChangeEventHandler(dt_RowDeleted);
            dt.RowDeleting -= new DataRowChangeEventHandler(dt_RowDeleting);
            //dt.TableNewRow -= new DataTableNewRowEventHandler (dt_TableNewRow);
        }

        public void ResetEventFlags()
        {
            _rowChanging = false;
            _rowChanged = false;
            _rowDeleting = false;
            _rowDeleted = false;
            _rowInAction_Changing = null;
            _rowAction_Changing = DataRowAction.Nothing;
            _rowInAction_Changed = null;
            _rowAction_Changed = DataRowAction.Nothing;
            _rowInAction_Deleting = null;
            _rowAction_Deleting = DataRowAction.Nothing;
            _rowInAction_Deleted = null;
            _rowAction_Deleted = DataRowAction.Nothing;
        }

        private void dt_RowDeleting(object sender, DataRowChangeEventArgs e)
        {
            _rowDeleting = true;
            _rowInAction_Deleting = e.Row;
            _rowAction_Deleting = e.Action;
        }

        private void dt_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
            _rowDeleted = true;
            _rowInAction_Deleted = e.Row;
            _rowAction_Deleted = e.Action;
        }

        private void dt_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            _rowChanged = true;
            _rowInAction_Changed = e.Row;
            _rowAction_Changed = e.Action;
        }

        private void dt_RowChanging(object sender, DataRowChangeEventArgs e)
        {
            _rowChanging = true;
            _rowInAction_Changing = e.Row;
            _rowAction_Changing = e.Action;
        }
    }
}

