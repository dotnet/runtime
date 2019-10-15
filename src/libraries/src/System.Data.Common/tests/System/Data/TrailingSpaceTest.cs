// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Tests
{
    public class ComparisonTest
    {
        [Fact]
        public void TestStringTrailingSpaceHandling()
        {
            DataTable dataTable = new DataTable("Person");
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Rows.Add(new object[] { "Mike   " });
            DataRow[] selectedRows = dataTable.Select("Name = 'Mike'");
            Assert.Equal(1, selectedRows.Length);
        }
    }
}
