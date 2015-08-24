// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ToLookupTests
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

        private struct Record
        {
            public string Name;
            public int Score;
        }

        private static void AssertMatches<K, T>(IEnumerable<K> keys, IEnumerable<T> elements, System.Linq.ILookup<K, T> lookup)
        {
            Assert.NotNull(lookup);
            Assert.NotNull(keys);
            Assert.NotNull(elements);

            int num = 0;
            using (IEnumerator<K> keyEnumerator = keys.GetEnumerator())
            using (IEnumerator<T> elEnumerator = elements.GetEnumerator())
            {
                while (keyEnumerator.MoveNext())
                {
                    Assert.True(lookup.Contains(keyEnumerator.Current));

                    foreach (T e in lookup[keyEnumerator.Current])
                    {
                        Assert.True(elEnumerator.MoveNext());
                        Assert.Equal(e, elEnumerator.Current);
                    }
                    ++num;
                }
                Assert.False(elEnumerator.MoveNext());
            }
            Assert.Equal(num, lookup.Count);
        }

        [Fact]
        public void SameResultsRepeatCall()
        {
            var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                     select x1; ;

            var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                     select x2;

            var q = from x3 in q1
                    from x4 in q2
                    select new { a1 = x3, a2 = x4 };
            
            Assert.Equal(q.ToLookup((e) => e.a1), q.ToLookup((e) => e.a1));
        }

        [Fact]
        public void NullKeyIncluded()
        {
            string[] key = { "Chris", "Bob", null, "Tim" };
            int[] element = { 50, 95, 55, 90 };
            Record[] source = new Record[4];

            source[0].Name = key[0]; source[0].Score = element[0];
            source[1].Name = key[1]; source[1].Score = element[1];
            source[2].Name = key[2]; source[2].Score = element[2];
            source[3].Name = key[3]; source[3].Score = element[3];

            AssertMatches(key, source, source.ToLookup((e) => e.Name));
        }

        [Fact]
        public void OneElementCustomComparer()
        {
            string[] key = { "Chris" };
            int[] element = { 50 };
            Record[] source = new [] { new Record{Name = "risCh", Score = 50} };

            AssertMatches(key, source, source.ToLookup((e) => e.Name, new AnagramEqualityComparer()));
        }

        [Fact]
        public void UniqueElementsElementSelector()
        {
            string[] key = { "Chris", "Prakash", "Tim", "Robert", "Brian" };
            int[] element = { 50, 100, 95, 60, 80 };
            Record[] source = new []
            {
                new Record{ Name = key[0], Score = element[0] },
                new Record{ Name = key[1], Score = element[1] },
                new Record{ Name = key[2], Score = element[2] },
                new Record{ Name = key[3], Score = element[3] },
                new Record{ Name = key[4], Score = element[4] }
            };

            AssertMatches(key, element, source.ToLookup((e) => e.Name, (e) => e.Score));
        }

        [Fact]
        public void DuplicateKeys()
        {
            string[] key = { "Chris", "Prakash", "Robert" };
            int[] element = { 50, 80, 100, 95, 99, 56 };
            Record[] source = new []
            {
                new Record{ Name = key[0], Score = element[0] },
                new Record{ Name = key[1], Score = element[2] },
                new Record{ Name = key[2], Score = element[5] },
                new Record{ Name = key[1], Score = element[3] },
                new Record{ Name = key[0], Score = element[1] },
                new Record{ Name = key[1], Score = element[4] }
            };

            AssertMatches(key, element, source.ToLookup((e) => e.Name, (e) => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void EmptySource()
        {
            string[] key = { };
            int[] element = { };
            Record[] source = new Record[] { };

            AssertMatches(key, element, source.ToLookup((e) => e.Name, (e) => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SingleNullKeyAndElement()
        {
            string[] key = { null };
            string[] element = { null };
            string[] source = new string[] { null };

            AssertMatches(key, element, source.ToLookup((e) => e, (e) => e, EqualityComparer<string>.Default));
        }
    }
}
