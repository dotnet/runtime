// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class AnyTests
    {
        public static IEnumerable<object[]> OnlyOneData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount))
            {
                foreach (int position in new[] { 0, count / 2, Math.Max(0, count - 1) }.Distinct())
                {
                    yield return new object[] { count, position };
                }
            }
        }

        //
        // Any
        //
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Any_Contents(int count)
        {
            Assert.Equal(count > 0, UnorderedSources.Default(count).Any());
        }

        [Fact]
        [OuterLoop]
        public static void Any_Contents_Longrunning()
        {
            Any_Contents(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Any_AllFalse(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.False(UnorderedSources.Default(count).Any(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Any_AllFalse_Longrunning()
        {
            Any_AllFalse(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Any_AllTrue(int count)
        {
            Assert.Equal(count > 0, UnorderedSources.Default(count).Any(x => x >= 0));
        }

        [Fact]
        [OuterLoop]
        public static void Any_AllTrue_Longrunning()
        {
            Any_AllTrue(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(OnlyOneData), new[] { 2, 16 })]
        public static void Any_OneFalse(int count, int position)
        {
            Assert.True(UnorderedSources.Default(count).Any(x => !(x == position)));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(OnlyOneData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Any_OneFalse_Longrunning(int count, int position)
        {
            Any_OneFalse(count, position);
        }

        [Theory]
        [MemberData(nameof(OnlyOneData), new[] { 2, 16 })]
        public static void Any_OneTrue(int count, int position)
        {
            Assert.True(UnorderedSources.Default(count).Any(x => x == position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(OnlyOneData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Any_OneTrue_Longrunning(int count, int position)
        {
            Any_OneTrue(count, position);
        }

        //
        // Tests the Any() operator applied to infinite enumerables
        //
        [Fact]
        public static void Any_Infinite()
        {
            Assert.True(InfiniteEnumerable().AsParallel().Any());
            Assert.True(InfiniteEnumerable().AsParallel().Any(x => true));
        }

        [Fact]
        public static void Any_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Any(x => { canceler(); return false; }));
        }

        [Fact]
        public static void Any_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Any(x => { canceler(); return false; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Any(x => { canceler(); return false; }));
        }

        [Fact]
        public static void Any_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Any());
            AssertThrows.AlreadyCanceled(source => source.Any(x => true));
        }

        [Fact]
        public static void Any_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Any(x => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Select((Func<int, int>)(x => { throw new DeliberateTestException(); })).Any());
        }

        [Fact]
        public static void Any_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Any(x => x));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().Any(null));
        }

        private static IEnumerable<int> InfiniteEnumerable()
        {
            while (true) yield return 0;
        }
    }
}
