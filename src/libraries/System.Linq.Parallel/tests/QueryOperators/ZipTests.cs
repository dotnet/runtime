// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ZipTests
    {
        //
        // Zip
        //

        // Get two ranges, where the right starts at the end of the left range.
        public static IEnumerable<object[]> ZipUnorderedData(int[] counts)
        {
            foreach (int leftCount in counts)
            {
                foreach (int rightCount in counts)
                {
                    yield return new object[] { leftCount, rightCount };
                }
            }
        }

        // Get two ranges, where the right starts and the end of the left range.
        // Either or both range will be ordered.
        public static IEnumerable<object[]> ZipData(int[] counts)
        {
            counts = counts.DefaultIfEmpty(Sources.OuterLoopCount / 8).ToArray();
            foreach (object[] parms in UnorderedSources.BinaryRanges(counts, (left, right) => left, counts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], ((Labeled<ParallelQuery<int>>)parms[2]).Order(), parms[3] };
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], parms[2], parms[3] };
                yield return new object[] { parms[0], parms[1], ((Labeled<ParallelQuery<int>>)parms[2]).Order(), parms[3] };
            }
        }

        // Get two ranges, both from 0 to each count, and having an extra parameter denoting the degree or parallelism to use.
        public static IEnumerable<object[]> ZipThreadedData(int[] counts, int[] degrees)
        {
            foreach (object[] left in Sources.Ranges(counts))
            {
                foreach (object[] right in Sources.Ranges(counts, x => degrees))
                {
                    yield return new object[] { left[0], left[1], right[0], right[1], right[2] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ZipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Zip_Unordered(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(leftCount, rightCount);
            IntegerRangeSet seen_left = new IntegerRangeSet(0, leftCount);
            IntegerRangeSet seen_right = new IntegerRangeSet(leftCount, rightCount);
            var expected_seen = Math.Min(leftCount, rightCount);
            foreach (var pair in leftQuery.Zip(rightQuery, (x, y) => KeyValuePair.Create(x, y)))
            {
                // Can only validate about whether the elements have been previously seen, not anything about the order.
                seen_left.Add(pair.Key);
                seen_right.Add(pair.Value);
            }

            // Zip truncates the longer collection, but which elements it leaves off is undefined when unordered.
            Assert.Equal(expected_seen, seen_left.Count(kv => kv.Value));
            Assert.Equal(expected_seen, seen_right.Count(kv => kv.Value));
            if (leftCount <= rightCount)
            {
                seen_left.AssertComplete();
            }
            if (rightCount <= leftCount)
            {
                seen_right.AssertComplete();
            }
        }

        [Fact]
        [OuterLoop]
        public static void Zip_Unordered_Longrunning()
        {
            Zip_Unordered(Sources.OuterLoopCount / 4, Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(ZipData), new[] { 0, 1, 2, 16 })]
        public static void Zip(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            // The ordering of Zip is only guaranteed when both operands are ordered,
            // however the current implementation manages to perform ordering if either operand is ordered _in most cases_.
            // If this test starts failing, consider revising the operators and mention the change in release notes.
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            foreach (var pair in leftQuery.Zip(rightQuery, (x, y) => KeyValuePair.Create(x, y)))
            {
                Assert.Equal(seen++, pair.Key);
                Assert.Equal(pair.Key + leftCount, pair.Value);
            }
            Assert.Equal(Math.Min(leftCount, rightCount), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ZipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Zip_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Zip(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(ZipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Zip_Unordered_NotPipelined(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(leftCount, rightCount);
            IntegerRangeSet seen_left = new IntegerRangeSet(0, leftCount);
            IntegerRangeSet seen_right = new IntegerRangeSet(leftCount, rightCount);
            var expected_seen = Math.Min(leftCount, rightCount);
            Assert.All(leftQuery.Zip(rightQuery, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                pair =>
                {
                    // Can only validate about whether the elements have been previously seen, not anything about the order.
                    seen_left.Add(pair.Key);
                    seen_right.Add(pair.Value);
                });

            // Zip truncates the longer collection, but which elements it leaves off is undefined when unordered.
            Assert.Equal(expected_seen, seen_left.Count(kv => kv.Value));
            Assert.Equal(expected_seen, seen_right.Count(kv => kv.Value));
            if (leftCount <= rightCount)
            {
                seen_left.AssertComplete();
            }
            if (rightCount <= leftCount)
            {
                seen_right.AssertComplete();
            }
        }

        [Fact]
        [OuterLoop]
        public static void Zip_Unordered_NotPipelined_Longrunning()
        {
            Zip_Unordered_NotPipelined(Sources.OuterLoopCount / 4, Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(ZipData), new[] { 0, 1, 2, 16 })]
        public static void Zip_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            // The ordering of Zip is only guaranteed when both operands are ordered,
            // however the current implementation manages to perform ordering if either operand is ordered _in most cases_.
            // If this test starts failing, consider revising the operators and mention the change in release notes.
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            Assert.All(leftQuery.Zip(rightQuery, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                pair =>
                {
                    Assert.Equal(seen++, pair.Key);
                    Assert.Equal(pair.Key + leftCount, pair.Value);
                });
            Assert.Equal(Math.Min(leftCount, rightCount), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ZipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Zip_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Zip_NotPipelined(left, leftCount, right, rightCount);
        }

        // Zip with ordering on showed issues, but it was due to the ordering component.
        // This is included as a regression test for that particular repro.
        [Theory]
        [OuterLoop]
        [MemberData(nameof(ZipThreadedData), new[] { 1, 2, 16, 128, 1024 }, new[] { 1, 2, 4, 7, 8, 31, 32 })]
        public static void Zip_AsOrdered_ThreadedDeadlock(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int degree)
        {
            _ = leftCount;
            _ = rightCount;
            ParallelQuery<int> query = left.Item.WithDegreeOfParallelism(degree).Zip<int, int, int>(right.Item, (a, b) => { throw new DeliberateTestException(); });

            AssertThrows.Wrapped<DeliberateTestException>(() => query.ToArray());
        }

        [Fact]
        public static void Zip_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Zip(Enumerable.Range(0, 1), (x, y) => x));
#pragma warning restore 618
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void Zip_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).Zip(ParallelEnumerable.Range(0, 1).WithCancellation(t), (l, r) => l));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).Zip(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1), (l, r) => l));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).Zip(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default), (l, r) => l));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).Zip(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default), (l, r) => l));
        }

        [Fact]
        public static void Zip_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => ((ParallelQuery<int>)null).Zip(ParallelEnumerable.Range(0, 1), (x, y) => x));
            AssertExtensions.Throws<ArgumentNullException>("second", () => ParallelEnumerable.Range(0, 1).Zip(null, (Func<int, int, int>)((x, y) => x)));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).Zip(ParallelEnumerable.Range(0, 1), (Func<int, int, int>)null));
        }
    }
}
