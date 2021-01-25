// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Tests
{
    public class TypedTableBaseExtensionsTests
    {
        [Fact]
        public void AsEnumerable_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.AsEnumerable<DataRow>(null));
        }

        [Fact]
        public void OrderBy_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderBy<DataRow, string>(null, row => "abc"));
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderBy<DataRow, string>(null, row => "abc", StringComparer.CurrentCulture));
        }

        [Fact]
        public void OrderByDescending_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderByDescending<DataRow, string>(null, row => "abc"));
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderByDescending<DataRow, string>(null, row => "abc", StringComparer.CurrentCulture));
        }

        [Fact]
        public void Select_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.Select<DataRow, string>(null, row => "abc"));
        }

        [Fact]
        public void Where_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.Where<DataRow>(null, row => true));
        }

        public class TestTypedTable<T> : TypedTableBase<T> where T : DataRow
        {
            public TestTypedTable() : base() { }
        }

        [Fact]
        public void ElementAtOrDefault_ValidIndex()
        {
            TypedTableBase<DataRow> table = new TestTypedTable<DataRow>();
            table.Columns.Add();
            DataRow zero = table.Rows.Add(0);

            Assert.Same(zero, table.ElementAtOrDefault(0));
        }

        [Fact]
        public void ElementAtOrDefault_InvalidIndex()
        {
            TypedTableBase<DataRow> table = new TestTypedTable<DataRow>();
            table.Columns.Add();
            DataRow zero = table.Rows.Add(0);

            Assert.Same(default(DataRow), table.ElementAtOrDefault(1));
        }

        [Fact]
        public void Select_ToListOfInts()
        {
            TypedTableBase<DataRow> table = new TestTypedTable<DataRow>();
            table.Columns.Add();
            table.Rows.Add(10);
            table.Rows.Add(5);

            var chosen = table.Select(row => int.Parse((string)row.ItemArray[0]));
            int total = 0;
            foreach (int num in chosen)
            {
                total += num;
            }
            Assert.Equal(15, total);
        }
    }
}
