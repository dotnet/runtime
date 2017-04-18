// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ToDictionaryTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToDictionary(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).ToDictionary(x => x * 2),
                p => { seen.Add(p.Key / 2); Assert.Equal(p.Key, p.Value * 2); });
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void ToDictionary_Longrunning()
        {
            ToDictionary(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToDictionary_ElementSelector(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).ToDictionary(x => x, y => y * 2),
                p => { seen.Add(p.Key); Assert.Equal(p.Key * 2, p.Value); });
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void ToDictionary_ElementSelector_Longrunning()
        {
            ToDictionary_ElementSelector(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToDictionary_CustomComparator(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).ToDictionary(x => x * 2, new ModularCongruenceComparer(count * 2)),
                p => { seen.Add(p.Key / 2); Assert.Equal(p.Key, p.Value * 2); });
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void ToDictionary_CustomComparator_Longrunning()
        {
            ToDictionary_CustomComparator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToDictionary_ElementSelector_CustomComparator(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).ToDictionary(x => x, y => y * 2, new ModularCongruenceComparer(count)),
                p => { seen.Add(p.Key); Assert.Equal(p.Key * 2, p.Value); });
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void ToDictionary_ElementSelector_CustomComparator_Longrunning()
        {
            ToDictionary_ElementSelector_CustomComparator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToDictionary_UniqueKeys_CustomComparator(int count)
        {
            if (count > 2)
            {
                ArgumentException e = AssertThrows.Wrapped<ArgumentException>(() => UnorderedSources.Default(count).ToDictionary(x => x, new ModularCongruenceComparer(2)));
            }
            else if (count == 1 || count == 2)
            {
                IntegerRangeSet seen = new IntegerRangeSet(0, count);
                foreach (KeyValuePair<int, int> entry in UnorderedSources.Default(count).ToDictionary(x => x, new ModularCongruenceComparer(2)))
                {
                    seen.Add(entry.Key);
                    Assert.Equal(entry.Key, entry.Value);
                }
                seen.AssertComplete();
            }
            else
            {
                Assert.Empty(UnorderedSources.Default(count).ToDictionary(x => x, new ModularCongruenceComparer(2)));
            }
        }

        [Fact]
        [OuterLoop]
        public static void ToDictionary_UniqueKeys_CustomComparator_Longrunning()
        {
            ToDictionary_UniqueKeys_CustomComparator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ToDictionary_ElementSelector_UniqueKeys_CustomComparator(int count)
        {
            if (count > 2)
            {
                AssertThrows.Wrapped<ArgumentException>(() => UnorderedSources.Default(count).ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)));
            }
            else if (count == 1 || count == 2)
            {
                IntegerRangeSet seen = new IntegerRangeSet(0, count);
                foreach (KeyValuePair<int, int> entry in UnorderedSources.Default(count).ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)))
                {
                    seen.Add(entry.Key);
                    Assert.Equal(entry.Key, entry.Value);
                }
                seen.AssertComplete();
            }
            else
            {
                Assert.Empty(UnorderedSources.Default(count).ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)));
            }
        }

        [Fact]
        [OuterLoop]
        public static void ToDictionary_ElementSelector_UniqueKeys_CustomComparator_Longrunning()
        {
            ToDictionary_ElementSelector_UniqueKeys_CustomComparator(Sources.OuterLoopCount);
        }

        [Fact]
        public static void ToDictionary_DuplicateKeys()
        {
            AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x));
        }

        [Fact]
        public static void ToDictionary_DuplicateKeys_ElementSelector()
        {
            AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x, y => y));
        }

        [Fact]
        public static void ToDictionary_DuplicateKeys_CustomComparator()
        {
            AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x, new ModularCongruenceComparer(2)));
        }

        [Fact]
        public static void ToDictionary_DuplicateKeys_ElementSelector_CustomComparator()
        {
            AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)));
        }

        [Fact]
        public static void ToDictionary_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.ToDictionary(x => x, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.EventuallyCanceled((source, canceler) => source.ToDictionary(x => x, y => y, new CancelingEqualityComparer<int>(canceler)));
        }

        [Fact]
        public static void ToDictionary_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.ToDictionary(x => x, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.ToDictionary(x => x, y => y, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.ToDictionary(x => x, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.ToDictionary(x => x, y => y, new CancelingEqualityComparer<int>(canceler)));
        }

        [Fact]
        public static void ToDictionary_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.ToDictionary(x => x));
            AssertThrows.AlreadyCanceled(source => source.ToDictionary(x => x, EqualityComparer<int>.Default));
            AssertThrows.AlreadyCanceled(source => source.ToDictionary(x => x, y => y));
            AssertThrows.AlreadyCanceled(source => source.ToDictionary(x => x, y => y, EqualityComparer<int>.Default));
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1 }, MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); }), y => y));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, (Func<int, int>)(y => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); }), EqualityComparer<int>.Default));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); }), y => y, EqualityComparer<int>.Default));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, (Func<int, int>)(y => { throw new DeliberateTestException(); }), EqualityComparer<int>.Default));

            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, new FailingEqualityComparer<int>()));
            AssertThrows.Wrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, y => y, new FailingEqualityComparer<int>()));
        }

        [Fact]
        public static void ToDictionary_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToDictionary(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToDictionary(x => x, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToDictionary(x => x, y => y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ToDictionary(x => x, y => y, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null, y => y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null, y => y, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Empty<int>().ToDictionary(x => x, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Empty<int>().ToDictionary(x => x, (Func<int, int>)null, EqualityComparer<int>.Default));
        }
    }
}
