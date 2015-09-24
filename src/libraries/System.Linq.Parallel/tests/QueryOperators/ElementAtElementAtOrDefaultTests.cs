// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ElementAtElementAtOrDefaultTests
    {
        public static IEnumerable<object[]> ElementAtUnorderedData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        public static IEnumerable<object[]> ElementAtData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
            foreach (object[] results in Sources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        public static IEnumerable<object[]> ElementAtOutOfRangeData(int[] counts)
        {
            Func<int, IEnumerable<int>> positions = x => new[] { -1, x, x * 2 }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), positions)) yield return results;
        }

        //
        // ElementAt and ElementAtOrDefault
        //
        [Theory]
        [MemberData("ElementAtUnorderedData", (object)(new int[] { 1, 2, 16 }))]
        [MemberData("ElementAtData", (object)(new int[] { 1, 2, 16 }))]
        public static void ElementAt(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(position, query.ElementAt(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("ElementAtUnorderedData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        [MemberData("ElementAtData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void ElementAt_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAt(labeled, count, position);
        }

        [Theory]
        [MemberData("ElementAtOutOfRangeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void ElementAt_OutOfRange(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Throws<ArgumentOutOfRangeException>(() => query.ElementAt(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("ElementAtOutOfRangeData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void ElementAt_OutOfRange_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAt_OutOfRange(labeled, count, position);
        }

        [Theory]
        [MemberData("ElementAtUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        [MemberData("ElementAtData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void ElementAtOrDefault(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(position, query.ElementAtOrDefault(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("ElementAtUnorderedData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        [MemberData("ElementAtData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void ElementAtOrDefault_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAtOrDefault(labeled, count, position);
        }

        [Theory]
        [MemberData("ElementAtOutOfRangeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void ElementAtOrDefault_OutOfRange(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(default(int), query.ElementAtOrDefault(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData("ElementAtOutOfRangeData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void ElementAtOrDefault_OutOfRange_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAtOrDefault_OutOfRange(labeled, count, position);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void ElementAt_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ElementAt(0));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ElementAtOrDefault(0));
        }

        [Fact]
        public static void ElementAt_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<object>)null).ElementAt(0));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<object>)null).ElementAtOrDefault(0));
        }
    }
}
