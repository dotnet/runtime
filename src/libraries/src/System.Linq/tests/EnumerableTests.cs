// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class EnumerableTests
    {
        [Fact]
        public void Average()
        {
            var one = new[] {0, 1, 2};
            var two = new[] {0, 1, 2, 3, 4};
            var fourByThree = new[] {-2, 1, 5};
            var oneByThree = new[] {-2, 1, 2};

            var oneWithNulls = new int?[] {0, 1, 2, null, null, null, null};
            var twoWithNulls = new int?[] {0, 1, 2, 3, 4, null, null, null};
            var fourByThreeWithNulls = new int?[] {-2, 1, 5, null};
            var oneByThreeWithNulls = new int?[] {-2, 1, 2, null, null};

            Assert.Equal(1, one.Average());
            Assert.Equal(2, two.Average());
            Assert.Equal(4.0/3, fourByThree.Average());
            Assert.Equal(1.0/3, oneByThree.Average());

            // Repeat the tests with some null values and ensure they don't affect the result.
            Assert.Equal(1, oneWithNulls.Average());
            Assert.Equal(2, twoWithNulls.Average());
            Assert.Equal(4.0/3, fourByThreeWithNulls.Average());
            Assert.Equal(1.0/3, oneByThreeWithNulls.Average());

            // Cast the arrays' elements types to different data types and repeat the tests.
            // 1. long and long?
            Assert.Equal(1, one.Select<int, long>(i => i).Average());
            Assert.Equal(2, two.Select<int, long>(i => i).Average());
            Assert.Equal(4.0/3, fourByThree.Select<int, long>(i => i).Average());
            Assert.Equal(1.0/3, oneByThree.Select<int, long>(i => i).Average());
            Assert.Equal(1, oneWithNulls.Select<int?, long?>(i => i).Average());
            Assert.Equal(2, twoWithNulls.Select<int?, long?>(i => i).Average());
            Assert.Equal(4.0/3, fourByThreeWithNulls.Select<int?, long?>(i => i).Average());
            Assert.Equal(1.0/3, oneByThreeWithNulls.Select<int?, long?>(i => i).Average());
            // 2. float and float?
            Assert.Equal(1, one.Select<int, float>(i => i).Average());
            Assert.Equal(2, two.Select<int, float>(i => i).Average());
            Assert.Equal(4.0f/3, fourByThree.Select<int, float>(i => i).Average());
            Assert.Equal(1.0f/3, oneByThree.Select<int, float>(i => i).Average());
            Assert.Equal(1, oneWithNulls.Select<int?, float?>(i => i).Average());
            Assert.Equal(2, twoWithNulls.Select<int?, float?>(i => i).Average());
            Assert.Equal(4.0f/3, fourByThreeWithNulls.Select<int?, float?>(i => i).Average());
            Assert.Equal(1.0f/3, oneByThreeWithNulls.Select<int?, float?>(i => i).Average());
            // 3. double and double?
            Assert.Equal(1, one.Select<int, double>(i => i).Average());
            Assert.Equal(2, two.Select<int, double>(i => i).Average());
            Assert.Equal(4.0/3, fourByThree.Select<int, double>(i => i).Average());
            Assert.Equal(1.0/3, oneByThree.Select<int, double>(i => i).Average());
            Assert.Equal(1, oneWithNulls.Select<int?, double?>(i => i).Average());
            Assert.Equal(2, twoWithNulls.Select<int?, double?>(i => i).Average());
            Assert.Equal(4.0/3, fourByThreeWithNulls.Select<int?, double?>(i => i).Average());
            Assert.Equal(1.0/3, oneByThreeWithNulls.Select<int?, double?>(i => i).Average());
            // 4. decimal and decimal?
            Assert.Equal(1, one.Select<int, decimal>(i => i).Average());
            Assert.Equal(2, two.Select<int, decimal>(i => i).Average());
            Assert.Equal(4.0m/3, fourByThree.Select<int, decimal>(i => i).Average());
            Assert.Equal(1.0m/3, oneByThree.Select<int, decimal>(i => i).Average());
            Assert.Equal(1, oneWithNulls.Select<int?, decimal?>(i => i).Average());
            Assert.Equal(2, twoWithNulls.Select<int?, decimal?>(i => i).Average());
            Assert.Equal(4.0m/3, fourByThreeWithNulls.Select<int?, decimal?>(i => i).Average());
            Assert.Equal(1.0m/3, oneByThreeWithNulls.Select<int?, decimal?>(i => i).Average());

            // Convert the array to different data structures and repeat the tests to
            // ensure that the method works with other data structures as well.
            // 1. List
            Assert.Equal(1, one.ToList().Average());
            Assert.Equal(2, two.ToList().Average());
            Assert.Equal(4.0/3, fourByThree.ToList().Average());
            Assert.Equal(1.0/3, oneByThree.ToList().Average());
            // 2. Stack
            Assert.Equal(1, new Stack<int>(one).Average());
            Assert.Equal(2, new Stack<int>(two).Average());
            Assert.Equal(4.0/3, new Stack<int>(fourByThree).Average());
            Assert.Equal(1.0/3, new Stack<int>(oneByThree).Average());
        }

        [Fact]
        public void All()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            Assert.True(array.All(i => i > 0));
            for (var j = 1; j <= 10; j++)
                Assert.False(array.All(i => i > j));
        }

        [Fact]
        public void Any()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            for (var j = 0; j <= 9; j++)
                Assert.True(array.Any(i => i > j));
            Assert.False(array.Any(i => i > 10));
        }

        private void CountAndValidate<T>(IEnumerable<T> enumerable, int expectedCount)
        {
            Assert.Equal(expectedCount, enumerable.Count());
            Assert.Equal(expectedCount, enumerable.ToList().Count());
            Assert.Equal(expectedCount, enumerable.ToArray().Count());
            Assert.Equal(expectedCount, new Stack<T>(enumerable).Count());
        }

        [Fact]
        public void Count()
        {
            const int count = 100;
            var range = Enumerable.Range(1, count).ToArray();
            CountAndValidate(range, count);
            CountAndValidate(range.Select<int, long>(i => i), count);
            CountAndValidate(range.Select<int, float>(i => i), count);
            CountAndValidate(range.Select<int, double>(i => i), count);
            CountAndValidate(range.Select<int, decimal>(i => i), count);
        }

        private void FindDistinctAndValidate<T>(IEnumerable<T> original)
        {
            // Convert to list to avoid repeated enumerations of the enumerables.
            var originalList = original.ToList();
            var distinctList = originalList.Distinct().ToList();

            // Ensure the result doesn't contain duplicates.
            var hashSet = new HashSet<T>();
            foreach (var i in distinctList)
            {
                Assert.False(hashSet.Contains(i), "Found a duplicate in the result of Distinct(): " + i);
                hashSet.Add(i);
            }

            // Ensure that every element in 'originalList' exists in 'distinctList'.
            Assert.DoesNotContain(originalList, x => !distinctList.Contains(x));

            // Ensure that every element in 'distinctList' exists in 'originalList'.
            Assert.DoesNotContain(distinctList, x => !originalList.Contains(x));
        }

        [Fact]
        public void Distinct()
        {
            // Validate an array of different numeric data types.
            FindDistinctAndValidate(new int[] {1, 1, 1, 2, 3, 5, 5, 6, 6, 10});
            FindDistinctAndValidate(new long[] {1, 1, 1, 2, 3, 5, 5, 6, 6, 10});
            FindDistinctAndValidate(new float[] {1, 1, 1, 2, 3, 5, 5, 6, 6, 10});
            FindDistinctAndValidate(new double[] {1, 1, 1, 2, 3, 5, 5, 6, 6, 10});
            FindDistinctAndValidate(new decimal[] {1, 1, 1, 2, 3, 5, 5, 6, 6, 10});
            // Try strings
            FindDistinctAndValidate(new []
            {
                "add",
                "add",
                "subtract",
                "multiply",
                "divide",
                "divide2",
                "subtract",
                "add",
                "power",
                "exponent",
                "hello",
                "class",
                "namespace",
                "namespace",
                "namespace",
            });
        }

        private class ExtremeComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x == y)
                    return 0;
                if (x < y)
                    return int.MinValue;
                return int.MaxValue;
            }
        }

        [Fact]
        public void OrderByExtremeComparer()
        {
            var outOfOrder = new[] { 7, 1, 0, 9, 3, 5, 4, 2, 8, 6 };
            Assert.Equal(Enumerable.Range(0, 10), outOfOrder.OrderBy(i => i, new ExtremeComparer()));
            Assert.Equal(Enumerable.Range(0, 10).Reverse(), outOfOrder.OrderByDescending(i => i, new ExtremeComparer()));
        }
    }
}


