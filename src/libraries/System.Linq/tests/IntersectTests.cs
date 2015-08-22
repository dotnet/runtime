// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class IntersectTests
    {
        // Class which is passed as an argument for EqualityComparer
        private class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null | y == null) return false;
                int length = x.Length;
                if (length != y.Length) return false;
                using (var en = x.OrderBy(i => i).GetEnumerator())
                {
                    foreach (char c in y.OrderBy(i => i))
                    {
                        en.MoveNext();
                        if (c != en.Current) return false;
                    }
                }
                return true;
            }

            public int GetHashCode(string obj)
            {
                int hash = 0;
                foreach (char c in obj)
                    hash ^= (int)c;
                return hash;
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var first = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                        select x1;
            var second = from x2 in new int?[] { 1, 9, null, 4 }
                         select x2;

            Assert.Equal(first.Intersect(second), first.Intersect(second));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var first = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                        select x1;
            var second = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                         select x2;

            Assert.Equal(first.Intersect(second), first.Intersect(second));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            int[] expected = { };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            string[] first = null;
            string[] second = { "ekiM", "bBo" };

            var ane = Assert.Throws<ArgumentNullException>("first", () => first.Intersect(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = null;

            var ane = Assert.Throws<ArgumentNullException>("second", () => first.Intersect(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SingleNullWithEmpty()
        {
            string[] first = { null };
            string[] second = new string[0];
            string[] expected = new string[0];

            Assert.Equal(expected, first.Intersect(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NullEmptyStringMix()
        {
            string[] first = { null, null, string.Empty };
            string[] second = { null, null };
            string[] expected = { null };

            Assert.Equal(expected, first.Intersect(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void DoubleNullWithEmpty()
        {
            string[] first = { null, null };
            string[] second = new string[0];
            string[] expected = new string[0];

            Assert.Equal(expected, first.Intersect(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyWithNonEmpty()
        {
            int?[] first = { };
            int?[] second = { -5, 0, null, 1, 2, 9, 2 };
            int?[] expected = { };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void NonEmptyWithEmpty()
        {
            int?[] first = { -5, 0, 1, 2, null, 9, 2 };
            int?[] second = { };
            int?[] expected = { };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void AllDistinct()
        {
            int[] first = { -5, 3, -2, 6, 9 };
            int[] second = { 0, 5, 2, 10, 20 };
            int[] expected = { };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void OverlapInConcatenation()
        {
            int?[] first = { 1, 2, null, 3, 4, 5, 6 };
            int?[] second = { 6, 7, 7, 7, null, 8, 1 };
            int?[] expected = { 1, null, 6 };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void EachHasRepeatsBetweenAndAmongstThemselves()
        {
            int[] first = { 1, 2, 2, 3, 4, 3, 5 };
            int[] second = { 1, 4, 4, 2, 2, 2 };
            int[] expected = { 1, 2, 4 };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void BothHaveSameElements()
        {
            int[] first = { 1, 1, 1, 1, 1, 1 };
            int[] second = { 1, 1, 1, 1, 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void NullEqualityComparer()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };
            string[] expected = { };

            Assert.Equal(expected, first.Intersect(second));
        }

        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };
            string[] expected = { "Bob", "Mike" };

            Assert.Equal(expected, first.Intersect(second, new AnagramEqualityComparer()));
        }
    }
}
