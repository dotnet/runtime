// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
