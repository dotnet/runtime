// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class SelectManyTests : EnumerableTests
    {
        [Fact]
        public void EmptySource()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany(e => e.total));
        }

        [Fact]
        public void EmptySourceIndexedSelector()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany((e, i) => e.total));
        }

        [Fact]
        public void EmptySourceResultSelector()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void EmptySourceResultSelectorIndexedSelector()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void SingleElement()
        {
            int?[] expected = { 90, 55, null, 43, 89 };
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name = "Prakash", total = expected }
            };
            Assert.Equal(expected, source.SelectMany(e => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmpty()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany(e => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexedSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexedSelectorWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void ResultsSelected()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.SelectMany(e => e.total));
        }

        [Fact]
        public void RunOnce()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.RunOnce().SelectMany(e => e.total.RunOnce()));
        }

        [Fact]
        public void SourceEmptyIndexUsed()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany((e, index) => e.total));
        }

        [Fact]
        public void SingleElementIndexUsed()
        {
            int?[] expected = { 90, 55, null, 43, 89 };
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name = "Prakash", total = expected }
            };
            Assert.Equal(expected, source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexUsed()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total= new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };
            Assert.Empty(source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void ResultsSelectedIndexUsed()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void IndexCausingFirstToBeSelected()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };

            Assert.Equal(source.First().total, source.SelectMany((e, i) => i == 0 ? e.total : Enumerable.Empty<int?>()));
        }

        [Fact]
        public void IndexCausingLastToBeSelected()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Robert", total=new int?[]{-10, 100} }
            };

            Assert.Equal(source.Last().total, source.SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>()));
        }

        [Fact(Skip = "Valid test but too intensive to enable even in OuterLoop")]
        public void IndexOverflow()
        {
            var selected = new FastInfiniteEnumerator<int>().SelectMany((e, i) => Enumerable.Empty<int>());
            using (var en = selected.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while(en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void ResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };

            Assert.Equal(expected, source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullResultSelector()
        {
            Func<StringWithIntArray, int?, string> resultSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany(e => e.total, resultSelector));
        }

        [Fact]
        public void NullResultSelectorIndexedSelector()
        {
            Func<StringWithIntArray, int?, string> resultSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany((e, i) => e.total, resultSelector));
        }

        [Fact]
        public void NullSourceWithResultSelector()
        {
            StringWithIntArray[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullCollectionSelector()
        {
            Func<StringWithIntArray, IEnumerable<int?>> collectionSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullIndexedCollectionSelector()
        {
            Func<StringWithIntArray, int, IEnumerable<int?>> collectionSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSource()
        {
            StringWithIntArray[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelector()
        {
            StringWithIntArray[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelectorWithResultSelector()
        {
            StringWithIntArray[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSelector()
        {
            Func<StringWithIntArray, int[]> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => new StringWithIntArray[0].SelectMany(selector));
        }

        [Fact]
        public void NullIndexedSelector()
        {
            Func<StringWithIntArray, int, int[]> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => new StringWithIntArray[0].SelectMany(selector));
        }

        [Fact]
        public void IndexCausingFirstToBeSelectedWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            string[] expected = { "1", "2", "3", "4" };
            Assert.Equal(expected, source.SelectMany((e, i) => i == 0 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
        }

        [Fact]
        public void IndexCausingLastToBeSelectedWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Robert", total=new int?[]{-10, 100} }
            };

            string[] expected = { "-10", "100" };
            Assert.Equal(expected, source.SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany(i => new int[0]);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexed()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany((e, i) => new int[0]);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateResultSel()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany(i => new int[0], (e, i) => e);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexedResultSel()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany((e, i) => new int[0], (e, i) => e);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Theory]
        [MemberData(nameof(ParameterizedTestsData))]
        public void ParameterizedTests(IEnumerable<int> source, Func<int, IEnumerable<int>> selector)
        {
            var expected = source.Select(i => selector(i)).Aggregate((l, r) => l.Concat(r));
            var actual = source.SelectMany(selector);

            Assert.Equal(expected, actual);
            Assert.Equal(expected.Count(), actual.Count()); // SelectMany may employ an optimized Count implementation.
            Assert.Equal(expected.ToArray(), actual.ToArray());
            Assert.Equal(expected.ToList(), actual.ToList());
        }

        public static IEnumerable<object[]> ParameterizedTestsData()
        {
            for (int i = 1; i <= 20; i++)
            {
                Func<int, IEnumerable<int>> selector = n => Enumerable.Range(i, n);
                yield return new object[] { Enumerable.Range(1, i), selector };
            }
        }

        [Theory]
        [MemberData(nameof(DisposeAfterEnumerationData))]
        public void DisposeAfterEnumeration(int sourceLength, int subLength)
        {
            int sourceState = 0;
            int subIndex = 0; // Index within the arrays the sub-collection is supposed to be at.
            int[] subState = new int[sourceLength];

            bool sourceDisposed = false;
            bool[] subCollectionDisposed = new bool[sourceLength];

            var source = new DelegateIterator<int>(
                moveNext: () => ++sourceState <= sourceLength,
                current: () => 0,
                dispose: () => sourceDisposed = true);

            var subCollection = new DelegateIterator<int>(
                moveNext: () => ++subState[subIndex] <= subLength, // Return true `subLength` times.
                current: () => subState[subIndex],
                dispose: () => subCollectionDisposed[subIndex++] = true); // Record that Dispose was called, and move on to the next index.

            var iterator = source.SelectMany(_ => subCollection);

            int index = 0; // How much have we gone into the iterator?
            IEnumerator<int> e = iterator.GetEnumerator();

            using (e)
            {
                while (e.MoveNext())
                {
                    int item = e.Current;

                    Assert.Equal(subState[subIndex], item); // Verify Current.
                    Assert.Equal(index / subLength, subIndex);

                    Assert.False(sourceDisposed); // Not yet.

                    // This represents whehter the sub-collection we're iterating thru right now
                    // has been disposed. Also not yet.
                    Assert.False(subCollectionDisposed[subIndex]);

                    // However, all of the sub-collections before us should have been disposed.
                    // Their indices should also be maxed out.
                    Assert.All(subState.Take(subIndex), s => Assert.Equal(subLength + 1, s));
                    Assert.All(subCollectionDisposed.Take(subIndex), t => Assert.True(t));

                    index++;
                }
            }

            Assert.True(sourceDisposed);
            Assert.Equal(sourceLength, subIndex);
            Assert.All(subState, s => Assert.Equal(subLength + 1, s));
            Assert.All(subCollectionDisposed, t => Assert.True(t));

            // .NET Core fixes an oversight where we wouldn't properly dispose
            // the SelectMany iterator. See https://github.com/dotnet/corefx/pull/13942.
            int expectedCurrent = 0;
            Assert.Equal(expectedCurrent, e.Current);
            Assert.False(e.MoveNext());
            Assert.Equal(expectedCurrent, e.Current);
        }

        public static IEnumerable<object[]> DisposeAfterEnumerationData()
        {
            int[] lengths = { 1, 2, 3, 5, 8, 13, 21, 34 };

            return lengths.SelectMany(l => lengths, (l1, l2) => new object[] { l1, l2 });
        }

        [Theory]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp, ".NET Core optimizes SelectMany and throws an OverflowException. On the full .NET Framework this takes a long time. See https://github.com/dotnet/corefx/pull/13942.")]
        [InlineData(new[] { int.MaxValue, 1 })]
        [InlineData(new[] { 2, int.MaxValue - 1 })]
        [InlineData(new[] { 123, 456, int.MaxValue - 100000, 123456 })]
        public void ThrowOverflowExceptionOnConstituentLargeCounts(int[] counts)
        {
            IEnumerable<int> iterator = counts.SelectMany(c => Enumerable.Range(1, c));
            Assert.Throws<OverflowException>(() => iterator.Count());
        }

        [Theory]
        [MemberData(nameof(GetToArrayDataSources))]
        public void CollectionInterleavedWithLazyEnumerables_ToArray(IEnumerable<int>[] arrays)
        {
            // See https://github.com/dotnet/corefx/issues/23680

            int[] results = arrays.SelectMany(ar => ar).ToArray();

            for (int i = 0; i < results.Length; i++)
            {
                Assert.Equal(i, results[i]);
            }
        }

        public static IEnumerable<object[]> GetToArrayDataSources()
        {
            // Marker at the end
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(new int[] { 0 }),
                    new TestEnumerable<int>(new int[] { 1 }),
                    new TestEnumerable<int>(new int[] { 2 }),
                    new int[] { 3 },
                }
            };

            // Marker at beginning
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new int[] { 0 },
                    new TestEnumerable<int>(new int[] { 1 }),
                    new TestEnumerable<int>(new int[] { 2 }),
                    new TestEnumerable<int>(new int[] { 3 }),
                }
            };

            // Marker in middle
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(new int[] { 0 }),
                    new int[] { 1 },
                    new TestEnumerable<int>(new int[] { 2 }),
                }
            };

            // Non-marker in middle
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new int[] { 0 },
                    new TestEnumerable<int>(new int[] { 1 }),
                    new int[] { 2 },
                }
            };

            // Big arrays (marker in middle)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(Enumerable.Range(0, 100).ToArray()),
                    Enumerable.Range(100, 100).ToArray(),
                    new TestEnumerable<int>(Enumerable.Range(200, 100).ToArray()),
                }
            };

            // Big arrays (non-marker in middle)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    Enumerable.Range(0, 100).ToArray(),
                    new TestEnumerable<int>(Enumerable.Range(100, 100).ToArray()),
                    Enumerable.Range(200, 100).ToArray(),
                }
            };

            // Interleaved (first marker)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new int[] { 0 },
                    new TestEnumerable<int>(new int[] { 1 }),
                    new int[] { 2 },
                    new TestEnumerable<int>(new int[] { 3 }),
                    new int[] { 4 },
                }
            };

            // Interleaved (first non-marker)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(new int[] { 0 }),
                    new int[] { 1 },
                    new TestEnumerable<int>(new int[] { 2 }),
                    new int[] { 3 },
                    new TestEnumerable<int>(new int[] { 4 }),
                }
            };
        }
    }
}
