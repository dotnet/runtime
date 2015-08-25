// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class DistinctTests
    {
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
            var q = from x in new[] { 0, 9999, 0, 888, -1, 66, -1, -777, 1, 2, -12345, 66, 66, -1, -1 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Distinct(), q.Distinct());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "Calling Twice", "SoS" }
                    where String.IsNullOrEmpty(x)
                    select x;


            Assert.Equal(q.Distinct(), q.Distinct());
        }

        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            int[] expected = { };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void SingleNullElementExplicitlyUseDefaultComparer()
        {
            string[] source = { null };
            string[] expected = { null };

            Assert.Equal(expected, source.Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyStringDistinctFromNull()
        {
            string[] source = { null, null, string.Empty };
            string[] expected = { null, string.Empty };

            Assert.Equal(expected, source.Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void CollapsDuplicateNulls()
        {
            string[] source = { null, null };
            string[] expected = { null };

            Assert.Equal(expected, source.Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void SourceAllDuplicates()
        {
            int[] source = { 5, 5, 5, 5, 5, 5 };
            int[] expected = { 5 };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void AllUnique()
        {
            int[] source = { 2, -5, 0, 6, 10, 9 };

            Assert.Equal(source, source.Distinct());
        }

        [Fact]
        public void SomeDuplicatesIncludingNulls()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
            int?[] expected = { 1, 2, null };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void LastSameAsFirst()
        {
            int[] source = { 1, 2, 3, 4, 5, 1 };
            int[] expected = { 1, 2, 3, 4, 5 };

            Assert.Equal(expected, source.Distinct());
        }

        // Multiple elements repeat non-consecutively
        [Fact]
        public void RepeatsNonConsecutive()
        {
            int[] source = { 1, 1, 2, 2, 4, 3, 1, 3, 2 };
            int[] expected = { 1, 2, 4, 3 };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void NullComparer()
        {
            string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
            string[] expected = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void NullSource()
        {
            string[] source = null;
            
            Assert.Throws<ArgumentNullException>(() => source.Distinct());
        }

        [Fact]
        public void CustomEqualityComparer()
        {
            string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
            string[] expected = { "Bob", "Tim", "Robert" };

            Assert.Equal(expected, source.Distinct(new AnagramEqualityComparer()), new AnagramEqualityComparer());
        }
    }
}
