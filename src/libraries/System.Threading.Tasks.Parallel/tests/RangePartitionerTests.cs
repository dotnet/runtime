// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class RangePartitionerTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunPartitionerStaticTest_SingleChunking()
        {
            CountdownEvent cde = new CountdownEvent(2);
            Action[] actions = new Action[256];

            // The thinking here is that we'll put enough "filler" into this array to
            // insure that "natural" chunk size is greater than 2.  Without the
            // NoBuffering option, the Parallel.ForEach below is certain to deadlock.
            // Somewhere a Signal() is going to be after a Wait() in the same chunk, and
            // the loop will deadlock.
            for (int i = 0; i < 252; i++) actions[i] = () => { };
            actions[252] = () => { cde.Wait(); };
            actions[253] = () => { cde.Signal(); };
            actions[254] = () => { cde.Wait(); };
            actions[255] = () => { cde.Signal(); };

            Debug.WriteLine("    * We'll hang here if EnumerablePartitionerOptions.NoBuffering is not working properly");
            Parallel.ForEach(Partitioner.Create(actions, EnumerablePartitionerOptions.NoBuffering), item =>
            {
                item();
            });
        }

        [Fact]
        public static void RunPartitionerStaticTest_SingleChunking_Negative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(new int[] { 1, 2, 3, 4, 5 }, (EnumerablePartitionerOptions)1000));
        }

        // Test proper range coverage
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RangePartitionerCoverageTest()
        {
            RangePartitionerCoverageTest_HelperInt(0, 1, -1);
            RangePartitionerCoverageTest_HelperInt(-15, -14, -1);
            RangePartitionerCoverageTest_HelperInt(14, 15, -1);
            RangePartitionerCoverageTest_HelperInt(0, 1, 1);
            RangePartitionerCoverageTest_HelperInt(-15, -14, 1);
            RangePartitionerCoverageTest_HelperInt(14, 15, 1);
            RangePartitionerCoverageTest_HelperInt(0, 1, 20);
            RangePartitionerCoverageTest_HelperInt(-15, -14, 20);
            RangePartitionerCoverageTest_HelperInt(14, 15, 20);
            RangePartitionerCoverageTest_HelperInt(0, 7, -1);
            RangePartitionerCoverageTest_HelperInt(-21, -14, -1);
            RangePartitionerCoverageTest_HelperInt(14, 21, -1);
            RangePartitionerCoverageTest_HelperInt(0, 7, 1);
            RangePartitionerCoverageTest_HelperInt(-21, -14, 1);
            RangePartitionerCoverageTest_HelperInt(14, 21, 1);
            RangePartitionerCoverageTest_HelperInt(0, 7, 2);
            RangePartitionerCoverageTest_HelperInt(-21, -14, 2);
            RangePartitionerCoverageTest_HelperInt(14, 21, 2);
            RangePartitionerCoverageTest_HelperInt(0, 7, 20);
            RangePartitionerCoverageTest_HelperInt(-21, -14, 20);
            RangePartitionerCoverageTest_HelperInt(14, 21, 20);
            RangePartitionerCoverageTest_HelperInt(0, 1000, -1);
            RangePartitionerCoverageTest_HelperInt(-2000, -1000, -1);
            RangePartitionerCoverageTest_HelperInt(1000, 2000, -1);
            RangePartitionerCoverageTest_HelperInt(0, 1000, 1);
            RangePartitionerCoverageTest_HelperInt(-2000, -1000, 1);
            RangePartitionerCoverageTest_HelperInt(1000, 2000, 1);
            RangePartitionerCoverageTest_HelperInt(0, 1000, 27);
            RangePartitionerCoverageTest_HelperInt(-2000, -1000, 27);
            RangePartitionerCoverageTest_HelperInt(1000, 2000, 27);
            RangePartitionerCoverageTest_HelperInt(0, 1000, 250);
            RangePartitionerCoverageTest_HelperInt(-2000, -1000, 250);
            RangePartitionerCoverageTest_HelperInt(1000, 2000, 250);
            RangePartitionerCoverageTest_HelperInt(0, 1000, 750);
            RangePartitionerCoverageTest_HelperInt(-2000, -1000, 750);
            RangePartitionerCoverageTest_HelperInt(1000, 2000, 750);
        }

        // Test that chunk sizes are being honored
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RangePartitionerChunkTest()
        {
            RangePartitionerChunkTest_HelperInt(0, 10, 1);
            RangePartitionerChunkTest_HelperInt(-20, -10, 1);
            RangePartitionerChunkTest_HelperInt(10, 20, 1);
            RangePartitionerChunkTest_HelperInt(0, 10, 3);
            RangePartitionerChunkTest_HelperInt(-20, -10, 3);
            RangePartitionerChunkTest_HelperInt(10, 20, 3);
            RangePartitionerChunkTest_HelperInt(0, 10, 5);
            RangePartitionerChunkTest_HelperInt(-20, -10, 5);
            RangePartitionerChunkTest_HelperInt(10, 20, 5);
            RangePartitionerChunkTest_HelperInt(0, 10, 7);
            RangePartitionerChunkTest_HelperInt(-20, -10, 7);
            RangePartitionerChunkTest_HelperInt(10, 20, 7);
            RangePartitionerChunkTest_HelperInt(0, 1000000, 32768);
            RangePartitionerChunkTest_HelperInt(-2000000, -1000000, 32768);
            RangePartitionerChunkTest_HelperInt(1000000, 2000000, 32768);
        }

        private static void RangePartitionerChunkTest_HelperInt(int from, int to, int rangeSize)
        {
            Debug.WriteLine("    RangePartitionChunkTest[int]({0},{1},{2})", from, to, rangeSize);
            int numLess = 0;
            int numMore = 0;

            Parallel.ForEach(Partitioner.Create(from, to, rangeSize), tuple =>
            {
                int range = tuple.Item2 - tuple.Item1;
                if (range > rangeSize)
                {
                    Assert.False(range > rangeSize, string.Format("    > FAILED.  Observed chunk size of {0}", range));
                    Interlocked.Increment(ref numMore);
                }
                else if (range < rangeSize)
                    Interlocked.Increment(ref numLess);
            });

            Assert.False(numMore > 0, string.Format("    > FAILED.  {0} chunks larger than desired range size.", numMore));

            Assert.False(numLess > 1, string.Format("    > FAILED.  {0} chunks smaller than desired range size.", numLess));

            RangePartitionerChunkTest_HelperLong((long)from, (long)to, (long)rangeSize);
        }

        private static void RangePartitionerChunkTest_HelperLong(long from, long to, long rangeSize)
        {
            Debug.WriteLine("    RangePartitionChunkTest[long]({0},{1},{2})", from, to, rangeSize);
            int numLess = 0;
            int numMore = 0;

            Parallel.ForEach(Partitioner.Create(from, to, rangeSize), tuple =>
            {
                long range = tuple.Item2 - tuple.Item1;
                if (range > rangeSize)
                {
                    Assert.False(range > rangeSize, string.Format("    > FAILED.  Observed chunk size of {0}", range));
                    Interlocked.Increment(ref numMore);
                }
                else if (range < rangeSize) Interlocked.Increment(ref numLess);
            });

            Assert.False(numMore > 0, string.Format("    > FAILED.  {0} chunks larger than desired range size.", numMore));

            Assert.False(numLess > 1, string.Format("    > FAILED.  {0} chunks smaller than desired range size.", numLess));
        }

        private static void RangePartitionerCoverageTest_HelperInt(int from, int to, int rangeSize)
        {
            Debug.WriteLine("    RangePartitionCoverageTest[int]({0},{1},{2})", from, to, rangeSize);

            int range = to - from;
            int[] visits = new int[range];

            Action<Tuple<int, int>> myDelegate = delegate (Tuple<int, int> myRange)
            {
                int _from = myRange.Item1;
                int _to = myRange.Item2;
                for (int i = _from; i < _to; i++) Interlocked.Increment(ref visits[i - from]);
            };

            if (rangeSize == -1) Parallel.ForEach(Partitioner.Create(from, to), myDelegate);
            else Parallel.ForEach(Partitioner.Create(from, to, rangeSize), myDelegate);

            for (int i = 0; i < range; i++)
            {
                Assert.False(visits[i] != 1, string.Format("    > FAILED.  Visits[{0}] = {1}", i, visits[i]));
            }

            RangePartitionerCoverageTest_HelperLong((long)from, (long)to, (long)rangeSize);
        }

        private static void RangePartitionerCoverageTest_HelperLong(long from, long to, long rangeSize)
        {
            Debug.WriteLine("    RangePartitionCoverageTest[long]({0},{1},{2})", from, to, rangeSize);

            long range = to - from;
            long[] visits = new long[range];

            Action<Tuple<long, long>> myDelegate = delegate (Tuple<long, long> myRange)
            {
                long _from = myRange.Item1;
                long _to = myRange.Item2;
                for (long i = _from; i < _to; i++) Interlocked.Increment(ref visits[i - from]);
            };

            if (rangeSize == -1) Parallel.ForEach(Partitioner.Create(from, to), myDelegate);
            else Parallel.ForEach(Partitioner.Create(from, to, rangeSize), myDelegate);

            for (long i = 0; i < range; i++)
            {
                Assert.False(visits[i] != 1, string.Format("    > FAILED.  Visits[{0}] = {1}", i, visits[i]));
            }
        }
    }
}
