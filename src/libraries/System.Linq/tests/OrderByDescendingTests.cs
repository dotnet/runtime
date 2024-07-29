// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderByDescendingTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                             from x2 in new int[] { 55, 49, 9, -100, 24, 25 }
                             select new { a1 = x1, a2 = x2 };

            Assert.Equal(q.OrderByDescending(e => e.a1), q.OrderByDescending(e => e.a1));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x1 in new[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                             from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", string.Empty }
                             where !string.IsNullOrEmpty(x2)
                             select new { a1 = x1, a2 = x2 };

            Assert.Equal(q.OrderByDescending(e => e.a1).ThenBy(f => f.a2), q.OrderByDescending(e => e.a1).ThenBy(f => f.a2));
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            Assert.Empty(source.OrderByDescending(e => e));
        }

        [Fact]
        public void KeySelectorReturnsNull()
        {
            int?[] source = { null, null, null };
            int?[] expected = { null, null, null };

            Assert.Equal(expected, source.OrderByDescending(e => e));
        }

        [Fact]
        public void ElementsAllSameKey()
        {
            int?[] source = { 9, 9, 9, 9, 9, 9 };
            int?[] expected = { 9, 9, 9, 9, 9, 9 };

            Assert.Equal(expected, source.OrderByDescending(e => e));
        }

        [Fact]
        public void KeySelectorCalled()
        {
            var source = new[]
            {

                new { Name = "Alpha", Score = 90 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 },
                new { Name = "Bob", Score = 0 }
            };
            var expected = new[]
            {
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 },
                new { Name = "Bob", Score = 0 },
                new { Name = "Alpha", Score = 90 }
            };

            Assert.Equal(expected, source.OrderByDescending(e => e.Name, null));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
            string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

            Assert.Equal(expected, source.OrderByDescending(e => e, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void RunOnce()
        {
            string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
            string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

            Assert.Equal(expected, source.RunOnce().OrderByDescending(e => e, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 5, 5, 3, 2, 1 };

            Assert.Equal(expected, source.OrderByDescending(e => e, null));
        }

        [Fact]
        public void SourceReverseOfResultNullPassedAsComparer()
        {
            int[] source = { -75, -50, 0, 5, 9, 30, 100 };
            int[] expected = { 100, 30, 9, 5, 0, -50, -75 };

            Assert.Equal(expected, source.OrderByDescending(e => e, null));
        }

        [Fact]
        public void SameKeysVerifySortStable()
        {
            var source = new[]
            {
                new { Name = "Alpha", Score = 90 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 },
                new { Name = "Bob", Score = 90 },
                new { Name = "Thomas", Score = 45 },
                new { Name = "Tim", Score = 45 },
                new { Name = "Mark", Score = 45 },
            };
            var expected = new[]
            {
                new { Name = "Prakash", Score = 99 },
                new { Name = "Alpha", Score = 90 },
                new { Name = "Bob", Score = 90 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Thomas", Score = 45 },
                new { Name = "Tim", Score = 45 },
                new { Name = "Mark", Score = 45 },
            };

            Assert.Equal(expected, source.OrderByDescending(e => e.Score));
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
            IEnumerable<int> ordered = outOfOrder.OrderByDescending(i => i, new ExtremeComparer()).ToArray();
            Assert.Equal(Enumerable.Range(0, 10).Reverse(), ordered);
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.OrderByDescending(i => i));
        }

        [Fact]
        public void NullKeySelector()
        {
            Func<DateTime, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().OrderByDescending(keySelector));
        }

        [Fact]
        public void SortsLargeAscendingEnumerableCorrectly()
        {
            const int Items = 1_000_000;
            IEnumerable<int> expected = NumberRangeGuaranteedNotCollectionType(0, Items);

            IEnumerable<int> unordered = expected.Select(i => i);
            IOrderedEnumerable<int> ordered = unordered.OrderByDescending(i => -i);

            Assert.Equal(expected, ordered);
        }

        [Fact]
        public void SortsLargeDescendingEnumerableCorrectly()
        {
            const int Items = 1_000_000;
            IEnumerable<int> expected = NumberRangeGuaranteedNotCollectionType(0, Items);

            IEnumerable<int> unordered = expected.Select(i => Items - i - 1);
            IOrderedEnumerable<int> ordered = unordered.OrderByDescending(i => -i);

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
            int[] ordered = ForceNotCollection(randomized).OrderByDescending(i => -i).ToArray();

            Array.Sort(randomized, (a, b) => a - b);
            Assert.Equal(randomized, ordered);
        }

        [Fact]
        public void OrderByDescending_FirstLast_MatchesArray()
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
                Assert.Same(objects.OrderByDescending(x => x).First(), objects.OrderByDescending(x => x).ToArray().First());
                Assert.Same(objects.OrderByDescending(x => x).Last(), objects.OrderByDescending(x => x).ToArray().Last());
            }
        }
    }
}
