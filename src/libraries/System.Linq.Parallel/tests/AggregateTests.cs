// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class AggregateTests
    {
        //
        // Aggregate
        //
        [Fact]
        public static void RunAggregationTests()
        {
            RunAggregationTest1_Sum1(1024);
            RunAggregationTest1_Sum2(1024);
            RunAggregationTest1_Sum3(1024);
            RunAggregationTest1_Sum4(1024);
            RunAggregationTest2(16);
        }

        private static void RunAggregationTest1_Sum1(int count)
        {
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int>(
                delegate (int x, int y) { return x + y; });
            if (realSum != expectSum)
                Assert.True(false, string.Format("RunAggregationTest1_Sum1: FAILED. (count: {2}):  FAIL  > Expect: {0}, real: {1}", expectSum, realSum, count));
        }

        private static void RunAggregationTest1_Sum2(int count)
        {
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int, int>(
                0, delegate (int x, int y) { return x + y; });

            if (realSum != expectSum)
                Assert.True(false, string.Format("RunAggregationTest1_Sum2(count={2}):  FAIL.  > Expect: {0}, real: {1}", expectSum, realSum, count));
        }

        private static void RunAggregationTest1_Sum3(int count)
        {
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int, int, int>(
                0, delegate (int x, int y) { return x + y; }, delegate (int x) { return x; });

            if (realSum != expectSum)
                Assert.True(false, string.Format("RunAggregationTest1_Sum3(count={2}): FAILED.  > Expect: {0}, real: {1}", expectSum, realSum, count));
        }

        private static void RunAggregationTest1_Sum4(int count)
        {
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int, IntWrapper, int>(
                () => new IntWrapper(0),
                (a, e) => { a.Value = a.Value + e; return a; },
                (a1, a2) => { a1.Value = a1.Value + a2.Value; return a1; },
                (a) => a.Value);

            if (realSum != expectSum)
                Assert.True(false, string.Format("RunAggregationTest1_Sum3(count={2}):  FAILED.  > Expect: {0}, real: {1}", expectSum, realSum, count));
        }

        private static void RunAggregationTest2(int count)
        {
            int[] arr = new int[count];
            int count1 = arr.AsParallel().Select(x => x).Aggregate<int, int>(0, (acc, x) => acc + 1);
            int count2 = arr.AsParallel().Select(x => x).Aggregate<int, int, int>(0, (acc, x) => acc + 1, res => res);

            if (count1 != arr.Length)
            {
                Assert.True(false, string.Format("RunAggregationTest2(count={2}): FAILED.  > Count1 expect: {0}, real: {1}", arr.Length, count1, count));
            }

            if (count2 != arr.Length)
            {
                Assert.True(false, string.Format("RunAggregationTest2(count={2}): FAILED.  > Count2 expect: {0}, real: {1}", arr.Length, count2, count));
            }
        }

        private class IntWrapper
        {
            public int Value { get; set; }
            public IntWrapper(int value)
            {
                Value = value;
            }
        }
    }
}
