// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class FirstFirstOrDefaultTests
    {
        public static IEnumerable<object[]> FirstUnorderedData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        public static IEnumerable<object[]> FirstData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in Sources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        //
        // First and FirstOrDefault
        //
        [Theory]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1, 2, 16 }))]
        [MemberData("FirstData", (object)(new int[] { 1, 2, 16 }))]
        public static void First(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.First());
            Assert.Equal(position, query.First(x => x >= position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        [MemberData("FirstData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void First_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            First(labeled, count, position);
        }

        [Theory]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1, 2, 16 }))]
        [MemberData("FirstData", (object)(new int[] { 1, 2, 16 }))]
        public static void FirstOrDefault(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.FirstOrDefault());
            Assert.Equal(position, query.FirstOrDefault(x => x >= position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        [MemberData("FirstData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void FirstOrDefault_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            FirstOrDefault(labeled, count, position);
        }

        [Theory]
        [MemberData("FirstUnorderedData", (object)(new int[] { 0 }))]
        [MemberData("FirstData", (object)(new int[] { 0 }))]
        public static void First_Empty(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Throws<InvalidOperationException>(() => query.First());
        }

        [Theory]
        [MemberData("FirstUnorderedData", (object)(new int[] { 0 }))]
        [MemberData("FirstData", (object)(new int[] { 0 }))]
        public static void FirstOrDefault_Empty(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(default(int), query.FirstOrDefault());
        }

        [Theory]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1, 2, 16 }))]
        [MemberData("FirstData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void First_NoMatch(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Throws<InvalidOperationException>(() => query.First(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        [MemberData("FirstData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void First_NoMatch_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            First_NoMatch(labeled, count, position);
        }

        [Theory]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1, 2, 16 }))]
        [MemberData("FirstData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void FirstOrDefault_NoMatch(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Equal(default(int), query.FirstOrDefault(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("FirstUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        [MemberData("FirstData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void FirstOrDefault_NoMatch_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            FirstOrDefault_NoMatch(labeled, count, position);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void First_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).First());
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).First(x => true));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).FirstOrDefault());
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).FirstOrDefault(x => true));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void First_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.First(x => { throw new DeliberateTestException(); }));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.FirstOrDefault(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void First_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).First());
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).FirstOrDefault());

            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().First(null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().FirstOrDefault(null));
        }
    }
}
