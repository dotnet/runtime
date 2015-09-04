// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class SequenceEqualTests
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
        
        private static IEnumerable<T> ForceNotCollection<T>(IEnumerable<T> source)
        {
            foreach (T item in source) yield return item;
        }
        
        private static IEnumerable<T> FlipIsCollection<T>(IEnumerable<T> source)
        {
            return source is ICollection<T> ? ForceNotCollection(source) : new List<T>(source);
        }
        
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
            var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                     select x2;

            Assert.Equal(q1.SequenceEqual(q2), q1.SequenceEqual(q2));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;
            var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                     select x2;

            Assert.Equal(q1.SequenceEqual(q2), q1.SequenceEqual(q2));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };

            Assert.True(first.SequenceEqual(second));
            Assert.True(FlipIsCollection(first).SequenceEqual(second));
            Assert.True(first.SequenceEqual(FlipIsCollection(second)));
            Assert.True(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void MismatchInMiddle()
        {
            int?[] first = { 1, 2, 3, 4 };
            int?[] second = { 1, 2, 6, 4 };
            
            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void NullComparer()
        {
            string[] first = { "Bob", "Tim", "Chris" };
            string[] second = { "Bbo", "mTi", "rishC" };

            Assert.False(first.SequenceEqual(second, null));
            Assert.False(FlipIsCollection(first).SequenceEqual(second, null));
            Assert.False(first.SequenceEqual(FlipIsCollection(second), null));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second), null));
        }

        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Bob", "Tim", "Chris" };
            string[] second = { "Bbo", "mTi", "rishC" };

            Assert.True(first.SequenceEqual(second, new AnagramEqualityComparer()));
            Assert.True(FlipIsCollection(first).SequenceEqual(second, new AnagramEqualityComparer()));
            Assert.True(first.SequenceEqual(FlipIsCollection(second), new AnagramEqualityComparer()));
            Assert.True(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second), new AnagramEqualityComparer()));
        }

        [Fact]
        public void BothSingleNullExplicitComparer()
        {
            string[] first = { null };
            string[] second = { null };
            
            Assert.True(first.SequenceEqual(second, StringComparer.Ordinal));
            Assert.True(FlipIsCollection(first).SequenceEqual(second, StringComparer.Ordinal));
            Assert.True(first.SequenceEqual(FlipIsCollection(second), StringComparer.Ordinal));
            Assert.True(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second), StringComparer.Ordinal));
        }

        [Fact]
        public void BothMatchIncludingNullElements()
        {
            int?[] first = { -6, null, 0, -4, 9, 10, 20 };
            int?[] second = { -6, null, 0, -4, 9, 10, 20 };

            Assert.True(first.SequenceEqual(second));
            Assert.True(FlipIsCollection(first).SequenceEqual(second));
            Assert.True(first.SequenceEqual(FlipIsCollection(second)));
            Assert.True(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void EmptyWithNonEmpty()
        {
            int?[] first = { };
            int?[] second = { 2, 3, 4 };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void NonEmptyWithEmpty()
        {
            int?[] first = { 2, 3, 4 };
            int?[] second = { };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void MismatchingSingletons()
        {
            int?[] first = { 2 };
            int?[] second = { 4 };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void MismatchOnFirst()
        {
            int?[] first = { 1, 2, 3, 4, 5 };
            int?[] second = { 2, 2, 3, 4, 5 };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }


        [Fact]
        public void MismatchOnLast()
        {
            int?[] first = { 1, 2, 3, 4, 4 };
            int?[] second = { 1, 2, 3, 4, 5 };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void SecondLargerThanFirst()
        {
            int?[] first = { 1, 2, 3, 4 };
            int?[] second = { 1, 2, 3, 4, 4 };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void FirstLargerThanSecond()
        {
            int?[] first = { 1, 2, 3, 4, 4 };
            int?[] second = { 1, 2, 3, 4 };

            Assert.False(first.SequenceEqual(second));
            Assert.False(FlipIsCollection(first).SequenceEqual(second));
            Assert.False(first.SequenceEqual(FlipIsCollection(second)));
            Assert.False(FlipIsCollection(first).SequenceEqual(FlipIsCollection(second)));
        }

        [Fact]
        public void FirstSourceNull()
        {
            int[] first = null;
            int[] second = { };
            
            Assert.Throws<ArgumentNullException>("first", () => first.SequenceEqual(second));
        }

        [Fact]
        public void SecondSourceNull()
        {
            int[] first = { };
            int[] second = null;
            
            Assert.Throws<ArgumentNullException>("second", () => first.SequenceEqual(second));
        }
    }
}
