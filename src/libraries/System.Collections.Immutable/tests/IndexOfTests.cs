// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Immutable.Test
{
    public static class IndexOfTests
    {
        public static void IndexOfTest<TCollection>(
            Func<IEnumerable<int>, TCollection> factory,
            Func<TCollection, int, int> indexOfItem,
            Func<TCollection, int, int, int> indexOfItemIndex,
            Func<TCollection, int, int, int, int> indexOfItemIndexCount,
            Func<TCollection, int, int, int, IEqualityComparer<int>, int> indexOfItemIndexCountEQ)
        {
            var emptyCollection = factory(new int[0]);
            var collection1256 = factory(new[] { 1, 2, 5, 6 });

            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(collection1256, 100, 1, 20, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(collection1256, 100, 1, -1, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, new CustomComparer(50)));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, new CustomComparer(50)));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(collection1256, 100, 1, 20, new CustomComparer(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => indexOfItemIndexCountEQ(collection1256, 100, 1, -1, new CustomComparer(1)));

            Assert.Equal(-1, indexOfItem(emptyCollection, 5));
            Assert.Equal(-1, indexOfItemIndex(emptyCollection, 5, 0));
            Assert.Equal(2, indexOfItemIndex(collection1256, 5, 1));
            Assert.Equal(-1, indexOfItemIndexCount(emptyCollection, 5, 0, 0));
            Assert.Equal(-1, indexOfItemIndexCount(collection1256, 5, 1, 1));
            Assert.Equal(2, indexOfItemIndexCount(collection1256, 5, 1, 2));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            var bclList = list.ToList();
            Assert.Equal(-1, indexOfItem(factory(list), 6));

            if (factory(list) is IList)
            {
                Assert.Equal(-1, ((IList)factory(list)).IndexOf(6));
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
            Func<TCollection, int, int, int> lastIndexOfItemIndex,
            Func<TCollection, int, int, int, int> lastIndexOfItemIndexCount,
            Func<TCollection, int, int, int, IEqualityComparer<int>, int> lastIndexOfItemIndexCountEQ)
        {
            var emptyCollection = factory(new int[0]);
            var collection1256 = factory(new[] { 1, 2, 5, 6 });

            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, 20, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, -1, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(emptyCollection, 100, 1, 1, new CustomComparer(50)));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(emptyCollection, 100, -1, 1, new CustomComparer(50)));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, 20, new CustomComparer(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => lastIndexOfItemIndexCountEQ(collection1256, 100, 1, -1, new CustomComparer(1)));

            Assert.Equal(-1, lastIndexOfItem(emptyCollection, 5));
            Assert.Equal(-1, lastIndexOfItemIndex(emptyCollection, 5, 0));
            Assert.Equal(-1, lastIndexOfItemIndexCount(emptyCollection, 5, 0, 0));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            var bclList = list.ToList();
            Assert.Equal(-1, lastIndexOfItem(factory(list), 6));

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= idx + 1; count++)
                {
                    foreach (int match in list.Concat(new[] { 88 }))
                    {
                        int expected = bclList.LastIndexOf(match, idx, count);
                        int actual = lastIndexOfItemIndexCount(factory(list), match, idx, count);
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
            private readonly int matchOnXIteration;
            private int iteration;

            public CustomComparer(int matchOnXIteration)
            {
                this.matchOnXIteration = matchOnXIteration;
            }

            public bool Equals(int x, int y)
            {
                return ++iteration == this.matchOnXIteration;
            }

            public int GetHashCode(int obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
