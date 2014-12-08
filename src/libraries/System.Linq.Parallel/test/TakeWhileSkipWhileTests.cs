// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class TakeWhileSkipWhileTests
    {
        //
        // TakeWhile and SkipWhile
        //

        [Fact]
        public static void RunTakeWhileTests()
        {
            // TakeWhile:
            RunTakeWhile_AllFalse(1024);
            RunTakeWhile_AllTrue(1024);
            RunTakeWhile_SomeTrues(1024, 512);
            RunTakeWhile_SomeTrues(1024, 0);
            RunTakeWhile_SomeTrues(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            RunTakeWhile_SomeTrues(1024, 1023);
            RunTakeWhile_SomeTrues(1024 * 1024, 1024 * 512);
            RunTakeWhile_SomeFalses(1024, 512);
            RunTakeWhile_SomeFalses(1024, 0);
            RunTakeWhile_SomeFalses(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            RunTakeWhile_SomeFalses(1024, 1023);
            RunTakeWhile_SomeFalses(1024 * 1024, 1024 * 512);
        }

        [Fact]
        public static void RunSkipWhileTests()
        {
            // SkipWhile:
            RunSkipWhile_AllFalse(1024);
            RunSkipWhile_AllTrue(1024);
            RunSkipWhile_SomeTrues(1024, 512);
            RunSkipWhile_SomeTrues(1024, 0);
            RunSkipWhile_SomeTrues(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            RunSkipWhile_SomeTrues(1024, 1023);
            RunSkipWhile_SomeTrues(1024 * 1024, 1024 * 512);
            RunSkipWhile_SomeTrues(1024, 512);
            RunSkipWhile_SomeTrues(1024, 0);
            RunSkipWhile_SomeTrues(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            RunSkipWhile_SomeTrues(1024, 1023);
            RunSkipWhile_SomeTrues(1024 * 1024, 1024 * 512);
        }

        //
        // TakeWhile
        //

        private static void RunTakeWhile_AllFalse(int size)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate (int x) { return false; });

            int count = 0;
            int expect = 0;

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
                Assert.Equal(expect, count);
        }

        private static void RunTakeWhile_AllTrue(int size)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate (int x) { return true; });

            int count = 0;
            int expect = size;

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
                Assert.Equal(expect, count);
        }

        private static void RunTakeWhile_SomeTrues(int size, params int[] truePositions)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate (int x) { return Array.IndexOf(truePositions, x) != -1; });

            int count = 0;

            // We expect TakeWhile to yield all elements up to (but not including) the smallest false
            // index in the array.
            int expect = 0;
            for (int i = 0; i < size; i++)
            {
                if (Array.IndexOf(truePositions, i) == -1)
                    break;
                expect++;
            }

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
            {
                Assert.Equal(expect, count);
            }
        }

        private static void RunTakeWhile_SomeFalses(int size, params int[] falsePositions)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate (int x) { return Array.IndexOf(falsePositions, x) == -1; });

            int count = 0;

            // We expect TakeWhile to yield all elements up to (but not including) the smallest false
            // index in the array.
            int expect = falsePositions[0];
            for (int i = 1; i < falsePositions.Length; i++)
            {
                expect = expect >= falsePositions[i] ? falsePositions[i] : expect;
            }
            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
            {
                Assert.Equal(expect, count);
            }
        }

        //
        // SkipWhile
        //

        private static void RunSkipWhile_AllFalse(int size)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate (int x) { return false; });

            int count = 0;
            int expect = size;

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
                Assert.Equal(expect, count);
        }

        private static void RunSkipWhile_AllTrue(int size)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate (int x) { return true; });

            int count = 0;
            int expect = 0;

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
                Assert.Equal(expect, count);
        }

        private static void RunSkipWhile_SomeTrues(int size, params int[] truePositions)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate (int x) { return Array.IndexOf(truePositions, x) != -1; });

            int count = 0;

            // We expect SkipWhile to yield all elements after (and including) the smallest false index in the array.
            int expect = 0;
            for (int i = 0; i < size; i++)
            {
                if (Array.IndexOf(truePositions, i) == -1)
                    break;
                expect++;
            }
            expect = size - expect;

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
            {
                Assert.Equal(expect, count);
            }
        }

        private static void RunSkipWhile_SomeFalses(int size, params int[] falsePositions)
        {
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate (int x) { return Array.IndexOf(falsePositions, x) == -1; });

            int count = 0;

            // We expect SkipWhile to yield all elements after (and including) the smallest false index in the array.
            int expect = falsePositions[0];
            for (int i = 1; i < falsePositions.Length; i++)
            {
                expect = expect >= falsePositions[i] ? falsePositions[i] : expect;
            }
            expect = size - expect;

            foreach (int x in q)
            {
                count++;
            }

            if (count != expect)
            {
                Assert.Equal(expect, count);
            }
        }
    }
}
