// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class ExceptConcatReverseTests
    {
        //
        // Except
        //

        [Fact]
        public static void RunExceptTest1()
        {
            RunExceptTest1Core(0, 0);
            RunExceptTest1Core(1, 0);
            RunExceptTest1Core(0, 1);
            RunExceptTest1Core(4, 4);
            RunExceptTest1Core(1024, 4);
            RunExceptTest1Core(4, 1024);
            RunExceptTest1Core(1024, 1024);
            RunExceptTest1Core(1024 * 4, 1024);
            RunExceptTest1Core(1024, 1024 * 4);
            RunExceptTest1Core(1024 * 1024, 1024 * 1024);
        }

        private static void RunExceptTest1Core(int leftDataSize, int rightDataSize)
        {
            string[] names1 = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };
            string[] names2 = new string[] { "balmer", "duffy", "gates", "essey", "crocker", "smith", "callahan", "jimbob", "beebop" };
            string method = string.Format("RunExceptTest1(leftSize={0}, rightSize={1}) - except of names", leftDataSize, rightDataSize);

            //Random r = new Random(33); // use constant seed for predictable test runs.
            string[] leftData = new string[leftDataSize];
            for (int i = 0; i < leftDataSize; i++)
            {
                int index = i % names1.Length;
                leftData[i] = names1[index];
            }
            string[] rightData = new string[rightDataSize];
            for (int i = 0; i < rightDataSize; i++)
            {
                int index = i % names2.Length;
                rightData[i] = names2[index];
            }
            // Just get the exception of thw two sets.
            ParallelQuery<string> q = leftData.AsParallel().Except<string>(rightData.AsParallel());

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  NotUnique: {0} is not unique, already seen (failure)", n));
                }
                // Ensure the data DOES NOT exist in the right source.
                if (Array.IndexOf(rightData, n) != -1)
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  FoundInRight: {0} found in the right data source, error", n));
                }

                seen.Add(n);
            }
        }

        [Fact]
        public static void RunOrderedExceptTest1()
        {
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));

                var output = data.AsParallel().AsOrdered().Except(Enumerable.Empty<int>().AsParallel()).ToArray();
                if (!Enumerable.Range(0, 4).SequenceEqual(output))
                {
                    Assert.True(false, string.Format("RunOrderedExceptTest1:  FAILED.  ** Incorrect output"));
                }
            }
        }

        //
        // Concat
        //

        [Fact]
        public static void RunConcatTest1()
        {
            // W/ pipelining.
            RunConcatTest1Core(0, 0);
            RunConcatTest1Core(0, 1);
            RunConcatTest1Core(1, 0);
            RunConcatTest1Core(1, 1);
            RunConcatTest1Core(1024, 1024);
            RunConcatTest1Core(0, 1024);
            RunConcatTest1Core(1024, 0);
            // @TODO: reenable this test when deadlock problem is solved.
            // RunConcatTest1(1023 * 1024, 1023 * 1024);
        }

        [Fact]
        public static void RunConcatTest2()
        {
            // W/out pipelining.
            RunConcatTest2Core(0, 0);
            RunConcatTest2Core(0, 1);
            RunConcatTest2Core(1, 0);
            RunConcatTest2Core(1, 1);
            RunConcatTest2Core(1024, 1024);
            RunConcatTest2Core(0, 1024);
            RunConcatTest2Core(1024, 0);
            RunConcatTest2Core(1023 * 1024, 1023 * 1024);
        }

        private static void RunConcatTest1Core(int leftSize, int rightSize)
        {
            string method = string.Format("RunConcatTest1(leftSize={0}, rightSize={1}) -- pipelined", leftSize, rightSize);

            int[] leftData = new int[leftSize];
            for (int i = 0; i < leftSize; i++) leftData[i] = i;
            int[] rightData = new int[rightSize];
            for (int i = 0; i < rightSize; i++) rightData[i] = i;

            ParallelQuery<int> q = leftData.AsParallel().AsOrdered().Concat(rightData.AsParallel());

            int cnt = 0;

            foreach (int x in q)
            {
                if (cnt < leftSize)
                {
                    if (x != leftData[cnt])
                    {
                        Assert.True(false, string.Format(method + "  > FAILED.  Expected element {0} to == {1} (from left)); got {2} instead",
                            cnt, leftData[cnt], x));
                    }
                }
                else
                {
                    if (x != rightData[cnt - leftSize])
                    {
                        Assert.True(false, string.Format(method + "  > FAILED.  Expected element {0} to == {1} (from right)); got {2} instead",
                            cnt, rightData[cnt - leftSize], x));
                    }
                }

                cnt++;
            }

            if (!(cnt == leftSize + rightSize))
            {
                Assert.True(false, string.Format(method + "  > FAILED.  Expect: {0}, real: {1}", leftSize + rightSize, cnt));
            }
        }

        private static void RunConcatTest2Core(int leftSize, int rightSize)
        {
            string method = string.Format("RunConcatTest2(leftSize={0}, rightSize={1}) -- w/out pipelining:  ", leftSize, rightSize);
            int[] leftData = new int[leftSize];
            for (int i = 0; i < leftSize; i++) leftData[i] = i;
            int[] rightData = new int[rightSize];
            for (int i = 0; i < rightSize; i++) rightData[i] = i;

            ParallelQuery<int> q = leftData.AsParallel().AsOrdered().Concat(rightData.AsParallel());
            List<int> r = q.ToList();

            int cnt = 0;

            foreach (int x in r)
            {
                if (cnt < leftSize)
                {
                    if (x != leftData[cnt])
                    {
                        Assert.True(false, string.Format(method + "  > FAILED.  Expected element {0} to == {1} (from left)); got {2} instead",
                            cnt, leftData[cnt], x));
                    }
                }
                else
                {
                    if (x != rightData[cnt - leftSize])
                    {
                        Assert.True(false, string.Format(method + "  > FAILED.  Expected element {0} to == {1} (from right)); got {2} instead",
                            cnt, rightData[cnt - leftSize], x));
                    }
                }

                cnt++;
            }

            if (!(cnt == leftSize + rightSize))
            {
                Assert.True(false, string.Format(method + "  > FAILED.  Expect: {0}, real: {1}", leftSize + rightSize, cnt));
            }
        }

        //
        // Reverse
        //

        [Fact]
        public static void RunReverseTest1()
        {
            RunReverseTest1Core(0);
            RunReverseTest1Core(33);
            RunReverseTest1Core(1024);
            RunReverseTest1Core(1024 * 1024);
        }

        [Fact]
        public static void RunReverseTest2_Range()
        {
            RunReverseTest2_RangeCore(0, 0);
            RunReverseTest2_RangeCore(0, 33);
            RunReverseTest2_RangeCore(33, 33);
            RunReverseTest2_RangeCore(33, 66);
        }

        [Fact]
        [OuterLoop]
        public static void RunReverseTest2_Range_LongRunning()
        {
            RunReverseTest2_RangeCore(0, 1024 * 3);
            RunReverseTest2_RangeCore(1024, 1024 * 1024 * 3);
        }

        private static void RunReverseTest1Core(int size)
        {
            string method = string.Format("RunReverseTest1(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().AsOrdered().Reverse();

            int cnt = 0;
            int last = size;
            foreach (int x in q)
            {
                if (x != last - 1)
                {
                    Assert.True(false, string.Format(method + "  > FAILED. Elems not in decreasing order: this={0}, last={1}", x, last));
                }
                last = x;
                cnt++;
            }

            if (cnt != size)
            {
                Assert.True(false, string.Format(method + "  > Expect: {0}, real: {1}", size, cnt));
            }
        }

        private static void RunReverseTest2_RangeCore(int start, int count)
        {
            string method = string.Format("RunReverseTest2_Range(start={0}, count={1})", start, count);

            ParallelQuery<int> q = ParallelEnumerable.Range(start, count).AsOrdered().Reverse();

            int seen = 0;
            int last = (start + count);
            foreach (int x in q)
            {
                if (x != last - 1)
                {
                    Assert.True(false, string.Format(method + "  > FAILED.  Elems not in decreasing order: this={0}, last={1}", x, last));
                }
                last = x;
                seen++;
            }

            if (seen != count)
            {
                Assert.True(false, string.Format(method + "  > FAILED.  Expect: {0}, real: {1}", count, seen));
            }
        }
    }
}
