// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace mdarray
{
    class Runtime_60957
    {
        public static int access_count = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        // Count the number of times this function gets called. It shouldn't change whether
        // all the MD functions get implemented as intrinsics or not.
        static int[,] GetArray(int[,] a)
        {
            ++access_count;
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void test1(int[] a)
        {
            Console.WriteLine(a.Rank);
            Console.WriteLine(a.GetLength(0));
            Console.WriteLine(a.GetLowerBound(0));
            Console.WriteLine(a.GetUpperBound(0));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void test2(int[,] a)
        {
            Console.WriteLine(a.Rank);
            Console.WriteLine(a.GetLength(0));
            Console.WriteLine(a.GetLength(1));
            Console.WriteLine(a.GetLowerBound(0));
            Console.WriteLine(a.GetUpperBound(0));
            Console.WriteLine(a.GetLowerBound(1));
            Console.WriteLine(a.GetUpperBound(1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        // The test guarantees that `a` is a 2-D array. Verifies that the intrinsics don't kick in.
        static void test3(System.Array a)
        {
            Console.WriteLine(a.Rank);
            Console.WriteLine(a.GetLength(0));
            Console.WriteLine(a.GetLength(1));
            Console.WriteLine(a.GetLowerBound(0));
            Console.WriteLine(a.GetUpperBound(0));
            Console.WriteLine(a.GetLowerBound(1));
            Console.WriteLine(a.GetUpperBound(1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        // General case: non-constant arguments to GetLength()/GetLowerBound()/GetUpperBound()
        static void test4(System.Array a)
        {
            int rank = a.Rank;
            Console.WriteLine(rank);
            for (int i = 0; i < rank; i++)
            {
                Console.WriteLine(a.GetLength(i));
                Console.WriteLine(a.GetLowerBound(i));
                Console.WriteLine(a.GetUpperBound(i));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test5(int[] a)
        {
            int sum = 0;
            for (int i = 0; i < a.GetLength(0); i++) {
                sum += a[i];
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test6(int[] a)
        {
            int sum = 0;
            for (int i = a.GetLowerBound(0); i <= a.GetUpperBound(0); i++) {
                sum += a[i];
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test7(int[,] a)
        {
            int sum = 0;
            for (int i = 0; i < a.GetLength(0); i++) {
                for (int j = 0; j < a.GetLength(1); j++) {
                    sum += a[i,j];
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test8(int[,] a)
        {
            int sum = 0;
            for (int i = a.GetLowerBound(0); i <= a.GetUpperBound(0); i++) {
                for (int j = a.GetLowerBound(1); j <= a.GetUpperBound(1); j++) {
                    sum += a[i,j];
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test9(System.Array a)
        {
            int sum = 0;
            for (int i = a.GetLowerBound(0); i <= a.GetUpperBound(0); i++) {
                for (int j = a.GetLowerBound(1); j <= a.GetUpperBound(1); j++) {
                    sum += (int)a.GetValue(i,j);
                }
            }
            return sum;
        }

        ///////////////////////////////////////////////////////////////////
        //
        // Handle cases where the intrinsic function `this` pointer is more than a local variable
        // (call, struct field, etc.)
        //

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void test10(int[,] a)
        {
            Console.WriteLine(GetArray(a).Rank);
            Console.WriteLine(GetArray(a).GetLength(0));
            Console.WriteLine(GetArray(a).GetLength(1));
            Console.WriteLine(GetArray(a).GetLowerBound(0));
            Console.WriteLine(GetArray(a).GetUpperBound(0));
            Console.WriteLine(GetArray(a).GetLowerBound(1));
            Console.WriteLine(GetArray(a).GetUpperBound(1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test11(int[,] a)
        {
            int sum = 0;
            for (int i = 0; i < GetArray(a).GetLength(0); i++) {
                for (int j = 0; j < GetArray(a).GetLength(1); j++) {
                    sum += a[i,j];
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test12(int[,] a)
        {
            int sum = 0;
            for (int i = GetArray(a).GetLowerBound(0); i <= GetArray(a).GetUpperBound(0); i++) {
                for (int j = GetArray(a).GetLowerBound(1); j <= GetArray(a).GetUpperBound(1); j++) {
                    sum += a[i,j];
                }
            }
            return sum;
        }

        ///////////////////////////////////////////////////////////////////
        //
        // Struct that doesn't get promoted
        //

        public struct S1
        {
            public object dummy1;
            public int dummy2;
            public int dummy3;
            public int dummy4;
            public int[,] a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void test13(S1 s)
        {
            Console.WriteLine(s.a.Rank);
            Console.WriteLine(s.a.GetLength(0));
            Console.WriteLine(s.a.GetLength(1));
            Console.WriteLine(s.a.GetLowerBound(0));
            Console.WriteLine(s.a.GetUpperBound(0));
            Console.WriteLine(s.a.GetLowerBound(1));
            Console.WriteLine(s.a.GetUpperBound(1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test14(S1 s)
        {
            int sum = 0;
            for (int i = 0; i < s.a.GetLength(0); i++) {
                for (int j = 0; j < s.a.GetLength(1); j++) {
                    sum += s.a[i,j];
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test15(S1 s)
        {
            int sum = 0;
            for (int i = s.a.GetLowerBound(0); i <= s.a.GetUpperBound(0); i++) {
                for (int j = s.a.GetLowerBound(1); j <= s.a.GetUpperBound(1); j++) {
                    sum += s.a[i,j];
                }
            }
            return sum;
        }

        ///////////////////////////////////////////////////////////////////
        //
        // Struct that gets promoted
        //

        public struct S2
        {
            public object dummy1;
            public int[,] a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void test16(S2 s)
        {
            Console.WriteLine(s.a.Rank);
            Console.WriteLine(s.a.GetLength(0));
            Console.WriteLine(s.a.GetLength(1));
            Console.WriteLine(s.a.GetLowerBound(0));
            Console.WriteLine(s.a.GetUpperBound(0));
            Console.WriteLine(s.a.GetLowerBound(1));
            Console.WriteLine(s.a.GetUpperBound(1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test17(S2 s)
        {
            int sum = 0;
            for (int i = 0; i < s.a.GetLength(0); i++) {
                for (int j = 0; j < s.a.GetLength(1); j++) {
                    sum += s.a[i,j];
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test18(S2 s)
        {
            int sum = 0;
            for (int i = s.a.GetLowerBound(0); i <= s.a.GetUpperBound(0); i++) {
                for (int j = s.a.GetLowerBound(1); j <= s.a.GetUpperBound(1); j++) {
                    sum += s.a[i,j];
                }
            }
            return sum;
        }

        ///////////////////////////////////////////////////////////////////
        //
        // Array element
        //

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void test19(int[][,] a)
        {
            Console.WriteLine(a[0].Rank);
            Console.WriteLine(a[0].GetLength(0));
            Console.WriteLine(a[0].GetLength(1));
            Console.WriteLine(a[0].GetLowerBound(0));
            Console.WriteLine(a[0].GetUpperBound(0));
            Console.WriteLine(a[0].GetLowerBound(1));
            Console.WriteLine(a[0].GetUpperBound(1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test20(int[][,] a)
        {
            int sum = 0;
            for (int i = 0; i < a[0].GetLength(0); i++) {
                for (int j = 0; j < a[0].GetLength(1); j++) {
                    sum += a[0][i,j];
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int test21(int[][,] a)
        {
            int sum = 0;
            for (int i = a[0].GetLowerBound(0); i <= a[0].GetUpperBound(0); i++) {
                for (int j = a[0].GetLowerBound(1); j <= a[0].GetUpperBound(1); j++) {
                    sum += a[0][i,j];
                }
            }
            return sum;
        }

        ///////////////////////////////////////////////////////////////////

        private const int PASS = 100;
        private const int FAIL = 1;
        private static int result = PASS; // by default, pass

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Check(int value, int expected, string test)
        {
            if (value != expected)
            {
                Console.WriteLine($"Test {test} failed: value {value} != expected value {expected}");
                result = FAIL;
            }
        }

        static int Main(string[] args)
        {
            const int n = 10;
            int[,] a = new int[n,n];
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    a[i,j] = i + j;
                }
            }
            int[] b = new int[n];
            for (int i = 0; i < n; i++) {
                b[i] = i;
            }

            Console.WriteLine("test1");
            test1(b);

            Console.WriteLine("test2");
            test2(a);

            Console.WriteLine("test3");
            test3(a);

            Console.WriteLine("test4");
            test4(a);

            Console.WriteLine("test5");
            int s1 = test5(b);
            Check(s1, 45, "test5");

            Console.WriteLine("test6");
            int s2 = test6(b);
            Check(s2, 45, "test6");

            Console.WriteLine("test7");
            int s3 = test7(a);
            Check(s3, 900, "test7");

            Console.WriteLine("test8");
            int s4 = test8(a);
            Check(s4, 900, "test8");

            Console.WriteLine("test9");
            int s5 = test9(a);
            Check(s5, 900, "test9");

            // The following get the array from a function call

            Console.WriteLine("test10");
            test10(a);

            Console.WriteLine("test11");
            int s6 = test11(a);
            Check(s6, 900, "test11");

            Console.WriteLine("test12");
            int s7 = test12(a);
            Check(s7, 900, "test12");

            Check(access_count, 260, "access_count");

            // The following get the array from an unpromoted struct member

            S1 st = new S1();
            st.dummy1 = new Object();
            st.dummy2 = 8;
            st.dummy3 = 9;
            st.dummy4 = 10;
            st.a = a;

            Console.WriteLine("test13");
            test13(st);

            Console.WriteLine("test14");
            int s8 = test14(st);
            Check(s8, 900, "test14");

            Console.WriteLine("test15");
            int s9 = test15(st);
            Check(s9, 900, "test15");

            // The following get the array from a promoted struct member

            S2 st2 = new S2();
            st2.dummy1 = new Object();
            st2.a = a;

            Console.WriteLine("test16");
            test16(st2);

            Console.WriteLine("test17");
            int s10 = test17(st2);
            Check(s10, 900, "test17");

            Console.WriteLine("test18");
            int s11 = test18(st2);
            Check(s11, 900, "test18");

            // The following get the array from an array element.

            int[][,] a2 = new int[1][,];
            a2[0] = new int[n,n];
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    a2[0][i,j] = i + j;
                }
            }

            Console.WriteLine("test19");
            test19(a2);

            Console.WriteLine("test20");
            int s12 = test20(a2);
            Check(s12, 900, "test20");

            Console.WriteLine("test21");
            int s13 = test21(a2);
            Check(s13, 900, "test21");

            if (result == PASS)
            {
                Console.WriteLine("PASS");
            }
            else if (result == FAIL)
            {
                Console.WriteLine("FAIL");
            }
            else
            {
                // This shouldn't happen.
                Console.WriteLine("UNKNOWN RESULT: FAIL");
                result = FAIL;
            }

            return result;
        }
    }
}
