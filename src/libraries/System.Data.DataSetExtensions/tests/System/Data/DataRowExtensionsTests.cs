using Xunit;

namespace System.Data.Tests
{
    public class DataRowExtensionsTests
    {
        [Fact]
        public void Field_NullSource_ThrowsArgumentNullException()
        {
            var table = new DataTable();
            DataColumn column = table.Columns.Add("Column");

            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, column));
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, "Column"));
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, column, DataRowVersion.Current));
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, 0, DataRowVersion.Current));
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowExtensions.Field<int>(null, "Column", DataRowVersion.Current));

        }
    }
}