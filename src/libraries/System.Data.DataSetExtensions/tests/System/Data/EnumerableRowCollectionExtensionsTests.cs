// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Data
{
    public class EnumerableRowCollectionExtensionsTests
    {
        public class TestTypedTable<T> : TypedTableBase<T> where T : DataRow
        {
            public TestTypedTable() : base() { }
        }

        private class TestDataRowComparer<T> : Comparer<T> where T : DataRow
        {
            public override int Compare(T x, T y)
            {
                return int.Parse((string)x.ItemArray[0]).CompareTo(int.Parse((string)y.ItemArray[0]));
            }
        }

        private (TypedTableBase<DataRow> table, DataRow one, DataRow two, DataRow three) InstantiateTable()
        {
            TypedTableBase<DataRow> table = new TestTypedTable<DataRow>();
            table.Columns.Add();
            DataRow two = table.Rows.Add(2);
            DataRow one = table.Rows.Add(1);
            DataRow three = table.Rows.Add(3);

            return (table, one, two, three);
        }

        [Fact]
        public void Where_SuccessfullyFindRow()
        {
            TypedTableBase<DataRow> table = new TestTypedTable<DataRow>();
            table.Columns.Add();
            DataRow two = table.Rows.Add("two");

            EnumerableRowCollection<DataRow> source = table.Cast<DataRow>();

            var filtered = source.Where(row => "two".Equals(row.ItemArray[0]));

            // Check that only one row matches predicate condition
            Assert.Equal(1, filtered.Count());

            // Check that matching row is the same object as the second data row
            Assert.Same(two, filtered.First());
        }

        [Fact]
        public void OrderBy_AddSortExpressionValidation()
        {
            var (table, one, two, three) = InstantiateTable();

            EnumerableRowCollection<DataRow> source = table.Cast<DataRow>();
            var ordered = source.OrderBy(row => int.Parse((string)row.ItemArray[0]));
            Assert.Equal(new DataRow[] { one, two, three }, ordered);

            DataRow zero = table.Rows.Add(0);
            var compared = source.OrderBy((row => row), new TestDataRowComparer<DataRow>());
            Assert.Equal(new DataRow[] { zero, one, two, three }, compared);
        }

        [Fact]
        public void OrderByDescending_AddSortExpressionValidation()
        {
            var (table, one, two, three) = InstantiateTable();

            EnumerableRowCollection<DataRow> source = table.Cast<DataRow>();
            var orderedBackwards = source.OrderByDescending(row => int.Parse((string)row.ItemArray[0]));
            Assert.Equal(new DataRow[] { three, two, one }, orderedBackwards);

            DataRow four = table.Rows.Add(4);
            var comparedBackwards = source.OrderByDescending((row => row), new TestDataRowComparer<DataRow>());
            Assert.Equal(new DataRow[] { four, three, two, one }, comparedBackwards);
        }

        [Fact]
        public void ThenBy_AddSortExpressionValidation()
        {
            var (table, one, two, three) = InstantiateTable();

            // Order the EnumerableRowCollection
            OrderedEnumerableRowCollection<DataRow> orderedSource = table.Cast<DataRow>().OrderBy(row => int.Parse((string)row.ItemArray[0]));

            DataRow zero = table.Rows.Add(0);
            var orderedAgain = orderedSource.ThenBy(row => int.Parse((string)row.ItemArray[0]));
            Assert.Equal(new DataRow[] { zero, one, two, three }, orderedAgain);

            DataRow negative = table.Rows.Add(-1);
            var comparedAgain = orderedSource.ThenBy((row => row), new TestDataRowComparer<DataRow>());
            Assert.Equal(new DataRow[] { negative, zero, one, two, three }, comparedAgain);

        }

        [Fact]
        public void ThenByDescending_AddSortExpressionValidation()
        {
            var (table, one, two, three) = InstantiateTable();

            // Order the EnumerableRowCollection
            OrderedEnumerableRowCollection<DataRow> orderedSource = table.Cast<DataRow>().OrderByDescending(row => int.Parse((string)row.ItemArray[0]));

            DataRow zero = table.Rows.Add(0);
            var orderedBackwardsAgain = orderedSource.ThenByDescending(row => int.Parse((string)row.ItemArray[0]));
            Assert.Equal(new DataRow[] { three, two, one, zero }, orderedBackwardsAgain);

            DataRow negative = table.Rows.Add(-1);
            var comparedBackwardsAgain = orderedSource.ThenByDescending((row => row), new TestDataRowComparer<DataRow>());
            Assert.Equal(new DataRow[] { three, two, one, zero, negative }, comparedBackwardsAgain);

        }

    }
}
