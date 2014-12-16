// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class RangeRepeatTests
    {
        //
        // Range
        //
        [Fact]
        public static void RunRangeTests()
        {
            RunRangeTest1(0, 100);
            RunRangeTest1(50, 75);
            RunRangeTest1(-10, 1033);
            RunRangeTest1(100, 0);
            RunRangeTest1(int.MaxValue, 1);
            RunRangeTest1(int.MaxValue - 9, 10);
        }

        private static void RunRangeTest1(int from, int count)
        {
            string method = string.Format("RunRangeTest1(from={0}, count={1}):  FAILED on the given inputs.", from, count);

            Assert.True(Enumerable.Range(from, count).SequenceEqual(
               ParallelEnumerable.Range(from, count).AsSequential().OrderBy(i => i)),

               "Enumerable and ParallelEnumerable AsSequential.ORderBy is not equal.");
            Assert.True(Enumerable.Range(from, count).SequenceEqual(
               ParallelEnumerable.Range(from, count).Select(i => i).AsSequential().OrderBy(i => i)),
               "Enumerable and ParallelEnumerable Select.ORderBy is not equal.");

            Assert.True(Enumerable.Range(from, count).SequenceEqual(
               ParallelEnumerable.Range(from, count).AsSequential().OrderBy(i => i)),
               "Enumerable and ParallelEnumerable AsSequential.ORderBy is not equal.");

            Assert.True(Enumerable.Range(from, count).Take(count / 2).SequenceEqual(
               ParallelEnumerable.Range(from, count).Take(count / 2).AsSequential().OrderBy(i => i)),
               "Enumerable and ParallelEnumerable Take.ORderBy is not equal.");

            Assert.True(Enumerable.Range(from, count).Skip(count / 2).SequenceEqual(
               ParallelEnumerable.Range(from, count).Skip(count / 2).AsSequential().OrderBy(i => i)),
               "Enumerable and ParallelEnumerable Skip.ORderBy is not equal.");

            Assert.Equal(Enumerable.Range(from, count).Skip(count / 2).FirstOrDefault(),
                ParallelEnumerable.Range(from, count).Skip(count / 2).FirstOrDefault());

            Assert.Equal(Enumerable.Range(from, count).Take(count / 2).LastOrDefault(),
               ParallelEnumerable.Range(from, count).Take(count / 2).LastOrDefault());
        }

        //
        // Repeat
        //
        [Fact]
        public static void RunRepeatTests()
        {
            RunRepeatTest1<int>(0, 10779, EqualityComparer<int>.Default);
            RunRepeatTest1<int>(1024 * 8, 10779, EqualityComparer<int>.Default);
            RunRepeatTest1<int>(1024 * 8, 10779, EqualityComparer<int>.Default);
            RunRepeatTest1<int>(73 * 7, 10779, EqualityComparer<int>.Default);
            RunRepeatTest1<int>(73 * 7, 0, EqualityComparer<int>.Default);
            RunRepeatTest1<object>(0, null, EqualityComparer<object>.Default);
            RunRepeatTest1<object>(1024 * 8, null, EqualityComparer<object>.Default);
            RunRepeatTest1<string>(1024 * 1024, "hello", EqualityComparer<string>.Default);

            //Random r = new Random(103);
            for (int i = 0; i < 8; i++)
            {
                RunRepeatTest1<string>(1024 * i, "hello", EqualityComparer<string>.Default);
            }
        }

        private static void RunRepeatTest1<T>(int count, T element, IEqualityComparer<T> cmp)
        {
            string methodFailed = string.Format("RunRepeatTest1<{0}>(count={1}, element={2})  --  used in a query:  FAILED. ", typeof(T), count, element);

            int cnt = 0;
            ParallelQuery<T> q = ParallelEnumerable.Repeat(element, count).Select<T, T>(
                delegate (T e) { return e; });
            foreach (T e in q)
            {
                cnt++;

                if (!cmp.Equals(e, element))
                {
                    Console.WriteLine(methodFailed + "  > Expected {0} but found {1} instead", element, e);
                }
            }

            ;
            if (cnt != count)
                Console.WriteLine(methodFailed + "  > Total should be {0} -- real total is {1}", count, cnt);
        }
    }
}
