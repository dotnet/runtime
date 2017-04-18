// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ElementAtElementAtOrDefaultTests
    {
        private static readonly Func<int, IEnumerable<int>> Positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();
        private static readonly Func<int, IEnumerable<int>> InvalidPositions = x => new[] { -1, x, x * 2 }.Distinct();

        public static IEnumerable<object[]> ElementAtUnorderedData(int[] counts)
        {
            // A deliberate decision was made here to test with all types, because this reflects partitioning/indexing
            foreach (object[] results in UnorderedSources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), Positions)) yield return results;
        }

        public static IEnumerable<object[]> ElementAtData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), Positions)) yield return results;
        }

        public static IEnumerable<object[]> ElementAtOutOfRangeData(int[] counts)
        {
            foreach (object[] results in UnorderedSources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), InvalidPositions)) yield return results;
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), InvalidPositions)) yield return results;
        }

        //
        // ElementAt and ElementAtOrDefault
        //
        [Theory]
        [MemberData(nameof(ElementAtUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(ElementAtData), new[] { 1, 2, 16 })]
        public static void ElementAt(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(position, query.ElementAt(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ElementAtUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(ElementAtData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void ElementAt_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAt(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(ElementAtOutOfRangeData), new[] { 0, 1, 2, 16 })]
        public static void ElementAt_OutOfRange(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Throws<ArgumentOutOfRangeException>(() => query.ElementAt(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ElementAtOutOfRangeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void ElementAt_OutOfRange_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAt_OutOfRange(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(ElementAtUnorderedData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(ElementAtData), new[] { 0, 1, 2, 16 })]
        public static void ElementAtOrDefault(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(position, query.ElementAtOrDefault(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ElementAtUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(ElementAtData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void ElementAtOrDefault_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAtOrDefault(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(ElementAtOutOfRangeData), new[] { 0, 1, 2, 16 })]
        public static void ElementAtOrDefault_OutOfRange(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(default(int), query.ElementAtOrDefault(position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ElementAtOutOfRangeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void ElementAtOrDefault_OutOfRange_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ElementAtOrDefault_OutOfRange(labeled, count, position);
        }

        [Fact]
        public static void ElementAt_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.ElementAt(0));
            AssertThrows.AlreadyCanceled(source => source.ElementAtOrDefault(0));
        }

        [Fact]
        public static void ElementAt_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<object>)null).ElementAt(0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<object>)null).ElementAtOrDefault(0));
        }
    }
}
