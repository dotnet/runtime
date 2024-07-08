// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public sealed class OrderDescendingTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                    select x1;

            Assert.Equal(q.OrderDescending(), q.OrderDescending());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x1 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x1)
                    select x1;

            Assert.Equal(q.OrderDescending().ThenBy(f => f.Replace("C", "")), q.OrderDescending().ThenBy(f => f.Replace("C", "")));
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            Assert.Empty(source.OrderDescending());
        }

        [Fact]
        public void KeySelectorReturnsNull()
        {
            int?[] source = { null, null, null };
            int?[] expected = { null, null, null };

            Assert.Equal(expected, source.OrderDescending());
        }

        [Fact]
        public void ElementsAllSameKey()
        {
            int?[] source = { 9, 9, 9, 9, 9, 9 };
            int?[] expected = { 9, 9, 9, 9, 9, 9 };

            Assert.Equal(expected, source.OrderDescending());
        }

        [Fact]
        public void KeySelectorCalled()
        {
            var source = new[]
            {
                90, 45, 0, 99
            };
            var expected = new[]
            {
                99, 90, 45, 0
            };

            Assert.Equal(expected, source.OrderDescending(null));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
            string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

            Assert.Equal(expected, source.OrderDescending(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void RunOnce()
        {
            string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
            string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

            Assert.Equal(expected, source.RunOnce().OrderDescending(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 5, 5, 3, 2, 1 };

            Assert.Equal(expected, source.OrderDescending(null));
        }

        [Fact]
        public void SourceReverseOfResultNullPassedAsComparer()
        {
            int[] source = { -75, -50, 0, 5, 9, 30, 100 };
            int[] expected = { 100, 30, 9, 5, 0, -50, -75 };

            Assert.Equal(expected, source.OrderDescending(null));
        }

        [Fact]
        public void OrderedDescendingToArray()
        {
            var source = new[]
            {
                5, 9, 6, 7, 8, 5, 20
            };
            var expected = new[]
            {
                20, 9, 8, 7, 6, 5, 5
            };

            Assert.Equal(expected, source.OrderDescending().ToArray());
        }

        [Fact]
        public void EmptyOrderedDescendingToArray()
        {
            Assert.Empty(Enumerable.Empty<int>().OrderDescending().ToArray());
        }

        [Fact]
        public void OrderedDescendingToList()
        {
            var source = new[]
            {
                5, 9, 6, 7, 8, 5, 20
            };
            var expected = new[]
            {
                20, 9, 8, 7, 6, 5, 5
            };

            Assert.Equal(expected, source.OrderDescending().ToList());
        }

        [Fact]
        public void EmptyOrderedDescendingToList()
        {
            Assert.Empty(Enumerable.Empty<int>().OrderDescending().ToList());
        }

        [Fact]
        public void SameKeysVerifySortStable()
        {
            var source = new[]
            {
                90, 45, 0, 99
            };
            var expected = new[]
            {
                99, 90, 45, 0
            };

            Assert.Equal(expected, source.OrderDescending());
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
            int[] outOfOrder = new[] { 7, 1, 0, 9, 3, 5, 4, 2, 8, 6 };

            // The .NET Framework has a bug where the input is incorrectly ordered if the comparer
            // returns int.MaxValue or int.MinValue. See https://github.com/dotnet/corefx/pull/2240.
            IEnumerable<int> ordered = outOfOrder.OrderDescending(new ExtremeComparer()).ToArray();
            Assert.Equal(Enumerable.Range(0, 10).Reverse(), ordered);
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.OrderDescending());
        }

        [Fact]
        public void SortsLargeAscendingEnumerableCorrectly()
        {
            const int Items = 1_000_000;
            IEnumerable<int> expected = NumberRangeGuaranteedNotCollectionType(0, Items).Reverse();

            IEnumerable<int> unordered = expected.Select(i => i);
            IOrderedEnumerable<int> ordered = unordered.OrderDescending();

            Assert.Equal(expected, ordered);
        }

        [Fact]
        public void SortsLargeDescendingEnumerableCorrectly()
        {
            const int Items = 1_000_000;
            IEnumerable<int> expected = NumberRangeGuaranteedNotCollectionType(0, Items).Reverse();

            IEnumerable<int> unordered = expected.Select(i => Items - i - 1);
            IOrderedEnumerable<int> ordered = unordered.OrderDescending();

            Assert.Equal(expected, ordered);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(1_000_000)]
        public void SortsRandomizedEnumerableCorrectly(int items)
        {
            var r = new Random(42);

            int[] randomized = Enumerable.Range(0, items).Select(i => r.Next()).ToArray();
            int[] ordered = ForceNotCollection(randomized).OrderDescending().ToArray();

            Array.Sort(randomized, (a, b) => a - b);
            Array.Reverse(randomized);
            Assert.Equal(randomized, ordered);
        }

        [Fact]
        public void OrderDescending_FirstLast_MatchesArray()
        {
            object[][] arrays =
            [
                [1],
                [1, 1],
                [1, 2, 1],
                [1, 2, 1, 3],
                [2, 1, 3, 1, 4],
            ];

            foreach (object[] objects in arrays)
            {
                Assert.Same(objects.OrderDescending().First(), objects.OrderDescending().ToArray().First());
                Assert.Same(objects.OrderDescending().Last(), objects.OrderDescending().ToArray().Last());
            }
        }
    }
}
