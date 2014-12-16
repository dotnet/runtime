// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class TakeSkipTests
    {
        //
        // Take and Skip
        //

        [Fact]
        public static void RunTakeTest1()
        {
            // TakeWhile:
            RunTakeTest1Core(16, 8);
            RunTakeTest1Core(1024, 32);
            RunTakeTest1Core(1024, 1024);
            RunTakeTest1Core(1024, 0);
            RunTakeTest1Core(0, 32);
            RunTakeTest1Core(1024 * 1024, 1024 * 64);
        }

        [Fact]
        public static void RunTakeTest2_Range()
        {
            // TakeWhile:
            RunTakeTest2_RangeCore(0, 0, 0);
            RunTakeTest2_RangeCore(0, 1, 1);
            RunTakeTest2_RangeCore(0, 16, 0);
            RunTakeTest2_RangeCore(0, 16, 8);
            RunTakeTest2_RangeCore(0, 16, 16);
            RunTakeTest2_RangeCore(16, 16, 0);
            RunTakeTest2_RangeCore(16, 16, 8);
            RunTakeTest2_RangeCore(16, 16, 16);
            RunTakeTest2_RangeCore(0, 1024 * 1024, 0);
            RunTakeTest2_RangeCore(0, 1024 * 1024, 1024);
            RunTakeTest2_RangeCore(0, 1024 * 1024, 1024 * 1024);
        }

        [Fact]
        public static void RunSkipTest1()
        {
            // SkipWhile:
            RunSkipTest1Core(1024, 32);
            RunSkipTest1Core(1024, 1024);
            RunSkipTest1Core(1024, 0);
            RunSkipTest1Core(0, 32);
            RunSkipTest1Core(32, 32);
            RunSkipTest1Core(1024 * 1024, 1024 * 64);
        }

        [Fact]
        public static void RunSkipTest2_Range()
        {
            // SkipWhile:
            RunSkipTest2_RangeCore(0, 0, 0);
            RunSkipTest2_RangeCore(0, 1, 1);
            RunSkipTest2_RangeCore(0, 16, 0);
            RunSkipTest2_RangeCore(0, 16, 8);
            RunSkipTest2_RangeCore(0, 16, 16);
            RunSkipTest2_RangeCore(16, 16, 0);
            RunSkipTest2_RangeCore(16, 16, 8);
            RunSkipTest2_RangeCore(16, 16, 16);
            RunSkipTest2_RangeCore(0, 1024 * 1024, 0);
            RunSkipTest2_RangeCore(0, 1024 * 1024, 1024);
            RunSkipTest2_RangeCore(0, 1024 * 1024, 1024 * 1024);
        }

        //
        // Take
        //

        private static void RunTakeTest1Core(int size, int take)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().Take(take);

            int seen = 0;
            int expect = size >= take ? take : size;

            foreach (int x in q)
            {
                if (x >= expect)
                {
                    Assert.True(false, string.Format("RunTakeTest1(size={2}, take={3})  > FAILED: expected no values >= {0} (saw {1})", expect, x, size, take));
                }
                seen++;
            }

            if (expect != seen)
                Assert.True(false, string.Format("RunTakeTest1(size={0}, take={1})  > FAILED. Expect: {2}, real: {3}", size, take, expect, seen));
        }

        private static void RunTakeTest2_RangeCore(int start, int count, int take)
        {
            string methodFailed = string.Format("* RunTakeTest2_Range(start={0}, count={1}, take={2}):", start, count, take);

            ParallelQuery<int> q = ParallelEnumerable.Range(start, count).Take(take);

            int seen = 0;
            int expect = count >= take ? take : count;
            foreach (int x in q)
            {
                if (x >= (start + expect))
                {
                    Assert.True(false, string.Format(methodFailed + "  > FAILED: expected no values >= {0} (saw {1})", (start + expect), x));
                }
                seen++;
            }

            if (expect != seen)
                Assert.True(false, string.Format(methodFailed + "  >  FAILED.  Expect: {0}, real: {1}", expect, seen));
        }

        //
        // Skip
        //

        private static void RunSkipTest1Core(int size, int skip)
        {
            string methodFailed = string.Format("RunSkipTest1(size={0}, count={1}):", size, skip);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().Skip(skip);

            int seen = 0;
            int difference = size - skip;
            int expect = 0 >= difference ? 0 : difference;

            foreach (int x in q)
            {
                if (skip > x)
                {
                    Assert.True(false, string.Format(methodFailed + "  > FAILED: expected no values < {0} (saw {1})", skip, x));
                }
                seen++;
            }


            if (expect != seen)
                Assert.True(false, string.Format(methodFailed + "  > FAILED. Expect: {0}, real: {1}", expect, seen));
        }

        private static void RunSkipTest2_RangeCore(int start, int count, int skip)
        {
            string methodFailed = string.Format("RunSkipTest2_Range(start={0}, count={1}, skip={2}):", start, count, skip);

            ParallelQuery<int> q = ParallelEnumerable.Range(start, count).Skip(skip);

            int seen = 0;
            int difference = count - skip;
            int expect = 0 >= difference ? 0 : difference;

            foreach (int x in q)
            {
                if ((skip + start) > x)
                {
                    Assert.True(false, string.Format(methodFailed + "  > FAILED: expected no values < {0} (saw {1})", skip, x));
                }
                seen++;
            }

            if (expect != seen)
                Assert.True(false, string.Format(methodFailed + "  > *FAIL* Expect: {0}, real: {1}", expect, seen));
        }
    }
}
