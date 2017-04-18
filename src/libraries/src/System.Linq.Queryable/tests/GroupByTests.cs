// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class GroupByTests : EnumerableBasedTests
    {
        public static void AssertGroupingCorrect<TKey, TElement>(IQueryable<TKey> keys, IQueryable<TElement> elements, IQueryable<IGrouping<TKey, TElement>> grouping)
        {
            AssertGroupingCorrect<TKey, TElement>(keys, elements, grouping, EqualityComparer<TKey>.Default);
        }
        public static void AssertGroupingCorrect<TKey, TElement>(IQueryable<TKey> keys, IQueryable<TElement> elements, IQueryable<IGrouping<TKey, TElement>> grouping, IEqualityComparer<TKey> keyComparer)
        {
            if (grouping == null)
            {
                Assert.Null(elements);
                Assert.Null(keys);
                return;
            }

            Assert.NotNull(elements);
            Assert.NotNull(keys);

            Dictionary<TKey, List<TElement>> dict = new Dictionary<TKey, List<TElement>>(keyComparer);
            List<TElement> groupingForNullKeys = new List<TElement>();
            using (IEnumerator<TElement> elEn = elements.GetEnumerator())
            using (IEnumerator<TKey> keyEn = keys.GetEnumerator())
            {
                while (keyEn.MoveNext())
                {
                    Assert.True(elEn.MoveNext());

                    TKey key = keyEn.Current;

                    if (key == null)
                    {
                        groupingForNullKeys.Add(elEn.Current);
                    }
                    else
                    {
                        List<TElement> list;
                        if (!dict.TryGetValue(key, out list))
                            dict.Add(key, list = new List<TElement>());
                        list.Add(elEn.Current);
                    }
                }
                Assert.False(elEn.MoveNext());
            }
            foreach (IGrouping<TKey, TElement> group in grouping)
            {
                Assert.NotEmpty(group);
                TKey key = group.Key;
                List<TElement> list;

                if (key == null)
                {
                    Assert.Equal(groupingForNullKeys, group);
                    groupingForNullKeys.Clear();
                }
                else
                {
                    Assert.True(dict.TryGetValue(key, out list));
                    Assert.Equal(list, group);
                    dict.Remove(key);
                }
            }
            Assert.Empty(dict);
            Assert.Empty(groupingForNullKeys);
        }

        public struct Record
        {
            public string Name;
            public int Score;
        }

        [Fact]
        public void SingleNullKeySingleNullElement()
        {
            string[] key = { null };
            string[] element = { null };

            AssertGroupingCorrect(key.AsQueryable(), element.AsQueryable(), new string[] { null }.AsQueryable().GroupBy(e => e, e => e, EqualityComparer<string>.Default), EqualityComparer<string>.Default);
        }

        [Fact]
        public void EmptySource()
        {
            Assert.Empty(new Record[] { }.AsQueryable().GroupBy(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SourceIsNull()
        {
            IQueryable<Record> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score, (k, es) => es.Sum(), new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score, (k, es) => es.Sum()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, (k, es) => es.Sum(e => e.Score)));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, (k, es) => es.Sum(e => e.Score), new AnagramEqualityComparer()));
        }

        [Fact]
        public void KeySelectorNull()
        {
            Record[] source = new[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };
            Expression<Func<Record, string>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(null, e => e.Score, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(null, e => e.Score, (k, es) => es.Sum(), new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(keySelector, e => e.Score, (k, es) => es.Sum()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(keySelector, e => e.Score));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(keySelector, e => e.Score, (k, es) => es.Sum()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(null, (k, es) => es.Sum(e => e.Score), new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(keySelector, (k, es) => es.Sum(e => e.Score)));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(null, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AsQueryable().GroupBy(keySelector));
        }

        [Fact]
        public void ElementSelectorNull()
        {
            Record[] source = new[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Expression<Func<Record, int>> elementSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.AsQueryable().GroupBy(e => e.Name, elementSelector));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.AsQueryable().GroupBy(e => e.Name, elementSelector, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.AsQueryable().GroupBy(e => e.Name, elementSelector, (k, es) => es.Sum()));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.AsQueryable().GroupBy(e => e.Name, elementSelector, (k, es) => es.Sum(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void ResultSelectorNull()
        {
            Record[] source = {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Expression<Func<string, IEnumerable<int>, long>> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.AsQueryable().GroupBy(e => e.Name, e => e.Score, resultSelector, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ResultSelectorNullNoComparer()
        {
            Record[] source = {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Expression<Func<string, IEnumerable<int>, long>> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.AsQueryable().GroupBy(e => e.Name, e => e.Score, resultSelector));
        }

        [Fact]
        public void ResultSelectorNullNoElementSelector()
        {
            Record[] source = {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Expression<Func<string, IEnumerable<Record>, long>> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.AsQueryable().GroupBy(e => e.Name, resultSelector));
        }

        [Fact]
        public void ResultSelectorNullNoElementSelectorCustomComparer()
        {
            string[] key = { "Tim", "Tim", "Tim", "Tim" };
            int[] element = { 60, -10, 40, 100 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            Expression<Func<string, IEnumerable<Record>, long>> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.AsQueryable().GroupBy(e => e.Name, resultSelector, new AnagramEqualityComparer()));
        }

        [Fact]
        public void EmptySourceWithResultSelector()
        {
            Assert.Empty(new Record[] { }.AsQueryable().GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void DuplicateKeysCustomComparer()
        {
            string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
            int[] element = { 55, 25, 49, 24, -100, 9 };
            Record[] source = {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "miT", Score = 25 }
            };
            long[] expected = { 240, 365, -600, 63 };

            Assert.Equal(expected, source.AsQueryable().GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void NullComparer()
        {
            string[] key = { "Tim", null, null, "Robert", "Chris", "miT" };
            int[] element = { 55, 49, 9, -100, 24, 25 };
            Record[] source = {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = null, Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = null, Score = 9 },
                new Record { Name = "miT", Score = 25 }
            };
            long[] expected = { 165, 58, -600, 120, 75 };

            Assert.Equal(expected, source.AsQueryable().GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), null));
        }

        [Fact]
        public void SingleNonNullElement()
        {
            string[] key = { "Tim" };
            Record[] source = { new Record { Name = key[0], Score = 60 } };

            AssertGroupingCorrect(key.AsQueryable(), source.AsQueryable(), source.AsQueryable().GroupBy(e => e.Name));
        }

        [Fact]
        public void AllElementsSameKey()
        {
            string[] key = { "Tim", "Tim", "Tim", "Tim" };
            int[] scores = { 60, -10, 40, 100 };
            var source = key.Zip(scores, (k, e) => new Record { Name = k, Score = e });

            AssertGroupingCorrect(key.AsQueryable(), source.AsQueryable(), source.AsQueryable().GroupBy(e => e.Name, new AnagramEqualityComparer()), new AnagramEqualityComparer());
        }


        [Fact]
        public void AllElementsSameKeyResultSelectorUsed()
        {
            int[] element = { 60, -10, 40, 100 };
            long[] expected = { 570 };
            Record[] source = {
                new Record { Name = "Tim", Score = element[0] },
                new Record { Name = "Tim", Score = element[1] },
                new Record { Name = "miT", Score = element[2] },
                new Record { Name = "miT", Score = element[3] }
            };

            Assert.Equal(expected, source.AsQueryable().GroupBy(e => e.Name, (k, es) => k.Length * es.Sum(e => (long)e.Score), new AnagramEqualityComparer()));
        }

        [Fact]
        public void NullComparerResultSelectorUsed()
        {
            int[] element = { 60, -10, 40, 100 };
            Record[] source = {
                new Record { Name = "Tim", Score = element[0] },
                new Record { Name = "Tim", Score = element[1] },
                new Record { Name = "miT", Score = element[2] },
                new Record { Name = "miT", Score = element[3] },
            };

            long[] expected = { 150, 420 };

            Assert.Equal(expected, source.AsQueryable().GroupBy(e => e.Name, (k, es) => k.Length * es.Sum(e => (long)e.Score), null));
        }

        [Fact]
        public void GroupBy1()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy2()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy3()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy4()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy5()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n, (k, g) => k).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy6()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, (k, g) => k).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy7()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n, (k, g) => k, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy8()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, (k, g) => k, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
