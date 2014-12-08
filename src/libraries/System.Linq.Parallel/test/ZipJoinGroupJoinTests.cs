// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Test
{
    public class ZipJoinGroupJoinTests
    {
        //
        // Zip
        //
        [Fact]
        public static void RunZipTests()
        {
            RunZipTest1(0);
            RunZipTest1(1024 * 4);
        }

        private static void RunZipTest1(int dataSize)
        {
            int[] ints = new int[dataSize];
            int[] ints2 = new int[dataSize];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                ints2[i] = i;
            }

            ParallelQuery<Pair<int, int>> q = ints.AsParallel().Zip<int, int, Pair<int, int>>(ints2.AsParallel(), (i, j) => new Pair<int, int>(i, j));
            Pair<int, int>[] p = q.ToArray<Pair<int, int>>();

            foreach (Pair<int, int> x in p)
            {
                if (x.First != x.Second)
                {
                    Assert.True(false, string.Format("RunZipTest1({2}): > Failed... {0} != {1}", x.First, x.Second, dataSize));
                }
            }
        }

        //
        // Join
        //

        [Fact]
        public static void RunJoinTests()
        {
            // Tiny, and empty, input sizes.
            RunJoinTest1(0, 0);
            RunJoinTest1(1, 0);
            RunJoinTest1(32, 0);
            RunJoinTest1(0, 1);
            RunJoinTest1(0, 32);
            RunJoinTest1(1, 1);
            RunJoinTest1(32, 1);
            RunJoinTest1(1, 32);

            // Normal input sizes.
            RunJoinTest1(32, 32);
            RunJoinTest1(1024 * 8, 1024);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunJoinTest1_LongRunning()
        {
            RunJoinTest1(1024, 1024 * 8);
            RunJoinTest1(1024 * 8, 1024 * 8);
            RunJoinTest1(1024 * 512, 1024 * 256);
            RunJoinTest1(1024 * 256, 1024 * 512);
            RunJoinTest1(1024 * 1024 * 2, 1024 * 1024);
        }

        [Fact]
        public static void RunJoinTest2()
        {
            // Tiny, and empty, input sizes.
            RunJoinTest2Core(0, 0);
            RunJoinTest2Core(1, 0);
            RunJoinTest2Core(32, 0);
            RunJoinTest2Core(0, 1);
            RunJoinTest2Core(0, 32);
            RunJoinTest2Core(1, 1);
            RunJoinTest2Core(32, 1);
            RunJoinTest2Core(1, 32);

            // Normal input sizes.
            RunJoinTest2Core(32, 32);
            RunJoinTest2Core(1024 * 8, 1024);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunJoinTest2_LongRunning()
        {
            RunJoinTest2Core(1024, 1024 * 8);
            RunJoinTest2Core(1024 * 8, 1024 * 8);
            RunJoinTest2Core(1024 * 512, 1024 * 256);
            RunJoinTest2Core(1024 * 256, 1024 * 512);
            RunJoinTest2Core(1024 * 1024 * 2, 1024 * 1024);
        }

        [Fact]
        public static void RunJoinTest3()
        {
            RunJoinTest3Core(0, 0);
            RunJoinTest3Core(1, 0);
            RunJoinTest3Core(0, 1);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunJoinTest3_LongRunning()
        {
            RunJoinTest3Core(1024 * 8, 1024);
            RunJoinTest3Core(1024, 1024 * 8);
            RunJoinTest3Core(1024 * 1024 * 2, 1024 * 1024);
        }

        [Fact]
        public static void RunJoinTestWithWhere1()
        {
            RunJoinTestWithWhere1Core(0, 0);
            RunJoinTestWithWhere1Core(1024 * 8, 1024);
            RunJoinTestWithWhere1Core(1024, 1024 * 8);
        }

        [Fact]
        public static void RunJoinWithInnerJoinTest1()
        {
            RunJoinWithInnerJoinTest1Core(4, 4, 4, true);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunJoinWithInnerJoinTest1_LongRunning()
        {
            RunJoinWithInnerJoinTest1Core(1024 * 1024, 1024 * 4, 1024, true);
            RunJoinWithInnerJoinTest1Core(1024 * 1024, 1024 * 4, 1024, false);
        }

        [Fact]
        public static void RunJoinTestWithTakeWhile()
        {
            RunJoinTestWithTakeWhileCore(0, 0);
            RunJoinTestWithTakeWhileCore(1024 * 8, 1024);
            RunJoinTestWithTakeWhileCore(1024, 1024 * 8);
        }

        private static void RunJoinTest1(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunJoinTest1(outerSize = {0}, innerSize = {1}) - async/pipeline:  FAILED.", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            IEnumerable<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in joinResults)
            {
                cnt++;
                if (!(p.First == p.Second))
                {
                    Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second));
                }
            }

            // And that we have the correct number of elements.
            int outer = (outerSize + 7) / 8;
            int expect = outer >= innerSize ? innerSize : outer;
            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}", cnt, expect));
        }

        private static void RunJoinTest2Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunJoinTest2(outerSize = {0}, innerSize = {1}) - sync/ no pipeline: FAILED. ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            ParallelQuery<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            List<Pair<int, int>> r = joinResults.ToList<Pair<int, int>>();

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in r)
            {
                cnt++;
                if (!(p.First == p.Second))
                {
                    Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second));
                }
            }

            // And that we have the correct number of elements.
            //int expect = Math.Min((outerSize + 7) / 8, innerSize);
            int outer = (outerSize + 7) / 8;
            int expect = outer >= innerSize ? innerSize : outer;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}", cnt, expect));
        }

        private static void RunJoinTest3Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunJoinTest3(outerSize = {0}, innerSize = {1}) - FORALL: FAILED. ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            ParallelQuery<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            int cnt = 0;

            joinResults.ForAll<Pair<int, int>>(delegate (Pair<int, int> x) { Interlocked.Increment(ref cnt); });

            // Check that we have the correct number of elements.
            //int expect = Math.Min((outerSize + 7) / 8, innerSize);
            int outer = (outerSize + 7) / 8;
            int expect = outer >= innerSize ? innerSize : outer;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}", cnt, expect));
        }

        private static void RunJoinTestWithWhere1Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunJoinTestWithWhere1(outerSize = {0}, innerSize = {1}): FAILED.  ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            IEnumerable<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel().Where<int>(delegate (int x) { return (x % 16) == 0; }),
                identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in joinResults)
            {
                cnt++;
                if (!(p.First == p.Second))
                {
                    Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second));
                    break;
                }
            }

            // And that we have the correct number of elements.
            int outer = outerSize / 16;
            int minimum = outer >= innerSize ? innerSize : outer;

            int expect = outer > innerSize ? outerSize : minimum;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}", cnt, expect));
        }

        private static void RunJoinWithInnerJoinTest1Core(int dataSize, int innerLeftSize, int innerRightSize, bool innerAsLeft)
        {
            string methodFailed = string.Format("RunJoinWithInnerJoinTest1(dataSize={0},innerLeftSize={1},innerRightSize={2},innerAsLeft = {3}):  FAILED.  ",
                dataSize, innerLeftSize, innerRightSize, innerAsLeft);


            int[] data = new int[dataSize];

            int[] innerLeft = new int[innerLeftSize];
            int[] innerRight = new int[innerRightSize];

            for (int i = 0; i < data.Length; i++) data[i] = i;
            for (int i = 0; i < innerLeft.Length; i++) innerLeft[i] = i * 2;
            for (int i = 0; i < innerRight.Length; i++) innerRight[i] = i * 4;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };

            if (innerAsLeft)
            {
                IEnumerable<Pair<int, Pair<int, int>>> q =
                    data.AsParallel().Join<int, Pair<int, int>, int, Pair<int, Pair<int, int>>>(
                        innerLeft.AsParallel()
                            .Join<int, int, int, Pair<int, int>>(innerRight.AsParallel(), identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); }),
                        identityKeySelector, delegate (Pair<int, int> p) { return p.First; }, delegate (int x, Pair<int, int> p) { return new Pair<int, Pair<int, int>>(x, p); });

                int cnt = 0;
                foreach (Pair<int, Pair<int, int>> x in q)
                {
                    cnt++;
                    if (!(x.First == x.Second.First && x.Second.First == x.Second.Second))
                    {
                        Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} // ( {1} // {2} )", x.First, x.Second.First, x.Second.Second));
                    }
                }
            }
            else
            {
                IEnumerable<Pair<Pair<int, int>, int>> q = innerLeft.AsParallel()
                    .Join(innerRight.AsParallel(), identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); })
                    .Join<Pair<int, int>, int, int, Pair<Pair<int, int>, int>>(
                        data.AsParallel(), delegate (Pair<int, int> p) { return p.First; }, identityKeySelector, delegate (Pair<int, int> p, int x) { return new Pair<Pair<int, int>, int>(p, x); });

                int cnt = 0;
                foreach (Pair<Pair<int, int>, int> x in q)
                {
                    cnt++;
                    if (!(x.First.First == x.Second && x.First.First == x.First.Second))
                    {
                        Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- ( {0} // {1} ) // {2}", x.First.First, x.First.Second, x.Second));
                    }
                }
            }
            // @TODO: validate xcxount.

            //int expect = (outerSize/8) > innerSize ? outerSize : Math.Min(outerSize/8, innerSize);
            //passed &= cnt == expect;
            //Assert.True(false, string.Format("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed));
        }

        //
        //  Running a join followed by a TakeWhile will ensure order preservation code paths are hit
        //  in combination with hash partitioning.
        //
        private static void RunJoinTestWithTakeWhileCore(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunJoinTestWithTakeWhile(outerSize = {0}, innerSize = {1}): FAILED. ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            ParallelQuery<Pair<int, int>> results = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                    right.AsParallel().Where<int>(delegate (int x) { return (x % 16) == 0; }),
                    identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); }).TakeWhile<Pair<int, int>>((x) => true);

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in results)
            {
                cnt++;
                if (!(p.First == p.Second))
                {
                    Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second));
                }
            }

            // And that we have the correct number of elements.
            //int expect = (outerSize / 16) > innerSize ? outerSize : Math.Min(outerSize / 16, innerSize);
            int outer = outerSize / 16;
            int minimum = outer >= innerSize ? innerSize : outer;
            int expect = outer > innerSize ? outerSize : minimum;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}:", cnt, expect));
        }

        //
        // GroupJoin
        //
        [Fact]
        public static void RunGroupJoinTest1()
        {
            // Tiny, and empty, input sizes.
            RunGroupJoinTest1Core(0, 0);
            RunGroupJoinTest1Core(1, 0);
            RunGroupJoinTest1Core(32, 0);
            RunGroupJoinTest1Core(0, 1);
            RunGroupJoinTest1Core(0, 32);
            RunGroupJoinTest1Core(1, 1);
            RunGroupJoinTest1Core(32, 1);
            RunGroupJoinTest1Core(1, 32);

            // Normal input sizes.
            RunGroupJoinTest1Core(32, 32);
            RunGroupJoinTest1Core(1024 * 8, 1024);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunGroupJoinTest1_LongRunning()
        {
            RunGroupJoinTest1Core(1024, 1024 * 8);
            RunGroupJoinTest1Core(1024 * 8, 1024 * 8);
            RunGroupJoinTest1Core(1024 * 512, 1024 * 256);
            RunGroupJoinTest1Core(1024 * 256, 1024 * 512);
            RunGroupJoinTest1Core(1024 * 1024 * 2, 1024 * 1024);
        }

        [Fact]
        public static void RunGroupJoinTest2()
        {
            // Tiny, and empty, input sizes.
            RunGroupJoinTest2Core(0, 0);
            RunGroupJoinTest2Core(1, 0);
            RunGroupJoinTest2Core(32, 0);
            RunGroupJoinTest2Core(0, 1);
            RunGroupJoinTest2Core(0, 32);
            RunGroupJoinTest2Core(1, 1);
            RunGroupJoinTest2Core(32, 1);
            RunGroupJoinTest2Core(1, 32);

            // Normal input sizes.
            RunGroupJoinTest2Core(32, 32);
            RunGroupJoinTest2Core(1024 * 8, 1024);
        }

        // To-do: Re-enable this long-running test as an outer-loop test
        // [Fact]
        public static void RunGroupJoinTest2_LongRunning()
        {
            RunGroupJoinTest2Core(1024, 1024 * 8);
            RunGroupJoinTest2Core(1024 * 8, 1024 * 8);
            RunGroupJoinTest2Core(1024 * 512, 1024 * 256);
            RunGroupJoinTest2Core(1024 * 256, 1024 * 512);
            RunGroupJoinTest2Core(1024 * 1024 * 2, 1024 * 1024);
        }

        private static void RunGroupJoinTest1Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunGroupJoinTest1(outerSize = {0}, innerSize = {1}) - async/pipeline:  FAILED.  ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            IEnumerable<Pair<int, IEnumerable<int>>> joinResults = left.AsParallel().GroupJoin<int, int, int, Pair<int, IEnumerable<int>>>(
                right.AsParallel(), identityKeySelector, identityKeySelector,
                delegate (int x, IEnumerable<int> y) { return new Pair<int, IEnumerable<int>>(x, y); });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, IEnumerable<int>> p in joinResults)
            {
                foreach (int p2 in p.Second)
                {
                    cnt++;
                    if (!(p.First == p2))
                    {
                        Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} != {1}", p.First, p2));
                    }
                }
            }

            // And that we have the correct number of elements.
            //int expect = Math.Min((outerSize + 7) / 8, innerSize);
            int outer = (outerSize + 7) / 8;
            int expect = outer >= innerSize ? innerSize : outer;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}", cnt, expect));
        }

        private static void RunGroupJoinTest2Core(int outerSize, int innerSize)
        {
            string methodFailed = string.Format("RunGroupJoinTest2(outerSize = {0}, innerSize = {1}) - sync/ no pipeline:  FAILED. ", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };
            ParallelQuery<Pair<int, IEnumerable<int>>> joinResults = left.AsParallel().GroupJoin<int, int, int, Pair<int, IEnumerable<int>>>(
                right.AsParallel(), identityKeySelector, identityKeySelector,
                delegate (int x, IEnumerable<int> y) { return new Pair<int, IEnumerable<int>>(x, y); });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            List<Pair<int, IEnumerable<int>>> r = joinResults.ToList<Pair<int, IEnumerable<int>>>();

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, IEnumerable<int>> p in r)
            {
                foreach (int p2 in p.Second)
                {
                    cnt++;
                    if (!(p.First == p2))
                    {
                        Assert.True(false, string.Format(methodFailed + "  > *ERROR: pair members not equal -- {0} != {1}", p.First, p2));
                    }
                }
            }

            // And that we have the correct number of elements.
            int outer = (outerSize + 7) / 8;
            int expect = outer >= innerSize ? innerSize : outer;

            if (cnt != expect)
                Assert.True(false, string.Format(methodFailed + "  > Saw expected count? Saw = {0}, expect = {1}", cnt, expect));
        }

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
    }
}
