// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class UnionTests
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
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
            var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                     select x2;
                     
            Assert.Equal(q1.Union(q2), q1.Union(q2));
        }
        
        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;
            var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                     select x2;
                     
            Assert.Equal(q1.Union(q2), q1.Union(q2));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            int[] expected = { };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "Charlie" };
            
            var comparer = new AnagramEqualityComparer();

            Assert.Equal(expected, first.Union(second, comparer), comparer);
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            string[] first = null;
            string[] second = { "ttaM", "Charlie", "Bbo" };
            
            var ane = Assert.Throws<ArgumentNullException>("first", () => first.Union(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = null;

            var ane = Assert.Throws<ArgumentNullException>("second", () => first.Union(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SingleNullWithEmpty()
        {
            string[] first = { null };
            string[] second = new string[0];
            string[] expected = { null };
            
            Assert.Equal(expected, first.Union(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NullEmptyStringMix()
        {
            string[] first = { null, null, string.Empty };
            string[] second = { null, null };
            string[] expected = { null, string.Empty };

            Assert.Equal(expected, first.Union(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void DoubleNullWithEmpty()
        {
            string[] first = { null, null };
            string[] second = new string[0];
            string[] expected = { null };

            Assert.Equal(expected, first.Union(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyWithNonEmpty()
        {
            int[] first = { };
            int[] second = { 2, 4, 5, 3, 2, 3, 9 };
            int[] expected = { 2, 4, 5, 3, 9 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void NonEmptyWithEmpty()
        {
            int[] first = { 2, 4, 5, 3, 2, 3, 9 };
            int[] second = { };
            int[] expected = { 2, 4, 5, 3, 9 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void CommonElementsShared()
        {
            int[] first = { 1, 2, 3, 4, 5, 6 };
            int[] second = { 6, 7, 7, 7, 8, 1 };
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void SameElementRepeated()
        {
            int[] first = { 1, 1, 1, 1, 1, 1 };
            int[] second = { 1, 1, 1, 1, 1, 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void RepeatedElementsWithSingleElement()
        {
            int[] first = { 1, 2, 3, 5, 3, 6 };
            int[] second = { 7 };
            int[] expected = { 1, 2, 3, 5, 6, 7 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void SingleWithAllUnique()
        {
            int?[] first = { 2 };
            int?[] second = { 3, null, 4, 5 };
            int?[] expected = { 2, 3, null, 4, 5 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void EachHasRepeatsBetweenAndAmongstThemselves()
        {
            int?[] first = { 1, 2, 3, 4, null, 5, 1 };
            int?[] second = { 6, 2, 3, 4, 5, 6 };
            int?[] expected = { 1, 2, 3, 4, null, 5, 6 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void NullEqualityComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "miT", "ttaM", "Charlie", "Bbo" };

            Assert.Equal(expected, first.Union(second, null));
        }
    }
}
