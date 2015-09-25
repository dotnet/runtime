// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderByTests : EnumerableTests
    {
        private class BadComparer1 : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return 1;
            }
        }

        private class BadComparer2 : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return -1;
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                             from x2 in new int[] { 55, 49, 9, -100, 24, 25 }
                             select new { a1 = x1, a2 = x2 };

            Assert.Equal(q.OrderBy(e => e.a1).ThenBy(f => f.a2), q.OrderBy(e => e.a1).ThenBy(f => f.a2));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x1 in new[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                             from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", String.Empty }
                             where !String.IsNullOrEmpty(x2)
                             select new { a1 = x1, a2 = x2 };

            Assert.Equal(q.OrderBy(e => e.a1), q.OrderBy(e => e.a1));
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            Assert.Empty(source.OrderBy(e => e));
        }

        //FIXME: This will hang with a larger source. Do we want to deal with that case?
        [Fact]
        public void SurviveBadComparerAlwaysReturnsNegative()
        {
            int[] source = { 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, source.OrderBy(e => e, new BadComparer2()));
        }

        [Fact]
        public void KeySelectorReturnsNull()
        {
            int?[] source = { null, null, null };
            int?[] expected = { null, null, null };

            Assert.Equal(expected, source.OrderBy(e => e));
        }

        [Fact]
        public void ElementsAllSameKey()
        {
            int?[] source = { 9, 9, 9, 9, 9, 9 };
            int?[] expected = { 9, 9, 9, 9, 9, 9 };

            Assert.Equal(expected, source.OrderBy(e => e));
        }

        [Fact]
        public void KeySelectorCalled()
        {
            var source = new[]
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 }
            };
            var expected = new[]
            {
                new { Name = "Prakash", Score = 99 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Tim", Score = 90 }
            };

            Assert.Equal(expected, source.OrderBy(e => e.Name, null));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
            string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

            Assert.Equal(expected, source.OrderBy(e => e, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 1, 2, 3, 5, 5 };

            Assert.Equal(expected, source.OrderBy(e => e, null));
        }

        [Fact]
        public void SourceReverseOfResultNullPassedAsComparer()
        {
            int?[] source = { 100, 30, 9, 5, 0, -50, -75, null };
            int?[] expected = { null, -75, -50, 0, 5, 9, 30, 100 };

            Assert.Equal(expected, source.OrderBy(e => e, null));
        }

        [Fact]
        public void SameKeysVerifySortStable()
        {
            var source = new[]
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 90 },
                new { Name = "Prakash", Score = 90 },
                new { Name = "Jim", Score = 90 },
                new { Name = "John", Score = 90 },
                new { Name = "Albert", Score = 90 },
            };
            var expected = new[]
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 90 },
                new { Name = "Prakash", Score = 90 },
                new { Name = "Jim", Score = 90 },
                new { Name = "John", Score = 90 },
                new { Name = "Albert", Score = 90 },
            };

            Assert.Equal(expected, source.OrderBy(e => e.Score));
        }

        [Fact]
        public void OrderedToArray()
        {
            var source = new []
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 90 },
                new { Name = "Prakash", Score = 90 },
                new { Name = "Jim", Score = 90 },
                new { Name = "John", Score = 90 },
                new { Name = "Albert", Score = 90 },
            };
            var expected = new []
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 90 },
                new { Name = "Prakash", Score = 90 },
                new { Name = "Jim", Score = 90 },
                new { Name = "John", Score = 90 },
                new { Name = "Albert", Score = 90 },
            };

            Assert.Equal(expected, source.OrderBy(e => e.Score).ToArray());
        }

        [Fact]
        public void EmptyOrderedToArray()
        {
            Assert.Empty(Enumerable.Empty<int>().OrderBy(e => e).ToArray());
        }

        [Fact]
        public void OrderedToList()
        {
            var source = new[]
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 90 },
                new { Name = "Prakash", Score = 90 },
                new { Name = "Jim", Score = 90 },
                new { Name = "John", Score = 90 },
                new { Name = "Albert", Score = 90 },
            };
            var expected = new[]
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 90 },
                new { Name = "Prakash", Score = 90 },
                new { Name = "Jim", Score = 90 },
                new { Name = "John", Score = 90 },
                new { Name = "Albert", Score = 90 },
            };

            Assert.Equal(expected, source.OrderBy(e => e.Score).ToList());
        }

        [Fact]
        public void EmptyOrderedToList()
        {
            Assert.Empty(Enumerable.Empty<int>().OrderBy(e => e).ToList());
        }

        //FIXME: This will hang with a larger source. Do we want to deal with that case?
        [Fact]
        public void SurviveBadComparerAlwaysReturnsPositive()
        {
            int[] source = { 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, source.OrderBy(e => e, new BadComparer1()));
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
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.OrderBy(i => i));
        }

        [Fact]
        public void NullKeySelector()
        {
            Func<DateTime, int> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().OrderBy(keySelector));
        }
    }
}
