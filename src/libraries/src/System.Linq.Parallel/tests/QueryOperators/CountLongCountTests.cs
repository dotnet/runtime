// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class CountLongCountTests
    {
        public static IEnumerable<object[]> OnlyOneData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        //
        // Count and LongCount
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Count_All(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count, query.Count());
            Assert.Equal(count, query.Count(i => i < count));
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void Count_All_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Count_All(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void LongCount_All(Labeled<ParallelQuery<int>> labeled, long count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count, query.LongCount());
            Assert.Equal(count, query.LongCount(i => i < count));
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void LongCount_All_Longrunning(Labeled<ParallelQuery<int>> labeled, long count)
        {
            LongCount_All(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Count_None(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Count(i => i == -1));
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void Count_None_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Count_None(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void LongCount_None(Labeled<ParallelQuery<int>> labeled, long count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.LongCount(i => i == -1));
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void LongCount_None_Longrunning(Labeled<ParallelQuery<int>> labeled, long count)
        {
            LongCount_None(labeled, count);
        }

        [Theory]
        [MemberData("OnlyOneData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Count_One(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Math.Min(1, count), query.Count(i => i == position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("OnlyOneData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Count_One_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            Count_One(labeled, count, position);
        }

        [Theory]
        [MemberData("OnlyOneData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void LongCount_One(Labeled<ParallelQuery<int>> labeled, int count, long position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Math.Min(1, count), query.LongCount(i => i == position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("OnlyOneData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void LongCount_One_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, long position)
        {
            LongCount_One(labeled, count, position);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void CountLongCount_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Count());
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Count(x => true));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).LongCount());
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).LongCount(x => true));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void CountLongCount_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Count(x => { throw new DeliberateTestException(); }));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.LongCount(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void CountLongCount_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Count());
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Count(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().Count(null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).LongCount());
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).LongCount(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().LongCount(null));
        }
    }
}
