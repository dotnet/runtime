// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Test
{
    public class ExceptionAndParallelEnumerableExceptionTests
    {
        //
        // exceptions...
        //

        [Fact]
        public static void RunExceptionTestSync1()
        {
            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate (int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            Assert.Throws<AggregateException>(() =>
            {
                List<int> aa = q.ToList<int>();
            });
        }

        [Fact]
        public static void RunExceptionTestAsync1()
        {
            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate (int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            Assert.Throws<AggregateException>(() =>
            {
                foreach (int y in q) { }
            });
        }

        //
        // ParallelEnumerable Exceptions
        //
        [Fact]
        public static void RunParallelEnumerableExceptionsTests()
        {
            int[] ints = new int[0];
            ParallelQuery<int> pquery = ints.AsParallel();

            //With*
            Assert.Throws<ArgumentException>(() => ParallelEnumerable.WithExecutionMode(pquery, (ParallelExecutionMode)100));
            Assert.Throws<ArgumentException>(() => ParallelEnumerable.WithMergeOptions(pquery, (ParallelMergeOptions)100));
        }
    }
}
