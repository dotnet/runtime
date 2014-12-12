// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class WhereTests
    {
        //
        // Where
        //
        [Fact]
        public static void RunWhereTests()
        {
            RunWhereTest1(0);
            RunWhereTest1(1);
            RunWhereTest1(32);
            RunWhereTest1(1024);
            RunWhereTest1(1024 * 2);

            RunWhereTest2(0);
            RunWhereTest2(1);
            RunWhereTest2(32);
            RunWhereTest2(1024);
            RunWhereTest2(1024 * 2);
        }

        [Fact]
        public static void RunIndexedWhereTest()
        {
            RunIndexedWhereTest1(0);
            RunIndexedWhereTest1(1);
            RunIndexedWhereTest1(32);

            RunIndexedWhereTest2(0);
            RunIndexedWhereTest2(1);
            RunIndexedWhereTest2(32);
        }

        [Fact]
        [OuterLoop]
        public static void RunIndexedWhereTest_LongRunning()
        {
            RunIndexedWhereTest1(1024);
            RunIndexedWhereTest1(1024 * 2);
            RunIndexedWhereTest1(1024 * 1024 * 4);

            RunIndexedWhereTest2(1024);
            RunIndexedWhereTest2(1024 * 2);
            RunIndexedWhereTest2(1024 * 1024 * 4);
        }

        private static void RunWhereTest1(int dataSize)
        {
            string methodFailed = string.Format("RunWhereTest1(dataSize = {0}) - async/pipeline: FAILED.  ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Filter out odd elements.
            IEnumerable<int> q = data.AsParallel().Where<int>(
                delegate (int x) { return (x % 2) == 0; });

            int cnt = 0;
            foreach (int p in q)
            {
                if ((p % 2) != 0)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: {0} is odd, shouldn't be present", p));
                }
                cnt++;
            }

            bool passed = (cnt == ((dataSize + 1) / 2));
            if (!passed)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, ((dataSize + 1) / 2), passed));
        }

        private static void RunWhereTest2(int dataSize)
        {
            string methodFailed = string.Format("RunWhereTest2(dataSize = {0}) - async/pipeline:  FAILED.  ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Filter out odd elements.
            ParallelQuery<int> q = data.AsParallel().Where<int>(
                delegate (int x) { return (x % 2) == 0; });

            int cnt = 0;
            List<int> r = q.ToList<int>();
            foreach (int p in r)
            {
                if ((p % 2) != 0)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: {0} is odd, shouldn't be present", p));
                }
                cnt++;
            }

            bool passed = (cnt == ((dataSize + 1) / 2));
            if (!passed)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, ((dataSize + 1) / 2), passed));
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static void RunIndexedWhereTest1(int dataSize)
        {
            string methodFailed = string.Format("RunIndexedWhereTest1(dataSize = {0}) - async/pipelining: FAILED. ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Filter out elements where index isn't equal to the value (shouldn't filter any!).
            ParallelQuery<int> q = data.AsParallel().AsOrdered().Where<int>(
                delegate (int x, int idx) { return (x == idx); });

            int cnt = 0;
            foreach (int p in q)
            {
                if (p != cnt)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p));
                }
                cnt++;
            }

            if (cnt != dataSize)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED", cnt, dataSize));
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static void RunIndexedWhereTest2(int dataSize)
        {
            string methodFailed = string.Format("RunIndexedWhereTest2(dataSize = {0}) - not pipelined:  FAILED.  ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Filter out elements where index isn't equal to the value (shouldn't filter any!).
            ParallelQuery<int> q = data.AsParallel().AsOrdered().Where<int>(
                delegate (int x, int idx) { return (x == idx); });

            int cnt = 0;
            List<int> r = q.ToList<int>();
            foreach (int p in r)
            {
                if (p != cnt)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p));
                }
                cnt++;
            }

            if (cnt != dataSize)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED.", cnt, dataSize));
        }
    }
}
