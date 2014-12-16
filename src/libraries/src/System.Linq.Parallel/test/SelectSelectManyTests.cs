// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class SelectSelectManyTests
    {
        //
        // Select
        //

        [Fact]
        public static void RunSelectTests()
        {
            RunSelectTest1(0);
            RunSelectTest1(1);
            RunSelectTest1(32);
            RunSelectTest1(1024);
            RunSelectTest1(1024 * 2);

            RunSelectTest2(0);
            RunSelectTest2(1);
            RunSelectTest2(32);
            RunSelectTest2(1024);
            RunSelectTest2(1024 * 2);
        }

        [Fact]
        public static void RunIndexedSelectTest()
        {
            RunIndexedSelectTest1(0);
            RunIndexedSelectTest1(1);
            RunIndexedSelectTest1(32);

            RunIndexedSelectTest2(0);
            RunIndexedSelectTest2(1);
            RunIndexedSelectTest2(32);
        }

        [Fact]
        [OuterLoop]
        public static void RunIndexedSelectTest_LongRunning()
        {
            RunIndexedSelectTest1(1024);
            RunIndexedSelectTest1(1024 * 2);
            RunIndexedSelectTest1(1024 * 1024 * 4);

            RunIndexedSelectTest2(1024);
            RunIndexedSelectTest2(1024 * 2);
            RunIndexedSelectTest2(1024 * 1024 * 4);
        }

        private static void RunSelectTest1(int dataSize)
        {
            string methodFailed = string.Format("RunSelectTest1(dataSize = {0}) - async/pipeline:  FAILED. ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Select the square. We will validate it during results.
            IEnumerable<Pair<int, int>> q = data.AsParallel().Select<int, Pair<int, int>>(
                delegate (int x) { return new Pair<int, int>(x, x * x); });

            int cnt = 0;
            foreach (Pair<int, int> p in q)
            {
                if (p.Second != p.First * p.First)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second, p.First, p.First * p.First));
                }
                cnt++;
            }

            if (cnt != dataSize)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED", cnt, dataSize));
        }

        private static void RunSelectTest2(int dataSize)
        {
            string methodFailed = string.Format("RunSelectTest2(dataSize = {0}) - NO pipelining:  FAILED. ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Select the square. We will validate it during results.
            ParallelQuery<Pair<int, int>> q = data.AsParallel().Select<int, Pair<int, int>>(
                delegate (int x) { return new Pair<int, int>(x, x * x); });

            int cnt = 0;
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();
            foreach (Pair<int, int> p in r)
            {
                if (p.Second != p.First * p.First)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second, p.First, p.First * p.First)); // plz
                }
                cnt++;
            }

            if (cnt != dataSize)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED", cnt, dataSize));
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static void RunIndexedSelectTest1(int dataSize)
        {
            string methodFailed = string.Format("RunIndexedSelectTest1(dataSize = {0}) - async/pipelining: FAILED. ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Select the square. We will validate it during results.
            IEnumerable<Pair<int, Pair<int, int>>> q = data.AsParallel().AsOrdered().Select<int, Pair<int, Pair<int, int>>>(
                delegate (int x, int idx)
                {
                    return new Pair<int, Pair<int, int>>(x, new Pair<int, int>(idx, x * x));
                });

            int cnt = 0;
            foreach (Pair<int, Pair<int, int>> p in q)
            {
                if (p.Second.First != cnt)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p.Second.First));
                }

                if (p.First != p.Second.First)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: expected element value {0} to equal index {1}", p.First, p.Second.First));
                }

                if (p.Second.Second != p.First * p.First)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second.Second, p.First, p.First * p.First));
                }
                cnt++;
            }

            if (cnt != dataSize)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED", cnt, dataSize));
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static void RunIndexedSelectTest2(int dataSize)
        {
            string methodFailed = string.Format("RunIndexedSelectTest2(dataSize = {0}) - NO pipelining: FAILED.  ", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            // Select the square. We will validate it during results.
            ParallelQuery<Pair<int, Pair<int, int>>> q = data.AsParallel().AsOrdered().Select<int, Pair<int, Pair<int, int>>>(
                delegate (int x, int idx)
                {
                    return new Pair<int, Pair<int, int>>(x, new Pair<int, int>(idx, x * x));
                });

            int cnt = 0;
            List<Pair<int, Pair<int, int>>> r = q.ToList<Pair<int, Pair<int, int>>>();
            foreach (Pair<int, Pair<int, int>> p in r)
            {
                if (p.Second.First != cnt)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p.Second.First));
                }

                if (p.First != p.Second.First)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: expected element value {0} to equal index {1}", p.First, p.Second.First));
                }

                if (p.Second.Second != p.First * p.First)
                {
                    Assert.True(false, string.Format(methodFailed + "  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second.Second, p.First, p.First * p.First));
                }
                cnt++;
            }

            if (cnt != dataSize)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED", cnt, dataSize));
        }

        //
        // SelectMany
        //

        [Fact]
        public static void RunSelectManyTest1()
        {
            RunSelectManyTest1Core(0, 0);
            RunSelectManyTest1Core(1, 0);
            RunSelectManyTest1Core(0, 1);
            RunSelectManyTest1Core(1, 1);
            RunSelectManyTest1Core(32, 32);
        }

        [Fact]
        [OuterLoop]
        public static void RunSelectManyTest1_LongRunning()
        {
            RunSelectManyTest1Core(1024 * 2, 1024);
            RunSelectManyTest1Core(1024, 1024 * 2);
            RunSelectManyTest1Core(1024 * 2, 1024 * 2);
        }

        [Fact]
        public static void RunSelectManyTest2()
        {
            RunSelectManyTest2Core(0, 0);
            RunSelectManyTest2Core(1, 0);
            RunSelectManyTest2Core(0, 1);
            RunSelectManyTest2Core(1, 1);
            RunSelectManyTest2Core(32, 32);
        }

        [Fact]
        [OuterLoop]
        public static void RunSelectManyTest2_LongRunning()
        {
            RunSelectManyTest2Core(1024 * 2, 1024);
            RunSelectManyTest2Core(1024, 1024 * 2);
            RunSelectManyTest2Core(1024 * 2, 1024 * 2);
        }

        [Fact]
        public static void RunSelectManyTest3()
        {
            RunSelectManyTest3Core(10, ParallelExecutionMode.Default);
            RunSelectManyTest3Core(123, ParallelExecutionMode.Default);
            RunSelectManyTest3Core(10, ParallelExecutionMode.ForceParallelism);
            RunSelectManyTest3Core(123, ParallelExecutionMode.ForceParallelism);
        }

        private static void RunSelectManyTest1Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunSelectManyTest1(outerSize = {0}, innerSize = {1}) - async/pipeline:  FAILED. ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            //Assert.True(false, string.Format("  > Invoking SelectMany of {0} outer elems with {1} inner elems", left.Length, right.Length));
            IEnumerable<int> results = left.AsParallel().AsOrdered().SelectMany<int, int, int>(x => right.AsParallel(), delegate (int x, int y) { return x + y; });

            // Just validate the count.
            int cnt = 0;
            foreach (int p in results)
                cnt++;

            int expect = outerSize * innerSize;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED", cnt, expect));
        }

        private static void RunSelectManyTest2Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunSelectManyTest2(outerSize = {0}, innerSize = {1}) - sync/ no pipeline: FAILED. ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            //Assert.True(false, string.Format("  > Invoking SelectMany of {0} outer elems with {1} inner elems", left.Length, right.Length));
            ParallelQuery<int> results = left.AsParallel().AsOrdered().SelectMany<int, int, int>(x => right.AsParallel(), delegate (int x, int y) { return x + y; });
            List<int> r = results.ToList<int>();

            // Just validate the count.
            int cnt = 0;
            foreach (int p in r)
                cnt++;

            int expect = outerSize * innerSize;
            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}: FAILED.", cnt, expect));
        }

        /// <summary>
        /// Tests OrderBy() followed by a SelectMany, where the outer input sequence
        /// contains duplicates.
        /// </summary>
        private static void RunSelectManyTest3Core(int size, ParallelExecutionMode mode)
        {
            int[] srcOuter = Enumerable.Repeat(0, size).ToArray();
            int[] srcInner = Enumerable.Range(0, size).ToArray();

            IEnumerable<int> query =
                srcOuter.AsParallel()
                .WithExecutionMode(mode)
                .OrderBy(x => x)
                .SelectMany(x => srcInner);

            int next = 0;
            foreach (var x in query)
            {
                if (x != next)
                {
                    Assert.True(false, string.Format("RunSelectManyTest3(size = {2}: FAILED. expected {0} got {1}", next, x, size));
                }
                next = (next + 1) % size;
            }
        }

        #region Helper Classes / Methods

        //-----------------------------------------------------------------------------------
        // A pair just wraps two bits of data into a single addressable unit. This is a
        // value type to ensure it remains very lightweight, since it is frequently used
        // with other primitive data types as well.
        //
        // Note: this class is another copy of the Pair<T, U> class defined in CommonDataTypes.cs.
        // For now, we have a copy of the class here, because we can't import the System.Linq.Parallel
        // namespace.
        //
        private struct Pair<T, U>
        {
            // The first and second bits of data.
            internal T m_first;
            internal U m_second;

            //-----------------------------------------------------------------------------------
            // A simple constructor that initializes the first/second fields.
            //

            public Pair(T first, U second)
            {
                m_first = first;
                m_second = second;
            }

            //-----------------------------------------------------------------------------------
            // Accessors for the left and right data.
            //

            public T First
            {
                get { return m_first; }
                set { m_first = value; }
            }

            public U Second
            {
                get { return m_second; }
                set { m_second = value; }
            }
        }
        #endregion
    }
}
