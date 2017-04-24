// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Linq.Tests
{
    public partial class GroupByTests : EnumerableTests
    {
        public static void AssertGroupingCorrect<TKey, TElement>(IEnumerable<TKey> keys, IEnumerable<TElement> elements, IEnumerable<IGrouping<TKey, TElement>> grouping)
        {
            AssertGroupingCorrect<TKey, TElement>(keys, elements, grouping, EqualityComparer<TKey>.Default);
        }
        public static void AssertGroupingCorrect<TKey, TElement>(IEnumerable<TKey> keys, IEnumerable<TElement> elements, IEnumerable<IGrouping<TKey, TElement>> grouping, IEqualityComparer<TKey> keyComparer)
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
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                     select x1; ;

            var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                     select x2;

            var q = from x3 in q1
                    from x4 in q2
                    select new { a1 = x3, a2 = x4 };

            Assert.NotNull(q.GroupBy(e => e.a1, e => e.a2));
            Assert.Equal(q.GroupBy(e => e.a1, e => e.a2), q.GroupBy(e => e.a1, e => e.a2));
        }

        [Fact]
        public void Grouping_IList_IsReadOnly()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            foreach (IList<int> grouping in oddsEvens)
            {
                Assert.True(grouping.IsReadOnly);
            }
        }

        [Fact]
        public void Grouping_IList_NotSupported()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            foreach (IList<int> grouping in oddsEvens)
            {
                Assert.Throws<NotSupportedException>(() => grouping.Add(5));
                Assert.Throws<NotSupportedException>(() => grouping.Clear());
                Assert.Throws<NotSupportedException>(() => grouping.Insert(0, 1));
                Assert.Throws<NotSupportedException>(() => grouping.Remove(1));
                Assert.Throws<NotSupportedException>(() => grouping.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => grouping[0] = 1);
            }
        }

        [Fact]
        public void Grouping_IList_IndexerGetter()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            IList<int> odds = (IList<int>)e.Current;
            Assert.Equal(1, odds[0]);
            Assert.Equal(3, odds[1]);

            Assert.True(e.MoveNext());
            IList<int> evens = (IList<int>)e.Current;
            Assert.Equal(2, evens[0]);
            Assert.Equal(4, evens[1]);
        }

        [Fact]
        public void Grouping_IList_IndexGetterOutOfRange()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            IList<int> odds = (IList<int>)e.Current;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => odds[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => odds[23]);
        }

        [Fact]
        public void Grouping_ICollection_Contains()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            ICollection<int> odds = (IList<int>)e.Current;
            Assert.True(odds.Contains(1));
            Assert.True(odds.Contains(3));
            Assert.False(odds.Contains(2));
            Assert.False(odds.Contains(4));

            Assert.True(e.MoveNext());
            ICollection<int> evens = (IList<int>)e.Current;
            Assert.True(evens.Contains(2));
            Assert.True(evens.Contains(4));
            Assert.False(evens.Contains(1));
            Assert.False(evens.Contains(3));
        }

        [Fact]
        public void Grouping_IList_IndexOf()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            IList<int> odds = (IList<int>)e.Current;
            Assert.Equal(0, odds.IndexOf(1));
            Assert.Equal(1, odds.IndexOf(3));
            Assert.Equal(-1, odds.IndexOf(2));
            Assert.Equal(-1, odds.IndexOf(4));

            Assert.True(e.MoveNext());
            IList<int> evens = (IList<int>)e.Current;
            Assert.Equal(0, evens.IndexOf(2));
            Assert.Equal(1, evens.IndexOf(4));
            Assert.Equal(-1, evens.IndexOf(1));
            Assert.Equal(-1, evens.IndexOf(3));
        }

        [Fact]
        public void SingleNullKeySingleNullElement()
        {
            string[] key = { null };
            string[] element = { null };

            AssertGroupingCorrect(key, element, new string[] { null }.GroupBy(e => e, e => e, EqualityComparer<string>.Default), EqualityComparer<string>.Default);
        }

        [Fact]
        public void EmptySource()
        {
            string[] key = { };
            int[] element = { };
            Record[] source = { };
            Assert.Empty(new Record[] { }.GroupBy(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void EmptySourceRunOnce()
        {
            string[] key = { };
            int[] element = { };
            Record[] source = { };
            Assert.Empty(new Record[] { }.RunOnce().GroupBy(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SourceIsNull()
        {
            Record[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SourceIsNullResultSelectorUsed()
        {
            Record[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score, (k, es) => es.Sum(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void SourceIsNullResultSelectorUsedNoComparer()
        {
            Record[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, e => e.Score, (k, es) => es.Sum()));
        }

        [Fact]
        public void SourceIsNullResultSelectorUsedNoComparerOrElementSelector()
        {
            Record[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.GroupBy(e => e.Name, (k, es) => es.Sum(e => e.Score)));
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

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.GroupBy(null, e => e.Score, new AnagramEqualityComparer()));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.GroupBy(null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void KeySelectorNullResultSelectorUsed()
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

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.GroupBy(null, e => e.Score, (k, es) => es.Sum(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void KeySelectorNullResultSelectorUsedNoComparer()
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

            Func<Record, string> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.GroupBy(keySelector, e => e.Score, (k, es) => es.Sum()));
        }

        [Fact]
        public void KeySelectorNullResultSelectorUsedNoElementSelector()
        {
            string[] key = { "Tim", "Tim", "Tim", "Tim" };
            int[] element = { 60, -10, 40, 100 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.GroupBy(null, (k, es) => es.Sum(e => e.Score), new AnagramEqualityComparer()));
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

            Func<Record, int> elementSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.GroupBy(e => e.Name, elementSelector, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ElementSelectorNullResultSelectorUsedNoComparer()
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

            Func<Record, int> elementSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.GroupBy(e => e.Name, elementSelector, (k, es) => es.Sum()));
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

            Func<string, IEnumerable<int>, long> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.GroupBy(e => e.Name, e => e.Score, resultSelector, new AnagramEqualityComparer()));
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

            Func<string, IEnumerable<int>, long> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.GroupBy(e => e.Name, e => e.Score, resultSelector));
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

            Func<string, IEnumerable<Record>, long> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.GroupBy(e => e.Name, resultSelector));
        }

        [Fact]
        public void ResultSelectorNullNoElementSelectorCustomComparer()
        {
            string[] key = { "Tim", "Tim", "Tim", "Tim" };
            int[] element = { 60, -10, 40, 100 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            Func<string, IEnumerable<Record>, long> resultSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => source.GroupBy(e => e.Name, resultSelector, new AnagramEqualityComparer()));
        }

        [Fact]
        public void EmptySourceWithResultSelector()
        {
            string[] key = { };
            int[] element = { };
            Record[] source = { };
            Assert.Empty(new Record[] { }.GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), new AnagramEqualityComparer()));
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

            Assert.Equal(expected, source.GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void DuplicateKeysCustomComparerRunOnce()
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

            Assert.Equal(expected, source.RunOnce().GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), new AnagramEqualityComparer()));
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

            Assert.Equal(expected, source.GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), null));
        }

        [Fact]
        public void NullComparerRunOnce()
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

            Assert.Equal(expected, source.RunOnce().GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum(), null));
        }

        [Fact]
        public void SingleNonNullElement()
        {
            string[] key = { "Tim" };
            Record[] source = { new Record { Name = key[0], Score = 60 } };

            AssertGroupingCorrect(key, source, source.GroupBy(e => e.Name));
        }

        [Fact]
        public void AllElementsSameKey()
        {
            string[] key = { "Tim", "Tim", "Tim", "Tim" };
            int[] scores = { 60, -10, 40, 100 };
            var source = key.Zip(scores, (k, e) => new Record { Name = k, Score = e });

            AssertGroupingCorrect(key, source, source.GroupBy(e => e.Name, new AnagramEqualityComparer()), new AnagramEqualityComparer());
        }

        [Fact]
        public void AllElementsDifferentKeyElementSelectorUsed()
        {
            string[] key = { "Tim", "Chris", "Robert", "Prakash" };
            int[] element = { 60, -10, 40, 100 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            AssertGroupingCorrect(key, element, source.GroupBy(e => e.Name, e => e.Score));
        }

        [Fact]
        public void SomeDuplicateKeys()
        {
            string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
            int[] element = { 55, 25, 49, 24, -100, 9 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            AssertGroupingCorrect(key, element, source.GroupBy(e => e.Name, e => e.Score));
        }

        [Fact]
        public void SomeDuplicateKeysIncludingNulls()
        {
            string[] key = { null, null, "Chris", "Chris", "Prakash", "Prakash" };
            int[] element = { 55, 25, 49, 24, 9, 9 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            AssertGroupingCorrect(key, element, source.GroupBy(e => e.Name, e => e.Score));
        }

        [Fact]
        public void SingleElementResultSelectorUsed()
        {
            string[] key = { "Tim" };
            int[] element = { 60 };
            long[] expected = { 180 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });

            Assert.Equal(expected, source.GroupBy(e => e.Name, (k, es) => (long)(k ?? " ").Length * es.Sum(e => e.Score)));
        }

        [Fact]
        public void GroupedResultCorrectSize()
        {
            var elements = Enumerable.Repeat('q', 5);

            var result = elements.GroupBy(e => e, (e, f) => new { Key = e, Element = f });

            Assert.Equal(1, result.Count());

            var grouping = result.First();

            Assert.Equal(5, grouping.Element.Count());
            Assert.Equal('q', grouping.Key);
            Assert.True(grouping.Element.All(e => e == 'q'));
        }

        [Fact]
        public void AllElementsDifferentKeyElementSelectorUsedResultSelector()
        {
            string[] key = { "Tim", "Chris", "Robert", "Prakash" };
            int[] element = { 60, -10, 40, 100 };
            var source = key.Zip(element, (k, e) => new Record { Name = k, Score = e });
            long[] expected = { 180, -50, 240, 700 };

            Assert.Equal(expected, source.GroupBy(e => e.Name, e => e.Score, (k, es) => (long)(k ?? " ").Length * es.Sum()));
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

            Assert.Equal(expected, source.GroupBy(e => e.Name, (k, es) => k.Length * es.Sum(e => (long)e.Score), new AnagramEqualityComparer()));
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

            Assert.Equal(expected, source.GroupBy(e => e.Name, (k, es) => k.Length * es.Sum(e => (long)e.Score), null));
        }

        [Fact]
        public void GroupingToArray()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Tim", Score = 55 },
                new Record{ Name = "Chris", Score = 49 },
                new Record{ Name = "Robert", Score = -100 },
                new Record{ Name = "Chris", Score = 24 },
                new Record{ Name = "Prakash", Score = 9 },
                new Record{ Name = "Tim", Score = 25 }
            };

            IGrouping<string, Record>[] groupedArray = source.GroupBy(r => r.Name).ToArray();
            Assert.Equal(4, groupedArray.Length);
            Assert.Equal(source.GroupBy(r => r.Name), groupedArray);
        }

        [Fact]
        public void GroupingWithElementSelectorToArray()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Tim", Score = 55 },
                new Record{ Name = "Chris", Score = 49 },
                new Record{ Name = "Robert", Score = -100 },
                new Record{ Name = "Chris", Score = 24 },
                new Record{ Name = "Prakash", Score = 9 },
                new Record{ Name = "Tim", Score = 25 }
            };

            IGrouping<string, int>[] groupedArray = source.GroupBy(r => r.Name, e => e.Score).ToArray();
            Assert.Equal(4, groupedArray.Length);
            Assert.Equal(source.GroupBy(r => r.Name, e => e.Score), groupedArray);
        }

        [Fact]
        public void GroupingWithResultsToArray()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Tim", Score = 55 },
                new Record{ Name = "Chris", Score = 49 },
                new Record{ Name = "Robert", Score = -100 },
                new Record{ Name = "Chris", Score = 24 },
                new Record{ Name = "Prakash", Score = 9 },
                new Record{ Name = "Tim", Score = 25 }
            };

            IEnumerable<Record>[] groupedArray = source.GroupBy(r => r.Name, (r, e) => e).ToArray();
            Assert.Equal(4, groupedArray.Length);
            Assert.Equal(source.GroupBy(r => r.Name, (r, e) => e), groupedArray);
        }

        [Fact]
        public void GroupingWithElementSelectorAndResultsToArray()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Tim", Score = 55 },
                new Record{ Name = "Chris", Score = 49 },
                new Record{ Name = "Robert", Score = -100 },
                new Record{ Name = "Chris", Score = 24 },
                new Record{ Name = "Prakash", Score = 9 },
                new Record{ Name = "Tim", Score = 25 }
            };

            IEnumerable<Record>[] groupedArray = source.GroupBy(r => r.Name, e => e, (r, e) => e).ToArray();
            Assert.Equal(4, groupedArray.Length);
            Assert.Equal(source.GroupBy(r => r.Name, e => e, (r, e) => e), groupedArray);
        }

        [Fact]
        public void GroupingToList()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            List<IGrouping<string, Record>> groupedList = source.GroupBy(r => r.Name).ToList();
            Assert.Equal(4, groupedList.Count);
            Assert.Equal(source.GroupBy(r => r.Name), groupedList);
        }

        [Fact]
        public void GroupingWithElementSelectorToList()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            List<IGrouping<string, int>> groupedList = source.GroupBy(r => r.Name, e => e.Score).ToList();
            Assert.Equal(4, groupedList.Count);
            Assert.Equal(source.GroupBy(r => r.Name, e => e.Score), groupedList);
        }

        [Fact]
        public void GroupingWithResultsToList()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            List<IEnumerable<Record>> groupedList = source.GroupBy(r => r.Name, (r, e) => e).ToList();
            Assert.Equal(4, groupedList.Count);
            Assert.Equal(source.GroupBy(r => r.Name, (r, e) => e), groupedList);
        }

        [Fact]
        public void GroupingWithElementSelectorAndResultsToList()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            List<IEnumerable<Record>> groupedList = source.GroupBy(r => r.Name, e => e, (r, e) => e).ToList();
            Assert.Equal(4, groupedList.Count);
            Assert.Equal(source.GroupBy(r => r.Name, e => e, (r, e) => e), groupedList);
        }

        [Fact]
        public void GroupingCount()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Assert.Equal(4, source.GroupBy(r => r.Name).Count());
        }

        [Fact]
        public void GroupingWithElementSelectorCount()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Assert.Equal(4, source.GroupBy(r => r.Name, e => e.Score).Count());
        }

        [Fact]
        public void GroupingWithResultsCount()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Assert.Equal(4, source.GroupBy(r => r.Name, (r, e) => e).Count());
        }

        [Fact]
        public void GroupingWithElementSelectorAndResultsCount()
        {
            Record[] source = new Record[]
            {
                new Record { Name = "Tim", Score = 55 },
                new Record { Name = "Chris", Score = 49 },
                new Record { Name = "Robert", Score = -100 },
                new Record { Name = "Chris", Score = 24 },
                new Record { Name = "Prakash", Score = 9 },
                new Record { Name = "Tim", Score = 25 }
            };

            Assert.Equal(4, source.GroupBy(r => r.Name, e=> e, (r, e) => e).Count());
        }

        [Fact]
        public void EmptyGroupingToArray()
        {
            Assert.Empty(Enumerable.Empty<int>().GroupBy(i => i).ToArray());
        }

        [Fact]
        public void EmptyGroupingToList()
        {
            Assert.Empty(Enumerable.Empty<int>().GroupBy(i => i).ToList());
        }

        [Fact]
        public void EmptyGroupingCount()
        {
            Assert.Equal(0, Enumerable.Empty<int>().GroupBy(i => i).Count());
        }

        [Fact]
        public void EmptyGroupingWithResultToArray()
        {
            Assert.Empty(Enumerable.Empty<int>().GroupBy(i => i, (x, y) => x + y.Count()).ToArray());
        }

        [Fact]
        public void EmptyGroupingWithResultToList()
        {
            Assert.Empty(Enumerable.Empty<int>().GroupBy(i => i, (x, y) => x + y.Count()).ToList());
        }

        [Fact]
        public void EmptyGroupingWithResultCount()
        {
            Assert.Equal(0, Enumerable.Empty<int>().GroupBy(i => i, (x, y) => x + y.Count()).Count());
        }

        [Fact]
        public static void GroupingKeyIsPublic()
        {
            // Grouping.Key needs to be public (not explicitly implemented) for the sake of WPF.

            object[] objs = { "Foo", 1.0M, "Bar", new { X = "X" }, 2.00M };
            object group = objs.GroupBy(x => x.GetType()).First();

            Type grouptype = group.GetType();
            PropertyInfo key = grouptype.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(key);
        }
    }
}
