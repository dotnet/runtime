// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class BreakTests
    {
        [Theory]
        [InlineData(100, 10)]
        [InlineData(100, 20)]
        [InlineData(1000, 100)]
        [InlineData(1000, 200)]
        public static void TestFor_Break_Basic(int loopsize, int breakpoint)
        {
            var complete = new bool[loopsize];

            Parallel.For(0, loopsize, delegate(int i, ParallelLoopState ps)
            {
                complete[i] = true;
                if (i >= breakpoint) ps.Break();
                //Thread.Sleep(2);
            });

            // Should not be any omissions prior 
            // to break, and there should be some after.
            for (int i = 0; i <= breakpoint; i++)
            {
                Assert.True(complete[i], string.Format("TestForBreak:  Failed: incomplete at {0}, loopsize {1}, breakpoint {2}", i, loopsize, breakpoint));
            }

            bool result = true;
            for (int i = breakpoint + 1; i < loopsize; i++)
            {
                if (!complete[i])
                {
                    result = true;
                    break;
                }
            }

            Assert.True(result, "TestForBreak:  Failed: Could not detect any interruption of For-loop.");
        }

        [Theory]
        [InlineData(100, 10)]
        [InlineData(100, 20)]
        [InlineData(1000, 100)]
        [InlineData(1000, 200)]
        public static void TestFor_Break_64Bits(int loopsize, int breakpoint)
        {
            var complete = new bool[loopsize];

            // Throw a curveball here and loop from just-under-Int32.MaxValue to 
            // just-over-Int32.MaxValue.  Make sure that 64-bit indices are being
            // handled correctly.
            long loopbase = (long)Int32.MaxValue - 10;
            Parallel.For(loopbase, loopbase + loopsize, delegate(long i, ParallelLoopState ps)
            {
                complete[i - loopbase] = true;
                if ((i - loopbase) >= breakpoint) ps.Break();
                //Thread.Sleep(2);
            });

            // Should not be any omissions prior 
            // to break, and there should be some after.
            for (long i = 0; i <= breakpoint; i++)
            {
                Assert.True(complete[i], string.Format("TestFor64Break: Failed: incomplete at {0}, loopsize {1}, breakpoint {2}", i, loopsize, breakpoint));
            }

            bool result = false;
            for (long i = breakpoint + 1; i < loopsize; i++)
            {
                if (!complete[i])
                {
                    result = true;
                    break;
                }
            }
            
            Assert.True(result, "TestFor64Break:  Failed: Could not detect any interruption of For-loop.");
        }

        [Theory]
        [InlineData(500, 10)]
        [InlineData(500, 20)]
        [InlineData(1000, 100)]
        [InlineData(1000, 200)]
        public static void TestForEach_Break(int loopsize, int breakpoint)
        {
            var complete = new bool[loopsize];

            // NOTE: Make sure and use some collection that is NOT a list or an
            // array.  Lists/arrays will be essentially be passed through
            // Parallel.For() logic, which will make this test fail.
            var iqueue = new Queue<int>();
            for (int i = 0; i < loopsize; i++) iqueue.Enqueue(i);

            Parallel.ForEach(iqueue, delegate(int i, ParallelLoopState ps)
            {
                complete[i] = true;
                if (i >= breakpoint) ps.Break();
                //Thread.Sleep(2);
            });

            // Same rules as For-loop.  Should not be any omissions prior 
            // to break, and there should be some after.
            for (int i = 0; i <= breakpoint; i++)
            {
                Assert.True(complete[i], string.Format("TestForEachBreak(loopsize={0},breakpoint={1}):  Failed: incomplete at {2}", loopsize, breakpoint, i));
            }

            bool result = false;
            for (int i = breakpoint + 1; i < loopsize; i++)
            {
                if (!complete[i])
                {
                    result = true;
                    break;
                }
            }
            
            Assert.True(result, string.Format("TestForEachBreak(loopsize={0},breakpoint={1}): Failed: Could not detect any interruption of For-loop.", loopsize, breakpoint));

            // 
            // Now try it for OrderablePartitioner
            //
            var ilist = new List<int>();
            for (int i = 0; i < loopsize; i++)
            {
                ilist.Add(i);
                complete[i] = false;
            }
            OrderablePartitioner<int> mop = Partitioner.Create(ilist, true);
            Parallel.ForEach(mop, delegate(int item, ParallelLoopState ps, long index)
            {
                //break does not imply that the other iterations will not be run
                //http://msdn.microsoft.com/en-us/library/system.threading.tasks.parallelloopstate.break.aspx 
                //execute the test with high loop size and low break index
                complete[index] = true;
                if (index >= breakpoint) ps.Break();
                //Thread.Sleep(2);
            });

            for (int i = 0; i <= breakpoint; i++)
            {
                Assert.True(complete[i], string.Format("TestForEachBreak(loopsize={0},breakpoint={1}):  Failed: incomplete at {2}", loopsize, breakpoint, i));
            }

            result = false;
            for (int i = breakpoint + 1; i < loopsize; i++)
            {
                if (!complete[i])
                {
                    result = true;
                    break;
                }
            }
            
            Assert.True(result, string.Format("TestForEachBreak(loopsize={0},breakpoint={1}): Failed: Could not detect any interruption of For-loop.", loopsize, breakpoint));
        }
    }
}
