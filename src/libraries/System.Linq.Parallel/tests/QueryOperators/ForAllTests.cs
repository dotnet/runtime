// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ForAllTests
    {
        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ForAll(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            query.ForAll<int>(x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 1024, 1024 * 1024 * 4 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1024 * 1024, 1024 * 1024 * 4 }, MemberType = typeof(UnorderedSources))]
        public static void ForAll_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ForAll(labeled, count);
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

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1 }, MemberType = typeof(UnorderedSources))]
        public static void ForAll_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ForAll(x => { throw new DeliberateTestException(); }));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Select((Func<int, int>)(x => { throw new DeliberateTestException(); })).ForAll(x => { }));
        }

        [Fact]
        public static void ForAll_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).ForAll(x => { }));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).ForAll(null));
        }
    }
}
