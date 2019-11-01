// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ForAllTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void ForAll(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            ParallelEnumerable.Range(0, count).ForAll(x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void ForAll_Longrunning()
        {
            ForAll(Sources.OuterLoopCount);
        }

        [Fact]
        public static void ForAll_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.ForAll(x => canceler()));
        }

        [Fact]
        public static void ForAll_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.ForAll(x => canceler()));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.ForAll(x => canceler()));
        }

        [Fact]
        public static void ForAll_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.ForAll(x => { }));
        }

        [Fact]
        public static void ForAll_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).ForAll(x => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Select((Func<int, int>)(x => { throw new DeliberateTestException(); })).ForAll(x => { }));
        }

        [Fact]
        public static void ForAll_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).ForAll(x => { }));
            AssertExtensions.Throws<ArgumentNullException>("action", () => ParallelEnumerable.Range(0, 1).ForAll(null));
        }
    }
}
