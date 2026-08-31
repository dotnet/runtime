// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
    public class DataTableExtensionsTest
    {
        private DataTable _dt;

        public DataTableExtensionsTest()
        {
            _dt = new DataTable("test");
            _dt.Columns.Add("id", typeof(int));
            _dt.Columns.Add("name", typeof(string));
            _dt.Columns.Add("alias", typeof(string));
            _dt.PrimaryKey = new DataColumn[] { _dt.Columns["id"] };

            _dt.Rows.Add(new object[] { 1, "Dan", "danmosemsft" });
            _dt.Rows.Add(new object[] { 2, "Diego", "divega" });
            _dt.Rows.Add(new object[] { 3, "Stephen", "stephentoub" });

            _dt.AcceptChanges();
        }

        [Fact]
        public void AsDataView_NullTable_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("table", () => DataTableExtensions.AsDataView(null));
        }

        [Fact]
        public void AsDataView_DataTable_Succeeds()
        {
            DataView dv = _dt.AsDataView();
            Assert.NotNull(dv);
            Assert.Equal(_dt, dv.Table);
            dv.Sort = "id";
            int index = dv.Find(2);
            Assert.Equal(1, index);
            DataRowView[] rows = dv.FindRows(1);
            Assert.Single(rows);
            Assert.Equal(_dt.Rows[0], rows[0].Row);
        }

        [Fact]
        public void AsDataView_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => DataTableExtensions.AsDataView<DataRow>(null));
        }

        [Fact]
        public void AsDataView_Source_Succeeds()
        {
            DataView dv = _dt.AsEnumerable().Where(r => r.Field<string>("alias").Length > 6).AsDataView();
            Assert.NotNull(dv);
            Assert.Equal(_dt, dv.Table);
            dv.Sort = "name";
            int index = dv.Find("Stephen");
            Assert.Equal(1, index);
            DataRowView[] rows = dv.FindRows("Dan");
            Assert.Single(rows);
            Assert.Equal(_dt.Rows[0], rows[0].Row);
        }
    }
}
