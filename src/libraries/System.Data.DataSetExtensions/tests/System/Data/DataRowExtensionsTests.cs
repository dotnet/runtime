// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Tests
{
    public class DataRowExtensionsTests
    {
        [Fact]
        public void Field_String_NullRowThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, "foo"));
        }

        [Fact]
        public void Field_String_NullColumnNameThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            AssertExtensions.Throws<ArgumentNullException>("name", () => DataRowExtensions.Field<int>(row, columnName: null));
        }

        [Fact]
        public void Field_Column_NullRowThrows()
        {
            DataColumn column = new DataColumn();
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, column));
        }

        [Fact]
        public void Field_Column_NullColumnThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            AssertExtensions.Throws<ArgumentNullException>("column", () => DataRowExtensions.Field<int>(row, column: null));
        }

        [Fact]
        public void Field_Index_NullRowThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, 0));
        }

        [Fact]
        public void Field_Index_NegativeIndexThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            Assert.Throws<IndexOutOfRangeException>(() => DataRowExtensions.Field<int>(row, columnIndex: -1));
        }

        [Fact]
        public void Field_IndexVersion_NullRowThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, columnIndex: 0, version: DataRowVersion.Default));
        }

        [Fact]
        public void Field_IndexVersion_NegativeIndexThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            Assert.Throws<IndexOutOfRangeException>(() => DataRowExtensions.Field<int>(row, columnIndex: -1, version: DataRowVersion.Default));
        }

        [Fact]
        public void Field_NameVersion_NullRowThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, columnName: "foo", version: DataRowVersion.Default));
        }

        [Fact]
        public void Field_NameVersion_NullColumnNameThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            AssertExtensions.Throws<ArgumentNullException>("name", () => DataRowExtensions.Field<int>(row, columnName: null, version: DataRowVersion.Default));
        }

        [Fact]
        public void Field_ColumnVersion_NullRowThrows()
        {
            DataColumn column = new DataColumn();
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, column: column, version: DataRowVersion.Default));
        }

        [Fact]
        public void Field_ColumnVersion_NullColumnThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            AssertExtensions.Throws<ArgumentNullException>("column", () => DataRowExtensions.Field<int>(row, column: null, version: DataRowVersion.Default));
        }

        [Fact]
        public void SetField_IndexValue_NullRowThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.SetField(null, columnIndex: 0, value: 0));
        }

        [Fact]
        public void SetField_IndexValue_NullColumnThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            Assert.Throws<IndexOutOfRangeException>(() => DataRowExtensions.SetField(row, columnIndex: -1, value: 0));
        }

        [Fact]
        public void SetField_IndexValue_NullValueReplacedByDBNull()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            table.Columns.Add(new DataColumn());

            DataRowExtensions.SetField<string>(row, columnIndex: 0, value: null);
            Assert.Equal(DBNull.Value, row[0]);
        }

        [Fact]
        public void SetField_NameValue_NullRowThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.SetField(null, columnName: "foo", value: 0));
        }

        [Fact]
        public void SetField_NameValue_NullColumnNameThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            AssertExtensions.Throws<ArgumentNullException>("name", () => DataRowExtensions.SetField(row, columnName: null, value: 0));
        }

        [Fact]
        public void SetField_NameValue_NullValueReplacedByDBNull()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            table.Columns.Add(new DataColumn("foo"));

            DataRowExtensions.SetField<string>(row, columnName: "foo", value: null);
            Assert.Equal(DBNull.Value, row["foo"]);
        }

        [Fact]
        public void SetField_ColumnValue_NullRowThrows()
        {
            DataColumn column = new DataColumn();
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.SetField(null, column: column, value: 0));
        }

        [Fact]
        public void SetField_ColumnValue_NullColumnThrows()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            AssertExtensions.Throws<ArgumentNullException>("column", () => DataRowExtensions.SetField(row, column: null, value: 0));
        }

        [Fact]
        public void SetField_ColumnValue_NullValueReplacedByDBNull()
        {
            DataTable table = new DataTable("test");
            DataRow row = table.NewRow();
            DataColumn column = new DataColumn();
            table.Columns.Add(column);

            DataRowExtensions.SetField<string>(row, column: column, value: null);
            Assert.Equal(DBNull.Value, row[column]);
        }


    }
}
