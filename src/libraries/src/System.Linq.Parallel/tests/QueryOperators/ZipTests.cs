// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ZipTests
    {
        //
        // Zip
        //

        // Get two ranges, where the right starts at the end of the left range.
        public static IEnumerable<object[]> ZipUnorderedData(int[] counts)
        {
            foreach (object[] parms in UnorderedSources.BinaryRanges(counts.Cast<int>(), (left, right) => left, counts.Cast<int>()))
            {
                yield return parms.Take(4).ToArray();
            }
        }

        // Get two ranges, where the right starts and the end of the left range.
        // Either or both range will be ordered.
        public static IEnumerable<object[]> ZipData(int[] counts)
        {
            foreach (object[] parms in ZipUnorderedData(counts))
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
                foreach (object[] right in Sources.Ranges(counts.Cast<int>(), x => degrees.Cast<int>()))
                {
                    yield return new object[] { left[0], left[1], right[0], right[1], right[2] };
                }
            }
        }

        [Theory]
        [MemberData("ZipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Zip_Unordered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(leftCount, rightCount));
            foreach (var pair in leftQuery.Zip(rightQuery, (x, y) => KeyValuePair.Create(x, y)))
            {
                // For unordered collections the pairing isn't actually guaranteed, but an effect of the implementation.
                // If this test starts failing it should be updated, and possibly mentioned in release notes.
                Assert.Equal(pair.Key + leftCount, pair.Value);
                seen.Add(pair.Key);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("ZipUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 64 }))]
        public static void Zip_Unordered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Zip_Unordered(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("ZipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Zip(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
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
        [MemberData("ZipData", (object)(new int[] { 1024 * 4, 1024 * 64 }))]
        public static void Zip_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Zip(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("ZipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Zip_Unordered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(leftCount, rightCount));
            Assert.All(leftQuery.Zip(rightQuery, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                pair =>
                {
                    // For unordered collections the pairing isn't actually guaranteed, but an effect of the implementation.
                    // If this test starts failing it should be updated, and possibly mentioned in release notes.
                    Assert.Equal(pair.Key + leftCount, pair.Value);
                    seen.Add(pair.Key);
                });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("ZipUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 64 }))]
        public static void Zip_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Zip_Unordered_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("ZipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Zip_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
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
        [MemberData("ZipData", (object)(new int[] { 1024 * 4, 1024 * 64 }))]
        public static void Zip_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Zip_NotPipelined(left, leftCount, right, rightCount);
        }

        // Zip with ordering on showed issues, but it was due to the ordering component.
        // This is included as a regression test for that particular repro.
        [Theory]
        [OuterLoop]
        [MemberData("ZipThreadedData", new[] { 1, 2, 16, 128, 1024 }, new[] { 1, 2, 4, 7, 8, 31, 32 })]
        public static void Zip_AsOrdered_ThreadedDeadlock(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int degree)
        {
            ParallelQuery<int> query = left.Item.WithDegreeOfParallelism(degree).Zip<int, int, int>(right.Item, (a, b) => { throw new DeliberateTestException(); });

            AggregateException ae = Assert.Throws<AggregateException>(() => query.ToArray());
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
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
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Zip(ParallelEnumerable.Range(0, 1), (x, y) => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Zip(null, (Func<int, int, int>)((x, y) => x)));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Zip(ParallelEnumerable.Range(0, 1), (Func<int, int, int>)null));
        }
    }
}
