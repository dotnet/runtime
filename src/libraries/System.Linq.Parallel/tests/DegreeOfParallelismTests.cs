// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class DegreeOfParallelismTests
    {
        // If ThreadPool becomes available, uncomment the below
        //private static ThreadPoolManager _poolManager = new ThreadPoolManager();

        public static IEnumerable<object[]> DegreeData(int[] counts, int[] degrees)
        {
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), x => degrees.Cast<int>().DefaultIfEmpty(x)))
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
                    yield return new object[] { Labeled.Label(query.ToString(), Partitioner.Create(UnorderedSources.GetRangeArray(0, (int)results[1]), false).AsParallel()), results[1], results[2] };
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
        [MemberData("DegreeData", new[] { 1024 }, new[] { 1, 4, 512 })]
        [OuterLoop]
        public static void DegreeOfParallelism(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            Assert.Equal(Functions.SumRange(0, count), labeled.Item.WithDegreeOfParallelism(degree).Sum());
        }

        [Theory]
        [MemberData("DegreeData", new[] { 1, 4, 32 }, new int[] { })]
        [OuterLoop]
        public static void DegreeOfParallelism_Barrier(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            Barrier barrier = new Barrier(degree);
            //If ThreadPool becomes available, uncomment the below.
            //_poolManager.ReserveThreads(degree);
            //try
            //{
            Assert.Equal(Functions.SumRange(0, count), labeled.Item.WithDegreeOfParallelism(degree).Sum(x => { barrier.SignalAndWait(); return x; }));
            //}
            //finally
            //{
            //    _poolManager.ReleaseThreads();
            //}
        }

        [Theory]
        [MemberData("DegreeData", new[] { 128 * 1024 }, new[] { 1, 4, 64, 512 })]
        [OuterLoop]
        public static void DegreeOfParallelism_Pipelining(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            Assert.True(labeled.Item.WithDegreeOfParallelism(degree).Select(x => -x).OrderBy(x => x).SequenceEqual(ParallelEnumerable.Range(1 - count, count).AsOrdered()));
        }

        [Theory]
        [MemberData("DegreeData", new[] { 1, 4 }, new int[] { })]
        [MemberData("DegreeData", new[] { 32 }, new[] { 4 })]
        // Without the ability to ask the thread pool to create a minimum number of threads ahead of time,
        // higher thread counts take a prohibitive amount of time spooling them up.
        //[MemberData("DegreeSourceData", new[] { 64, 512 }, new object[] { })]
        [OuterLoop]
        public static void DegreeOfParallelism_Throttled_Pipelining(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            // If ThreadPool becomes available, uncomment the below.
            //_poolManager.ReserveThreads(degree);
            //try
            //{
            Assert.True(labeled.Item.WithDegreeOfParallelism(degree).Select(x => { Task.Delay(100).Wait(); return -x; }).OrderBy(x => x).SequenceEqual(ParallelEnumerable.Range(1 - count, count).AsOrdered()));
            //}
            //finally
            //{
            //    _poolManager.ReleaseThreads();
            //}
        }

        [Theory]
        [MemberData("NotLoadBalancedDegreeData", new[] { 1, 4 }, new int[] { })]
        [MemberData("NotLoadBalancedDegreeData", new[] { 32, 512, 1024 }, new[] { 4, 16 })]
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
        [MemberData("NotLoadBalancedDegreeData", new[] { 1, 4 }, new int[] { })]
        [MemberData("NotLoadBalancedDegreeData", new[] { 32, 512, 1024 }, new[] { 4, 16 })]
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
        [MemberData("DegreeData", new[] { 1024 }, new[] { 0, 513 })]
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
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).WithDegreeOfParallelism(2));
        }

        // ThreadPool is not currently exposed.
        // When available, uncomment below to reserve threads (should help test run time).
        /*
        private class ThreadPoolManager
        {
            private int _minWorker;
            private int _minAsyncIO;
            private int _maxWorker;
            private int _maxAsyncIO;

            private object _switch = new object();

            public void ReserveThreads(int degree)
            {
                Monitor.Enter(_switch);
                ThreadPool.GetMinThreads(out _minWorker, out _minAsyncIO);
                ThreadPool.GetMaxThreads(out _maxWorker, out _maxAsyncIO);
                ThreadPool.SetMinThreads(degree, _minAsyncIO);
            }

            public void ReleaseThreads()
            {
                ThreadPool.SetMinThreads(_minWorker, _minAsyncIO);
                ThreadPool.SetMaxThreads(_maxWorker, _maxAsyncIO);
                Monitor.Exit(_switch);
            }
        }
        */
    }
}
