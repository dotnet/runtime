// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class DegreeOfParallelismTests
    {
        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunDopTests()
        {
            RunDopTest(1, false);
            RunDopTest(4, false);
            RunDopTest(512, false);
            RunDopTest(513, true);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunDopBarrierTest()
        {
            RunDopBarrierTestCore(1);
            RunDopBarrierTestCore(4);
            RunDopBarrierTestCore(32);
            // These variations take way too long and hit the 100s timeout.
            //RunDopBarrierTest(64);
            //RunDopBarrierTest(512);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunDopPipeliningTest()
        {
            RunDopPipeliningTestCore(1000000, 1);
            RunDopPipeliningTestCore(1000000, 4);
            RunDopPipeliningTestCore(1000000, 64);
            RunDopPipeliningTestCore(1000000, 512);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunDopPipeliningTestSleep()
        {
            RunDopPipeliningTestSleepCore(1, 1);
            RunDopPipeliningTestSleepCore(4, 4);
            RunDopPipeliningTestSleepCore(32, 4);
            //Re-enable for perf testing
            //RunDopPipeliningTestSleep(64, 64);            
            //RunDopPipeliningTestSleep(512, 512);
        }

        //
        // A simple test to check whether PLINQ appears to work with a particular DOP
        //
        private static void RunDopTest(int dop, bool expectException)
        {
            int[] arr = Enumerable.Repeat(5, 1000).ToArray();

            int real = 0;

            try
            {
                real = arr.AsParallel().WithDegreeOfParallelism(dop)
                     .Select(x => 2 * x)
                     .Sum();

                int expect = arr.Length * 10;
                if (real != expect)
                {
                    Assert.True(false, string.Format("RunDopTest(dop={0},expectException={1}):  > Incorrect result: expected {2} got {3}",
                        dop, expectException, expect, real));
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                if (!expectException)
                    Assert.True(false, string.Format("ArgumentOutOfRangeException should not have been thrown."));
                else
                    return;
            }

            if (expectException)
                Assert.True(false, string.Format("ArgumentOutOfRangeException was expected."));
        }

        //
        // A simple test to verify that PLINQ will really create the desired number of tasks.
        //
        // Warning: the test will not complete until ThreadPool creates at least dop concurrent threads.
        // To avoid this issue, we call ThreadPool.SetMinThreads before running this test.
        //
        private static void RunDopBarrierTestCore(int dop)
        {
            Console.WriteLine("RunDopBarrierTest(dop={0}):  [Hangs on failure]", dop);
            Barrier barrier = new Barrier(dop);
            ParallelEnumerable.Range(0, dop).WithDegreeOfParallelism(dop).Select(x => { barrier.SignalAndWait(); return x; }).ToArray();
            // The test passed, since it did not hang
        }

        //
        // A test to verify that PLINQ pipelining works with a particular DOP
        //
        private static void RunDopPipeliningTestCore(int n, int dop)
        {
            var q = ParallelEnumerable.Range(0, n).WithDegreeOfParallelism(dop).Select(x => -x);
            var res = q.AsEnumerable().ToArray();
            Array.Sort(res);
            Array.Reverse(res);

            Assert.True(res.SequenceEqual(Enumerable.Range(0, n).Select(x => -x)),
                string.Format("RunDopPipeliningTest(n={0}, dop={1}):  FAILED. Query generated wrong output", n, dop));
        }

        //
        // A test to verify that PLINQ pipelining works with a particular DOP in a case that throttles the producers
        //
        private static void RunDopPipeliningTestSleepCore(int n, int dop)
        {
            var q = ParallelEnumerable.Range(0, n).WithDegreeOfParallelism(dop).Select(x =>
            {
                Task.WaitAll(Task.Delay(3000));
                return -x;
            });
            var res = q.AsEnumerable().ToArray();
            Array.Sort(res);
            Array.Reverse(res);

            Assert.True(res.SequenceEqual(Enumerable.Range(0, n).Select(x => -x)),
                string.Format("RunDopPipeliningTestSleep(n={0}, dop={1}):  FAILED.  Query generated wrong output.", n, dop));
        }
    }
}
