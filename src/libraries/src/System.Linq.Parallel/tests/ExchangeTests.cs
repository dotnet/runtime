// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Test
{
    public class ExchangeTests
    {
        [Fact]
        public static void SimplePartitionMergeWhereScanTest()
        {
            for (int i = 1; i <= 128; i *= 2)
            {
                SimplePartitionMergeWhereScanTest1(1024 * 8, i, true);
                SimplePartitionMergeWhereScanTest1(1024 * 8, i, false);
            }
        }

        [Fact]
        [OuterLoop]
        public static void SimplePartitionMergeWhereScanTest_LongRunning()
        {
            for (int i = 1; i <= 128; i *= 2)
            {
                SimplePartitionMergeWhereScanTest1(1024 * 1024 * 2, i, true);
                SimplePartitionMergeWhereScanTest1(1024 * 1024 * 2, i, false);
            }
        }

        private static void SimplePartitionMergeWhereScanTest1(int dataSize, int partitions, bool pipeline)
        {
            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++)
                data[i] = i;

            var whereOp = data.AsParallel()
                .Where(delegate(int x) { return (x % 2) == 0; }); // select only even elements

            IEnumerator<int> stream = whereOp.GetEnumerator();

            int count = 0;
            while (stream.MoveNext())
            {
                // @TODO: verify all the elements we expected are present.
                count++;
            }

            if (count != (dataSize / 2))
            {
                Assert.True(false, string.Format("SimplePartitionMergeWhereScanTest1: {0}, {1}, {2}:  > FAILED:  count does not equal (dataSize/2)?", dataSize, partitions, pipeline));
            }
        }

        [Fact]
        public static void CheckWhereSelectComposition()
        {
            CheckWhereSelectCompositionCore(1024 * 8);
        }

        private static void CheckWhereSelectCompositionCore(int dataSize)
        {
            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++)
                data[i] = i;

            var whereOp = data.AsParallel().Where(
                delegate(int x) { return (x % 2) == 0; }); // select only even elements
            var selectOp = whereOp.Select(
                delegate(int x) { return x * 2; }); // just double the elements

            // Now verify the output is what we expect.

            int expectSum = 0;
            for (int i = 0; i < dataSize; i++)
                if ((i % 2) == 0)
                    expectSum += (i * 2);

            int realSum = 0;
            IEnumerator<int> e = selectOp.GetEnumerator();
            while (e.MoveNext())
            {
                realSum += e.Current;
            }

            if (realSum != expectSum)
            {
                Assert.True(false, string.Format("CheckWhereSelectComposition datasize({0}):  actual sum does not equal expected sum.", dataSize));
            }
        }

        [Fact]
        public static void PartitioningTest()
        {
            PartitioningTestCore(true, 1, 0, 10);
            PartitioningTestCore(false, 1, 0, 10);

            PartitioningTestCore(true, 1, 0, 500);
            PartitioningTestCore(false, 1, 0, 520);

            PartitioningTestCore(true, 4, 0, 500);
            PartitioningTestCore(false, 4, 0, 520);
        }

        [Fact]
        [OuterLoop]
        public static void PartitioningTest_LongRunning()
        {
            PartitioningTestCore(true, 2, 0, 900);
            PartitioningTestCore(false, 2, 0, 900);
        }

        private static void PartitioningTestCore(bool stripedPartitioning, int partitions, int minLen, int maxLen)
        {
            for (int len = minLen; len < maxLen; len++)
            {
                int[] arr = Enumerable.Range(0, len).ToArray();
                IEnumerable<int> query;

                if (stripedPartitioning)
                {
                    query = arr.AsParallel().AsOrdered().WithDegreeOfParallelism(partitions).Take(len).Select(i => i);
                }
                else
                {
                    query = arr.AsParallel().AsOrdered().WithDegreeOfParallelism(partitions).Select(i => i);
                }

                if (!arr.SequenceEqual(query))
                {
                    Console.WriteLine("PartitioningTest: {0}, {1}, {2}, {3}", stripedPartitioning, partitions, minLen, maxLen);
                    Assert.True(false, string.Format("  ** FAILED: incorrect output for array of length {0}", len));
                }
            }
        }

        [Fact]
        public static void OrderedPipeliningTest()
        {
            OrderedPipeliningTest1(4, true);
            OrderedPipeliningTest1(4, false);
            OrderedPipeliningTest1(10007, true);
            OrderedPipeliningTest1(10007, false);

            OrderedPipeliningTest2(true);
            OrderedPipeliningTest2(false);
        }

        /// <summary>
        /// Checks whether an ordered pipelining merge produces the correct output.
        /// </summary>
        private static void OrderedPipeliningTest1(int dataSize, bool buffered)
        {
            ParallelMergeOptions merge = buffered ? ParallelMergeOptions.FullyBuffered : ParallelMergeOptions.NotBuffered;

            IEnumerable<int> src = Enumerable.Range(0, dataSize);
            if (!Enumerable.SequenceEqual(src.AsParallel().AsOrdered().WithMergeOptions(merge).Select(x => x), src))
            {
                Assert.True(false, string.Format("OrderedPipeliningTest1: dataSize={0}, buffered={1}:  > FAILED: Incorrect output.", dataSize, buffered));
            }
        }

        /// <summary>
        /// Checks whether an ordered pipelining merge pipelines the results
        /// instead of running in a stop-and-go fashion.
        /// </summary>
        private static void OrderedPipeliningTest2(bool buffered)
        {
            ParallelMergeOptions merge = buffered ? ParallelMergeOptions.AutoBuffered : ParallelMergeOptions.NotBuffered;

            IEnumerable<int> src = Enumerable.Range(0, int.MaxValue)
                .Select(x => { if (x == 100000000) throw new Exception(); return x; });

            try
            {
                int expect = 0;
                int got = Enumerable.First(src.AsParallel().AsOrdered().WithMergeOptions(merge).Select(x => x));
                if (got != expect)
                {
                    Assert.True(false, string.Format("OrderedPipeliningTest2: buffered={0}  > FAILED: Expected {1}, got {2}.", buffered, expect, got));
                }
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("OrderedPipeliningTest2: buffered={0}:  > FAILED.  Caught an exception - {1}", buffered, e));
            }
        }

        /// <summary>
        /// Verifies that AsEnumerable causes subsequent LINQ operators to bind to LINQ-to-objects
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void RunAsEnumerableTest()
        {
            IEnumerable<int> src = Enumerable.Range(0, 100).AsParallel().AsEnumerable().Select(x => x);

            bool passed = !(src is ParallelQuery<int>);

            if (!passed)
                Assert.True(false, string.Format("AsEnumerableTest:  > Failed. AsEnumerable() didn't prevent the Select operator from binding to PLINQ."));
        }
    }
}
