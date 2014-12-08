// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class FirstLastSingleDefaultIfEmptyTests
    {
        #region DefaultIfEmpty / First / Last / Single Tests

        //
        // DefaultIfEmpty
        //

        [Fact]
        public static void RunDefaultIfEmptyTest1()
        {
            for (int i = 0; i <= 33; i++)
            {
                RunDefaultIfEmptyTest1Core(i);
            }
            RunDefaultIfEmptyTest1Core(1024);
            RunDefaultIfEmptyTest1Core(1024 * 1024);
        }

        [Fact]
        public static void RunDefaultIfEmptyOrderByTest1()
        {
            RunDefaultIfEmptyOrderByTest1Core(0);
            RunDefaultIfEmptyOrderByTest1Core(1024);
            RunDefaultIfEmptyOrderByTest2(0);
            RunDefaultIfEmptyOrderByTest2(1024);
        }

        private static void RunDefaultIfEmptyTest1Core(int size)
        {
            string methodInf = string.Format("RunDefaultIfEmptyTest1(size={0})", size);

            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().DefaultIfEmpty();

            int cnt = 0;
            foreach (int x in q)
            {
                if (size == 0 && x != default(int))
                {
                    Assert.True(false, string.Format(methodInf + "  > FAILED.  Only element should be {0} for empty inputs: saw {1}", default(int), x));
                }
                cnt++;
            }

            int expect = size == 0 ? 1 : size;

            if (cnt != expect)
            {
                Assert.True(false, string.Format(methodInf + "  > Expect: {0}, real: {1}", expect, cnt));
            }
        }

        private static void RunDefaultIfEmptyOrderByTest1Core(int size)
        {
            string methodInf = string.Format("RunDefaultIfEmptyOrderByTest1(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().OrderBy<int, int>(x => x).DefaultIfEmpty();

            int cnt = 0;
            int last = -1;
            foreach (int x in q)
            {
                if (size == 0 && x != default(int))
                {
                    Assert.True(false, string.Format(methodInf + "  > FAILED.  Only element should be {0} for empty inputs: saw {1}", default(int), x));
                }
                if (x < last)
                {
                    Assert.True(false, string.Format(methodInf + "  > FAILED.  Sort wasn't processed correctly: curr = {0}, but last = {1}", x, last));
                }
                last = x;

                cnt++;
            }

            int expect = size == 0 ? 1 : size;
            if (cnt != expect)
            {
                Assert.True(false, string.Format(methodInf + "  > Expect: {0}, real: {1}", expect, cnt));
            }
        }

        private static void RunDefaultIfEmptyOrderByTest2(int size)
        {
            string methodInf = string.Format("RunDefaultIfEmptyOrderByTest2(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().DefaultIfEmpty().
                OrderBy<int, int>(e => e);

            int cnt = 0;
            int last = -1;
            foreach (int x in q)
            {
                if (size == 0 && x != default(int))
                {
                    Assert.True(false, string.Format(methodInf + "  > FAILED.  Only element should be {0} for empty inputs: saw {1}", default(int), x));
                }
                if (x < last)
                {
                    Assert.True(false, string.Format(methodInf + "  > FAILED.  Sort wasn't processed correctly: curr = {0}, but last = {1}", x, last));
                }
                last = x;
                cnt++;
            }

            int expect = size == 0 ? 1 : size;
            if (cnt != expect)
            {
                Assert.True(false, string.Format(methodInf + "  > FAILED.  Expect: {0}, real: {1}", expect, cnt));
            }
        }

        //
        // First and FirstOrDefault
        //

        [Fact]
        public static void RunFirstTest1()
        {
            RunFirstTest1Core(0, false);
            RunFirstTest1Core(1024, false);
            RunFirstTest1Core(1024 * 1024, false);
            RunFirstTest1Core(0, true);
            RunFirstTest1Core(1024, true);
            RunFirstTest1Core(1024 * 1024, true);
        }

        [Fact]
        public static void RunFirstOrDefaultTest1()
        {
            RunFirstOrDefaultTest1Core(0, false);
            RunFirstOrDefaultTest1Core(1024, false);
            RunFirstOrDefaultTest1Core(1024 * 1024, false);
            RunFirstOrDefaultTest1Core(0, true);
            RunFirstOrDefaultTest1Core(1024, true);
            RunFirstOrDefaultTest1Core(1024 * 1024, true);
        }

        private static void RunFirstTest1Core(int size, bool usePredicate)
        {
            string method = string.Format("RunFirstTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int predSearch = 1033;

            bool expectExcept = (size == 0 || (usePredicate && size <= predSearch));

            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate (int x) { return (x >= predSearch); };
                    q = ints.AsParallel().First(pred);
                }
                else
                {
                    q = ints.AsParallel().First();
                }

                if (expectExcept)
                {
                    Assert.True(false, string.Format(method + "  > Failure: Expected an exception, but didn't get one"));
                }

                int expectReturn = usePredicate ? predSearch : 0;
                if (q != expectReturn)
                {
                    Assert.True(false, string.Format(method + "  > Failed.  Expected return value of {0}, saw {1} instead", expectReturn, q));
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    Assert.True(false, string.Format(method + "  > Failure: Got exception, but didn't expect it  {0}", ioex));
                }
            }
        }

        private static void RunFirstOrDefaultTest1Core(int size, bool usePredicate)
        {
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i + 1;

            int predSearch = 1033;
            bool expectDefault = (size == 0 || (usePredicate && size <= predSearch));

            int q;
            if (usePredicate)
            {
                Func<int, bool> pred = delegate (int x) { return (x >= predSearch); };
                q = ints.AsParallel().FirstOrDefault(pred);
            }
            else
            {
                q = ints.AsParallel().FirstOrDefault();
            }

            int expectReturn = expectDefault ? 0 : (usePredicate ? predSearch : 1);
            if (q != expectReturn)
            {
                string method = string.Format("RunFirstOrDefaultTest1(size={0}, usePredicate={1})", size, usePredicate);
                Assert.True(false, string.Format(method + "  > FAILED.  Expected return value of {0}, saw {1} instead", expectReturn, q));
            }
        }

        //
        // Last and LastOrDefault
        //

        [Fact]
        public static void RunLastTest1()
        {
            RunLastTest1Core(0, false);
            RunLastTest1Core(1024, false);
            RunLastTest1Core(1024 * 1024, false);
            RunLastTest1Core(0, true);
            RunLastTest1Core(1024, true);
            RunLastTest1Core(1024 * 1024, true);
        }

        [Fact]
        public static void RunLastOrDefaultTest1()
        {
            RunLastOrDefaultTest1Core(0, false);
            RunLastOrDefaultTest1Core(1024, false);
            RunLastOrDefaultTest1Core(1024 * 1024, false);
            RunLastOrDefaultTest1Core(0, true);
            RunLastOrDefaultTest1Core(1024, true);
            RunLastOrDefaultTest1Core(1024 * 1024, true);
        }

        private static void RunLastTest1Core(int size, bool usePredicate)
        {
            string methodInf = string.Format("RunLastTest1(size={0}, usePredicate={1})", size, usePredicate);

            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int predLo = 1033;
            int predHi = 1050;

            bool expectExcept = (size == 0 || (usePredicate && size <= predLo));

            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate (int x) { return (x >= predLo && x <= predHi); };
                    q = ints.AsParallel().Last(pred);
                }
                else
                {
                    q = ints.AsParallel().Last();
                }

                if (expectExcept)
                {
                    Assert.True(false, string.Format(methodInf + "  > Failure: Expected an exception, but didn't get one"));
                }

                int sizeLessOne = size - 1;
                int minPredHiSize = predHi >= sizeLessOne ? sizeLessOne : predHi;

                int expectReturn = usePredicate ? minPredHiSize : sizeLessOne;
                if (q != expectReturn)
                {
                    Assert.True(false, string.Format(methodInf + "  > FAILED.  Expected return value of {0}, saw {1} instead", expectReturn, q));
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    Assert.True(false, string.Format(methodInf + "  > Failure: Got exception, but didn't expect it  {0}", ioex));
                }
            }
        }

        private static void RunLastOrDefaultTest1Core(int size, bool usePredicate)
        {
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i + 1;

            int predLo = 1033;
            int predHi = 1050;

            bool expectDefault = (size == 0 || (usePredicate && size <= (predLo + 1)));

            int q;
            if (usePredicate)
            {
                Func<int, bool> pred = delegate (int x) { return (x >= predLo && x <= predHi); };
                q = ints.AsParallel().LastOrDefault(pred);
            }
            else
            {
                q = ints.AsParallel().LastOrDefault();
            }

            int minPredHiSize = predHi >= size ? size : predHi;

            int expectReturn = expectDefault ? 0 : (usePredicate ? minPredHiSize : size);
            if (q != expectReturn)
            {
                string methodInf = string.Format("RunLastOrDefaultTest1(size={0}, usePredicate={1})", size, usePredicate);
                Assert.True(false, string.Format(methodInf + "  > Expected return value of {0}, saw {1} instead", expectReturn, q));
            }
        }

        //
        // Single and SingleOrDefault
        //

        [Fact]
        public static void RunSingleTest1()
        {
            RunSingleTest1Core(0, false);
            RunSingleTest1Core(1, false);
            RunSingleTest1Core(1024, false);
            RunSingleTest1Core(1024 * 1024, false);
            RunSingleTest1Core(0, true);
            RunSingleTest1Core(1, true);
            RunSingleTest1Core(1024, true);
            RunSingleTest1Core(1024 * 1024, true);
        }

        [Fact]
        public static void RunSingleOrDefaultTest1()
        {
            RunSingleOrDefaultTest1Core(0, false);
            RunSingleOrDefaultTest1Core(1, false);
            RunSingleOrDefaultTest1Core(1024, false);
            RunSingleOrDefaultTest1Core(1024 * 1024, false);
            RunSingleOrDefaultTest1Core(0, true);
            RunSingleOrDefaultTest1Core(1, true);
            RunSingleOrDefaultTest1Core(1024, true);
            RunSingleOrDefaultTest1Core(1024 * 1024, true);
        }

        private static void RunSingleTest1Core(int size, bool usePredicate)
        {
            string methodInfo = string.Format("RunSingleTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int predNum = 1023;

            bool expectExcept = usePredicate ? predNum != (size - 1) : size != 1;

            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate (int x) { return x >= predNum; };
                    q = ints.AsParallel().Single(pred);
                }
                else
                {
                    q = ints.AsParallel().Single();
                }

                if (expectExcept)
                {
                    Assert.True(false, string.Format(methodInfo + "  > Failure: Expected an exception, but didn't get one"));
                }
                else
                {
                    int expectReturn = usePredicate ? predNum : 0;
                    if (q != expectReturn)
                    {
                        Assert.True(false, string.Format(methodInfo + "  > FAILED. Expected return value of {0}, saw {1} instead", expectReturn, q));
                    }
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    Assert.True(false, string.Format(methodInfo + "  > Failure: Got exception, but didn't expect it  {0}", ioex));
                }
            }
        }

        private static void RunSingleOrDefaultTest1Core(int size, bool usePredicate)
        {
            string methodInfo = string.Format("RunSingleOrDefaultTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i + 1;

            int predNum = 1023;

            bool expectDefault = usePredicate ? predNum >= (size + 1) : size == 0;
            bool expectExcept = usePredicate ? predNum < size : size > 1;
            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate (int x) { return x >= predNum; };
                    q = ints.AsParallel().SingleOrDefault(pred);
                }
                else
                {
                    q = ints.AsParallel().SingleOrDefault();
                }

                if (expectExcept)
                {
                    Assert.True(false, string.Format(methodInfo + "  > Failure: Expected an exception, but didn't get one"));
                }
                else
                {
                    int expectReturn = expectDefault ? 0 : (usePredicate ? predNum : 1);
                    if (q != expectReturn)
                    {
                        Assert.True(false, string.Format(methodInfo + "  > FAILED. Expected return value of {0}, saw {1} instead", expectReturn, q));
                    }
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    Assert.True(false, string.Format(methodInfo + "  > Failure: Got exception, but didn't expect it  {0}", ioex));
                }
            }
        }
        #endregion
    }
}
