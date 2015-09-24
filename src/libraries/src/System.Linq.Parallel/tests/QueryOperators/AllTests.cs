// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class AllTests
    {
        public static IEnumerable<object[]> OnlyOneData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        //
        // All
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void All_AllFalse(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count == 0, query.All(x => x < 0));
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void All_AllFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            All_AllFalse(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void All_AllTrue(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.True(query.All(x => seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void All_AllTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            All_AllTrue(labeled, count);
        }

        [Theory]
        [MemberData("OnlyOneData", (object)(new int[] { 2, 16 }))]
        public static void All_OneFalse(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.False(query.All(x => !(x == position)));
        }

        [Theory]
        [OuterLoop]
        [MemberData("OnlyOneData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void All_OneFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            All_OneFalse(labeled, count, position);
        }

        [Theory]
        [MemberData("OnlyOneData", (object)(new int[] { 2, 16 }))]
        public static void All_OneTrue(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.False(query.All(x => x == position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("OnlyOneData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void All_OneTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            All_OneTrue(labeled, count, position);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void All_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).All(x => true));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void All_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.All(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void All_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).All(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().All(null));
        }
    }
}
