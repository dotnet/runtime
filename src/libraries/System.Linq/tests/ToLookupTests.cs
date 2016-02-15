// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ToLookupTests : EnumerableTests
    {
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
            
            Assert.Equal(q.ToLookup(e => e.a1), q.ToLookup(e => e.a1));
        }

        [Fact]
        public void NullKeyIncluded()
        {
            string[] key = { "Chris", "Bob", null, "Tim" };
            int[] element = { 50, 95, 55, 90 };
            var source = key.Zip(element, (k, e) => new { Name = k, Score = e });

            AssertMatches(key, source, source.ToLookup(e => e.Name));
        }

        [Fact]
        public void OneElementCustomComparer()
        {
            string[] key = { "Chris" };
            int[] element = { 50 };
            var source = new [] { new {Name = "risCh", Score = 50} };

            AssertMatches(key, source, source.ToLookup(e => e.Name, new AnagramEqualityComparer()));
        }

        [Fact]
        public void UniqueElementsElementSelector()
        {
            string[] key = { "Chris", "Prakash", "Tim", "Robert", "Brian" };
            int[] element = { 50, 100, 95, 60, 80 };
            var source = new []
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[1] },
                new { Name = key[2], Score = element[2] },
                new { Name = key[3], Score = element[3] },
                new { Name = key[4], Score = element[4] }
            };

            AssertMatches(key, element, source.ToLookup(e => e.Name, e => e.Score));
        }

        [Fact]
        public void DuplicateKeys()
        {
            string[] key = { "Chris", "Prakash", "Robert" };
            int[] element = { 50, 80, 100, 95, 99, 56 };
            var source = new[]
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[2] },
                new { Name = key[2], Score = element[5] },
                new { Name = key[1], Score = element[3] },
                new { Name = key[0], Score = element[1] },
                new { Name = key[1], Score = element[4] }
            };

            AssertMatches(key, element, source.ToLookup(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void Count()
        {
            string[] key = { "Chris", "Prakash", "Robert" };
            int[] element = { 50, 80, 100, 95, 99, 56 };
            var source = new[]
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[2] },
                new { Name = key[2], Score = element[5] },
                new { Name = key[1], Score = element[3] },
                new { Name = key[0], Score = element[1] },
                new { Name = key[1], Score = element[4] }
            };

            Assert.Equal(3, source.ToLookup(e => e.Name, e => e.Score).Count());
        }

        [Fact]
        public void EmptySource()
        {
            string[] key = { };
            int[] element = { };
            var source = key.Zip(element, (k, e) => new { Name = k, Score = e });

            AssertMatches(key, element, source.ToLookup(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SingleNullKeyAndElement()
        {
            string[] key = { null };
            string[] element = { null };
            string[] source = new string[] { null };

            AssertMatches(key, element, source.ToLookup(e => e, e => e, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10));
        }

        [Fact]
        public void NullSourceExplicitComparer()
        {
            IEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullSourceElementSelector()
        {
            IEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10, i => i + 2));
        }

        [Fact]
        public void NullSourceElementSelectorExplicitComparer()
        {
            IEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10, i => i + 2, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullKeySelector()
        {
            Func<int, int> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector));
        }

        [Fact]
        public void NullKeySelectorExplicitComparer()
        {
            Func<int, int> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullKeySelectorElementSelector()
        {
            Func<int, int> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector, i => i + 2));
        }

        [Fact]
        public void NullKeySelectorElementSelectorExplicitComparer()
        {
            Func<int, int> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector, i => i + 2, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullElementSelector()
        {
            Func<int, int> elementSelector = null;
            Assert.Throws<ArgumentNullException>("elementSelector", () => Enumerable.Range(0, 1000).ToLookup(i => i / 10, elementSelector));
        }

        [Fact]
        public void NullElementSelectorExplicitComparer()
        {
            Func<int, int> elementSelector = null;
            Assert.Throws<ArgumentNullException>("elementSelector", () => Enumerable.Range(0, 1000).ToLookup(i => i / 10, elementSelector, EqualityComparer<int>.Default));
        }
    }
}
