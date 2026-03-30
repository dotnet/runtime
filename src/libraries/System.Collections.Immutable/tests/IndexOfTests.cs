// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public static class IndexOfTests
    {
        public static void IndexOfTest<TCollection>(
            Func<IEnumerable<int>, TCollection> factory,
            Func<TCollection, int, int> indexOfItem,
            Func<TCollection, int, int, int> indexOfItemIndex,
            Func<TCollection, int, int, int, int> indexOfItemIndexCount,
            Func<TCollection, int, int, int, IEqualityComparer<int>, int> indexOfItemIndexCountEQ,
            string indexParameterName)
        {
            TCollection emptyCollection = factory(new int[0]);
            TCollection singleCollection = factory(new[] { 10 });
            TCollection collection1256 = factory(new[] { 1, 2, 5, 6 });

            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(singleCollection, 100, 1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndexCountEQ(singleCollection, 100, -1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(collection1256, 100, 1, 20, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(collection1256, 100, 1, -1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(singleCollection, 100, 1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndexCountEQ(singleCollection, 100, -1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(collection1256, 100, 1, 20, new CustomComparer(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(collection1256, 100, 1, -1, new CustomComparer(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(indexParameterName, () => indexOfItemIndex(collection1256, 2, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => indexOfItemIndexCountEQ(collection1256, 6, 2, 4, EqualityComparer<int>.Default));

            Assert.Equal(-1, indexOfItem(emptyCollection, 5));
            Assert.Equal(-1, indexOfItemIndex(emptyCollection, 5, 0));
            Assert.Equal(-1, indexOfItemIndexCount(emptyCollection, 5, 0, 0));
            Assert.Equal(-1, indexOfItemIndexCountEQ(emptyCollection, 5, 0, 0, EqualityComparer<int>.Default));

            Assert.Equal(0, indexOfItem(singleCollection, 10));
            Assert.Equal(0, indexOfItemIndex(singleCollection, 10, 0));
            Assert.Equal(0, indexOfItemIndexCount(singleCollection, 10, 0, 1));
            Assert.Equal(0, indexOfItemIndexCountEQ(singleCollection, 10, 0, 1, EqualityComparer<int>.Default));
            Assert.Equal(-1, indexOfItemIndexCountEQ(singleCollection, 100, 0, 1, EqualityComparer<int>.Default));
            Assert.Equal(-1, indexOfItemIndexCountEQ(singleCollection, 10, 1, 0, EqualityComparer<int>.Default));

            Assert.Equal(2, indexOfItem(collection1256, 5));
            Assert.Equal(2, indexOfItemIndex(collection1256, 5, 1));
            Assert.Equal(-1, indexOfItemIndexCount(collection1256, 5, 1, 1));
            Assert.Equal(2, indexOfItemIndexCount(collection1256, 5, 1, 2));
            Assert.Equal(2, indexOfItemIndexCountEQ(collection1256, 5, 0, 4, EqualityComparer<int>.Default));
            Assert.Equal(3, indexOfItemIndexCountEQ(collection1256, 6, 0, 4, EqualityComparer<int>.Default));
            Assert.Equal(-1, indexOfItemIndexCountEQ(collection1256, 100, 0, 4, EqualityComparer<int>.Default));
            Assert.Equal(-1, indexOfItemIndexCountEQ(collection1256, 100, 4, 0, EqualityComparer<int>.Default));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            ImmutableList<int> list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            List<int> bclList = list.ToList();
            Assert.Equal(-1, indexOfItem(factory(list), 6));
            Assert.Equal(2, indexOfItemIndexCountEQ(factory(list), 102, 0, 4, null));

            if (factory(list) is IList)
            {
                Assert.Equal(-1, ((IList)factory(list)).IndexOf(6));
                Assert.Equal(-1, ((IList)factory(list)).IndexOf(null));
                Assert.Equal(-1, ((IList)factory(list)).IndexOf("a"));
            }

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= list.Count - idx; count++)
                {
                    foreach (int match in list.Concat(new[] { 88 }))
                    {
                        int expected = bclList.IndexOf(match, idx, count);
                        int actual = indexOfItemIndexCount(factory(list), match, idx, count);
                        Assert.Equal(expected, actual);

                        actual = indexOfItemIndexCountEQ(factory(list), match, idx, count, new CustomComparer(count));
                        Assert.Equal(count > 0 ? idx + count - 1 : -1, actual);

                        if (count == list.Count)
                        {
                            // Also test the IndexOf overload that takes no count parameter.
                            actual = indexOfItemIndex(factory(list), match, idx);
                            Assert.Equal(expected, actual);

                            if (idx == 0)
                            {
                                // Also test the IndexOf overload that takes no index parameter.
                                actual = indexOfItem(factory(list), match);
                                Assert.Equal(expected, actual);
                            }
                        }
                    }
                }
            }
        }

        public static void LastIndexOfTest<TCollection>(
            Func<IEnumerable<int>, TCollection> factory,
            Func<TCollection, int, int> lastIndexOfItem,
            Func<TCollection, int, IEqualityComparer<int>, int> lastIndexOfItemEQ,
            Func<TCollection, int, int, int> lastIndexOfItemIndex,
            Func<TCollection, int, int, int, int> lastIndexOfItemIndexCount,
            Func<TCollection, int, int, int, IEqualityComparer<int>, int> lastIndexOfItemIndexCountEQ)
        {
            TCollection emptyCollection = factory(new int[0]);
            TCollection singleCollection = factory(new[] { 10 });
            TCollection collection1256 = factory(new[] { 1, 2, 5, 6 });

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(singleCollection, 100, 1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(singleCollection, 100, -1, 1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, 20, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, -1, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(singleCollection, 100, 1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(singleCollection, 100, -1, 1, new CustomComparer(50)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, 20, new CustomComparer(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, -1, new CustomComparer(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndex(collection1256, 2, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => lastIndexOfItemIndexCountEQ(collection1256, 6, 2, 4, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => lastIndexOfItemIndexCountEQ(collection1256, 6, 4, 4, EqualityComparer<int>.Default));

            Assert.Equal(-1, lastIndexOfItem(emptyCollection, 5));
            Assert.Equal(-1, lastIndexOfItemEQ(emptyCollection, 5, EqualityComparer<int>.Default));
            Assert.Equal(-1, lastIndexOfItemIndex(emptyCollection, 5, 0));
            Assert.Equal(-1, lastIndexOfItemIndexCount(emptyCollection, 5, 0, 0));

            Assert.Equal(0, lastIndexOfItem(singleCollection, 10));
            Assert.Equal(0, lastIndexOfItemEQ(singleCollection, 10, EqualityComparer<int>.Default));
            Assert.Equal(0, lastIndexOfItemIndex(singleCollection, 10, 0));
            Assert.Equal(0, lastIndexOfItemIndexCount(singleCollection, 10, 0, 1));
            Assert.Equal(0, lastIndexOfItemIndexCountEQ(singleCollection, 10, 0, 1, EqualityComparer<int>.Default));
            Assert.Equal(-1, lastIndexOfItemIndexCountEQ(singleCollection, 100, 0, 1, EqualityComparer<int>.Default));

            Assert.Equal(1, lastIndexOfItem(collection1256, 2));
            Assert.Equal(1, lastIndexOfItemEQ(collection1256, 2, EqualityComparer<int>.Default));
            Assert.Equal(1, lastIndexOfItemIndex(collection1256, 2, 3));
            Assert.Equal(1, lastIndexOfItemIndexCount(collection1256, 2, 3, 4));
            Assert.Equal(2, lastIndexOfItemIndexCountEQ(collection1256, 5, 3, 4, EqualityComparer<int>.Default));
            Assert.Equal(3, lastIndexOfItemIndexCountEQ(collection1256, 6, 3, 4, EqualityComparer<int>.Default));
            Assert.Equal(-1, lastIndexOfItemIndexCountEQ(collection1256, 100, 3, 4, EqualityComparer<int>.Default));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            ImmutableList<int> list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            List<int> bclList = list.ToList();
            Assert.Equal(-1, lastIndexOfItem(factory(list), 6));
            Assert.Equal(2, lastIndexOfItemIndexCountEQ(factory(list), 102, 6, 5, null));

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= idx + 1; count++)
                {
                    foreach (int match in list.Concat(new[] { 88 }))
                    {
                        int expected = bclList.LastIndexOf(match, idx, count);
                        int actual = lastIndexOfItemIndexCount(factory(list), match, idx, count);
                        Assert.Equal(expected, actual);

                        expected = bclList.LastIndexOf(match);
                        actual = lastIndexOfItemEQ(factory(list), match, EqualityComparer<int>.Default);
                        Assert.Equal(expected, actual);

                        actual = lastIndexOfItemIndexCountEQ(factory(list), match, idx, count, new CustomComparer(count));
                        Assert.Equal(count > 0 ? (idx - count + 1) : -1, actual);

                        if (count == list.Count)
                        {
                            // Also test the LastIndexOf overload that takes no count parameter.
                            actual = lastIndexOfItemIndex(factory(list), match, idx);
                            Assert.Equal(expected, actual);

                            if (idx == list.Count - 1)
                            {
                                // Also test the LastIndexOf overload that takes no index parameter.
                                actual = lastIndexOfItem(factory(list), match);
                                Assert.Equal(expected, actual);
                            }
                        }
                    }
                }
            }
        }

        private class CustomComparer : IEqualityComparer<int>
        {
            private readonly int _matchOnXIteration;
            private int _iteration;

            public CustomComparer(int matchOnXIteration)
            {
                _matchOnXIteration = matchOnXIteration;
            }

            public bool Equals(int x, int y)
            {
                return ++_iteration == _matchOnXIteration;
            }

            public int GetHashCode(int obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
