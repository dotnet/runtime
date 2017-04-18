// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ToLookupTests
    {
        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ILookup_MembersBehaveCorrectly(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int NonExistentKey = count * 2;
            ILookup<int, int> lookup = labeled.Item.ToLookup(x => x);

            // Count
            Assert.Equal(count, lookup.Count);

            // Contains
            Assert.All(lookup, group => lookup.Contains(group.Key));
            Assert.False(lookup.Contains(NonExistentKey));

            // Indexer
            Assert.All(lookup, group => Assert.Equal(group, lookup[group.Key]));
            Assert.Equal(Enumerable.Empty<int>(), lookup[NonExistentKey]);

            // GetEnumerator
            IEnumerator e1 = ((IEnumerable)lookup).GetEnumerator();
            IEnumerator<IGrouping<int, int>> e2 = lookup.GetEnumerator();
            while (e1.MoveNext())
            {
                e2.MoveNext();
                Assert.Equal(((IGrouping<int, int>)e1.Current).Key, e2.Current.Key);
            }
            Assert.False(e2.MoveNext());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x * 2);
            Assert.All(lookup,
                group => { seen.Add(group.Key / 2); Assert.Equal(group.Key, Assert.Single(group) * 2); });
            seen.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_Longrunning()
        {
            ToLookup(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_ElementSelector(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x, y => y * 2);
            Assert.All(lookup,
                group => { seen.Add(group.Key); Assert.Equal(group.Key * 2, Assert.Single(group)); });
            seen.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_ElementSelector_Longrunning()
        {
            ToLookup_ElementSelector(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_CustomComparator(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x * 2, new ModularCongruenceComparer(count * 2));
            Assert.All(lookup,
                group => { seen.Add(group.Key / 2); Assert.Equal(group.Key, Assert.Single(group) * 2); });
            seen.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_CustomComparator_Longrunning()
        {
            ToLookup_CustomComparator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_ElementSelector_CustomComparator(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x, y => y * 2, new ModularCongruenceComparer(count));
            Assert.All(lookup,
                group => { seen.Add(group.Key); Assert.Equal(group.Key * 2, Assert.Single(group)); });
            seen.AssertComplete();
            if (count < 1)
            {
                Assert.Empty(lookup[-1]);
            }
        }

        [Theory]
        [OuterLoop]
        public static void ToLookup_ElementSelector_CustomComparator_Longrunning()
        {
            ToLookup_ElementSelector_CustomComparator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_DuplicateKeys(int count)
        {
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, Math.Min(count, 2));
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x % 2);
            Assert.All(lookup,
                group =>
                {
                    seenOuter.Add(group.Key);
                    IntegerRangeSet seenInner = new IntegerRangeSet(0, (count + ((1 + group.Key) % 2)) / 2);
                    Assert.All(group, y => { Assert.Equal(group.Key, y % 2); seenInner.Add(y / 2); });
                    seenInner.AssertComplete();
                });
            seenOuter.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_DuplicateKeys_Longrunning()
        {
            ToLookup_DuplicateKeys(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_DuplicateKeys_ElementSelector(int count)
        {
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, Math.Min(count, 2));
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x % 2, y => -y);
            Assert.All(lookup,
                group =>
                {
                    seenOuter.Add(group.Key);
                    IntegerRangeSet seenInner = new IntegerRangeSet(0, (count + ((1 + group.Key) % 2)) / 2);
                    Assert.All(group, y => { Assert.Equal(group.Key, -y % 2); seenInner.Add(-y / 2); });
                    seenInner.AssertComplete();
                });
            seenOuter.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_DuplicateKeys_ElementSelector_Longrunning()
        {
            ToLookup_DuplicateKeys_ElementSelector(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_DuplicateKeys_CustomComparator(int count)
        {
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, Math.Min(count, 2));
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x, new ModularCongruenceComparer(2));
            Assert.All(lookup,
                group =>
                {
                    seenOuter.Add(group.Key % 2);
                    IntegerRangeSet seenInner = new IntegerRangeSet(0, (count + ((1 + group.Key) % 2)) / 2);
                    Assert.All(group, y => { Assert.Equal(group.Key % 2, y % 2); seenInner.Add(y / 2); });
                    seenInner.AssertComplete();
                });
            seenOuter.AssertComplete();
            if (count < 2)
            {
                Assert.Empty(lookup[-1]);
            }
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_DuplicateKeys_CustomComparator_Longrunning()
        {
            ToLookup_DuplicateKeys_CustomComparator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToLookup_DuplicateKeys_ElementSelector_CustomComparator(int count)
        {
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, Math.Min(count, 2));
            ILookup<int, int> lookup = UnorderedSources.Default(count).ToLookup(x => x, y => -y, new ModularCongruenceComparer(2));
            Assert.All(lookup,
                group =>
                {
                    seenOuter.Add(group.Key % 2);
                    IntegerRangeSet seenInner = new IntegerRangeSet(0, (count + ((1 + group.Key) % 2)) / 2);
                    Assert.All(group, y => { Assert.Equal(group.Key % 2, -y % 2); seenInner.Add(-y / 2); });
                    seenInner.AssertComplete();
                });
            seenOuter.AssertComplete();
            if (count < 2)
            {
                Assert.Empty(lookup[-1]);
            }
        }

        [Fact]
        [OuterLoop]
        public static void ToLookup_DuplicateKeys_ElementSelector_CustomComparator_Longrunning()
        {
            ToLookup_DuplicateKeys_ElementSelector_CustomComparator(Sources.OuterLoopCount);
        }

        [Fact]
        public static void ToDictionary_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.ToLookup(x => x, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.EventuallyCanceled((source, canceler) => source.ToLookup(x => x, y => y, new CancelingEqualityComparer<int>(canceler)));
        }

        [Fact]
        public static void ToLookup_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.ToLookup(x => x, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.ToLookup(x => x, y => y, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.ToLookup(x => x, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.ToLookup(x => x, y => y, new CancelingEqualityComparer<int>(canceler)));
        }

        [Fact]
        public static void ToLookup_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.ToLookup(x => x));
            AssertThrows.AlreadyCanceled(source => source.ToLookup(x => x, EqualityComparer<int>.Default));

            AssertThrows.AlreadyCanceled(source => source.ToLookup(x => x, y => y));
            AssertThrows.AlreadyCanceled(source => source.ToLookup(x => x, y => y, EqualityComparer<int>.Default));
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1 }, MemberType = typeof(UnorderedSources))]
        public static void ToLookup_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup((Func<int, int>)(x => { throw new DeliberateTestException(); }), y => y));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup(x => x, (Func<int, int>)(y => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup((Func<int, int>)(x => { throw new DeliberateTestException(); }), EqualityComparer<int>.Default));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup((Func<int, int>)(x => { throw new DeliberateTestException(); }), y => y, EqualityComparer<int>.Default));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup(x => x, (Func<int, int>)(y => { throw new DeliberateTestException(); }), EqualityComparer<int>.Default));

            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup(x => x, new FailingEqualityComparer<int>()));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToLookup(x => x, y => y, new FailingEqualityComparer<int>()));
        }

        [Fact]
        public static void ToLookup_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToLookup(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToLookup(x => x, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToLookup(x => x, y => y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToLookup(x => x, y => y, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToLookup((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToLookup((Func<int, int>)null, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToLookup((Func<int, int>)null, y => y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToLookup((Func<int, int>)null, y => y, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Empty<int>().ToLookup(x => x, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Empty<int>().ToLookup(x => x, (Func<int, int>)null, EqualityComparer<int>.Default));
        }
    }
}
