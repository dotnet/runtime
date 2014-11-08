// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;
using Validation;
using System.Diagnostics;

namespace System.Collections.Immutable.Test
{
    public abstract class ImmutableListTestBase : SimpleElementImmutablesTestBase
    {
        internal abstract IImmutableListQueries<T> GetListQuery<T>(ImmutableList<T> list);

        [Fact]
        public void CopyToEmptyTest()
        {
            var array = new int[0];
            this.GetListQuery(ImmutableList<int>.Empty).CopyTo(array);
            this.GetListQuery(ImmutableList<int>.Empty).CopyTo(array, 0);
            this.GetListQuery(ImmutableList<int>.Empty).CopyTo(0, array, 0, 0);
            ((ICollection)this.GetListQuery(ImmutableList<int>.Empty)).CopyTo(array, 0);
        }

        [Fact]
        public void CopyToTest()
        {
            var listQuery = this.GetListQuery(ImmutableList.Create(1, 2));
            var list = (IEnumerable<int>)listQuery;

            var array = new int[2];
            listQuery.CopyTo(array);
            Assert.Equal(list, array);

            array = new int[2];
            listQuery.CopyTo(array, 0);
            Assert.Equal(list, array);

            array = new int[2];
            listQuery.CopyTo(0, array, 0, listQuery.Count);
            Assert.Equal(list, array);

            array = new int[1]; // shorter than source length
            listQuery.CopyTo(0, array, 0, array.Length);
            Assert.Equal(list.Take(array.Length), array);

            array = new int[3];
            listQuery.CopyTo(1, array, 2, 1);
            Assert.Equal(new[] { 0, 0, 2 }, array);

            array = new int[2];
            ((ICollection)listQuery).CopyTo(array, 0);
            Assert.Equal(list, array);
        }

        [Fact]
        public void ForEachTest()
        {
            this.GetListQuery(ImmutableList<int>.Empty).ForEach(n => Assert.True(false, "Empty list should not invoke this."));

            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(5, 3));
            var hitTest = new bool[list.Max() + 1];
            this.GetListQuery(list).ForEach(i =>
            {
                Assert.False(hitTest[i]);
                hitTest[i] = true;
            });

            for (int i = 0; i < hitTest.Length; i++)
            {
                Assert.Equal(list.Contains(i), hitTest[i]);
                Assert.Equal(((IList)list).Contains(i), hitTest[i]);
            }
        }

