// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class AnyTests
    {
        public static IEnumerable<object[]> OnlyOneData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        //
        // Any
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Any_Contents(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count > 0, query.Any());
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void Any_Contents_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Any_Contents(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Any_AllFalse(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.False(query.Any(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void Any_AllFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Any_AllFalse(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Any_AllTrue(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count > 0, query.Any(x => x >= 0));
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void Any_AllTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Any_AllTrue(labeled, count);
        }

        [Theory]
        [MemberData("OnlyOneData", (object)(new int[] { 2, 16 }))]
        public static void Any_OneFalse(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.True(query.Any(x => !(x == position)));
        }

        [Theory]
        [OuterLoop]
        [MemberData("OnlyOneData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Any_OneFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            Any_OneFalse(labeled, count, position);
        }

        [Theory]
        [MemberData("OnlyOneData", (object)(new int[] { 2, 16 }))]
        public static void Any_OneTrue(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.True(query.Any(x => x == position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("OnlyOneData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Any_OneTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            Any_OneTrue(labeled, count, position);
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

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Any_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Any());
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Any(x => true));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Any_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Any(x => { throw new DeliberateTestException(); }));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Select((Func<int, int>)(x => { throw new DeliberateTestException(); })).Any());
        }

        [Fact]
        public static void Any_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Any(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().Any(null));
        }

        private static IEnumerable<int> InfiniteEnumerable()
        {
            while (true) yield return 0;
        }
    }
}
