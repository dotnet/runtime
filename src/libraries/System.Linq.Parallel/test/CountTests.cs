// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class CountTests
    {
        //
        // LongCount
        //
        [Fact]
        public static void RunLongCountTests()
        {
            RunLongCountTest1(0);
            RunLongCountTest1(1);
            RunLongCountTest1(1024);
            RunLongCountTest1(1024 * 1024);
            RunLongCountTest2(0);
            RunLongCountTest2(1);
            RunLongCountTest2(1024);
            RunLongCountTest2(1024 * 1024);
        }

        private static void RunLongCountTest1(long count)
        {
            int[] ints = new int[count];
            long returnedCount = ints.AsParallel().LongCount();

            if (!count.Equals(returnedCount))
                Assert.True(false, string.Format("RRunLongCountTest1:  FAILED.   >Expected {0}, actual {1}", count, returnedCount));
        }

        private static void RunLongCountTest2(long count)
        {
            int[] ints = new int[count];
            long returnedCount = ints.AsParallel().LongCount(i => i == 0);

            if (!count.Equals(returnedCount))
                Assert.True(false, string.Format("RRunLongCountTest2:  FAILED.   >Expected {0}, actual {1}", count, returnedCount));

            Assert.Throws<ArgumentNullException>(() => ints.AsParallel().LongCount(null));
        }
    }
}