        [Fact]
        public void ExistsTest()
        {
            Assert.False(this.GetListQuery(ImmutableList<int>.Empty).Exists(n => true));

            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(1, 5));
            Assert.True(this.GetListQuery(list).Exists(n => n == 3));
            Assert.False(this.GetListQuery(list).Exists(n => n == 8));
        }

        [Fact]
        public void FindAllTest()
        {
            Assert.True(this.GetListQuery(ImmutableList<int>.Empty).FindAll(n => true).IsEmpty);
            var list = ImmutableList<int>.Empty.AddRange(new[] { 2, 3, 4, 5, 6 });
            var actual = this.GetListQuery(list).FindAll(n => n % 2 == 1);
            var expected = list.ToList().FindAll(n => n % 2 == 1);
            Assert.Equal<int>(expected, actual.ToList());
        }

        [Fact]
        public void FindTest()
        {
            Assert.Equal(0, this.GetListQuery(ImmutableList<int>.Empty).Find(n => true));
            var list = ImmutableList<int>.Empty.AddRange(new[] { 2, 3, 4, 5, 6 });
            Assert.Equal(3, this.GetListQuery(list).Find(n => (n % 2) == 1));
        }

        [Fact]
        public void FindLastTest()
        {
            Assert.Equal(0, this.GetListQuery(ImmutableList<int>.Empty).FindLast(n =>
            {
                Assert.True(false, "Predicate should not have been invoked.");
                return true;
            }));
            var list = ImmutableList<int>.Empty.AddRange(new[] { 2, 3, 4, 5, 6 });
            Assert.Equal(5, this.GetListQuery(list).FindLast(n => (n % 2) == 1));
        }

        [Fact]
        public void FindIndexTest()
        {
            Assert.Equal(-1, this.GetListQuery(ImmutableList<int>.Empty).FindIndex(n => true));
            Assert.Equal(-1, this.GetListQuery(ImmutableList<int>.Empty).FindIndex(0, n => true));
            Assert.Equal(-1, this.GetListQuery(ImmutableList<int>.Empty).FindIndex(0, 0, n => true));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            var bclList = list.ToList();
            Assert.Equal(-1, this.GetListQuery(list).FindIndex(n => n == 6));

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= list.Count - idx; count++)
                {
                    foreach (int c in list)
                    {
                        int predicateInvocationCount = 0;
                        Predicate<int> match = n =>
                        {
                            predicateInvocationCount++;
                            return n == c;
                        };
                        int expected = bclList.FindIndex(idx, count, match);
                        int expectedInvocationCount = predicateInvocationCount;
                        predicateInvocationCount = 0;
                        int actual = this.GetListQuery(list).FindIndex(idx, count, match);
                        int actualInvocationCount = predicateInvocationCount;
                        Assert.Equal(expected, actual);
                        Assert.Equal(expectedInvocationCount, actualInvocationCount);

                        if (count == list.Count)
                        {
                            // Also test the FindIndex overload that takes no count parameter.
                            predicateInvocationCount = 0;
                            actual = this.GetListQuery(list).FindIndex(idx, match);
                            Assert.Equal(expected, actual);
                            Assert.Equal(expectedInvocationCount, actualInvocationCount);

                            if (idx == 0)
                            {
                                // Also test the FindIndex overload that takes no index parameter.
                                predicateInvocationCount = 0;
                                actual = this.GetListQuery(list).FindIndex(match);
                                Assert.Equal(expected, actual);
                                Assert.Equal(expectedInvocationCount, actualInvocationCount);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void FindLastIndexTest()
        {
            Assert.Equal(-1, this.GetListQuery(ImmutableList<int>.Empty).FindLastIndex(n => true));
            Assert.Equal(-1, this.GetListQuery(ImmutableList<int>.Empty).FindLastIndex(0, n => true));
            Assert.Equal(-1, this.GetListQuery(ImmutableList<int>.Empty).FindLastIndex(0, 0, n => true));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            var bclList = list.ToList();
            Assert.Equal(-1, this.GetListQuery(list).FindLastIndex(n => n == 6));

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= idx + 1; count++)
                {
                    foreach (int c in list)
                    {
                        int predicateInvocationCount = 0;
                        Predicate<int> match = n =>
                        {
                            predicateInvocationCount++;
                            return n == c;
                        };
                        int expected = bclList.FindLastIndex(idx, count, match);
                        int expectedInvocationCount = predicateInvocationCount;
                        predicateInvocationCount = 0;
                        int actual = this.GetListQuery(list).FindLastIndex(idx, count, match);
                        int actualInvocationCount = predicateInvocationCount;
                        Assert.Equal(expected, actual);
                        Assert.Equal(expectedInvocationCount, actualInvocationCount);

                        if (count == list.Count)
                        {
                            // Also test the FindIndex overload that takes no count parameter.
                            predicateInvocationCount = 0;
                            actual = this.GetListQuery(list).FindLastIndex(idx, match);
                            Assert.Equal(expected, actual);
                            Assert.Equal(expectedInvocationCount, actualInvocationCount);

                            if (idx == list.Count - 1)
                            {
                                // Also test the FindIndex overload that takes no index parameter.
                                predicateInvocationCount = 0;
                                actual = this.GetListQuery(list).FindLastIndex(match);
                                Assert.Equal(expected, actual);
                                Assert.Equal(expectedInvocationCount, actualInvocationCount);
                            }
                        }
                    }
                }
            }
        }

#if PORTABLE
        public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
#endif

        [Fact]
        public void ConvertAllTest()
        {
            Assert.True(this.GetListQuery(ImmutableList<int>.Empty).ConvertAll<float>(n => n).IsEmpty);
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            Converter<int, double> converter = n => 2.0 * n;
            Func<int, double> funcConverter = n => converter(n);
#if PORTABLE
            var expected = list.ToList().Select(funcConverter).ToList();
#else
            var expected = list.ToList().ConvertAll(converter);
#endif
            var actual = this.GetListQuery(list).ConvertAll(funcConverter);
            Assert.Equal<double>(expected.ToList(), actual.ToList());
        }

        [Fact]
        public void GetRangeTest()
        {
            Assert.True(this.GetListQuery(ImmutableList<int>.Empty).GetRange(0, 0).IsEmpty);
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            var bclList = list.ToList();

            for (int index = 0; index < list.Count; index++)
            {
                for (int count = 0; count < list.Count - index; count++)
                {
                    var expected = bclList.GetRange(index, count);
                    var actual = this.GetListQuery(list).GetRange(index, count);
                    Assert.Equal<int>(expected.ToList(), actual.ToList());
                }
            }
        }

        [Fact]
        public void TrueForAllTest()
        {
            Assert.True(this.GetListQuery(ImmutableList<int>.Empty).TrueForAll(n => false));
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            this.TrueForAllTestHelper(list, n => n % 2 == 0);
            this.TrueForAllTestHelper(list, n => n % 2 == 1);
            this.TrueForAllTestHelper(list, n => true);
        }

        [Fact]
        public void RemoveAllTest()
        {
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            this.RemoveAllTestHelper(list, n => false);
            this.RemoveAllTestHelper(list, n => true);
            this.RemoveAllTestHelper(list, n => n < 7);
            this.RemoveAllTestHelper(list, n => n > 7);
            this.RemoveAllTestHelper(list, n => n == 7);
        }

        [Fact]
        public void ReverseTest()
        {
            var list = ImmutableList<int>.Empty.AddRange(Enumerable.Range(5, 10));

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list.Count - i; j++)
                {
                    this.ReverseTestHelper(list, i, j);
                }
            }
        }

        [Fact]
        public void SortTest()
        {
            var scenarios = new[] {
                ImmutableList<int>.Empty,
                ImmutableList<int>.Empty.AddRange(Enumerable.Range(1, 50)),
                ImmutableList<int>.Empty.AddRange(Enumerable.Range(1, 50).Reverse()),
            };

            foreach (var scenario in scenarios)
            {
                var expected = scenario.ToList();
                expected.Sort();
                var actual = this.SortTestHelper(scenario);
                Assert.Equal<int>(expected, actual);

                expected = scenario.ToList();
                Comparison<int> comparison = (x, y) => x > y ? 1 : (x < y ? -1 : 0);
                expected.Sort(comparison);
                actual = this.SortTestHelper(scenario, comparison);
                Assert.Equal<int>(expected, actual);

                expected = scenario.ToList();
                IComparer<int> comparer = Comparer<int>.Default;
                expected.Sort(comparer);
                actual = this.SortTestHelper(scenario, comparer);
                Assert.Equal<int>(expected, actual);

                for (int i = 0; i < scenario.Count; i++)
                {
                    for (int j = 0; j < scenario.Count - i; j++)
                    {
                        expected = scenario.ToList();
                        comparer = Comparer<int>.Default;
                        expected.Sort(i, j, comparer);
                        actual = this.SortTestHelper(scenario, i, j, comparer);
                        Assert.Equal<int>(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void BinarySearch()
        {
            var basis = new List<int>(Enumerable.Range(1, 50).Select(n => n * 2));
            var query = this.GetListQuery(basis.ToImmutableList());
            for (int value = basis.First() - 1; value <= basis.Last() + 1; value++)
            {
                int expected = basis.BinarySearch(value);
                int actual = query.BinarySearch(value);
                if (expected != actual) Debugger.Break();
                Assert.Equal(expected, actual);

                for (int index = 0; index < basis.Count - 1; index++)
                {
                    for (int count = 0; count <= basis.Count - index; count++)
                    {
                        expected = basis.BinarySearch(index, count, value, null);
                        actual = query.BinarySearch(index, count, value, null);
                        if (expected != actual) Debugger.Break();
                        Assert.Equal(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void BinarySearchPartialSortedList()
        {
            var reverseSorted = ImmutableArray.CreateRange(Enumerable.Range(1, 150).Select(n => n * 2).Reverse());
            this.BinarySearchPartialSortedListHelper(reverseSorted, 0, 50);
            this.BinarySearchPartialSortedListHelper(reverseSorted, 50, 50);
            this.BinarySearchPartialSortedListHelper(reverseSorted, 100, 50);
        }

        private void BinarySearchPartialSortedListHelper(ImmutableArray<int> inputData, int sortedIndex, int sortedLength)
        {
            Requires.Range(sortedIndex >= 0, "sortedIndex");
            Requires.Range(sortedLength > 0, "sortedLength");
            inputData = inputData.Sort(sortedIndex, sortedLength, Comparer<int>.Default);
            int min = inputData[sortedIndex];
            int max = inputData[sortedIndex + sortedLength - 1];

            var basis = new List<int>(inputData);
            var query = this.GetListQuery(inputData.ToImmutableList());
            for (int value = min - 1; value <= max + 1; value++)
            {
                for (int index = sortedIndex; index < sortedIndex + sortedLength; index++) // make sure the index we pass in is always within the sorted range in the list.
                {
                    for (int count = 0; count <= sortedLength - index; count++)
                    {
                        int expected = basis.BinarySearch(index, count, value, null);
                        int actual = query.BinarySearch(index, count, value, null);
                        if (expected != actual) Debugger.Break();
                        Assert.Equal(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void SyncRoot()
        {
            var collection = (ICollection)this.GetEnumerableOf<int>();
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Fact]
        public void GetEnumeratorTest()
        {
            var enumerable = this.GetEnumerableOf(1);
            Assert.Equal(new[] { 1 }, enumerable.ToList()); // exercises the enumerator

            IEnumerable enumerableNonGeneric = enumerable;
            Assert.Equal(new[] { 1 }, enumerableNonGeneric.Cast<int>().ToList()); // exercises the enumerator
        }

        protected abstract void RemoveAllTestHelper<T>(ImmutableList<T> list, Predicate<T> test);

        protected abstract void ReverseTestHelper<T>(ImmutableList<T> list, int index, int count);

        protected abstract List<T> SortTestHelper<T>(ImmutableList<T> list);

        protected abstract List<T> SortTestHelper<T>(ImmutableList<T> list, Comparison<T> comparison);

        protected abstract List<T> SortTestHelper<T>(ImmutableList<T> list, IComparer<T> comparer);

        protected abstract List<T> SortTestHelper<T>(ImmutableList<T> list, int index, int count, IComparer<T> comparer);

        private void TrueForAllTestHelper<T>(ImmutableList<T> list, Predicate<T> test)
        {
            var bclList = list.ToList();
            var expected = bclList.TrueForAll(test);
            var actual = this.GetListQuery(list).TrueForAll(test);
            Assert.Equal(expected, actual);
        }
    }
}
