// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;
using System.Threading;

namespace Test
{
    public class ForAllTests
    {
        //
        // ForAll
        //
        [Fact]
        public static void RunForAllTests()
        {
            RunForAllTest1(0);
            RunForAllTest1(1024);
            RunForAllTest1(1024 * 8);
            RunForAllTest1(1024 * 1024 * 2);
        }

        private static void RunForAllTest1(int dataSize)
        {
            int[] left = new int[dataSize];
            for (int i = 0; i < dataSize; i++)
                left[i] = i + 1;

            long counter = 0;

            // Just ensure that an addition done in parallel (with interlocked ops) adds
            // up to the correct sum in the end, i.e. no item goes missing or gets added.

            left.AsParallel().AsOrdered().ForAll<int>(
                delegate (int x) { Interlocked.Add(ref counter, x); });

            long expect;
            checked
            {
                expect = (long)(((float)dataSize / 2) * (dataSize + 1));
            }
            bool passed = (counter == expect);
            if (!passed)
                Assert.True(false, string.Format("RunForAllTest1({2}) - over an array:  FAILED.  > Expected a sum of {0} - found {1}", expect, counter, dataSize));
        }
    }
}
