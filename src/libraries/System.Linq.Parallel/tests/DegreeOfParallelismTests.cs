// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class DegreeOfParallelismTests
    {
        public static IEnumerable<object[]> DegreeData(int[] counts, int[] degrees)
        {
            foreach (object[] results in UnorderedSources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), x => degrees.DefaultIfEmpty(x)))
            {
                yield return results;
            }
        }

        public static IEnumerable<object[]> NotLoadBalancedDegreeData(int[] counts, int[] degrees)
        {
            foreach (object[] results in DegreeData(counts, degrees))
            {
                Labeled<ParallelQuery<int>> query = (Labeled<ParallelQuery<int>>)results[0];
                if (query.ToString().StartsWith("Partitioner"))
                {
                    yield return new object[] { Labeled.Label(query.ToString(), Partitioner.Create(Enumerable.Range(0, (int)results[1]).ToArray(), false).AsParallel()), results[1], results[2] };
                }
                else if (query.ToString().StartsWith("Enumerable.Range"))
                {
                    yield return new object[] { Labeled.Label(query.ToString(), new StrictPartitioner<int>(Partitioner.Create(Enumerable.Range(0, (int)results[1]), EnumerablePartitionerOptions.NoBuffering), (int)results[1]).AsParallel()), results[1], results[2] };
                }
                else
                {
                    yield return results;
                }
            }
        }

        [Theory]
        [MemberData(nameof(DegreeData), new[] { 1024 }, new[] { 1, 4, 512 })]
        [OuterLoop]
        public static void DegreeOfParallelism(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            Assert.Equal(Functions.SumRange(0, count), labeled.Item.WithDegreeOfParallelism(degree).Sum());
        }

        [Theory]
        [MemberData(nameof(DegreeData), new[] { 1, 4, 32 }, new int[] { /* same as count */ })]
        [OuterLoop]
        public static void DegreeOfParallelism_Barrier(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            using (ThreadPoolHelpers.EnsureMinThreadsAtLeast(degree))
            {
                var barrier = new Barrier(degree);
                Assert.Equal(Functions.SumRange(0, count), labeled.Item.WithDegreeOfParallelism(degree).Sum(x => { barrier.SignalAndWait(); return x; }));
            }
        }

        [Theory]
        [MemberData(nameof(DegreeData), new int[] { /* Sources.OuterLoopCount */ }, new[] { 1, 4, 64, 128 })]
        [OuterLoop]
        public static void DegreeOfParallelism_Pipelining(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            using (ThreadPoolHelpers.EnsureMinThreadsAtLeast(degree))
            {
                int expected = 1 - count;
                foreach (int result in labeled.Item.WithDegreeOfParallelism(degree).Select(x => -x).OrderBy(x => x))
                {
                    Assert.Equal(expected++, result);
                }
            }
        }

        [Theory]
        [MemberData(nameof(DegreeData), new[] { 1, 4 }, new int[] { /* same as count */ })]
        [MemberData(nameof(DegreeData), new[] { 32 }, new[] { 4 })]
        [OuterLoop]
        public static void DegreeOfParallelism_Throttled_Pipelining(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            using (ThreadPoolHelpers.EnsureMinThreadsAtLeast(degree))
            {
                Assert.True(labeled.Item.WithDegreeOfParallelism(degree).Select(x =>
                {
                    var sw = new SpinWait();
                    while (!sw.NextSpinWillYield) sw.SpinOnce(); // brief spin to wait a small amount of time
                    return -x;
                }).OrderBy(x => x).SequenceEqual(ParallelEnumerable.Range(1 - count, count).AsOrdered()));
            }
        }

        [Theory]
        [MemberData(nameof(NotLoadBalancedDegreeData), new[] { 1, 4 }, new int[] { /* same as count */ })]
        [MemberData(nameof(NotLoadBalancedDegreeData), new[] { 32, 512, 1024 }, new[] { 4, 16 })]
        [OuterLoop]
        public static void DegreeOfParallelism_Aggregate_Accumulator(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            ParallelQuery<int> query = labeled.Item;
            int accumulatorCombineCount = 0;

            int actual = query.WithDegreeOfParallelism(degree).Aggregate(
                0,
                (accumulator, x) => accumulator + x,
                (left, right) => { Interlocked.Increment(ref accumulatorCombineCount); return left + right; },
                result => result);
            Assert.Equal(Functions.SumRange(0, count), actual);
            Assert.Equal(degree - 1, accumulatorCombineCount);
        }

        [Theory]
        [MemberData(nameof(NotLoadBalancedDegreeData), new[] { 1, 4 }, new int[] { /* same as count */ })]
        [MemberData(nameof(NotLoadBalancedDegreeData), new[] { 32, 512, 1024 }, new[] { 4, 16 })]
        [OuterLoop]
        public static void DegreeOfParallelism_Aggregate_SeedFunction(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            ParallelQuery<int> query = labeled.Item;
            int accumulatorCombineCount = 0;
            int seedFunctionCallCount = 0;

            int actual = query.WithDegreeOfParallelism(degree).Aggregate(
                () => { Interlocked.Increment(ref seedFunctionCallCount); return 0; },
                (accumulator, x) => accumulator + x,
                (left, right) => { Interlocked.Increment(ref accumulatorCombineCount); return left + right; },
                result => result);
            Assert.Equal(Functions.SumRange(0, count), actual);
            Assert.Equal(seedFunctionCallCount, degree);
            Assert.Equal(seedFunctionCallCount - 1, accumulatorCombineCount);
            Assert.Equal(degree - 1, accumulatorCombineCount);
        }

        [Theory]
        [MemberData(nameof(DegreeData), new[] { 1024 }, new[] { 0, 513 })]
        [OuterLoop]
        public static void DegreeOfParallelism_OutOfRange(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => labeled.Item.WithDegreeOfParallelism(degree));
        }

        [Fact]
        public static void DegreeOfParallelism_Multiple()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 2).WithDegreeOfParallelism(2).WithDegreeOfParallelism(2));
        }

        [Fact]
        public static void DegreeOfParallelism_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).WithDegreeOfParallelism(2));
        }
    }
}
