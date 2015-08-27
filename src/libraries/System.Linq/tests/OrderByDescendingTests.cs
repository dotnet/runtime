// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderByDescendingTests
    {
        private struct Record
        {
#pragma warning disable 0649
            public string Name;
            public int Score;
#pragma warning restore 0649

        }

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
                             from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", String.Empty }
                             where !String.IsNullOrEmpty(x2)
                             select new { a1 = x1, a2 = x2 };

            Assert.Equal(q.OrderByDescending(e => e.a1).ThenBy(f => f.a2), q.OrderByDescending(e => e.a1).ThenBy(f => f.a2));
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            int[] expected = { };

            Assert.Equal(expected, source.OrderByDescending(e => e));
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
            Record[] source = new Record[]
            {

                new Record{ Name = "Alpha", Score = 90 },
                new Record{ Name = "Robert", Score = 45 },
                new Record{ Name = "Prakash", Score = 99 },
                new Record{ Name = "Bob", Score = 0 }
            };
            Record[] expected = new Record[]
            {
                new Record{ Name = "Robert", Score = 45 },
                new Record{ Name = "Prakash", Score = 99 },
                new Record{ Name = "Bob", Score = 0 },
                new Record{ Name = "Alpha", Score = 90 }
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
            Record[] source = new Record[]
            {
                new Record{ Name = "Alpha", Score = 90 },
                new Record{ Name = "Robert", Score = 45 },
                new Record{ Name = "Prakash", Score = 99 },
                new Record{ Name = "Bob", Score = 90 },
                new Record{ Name = "Thomas", Score = 45 },
                new Record{ Name = "Tim", Score = 45 },
                new Record{ Name = "Mark", Score = 45 },
            };
            Record[] expected = new Record[]
            {
                new Record{ Name = "Prakash", Score = 99 },
                new Record{ Name = "Alpha", Score = 90 },
                new Record{ Name = "Bob", Score = 90 },
                new Record{ Name = "Robert", Score = 45 },
                new Record{ Name = "Thomas", Score = 45 },
                new Record{ Name = "Tim", Score = 45 },
                new Record{ Name = "Mark", Score = 45 },
            };

            Assert.Equal(expected, source.OrderByDescending(e => e.Score));
        }

        private class ExtremeComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if(x == y)
                    return 0;
                if(x < y)
                    return int.MinValue;
                return int.MaxValue;
            }
        }

        [Fact]
        public void OrderByExtremeComparer()
        {
            var outOfOrder = new[] { 7, 1, 0, 9, 3, 5, 4, 2, 8, 6 };
            Assert.Equal(Enumerable.Range(0, 10).Reverse(), outOfOrder.OrderByDescending(i => i, new ExtremeComparer()));
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>(() => source.OrderByDescending(i => i));
        }
    }
}