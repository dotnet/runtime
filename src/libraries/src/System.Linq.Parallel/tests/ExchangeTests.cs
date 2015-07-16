// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ExchangeTests
    {
        private static readonly ParallelMergeOptions[] Options = new[] {
            ParallelMergeOptions.AutoBuffered,
            ParallelMergeOptions.Default,
            ParallelMergeOptions.FullyBuffered,
            ParallelMergeOptions.NotBuffered
        };

        /// <summary>
        /// Get a set of ranges, running for each count in `counts`, with 1, 2, and 4 counts for partitions.
        /// </summary>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the number of partitions or degrees of parallelism to use.</returns>
        public static IEnumerable<object[]> PartitioningData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.Cast<int>(), x => new[] { 1, 2, 4 }))
            {
                yield return results;
            }
        }

        // For each source, run with each buffering option.
        /// <summary>
        /// Get a set of ranges, and running for each count in `counts`, with each possible ParallelMergeOption
        /// </summary>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the ParallelMergeOption to use.</returns>
        public static IEnumerable<object[]> MergeData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.Cast<int>(), x => Options))
            {
                yield return results;
            }
        }

        /// <summary>
        ///For each count, return an Enumerable source that fails (throws an exception) on that count, with each buffering option.
        /// </summary>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the ParallelMergeOption to use.</returns>
        public static IEnumerable<object[]> ThrowOnCount_AllMergeOptions_MemberData(int[] counts)
        {
            foreach (int count in counts.Cast<int>())
            {
                var labeled = Labeled.Label("ThrowOnEnumeration " + count, Enumerables<int>.ThrowOnEnumeration(count).AsParallel().AsOrdered());
                foreach (ParallelMergeOptions option in Options)
                {
                    yield return new object[] { labeled, count, option };
                }
            }
        }

        /// <summary>
        /// Return merge option combinations, for testing multiple calls to WithMergeOptions
        /// </summary>
        /// <returns>Entries for test data.</returns>
        public static IEnumerable<object[]> AllMergeOptions_Multiple()
        {
            foreach (ParallelMergeOptions first in Options)
            {
                foreach (ParallelMergeOptions second in Options)
                {
                    yield return new object[] { first, second };
                }
            }
        }

        // The following tests attempt to test internal behavior,
        // but there doesn't appear to be a way to reliably (or automatically) observe it.
        // The basic tests are covered elsewhere, although without WithDegreeOfParallelism
        // or WithMergeOptions

        [Theory]
        [MemberData("PartitioningData", (object)(new int[] { 0, 1, 2, 16, 1024 }))]
        public static void Partitioning_Default(Labeled<ParallelQuery<int>> labeled, int count, int partitions)
        {
            int seen = 0;
            foreach (int i in labeled.Item.WithDegreeOfParallelism(partitions).Select(i => i))
            {
                Assert.Equal(seen++, i);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("PartitioningData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Partitioning_Default_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int partitions)
        {
            Partitioning_Default(labeled, count, partitions);
        }

        [Theory]
        [MemberData("PartitioningData", (object)(new int[] { 0, 1, 2, 16, 1024 }))]
        public static void Partitioning_Striped(Labeled<ParallelQuery<int>> labeled, int count, int partitions)
        {
            int seen = 0;
            foreach (int i in labeled.Item.WithDegreeOfParallelism(partitions).Take(count).Select(i => i))
            {
                Assert.Equal(seen++, i);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("PartitioningData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Partitioning_Striped_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int partitions)
        {
            Partitioning_Striped(labeled, count, partitions);
        }

        [Theory]
        [MemberData("MergeData", (object)(new int[] { 0, 1, 2, 16, 1024 }))]
        public static void Merge_Ordered(Labeled<ParallelQuery<int>> labeled, int count, ParallelMergeOptions options)
        {
            int seen = 0;
            foreach (int i in labeled.Item.WithMergeOptions(options).Select(i => i))
            {
                Assert.Equal(seen++, i);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("MergeData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Merge_Ordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, ParallelMergeOptions options)
        {
            Merge_Ordered(labeled, count, options);
        }

        [Theory]
        [MemberData("ThrowOnCount_AllMergeOptions_MemberData", (object)(new int[] { 4, 8 }))]
        // FailingMergeData has enumerables that throw errors when attempting to perform the nth enumeration.
        // This test checks whether the query runs in a pipelined or buffered fashion.
        public static void Merge_Ordered_Pipelining(Labeled<ParallelQuery<int>> labeled, int count, ParallelMergeOptions options)
        {
            Assert.Equal(0, labeled.Item.WithDegreeOfParallelism(count - 1).WithMergeOptions(options).First());
        }

        [Theory]
        [MemberData("MergeData", (object)(new int[] { 4, 8 }))]
        // This test checks whether the query runs in a pipelined or buffered fashion.
        public static void Merge_Ordered_Pipelining_Select(Labeled<ParallelQuery<int>> labeled, int count, ParallelMergeOptions options)
        {
            int countdown = count;
            Func<int, int> down = i =>
            {
                if (Interlocked.Decrement(ref countdown) == 0) throw new DeliberateTestException();
                return i;
            };
            Assert.Equal(0, labeled.Item.WithDegreeOfParallelism(count - 1).WithMergeOptions(options).Select(down).First());
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Merge_ArgumentException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;

            Assert.Throws<ArgumentException>(() => query.WithMergeOptions((ParallelMergeOptions)4));
        }

        [Theory]
        [MemberData("AllMergeOptions_Multiple")]
        public static void WithMergeOptions_Multiple(ParallelMergeOptions first, ParallelMergeOptions second)
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(first).WithMergeOptions(second));
        }

        [Fact]
        public static void Merge_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).WithMergeOptions(ParallelMergeOptions.AutoBuffered));
        }

        // The plinq chunk partitioner takes an IEnumerator over the source, and disposes the
        // enumerator when it is finished. If an exception occurs, the calling enumerator disposes
        // the source enumerator... but then other worker threads may generate ODEs.
        // This test verifies any such ODEs are not reflected in the output exception.
        [Theory]
        [MemberData("BinaryRanges", new int[] { 16 }, new int[] { 16 }, MemberType = typeof(UnorderedSources))]
        public static void PlinqChunkPartitioner_DontEnumerateAfterException(Labeled<ParallelQuery<int>> left, int leftCount,
            Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> query =
                left.Item.WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(x => { if (x == 4) throw new DeliberateTestException(); return x; })
                    .Zip(right.Item, (a, b) => a + b)
                .AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism);

            AggregateException ae = Assert.Throws<AggregateException>(() => query.ToArray());
            Assert.Single(ae.InnerExceptions);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
        }

        // The stand-alone chunk partitioner takes an IEnumerator over the source, and
        // disposes the enumerator when it is finished.  If an exception occurs, the calling
        // enumerator disposes the source enumerator... but then other worker threads may generate ODEs.
        // This test verifies any such ODEs are not reflected in the output exception.
        [Theory]
        [MemberData("BinaryRanges", new int[] { 16 }, new int[] { 16 }, MemberType = typeof(UnorderedSources))]
        public static void ManualChunkPartitioner_DontEnumerateAfterException(
            Labeled<ParallelQuery<int>> left, int leftCount,
            Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> query =
                Partitioner.Create(left.Item.WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(x => { if (x == 4) throw new DeliberateTestException(); return x; })
                    .Zip(right.Item, (a, b) => a + b))
                .AsParallel();

            AggregateException ae = Assert.Throws<AggregateException>(() => query.ToArray());
            Assert.Single(ae.InnerExceptions);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
        }
    }
}
