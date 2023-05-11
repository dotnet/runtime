// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace NativeVarargTest
{
    public class VarArg
    {
        ////////////////////////////////////////////////////////////////////////////
        // Member Variables
        ////////////////////////////////////////////////////////////////////////////

        private static int m_testCount;
        private static int m_passCount;
        private static int m_failCount;

        ////////////////////////////////////////////////////////////////////////////
        // Extern Definitions
        ////////////////////////////////////////////////////////////////////////////

        // printf
#if WIN32
        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl)]
        extern static void printf(string str, __arglist);
#else
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
        extern static void printf(string str, __arglist);
#endif

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static int test_passing_ints(int count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static long test_passing_longs(int count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static float test_passing_floats(int count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static double test_passing_doubles(int count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static long test_passing_int_and_longs(int int_count, int long_count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static double test_passing_floats_and_doubles(int float_count, int double_count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static double test_passing_int_and_double(double expected_value, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static double test_passing_long_and_double(double expected_value, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static double check_passing_four_three_double_struct(ThreeDoubleStruct a, ThreeDoubleStruct b, ThreeDoubleStruct c, ThreeDoubleStruct d, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static int check_passing_struct(int count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static int check_passing_four_sixteen_byte_structs(int count, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static byte echo_byte(byte arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static char echo_char(char arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static short echo_short(short arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static int echo_int(int arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static long echo_int64(long arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static float echo_float(float arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static double echo_double(double arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static OneIntStruct echo_one_int_struct(OneIntStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static TwoIntStruct echo_two_int_struct(TwoIntStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static OneLongStruct echo_one_long_struct(OneLongStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static TwoLongStruct echo_two_long_struct(TwoLongStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static EightByteStruct echo_eight_byte_struct(EightByteStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static FourIntStruct echo_four_int_struct(FourIntStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static FourLongStruct echo_four_long_struct_with_vararg(FourLongStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static FourLongStruct echo_four_long_struct(FourLongStruct arg);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static SixteenByteStruct echo_sixteen_byte_struct(SixteenByteStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static OneFloatStruct echo_one_float_struct(OneFloatStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static TwoFloatStruct echo_two_float_struct(TwoFloatStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static OneDoubleStruct echo_one_double_struct(OneDoubleStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static TwoDoubleStruct echo_two_double_struct(TwoDoubleStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static ThreeDoubleStruct echo_three_double_struct(ThreeDoubleStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static FourFloatStruct echo_four_float_struct(FourFloatStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static FourDoubleStruct echo_four_double_struct(FourDoubleStruct arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static byte short_in_byte_out(short arg, __arglist);

        [DllImport("varargnative", CallingConvention = CallingConvention.Cdecl)]
        extern static short byte_in_short_out(byte arg, __arglist);

        ////////////////////////////////////////////////////////////////////////////
        // Test PInvoke, native vararg calls.
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingInts(int[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            int expectedSum = test_passing_ints(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            int sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingLongs(long[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            long expectedSum = test_passing_longs(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            long sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingFloats(float[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            float expectedSum = test_passing_floats(expectedValues.Length, __arglist((double)expectedValues[0], (double)expectedValues[1], (double)expectedValues[2], (double)expectedValues[3]));

            float sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingDoubles(double[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            double expectedSum = test_passing_doubles(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyInts(int[] expectedValues)
        {
            int expectedSum = test_passing_ints(expectedValues.Length, __arglist());

            int sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyLongs(long[] expectedValues)
        {
            long expectedSum = test_passing_longs(expectedValues.Length, __arglist());

            long sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they are
        /// equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyFloats(float[] expectedValues)
        {
            float expectedSum = test_passing_floats(expectedValues.Length, __arglist());

            float sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyDouble(double[] expectedValues)
        {
            double expectedSum = test_passing_doubles(expectedValues.Length, __arglist());

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they are
        /// equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingIntsAndLongs(int[] expectedIntValues, long[] expectedLongValues)
        {
            Debug.Assert(expectedIntValues.Length == 2);
            Debug.Assert(expectedLongValues.Length == 2);
            long expectedSum = test_passing_int_and_longs(expectedIntValues.Length, expectedLongValues.Length, __arglist(expectedIntValues[0], expectedIntValues[1], expectedLongValues[0], expectedLongValues[1]));

            long sum = 0;
            for (int i = 0; i < expectedIntValues.Length; ++i)
            {
                sum += expectedIntValues[i];
            }

            for (int i = 0; i < expectedLongValues.Length; ++i)
            {
                sum += expectedLongValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingFloatsAndDoubles(float[] expectedFloatValues, double[] expectedDoubleValues)
        {
            Debug.Assert(expectedFloatValues.Length == 2);
            Debug.Assert(expectedDoubleValues.Length == 2);
            double expectedSum = test_passing_floats_and_doubles(expectedFloatValues.Length, expectedDoubleValues.Length, __arglist((double)expectedFloatValues[0], (double)expectedFloatValues[1], expectedDoubleValues[0], expectedDoubleValues[1]));

            double sum = 0;
            for (int i = 0; i < expectedFloatValues.Length; ++i)
            {
                sum += expectedFloatValues[i];
            }

            for (int i = 0; i < expectedDoubleValues.Length; ++i)
            {
                sum += expectedDoubleValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they are
        /// equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingIntsAndFloats()
        {
            int a = 10;
            int b = 20;
            int c = 30;

            double f1 = 1.0;
            double f2 = 2.0;
            double f3 = 3.0;

            double expectedSum = 0.0f;

            expectedSum = a + b + c + f1 + f2 + f3;

            double calculatedSum = test_passing_int_and_double(
                expectedSum,
                __arglist(
                    a,
                    f1,
                    b,
                    f2,
                    c,
                    f3
                )
            );

            return expectedSum == calculatedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingLongsAndDoubles()
        {
            long[] expectedIntValues = new long[] { 10, 20, 30 };
            double[] expectedFloatValues = new double[] { 1.0, 2.0, 3.0 };

            double expectedSum = 0.0f;

            for (int i = 0; i < expectedIntValues.Length; ++i) expectedSum += expectedIntValues[i];
            for (int i = 0; i < expectedFloatValues.Length; ++i) expectedSum += expectedFloatValues[i];


            double calculatedSum = test_passing_long_and_double(
                expectedSum,
                __arglist(
                    expectedIntValues[0],
                    expectedFloatValues[0],
                    expectedIntValues[1],
                    expectedFloatValues[1],
                    expectedIntValues[2],
                    expectedFloatValues[2]
                )
            );

            return expectedSum == calculatedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyInts(int[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            int expectedSum = test_passing_ints(expectedValues.Length, __arglist(expectedValues[0],
                                                                                expectedValues[1],
                                                                                expectedValues[2],
                                                                                expectedValues[3],
                                                                                expectedValues[4],
                                                                                expectedValues[5],
                                                                                expectedValues[6],
                                                                                expectedValues[7],
                                                                                expectedValues[8],
                                                                                expectedValues[9],
                                                                                expectedValues[10],
                                                                                expectedValues[11],
                                                                                expectedValues[12],
                                                                                expectedValues[13],
                                                                                expectedValues[14],
                                                                                expectedValues[15],
                                                                                expectedValues[16],
                                                                                expectedValues[17],
                                                                                expectedValues[18],
                                                                                expectedValues[19],
                                                                                expectedValues[20],
                                                                                expectedValues[21],
                                                                                expectedValues[22],
                                                                                expectedValues[23],
                                                                                expectedValues[24],
                                                                                expectedValues[25],
                                                                                expectedValues[26],
                                                                                expectedValues[27],
                                                                                expectedValues[28],
                                                                                expectedValues[29],
                                                                                expectedValues[30],
                                                                                expectedValues[31],
                                                                                expectedValues[32],
                                                                                expectedValues[33],
                                                                                expectedValues[34],
                                                                                expectedValues[35],
                                                                                expectedValues[36],
                                                                                expectedValues[37],
                                                                                expectedValues[38],
                                                                                expectedValues[39],
                                                                                expectedValues[40]));

            int sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyLongs(long[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            long expectedSum = test_passing_longs(expectedValues.Length, __arglist(expectedValues[0],
                                                                                expectedValues[1],
                                                                                expectedValues[2],
                                                                                expectedValues[3],
                                                                                expectedValues[4],
                                                                                expectedValues[5],
                                                                                expectedValues[6],
                                                                                expectedValues[7],
                                                                                expectedValues[8],
                                                                                expectedValues[9],
                                                                                expectedValues[10],
                                                                                expectedValues[11],
                                                                                expectedValues[12],
                                                                                expectedValues[13],
                                                                                expectedValues[14],
                                                                                expectedValues[15],
                                                                                expectedValues[16],
                                                                                expectedValues[17],
                                                                                expectedValues[18],
                                                                                expectedValues[19],
                                                                                expectedValues[20],
                                                                                expectedValues[21],
                                                                                expectedValues[22],
                                                                                expectedValues[23],
                                                                                expectedValues[24],
                                                                                expectedValues[25],
                                                                                expectedValues[26],
                                                                                expectedValues[27],
                                                                                expectedValues[28],
                                                                                expectedValues[29],
                                                                                expectedValues[30],
                                                                                expectedValues[31],
                                                                                expectedValues[32],
                                                                                expectedValues[33],
                                                                                expectedValues[34],
                                                                                expectedValues[35],
                                                                                expectedValues[36],
                                                                                expectedValues[37],
                                                                                expectedValues[38],
                                                                                expectedValues[39],
                                                                                expectedValues[40]));

            long sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyFloats(double[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            float expectedSum = test_passing_floats(expectedValues.Length, __arglist(expectedValues[0],
                                                                                     expectedValues[1],
                                                                                     expectedValues[2],
                                                                                     expectedValues[3],
                                                                                     expectedValues[4],
                                                                                     expectedValues[5],
                                                                                     expectedValues[6],
                                                                                     expectedValues[7],
                                                                                     expectedValues[8],
                                                                                     expectedValues[9],
                                                                                     expectedValues[10],
                                                                                     expectedValues[11],
                                                                                     expectedValues[12],
                                                                                     expectedValues[13],
                                                                                     expectedValues[14],
                                                                                     expectedValues[15],
                                                                                     expectedValues[16],
                                                                                     expectedValues[17],
                                                                                     expectedValues[18],
                                                                                     expectedValues[19],
                                                                                     expectedValues[20],
                                                                                     expectedValues[21],
                                                                                     expectedValues[22],
                                                                                     expectedValues[23],
                                                                                     expectedValues[24],
                                                                                     expectedValues[25],
                                                                                     expectedValues[26],
                                                                                     expectedValues[27],
                                                                                     expectedValues[28],
                                                                                     expectedValues[29],
                                                                                     expectedValues[30],
                                                                                     expectedValues[31],
                                                                                     expectedValues[32],
                                                                                     expectedValues[33],
                                                                                     expectedValues[34],
                                                                                     expectedValues[35],
                                                                                     expectedValues[36],
                                                                                     expectedValues[37],
                                                                                     expectedValues[38],
                                                                                     expectedValues[39],
                                                                                     expectedValues[40]));

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they are
        /// equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyDoubles(double[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            double expectedSum = test_passing_doubles(expectedValues.Length, __arglist(expectedValues[0],
                                                                                    expectedValues[1],
                                                                                    expectedValues[2],
                                                                                    expectedValues[3],
                                                                                    expectedValues[4],
                                                                                    expectedValues[5],
                                                                                    expectedValues[6],
                                                                                    expectedValues[7],
                                                                                    expectedValues[8],
                                                                                    expectedValues[9],
                                                                                    expectedValues[10],
                                                                                    expectedValues[11],
                                                                                    expectedValues[12],
                                                                                    expectedValues[13],
                                                                                    expectedValues[14],
                                                                                    expectedValues[15],
                                                                                    expectedValues[16],
                                                                                    expectedValues[17],
                                                                                    expectedValues[18],
                                                                                    expectedValues[19],
                                                                                    expectedValues[20],
                                                                                    expectedValues[21],
                                                                                    expectedValues[22],
                                                                                    expectedValues[23],
                                                                                    expectedValues[24],
                                                                                    expectedValues[25],
                                                                                    expectedValues[26],
                                                                                    expectedValues[27],
                                                                                    expectedValues[28],
                                                                                    expectedValues[29],
                                                                                    expectedValues[30],
                                                                                    expectedValues[31],
                                                                                    expectedValues[32],
                                                                                    expectedValues[33],
                                                                                    expectedValues[34],
                                                                                    expectedValues[35],
                                                                                    expectedValues[36],
                                                                                    expectedValues[37],
                                                                                    expectedValues[38],
                                                                                    expectedValues[39],
                                                                                    expectedValues[40]));

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// This function will test passing struct through varargs.
        ///
        /// </summary>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingStructs()
        {
            int success = 100;

            success = ReportFailure(TestPassingEightByteStructs(), "TestPassingEightByteStructs()", success, TestPassingEightByteStructs());
            success = ReportFailure(TestPassingSixteenByteStructs(), "TestPassingSixteenByteStructs()", success, TestPassingSixteenByteStructs());
            success = ReportFailure(TestPassingThirtyTwoByteStructs(), "TestPassingThirtyTwoByteStructs()", success, TestPassingThirtyTwoByteStructs());
            success = ReportFailure(TestFour16ByteStructs(), "TestFour16ByteStructs()", success, TestFour16ByteStructs());

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingEightByteStructs()
        {
            int success = 100;

            TwoIntStruct first = new TwoIntStruct();
            OneLongStruct second = new OneLongStruct();
            TwoFloatStruct third = new TwoFloatStruct();
            OneDoubleStruct fourth = new OneDoubleStruct();

            first.a = 20;
            first.b = -8;

            second.a = 4020120;

            third.a = 10.223f;
            third.b = 10331.1f;

            fourth.a = 120.1321321;

            int firstExpectedValue = first.a + first.b;
            long secondExpectedValue = second.a;
            double thirdExpectedValue = third.a + third.b;
            double fourthExpectedValue = fourth.a;

            success = ReportFailure(check_passing_struct(6, __arglist(0, 0, 0, 8, 1, firstExpectedValue, first)) == 0, "check_passing_struct(6, __arglist(0, 0, 0, 8, 1, firstExpectedValue, first)) == 0", success, 16);
            success = ReportFailure(check_passing_struct(6, __arglist(1, 0, 0, 8, 1, secondExpectedValue, second)) == 0, "check_passing_struct(6, __arglist(1, 0, 0, 8, 1, secondExpectedValue, second)) == 0", success, 17);
            success = ReportFailure(check_passing_struct(6, __arglist(0, 1, 0, 8, 1, thirdExpectedValue, third)) == 0, "check_passing_struct(6, __arglist(0, 1, 0, 8, 1, thirdExpectedValue, third)) == 0", success, 18);
            success = ReportFailure(check_passing_struct(6, __arglist(1, 1, 0, 8, 1, fourthExpectedValue, fourth)) == 0, "check_passing_struct(6, __arglist(1, 1, 0, 8, 1, fourthExpectedValue, fourth)) == 0", success, 19);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingSixteenByteStructs()
        {
            int success = 100;

            TwoLongStruct first = new TwoLongStruct();
            FourIntStruct second = new FourIntStruct();
            TwoDoubleStruct third = new TwoDoubleStruct();
            FourFloatStruct fourth = new FourFloatStruct();

            first.a = 30;
            first.b = -20;

            second.a = 10;
            second.b = 50;
            second.c = 80;
            second.b = 12000;

            third.a = 10.223;
            third.b = 10331.1;

            fourth.a = 1.0f;
            fourth.b = 2.0f;
            fourth.c = 3.0f;
            fourth.d = 4.0f;

            long firstExpectedValue = first.a + first.b;
            long secondExpectedValue = second.a + second.b + second.c + second.d;
            double thirdExpectedValue = third.a + third.b;
            double fourthExpectedValue = fourth.a + fourth.b + fourth.c + fourth.d;

            success = ReportFailure(check_passing_struct(6, __arglist(0, 0, 0, 16, 1, firstExpectedValue, first)) == 0, "check_passing_struct(6, __arglist(0, 0, 0, 16, 1, firstExpectedValue, first)) == 0", success, 20);
            success = ReportFailure(check_passing_struct(6, __arglist(1, 0, 0, 16, 1, secondExpectedValue, second)) == 0, "check_passing_struct(6, __arglist(1, 0, 0, 16, 1, secondExpectedValue, second)) == 0", success, 21);
            success = ReportFailure(check_passing_struct(6, __arglist(0, 1, 0, 16, 1, thirdExpectedValue, third)) == 0, "check_passing_struct(6, __arglist(0, 1, 0, 16, 1, thirdExpectedValue, third)) == 0", success, 22);
            success = ReportFailure(check_passing_struct(6, __arglist(1, 1, 0, 16, 1, fourthExpectedValue, fourth)) == 0, "check_passing_struct(6, __arglist(1, 1, 0, 16, 1, fourthExpectedValue, fourth)) == 0", success, 23);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingTwentyFourByteStructs()
        {
            int success = 100;

            ThreeDoubleStruct first = new ThreeDoubleStruct();
            ThreeDoubleStruct second = new ThreeDoubleStruct();
            ThreeDoubleStruct third = new ThreeDoubleStruct();
            ThreeDoubleStruct fourth = new ThreeDoubleStruct();

            first.a = 1.0;
            first.b = 2.0;
            first.c = 3.0;

            second.a = 4.0;
            second.b = 5.0;
            second.c = 6.0;

            third.a = 7.0;
            third.b = 8.0;
            third.c = 9.0;

            fourth.a = 10.0;
            fourth.b = 11.0;
            fourth.c = 12.0;

            double expectedSum = first.a + first.b + first.c;
            expectedSum += second.a + second.b + second.c;
            expectedSum += third.a + third.b + third.c;
            expectedSum += fourth.a + fourth.b + fourth.c;

            success = ReportFailure(expectedSum == check_passing_four_three_double_struct(first, second, third, fourth, __arglist()), "check_passing_four_three_double_struct(first, second, third, fourth)", success, 84);
            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingThirtyTwoByteStructs()
        {
            int success = 100;

            FourLongStruct first = new FourLongStruct();
            FourDoubleStruct second = new FourDoubleStruct();

            first.a = 20241231;
            first.b = -8213123;
            first.c = 1202;
            first.c = 1231;

            second.a = 10.102;
            second.b = 50.55;
            second.c = 80.341;
            second.b = 12000.00000000001;

            long firstExpectedValue = first.a + first.b + first.c + first.d;
            double secondExpectedValue = second.a + second.b + second.c + second.d;

            success = ReportFailure(check_passing_struct(6, __arglist(0, 0, 0, 32, 1, firstExpectedValue, first)) == 0, "check_passing_struct(6, __arglist(0, 0, 0, 32, 1, firstExpectedValue, first)) == 0", success, 24);
            success = ReportFailure(check_passing_struct(6, __arglist(0, 1, 0, 32, 1, secondExpectedValue, second)) == 0, "check_passing_struct(6, __arglist(0, 1, 0, 32, 1, secondExpectedValue, second)) == 0", success, 25);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestMany16ByteStructs()
        {
            int success = 100;

            TwoIntStruct s = new TwoIntStruct();

            s.a = 30;
            s.b = -20;

            long expectedValue = (s.a + s.b) * 5;

            success = ReportFailure(check_passing_struct(11, __arglist(0, 0, 0, 16, 5, expectedValue, s, s, s, s, s)) == 0, "check_passing_struct(11, __arglist(0, 0, 0, 16, 5, expectedValue, s, s, s, s, s)) == 0", success, 26);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestFour16ByteStructs()
        {
            int success = 100;

            TwoLongStruct s = new TwoLongStruct();
            TwoLongStruct s2 = new TwoLongStruct();
            TwoLongStruct s3 = new TwoLongStruct();
            TwoLongStruct s4 = new TwoLongStruct();

            s.a = 1;
            s.b = 2;

            s2.a = 3;
            s2.b = 4;

            s3.a = 5;
            s3.b = 6;

            s4.a = 7;
            s4.b = 8;

            long expectedValue = s.a + s.b + s2.a + s2.b + s3.a + s3.b + s4.a + s4.b;
            success = ReportFailure(check_passing_four_sixteen_byte_structs(5, __arglist(expectedValue, s, s2, s3, s4)) == 0, "check_passing_four_sixteen_byte_structs(5, __arglist(expectedValue, s, s2, s3, s4)) == 0", success, 27);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestMany32ByteStructs()
        {
            int success = 100;

            FourLongStruct s = new FourLongStruct();

            s.a = 30;
            s.b = -20;
            s.c = 100;
            s.d = 200;

            long expectedValue = (s.a + s.b + s.c + s.d) * 5;
            success = ReportFailure(check_passing_struct(11, __arglist(0, 0, 0, 32, 5, expectedValue, s, s, s, s, s)) == 0, "check_passing_struct(11, __arglist(0, 0, 0, 32, 5, expectedValue, s, s, s, s, s)) == 0", success, 28);

            return success;
        }

        ////////////////////////////////////////////////////////////////////////////
        // Test ArgIterator, managed to managed native vararg calls.
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingIntsManaged(int[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            int expectedSum = ManagedNativeVarargTests.TestPassingInts(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            int sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingLongsManaged(long[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            long expectedSum = ManagedNativeVarargTests.TestPassingLongs(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            long sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingFloatsManaged(float[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            float expectedSum = ManagedNativeVarargTests.TestPassingFloats(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            float sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingDoublesManaged(double[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 4);
            double expectedSum = ManagedNativeVarargTests.TestPassingDoubles(expectedValues.Length, __arglist(expectedValues[0], expectedValues[1], expectedValues[2], expectedValues[3]));

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyIntsManaged(int[] expectedValues)
        {
            int expectedSum = ManagedNativeVarargTests.TestPassingInts(expectedValues.Length, __arglist());

            int sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyLongsManaged(long[] expectedValues)
        {
            long expectedSum = ManagedNativeVarargTests.TestPassingLongs(expectedValues.Length, __arglist());

            long sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyFloatsManaged(float[] expectedValues)
        {
            float expectedSum = ManagedNativeVarargTests.TestPassingFloats(expectedValues.Length, __arglist());

            float sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they are
        /// equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingEmptyDoubleManaged(double[] expectedValues)
        {
            double expectedSum = ManagedNativeVarargTests.TestPassingDoubles(expectedValues.Length, __arglist());

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingIntsAndLongsManaged(int[] expectedIntValues, long[] expectedLongValues)
        {
            Debug.Assert(expectedIntValues.Length == 2);
            Debug.Assert(expectedLongValues.Length == 2);
            long expectedSum = ManagedNativeVarargTests.TestPassingIntsAndLongs(expectedIntValues.Length, expectedLongValues.Length, __arglist(expectedIntValues[0], expectedIntValues[1], expectedLongValues[0], expectedLongValues[1]));

            long sum = 0;
            for (int i = 0; i < expectedIntValues.Length; ++i)
            {
                sum += expectedIntValues[i];
            }

            for (int i = 0; i < expectedLongValues.Length; ++i)
            {
                sum += expectedLongValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingFloatsAndDoublesManaged(float[] expectedFloatValues, double[] expectedDoubleValues)
        {
            Debug.Assert(expectedFloatValues.Length == 2);
            Debug.Assert(expectedDoubleValues.Length == 2);
            double expectedSum = ManagedNativeVarargTests.TestPassingFloatsAndDoubles(expectedFloatValues.Length, expectedDoubleValues.Length, __arglist(expectedFloatValues[0], expectedFloatValues[1], expectedDoubleValues[0], expectedDoubleValues[1]));

            double sum = 0;
            for (int i = 0; i < expectedFloatValues.Length; ++i)
            {
                sum += expectedFloatValues[i];
            }

            for (int i = 0; i < expectedDoubleValues.Length; ++i)
            {
                sum += expectedDoubleValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingIntsAndFloatsManaged()
        {
            int a = 10;
            int b = 20;
            int c = 30;

            float f1 = 1.0f;
            float f2 = 2.0f;
            float f3 = 3.0f;

            float expectedSum = 0.0f;

            expectedSum = a + b + c + f1 + f2 + f3;

            float calculatedSum = ManagedNativeVarargTests.TestPassingIntsAndFloats(
                expectedSum,
                __arglist(
                    a,
                    f1,
                    b,
                    f2,
                    c,
                    f3
                )
            );

            return expectedSum == calculatedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingLongsAndDoublesManaged()
        {
            long[] expectedIntValues = new long[] { 10, 20, 30 };
            double[] expectedFloatValues = new double[] { 1.0, 2.0, 3.0 };

            double expectedSum = 0.0f;

            for (int i = 0; i < expectedIntValues.Length; ++i) expectedSum += expectedIntValues[i];
            for (int i = 0; i < expectedFloatValues.Length; ++i) expectedSum += expectedFloatValues[i];


            double calculatedSum = ManagedNativeVarargTests.TestPassingLongsAndDoubles(
                expectedSum,
                __arglist(
                    expectedIntValues[0],
                    expectedFloatValues[0],
                    expectedIntValues[1],
                    expectedFloatValues[1],
                    expectedIntValues[2],
                    expectedFloatValues[2]
                )
            );

            return expectedSum == calculatedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyIntsManaged(int[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            int expectedSum = ManagedNativeVarargTests.TestPassingInts(expectedValues.Length, __arglist(expectedValues[0],
                                                                                                        expectedValues[1],
                                                                                                        expectedValues[2],
                                                                                                        expectedValues[3],
                                                                                                        expectedValues[4],
                                                                                                        expectedValues[5],
                                                                                                        expectedValues[6],
                                                                                                        expectedValues[7],
                                                                                                        expectedValues[8],
                                                                                                        expectedValues[9],
                                                                                                        expectedValues[10],
                                                                                                        expectedValues[11],
                                                                                                        expectedValues[12],
                                                                                                        expectedValues[13],
                                                                                                        expectedValues[14],
                                                                                                        expectedValues[15],
                                                                                                        expectedValues[16],
                                                                                                        expectedValues[17],
                                                                                                        expectedValues[18],
                                                                                                        expectedValues[19],
                                                                                                        expectedValues[20],
                                                                                                        expectedValues[21],
                                                                                                        expectedValues[22],
                                                                                                        expectedValues[23],
                                                                                                        expectedValues[24],
                                                                                                        expectedValues[25],
                                                                                                        expectedValues[26],
                                                                                                        expectedValues[27],
                                                                                                        expectedValues[28],
                                                                                                        expectedValues[29],
                                                                                                        expectedValues[30],
                                                                                                        expectedValues[31],
                                                                                                        expectedValues[32],
                                                                                                        expectedValues[33],
                                                                                                        expectedValues[34],
                                                                                                        expectedValues[35],
                                                                                                        expectedValues[36],
                                                                                                        expectedValues[37],
                                                                                                        expectedValues[38],
                                                                                                        expectedValues[39],
                                                                                                        expectedValues[40]));

            int sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyLongsManaged(long[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            long expectedSum = ManagedNativeVarargTests.TestPassingLongs(expectedValues.Length, __arglist(expectedValues[0],
                                                                                                          expectedValues[1],
                                                                                                          expectedValues[2],
                                                                                                          expectedValues[3],
                                                                                                          expectedValues[4],
                                                                                                          expectedValues[5],
                                                                                                          expectedValues[6],
                                                                                                          expectedValues[7],
                                                                                                          expectedValues[8],
                                                                                                          expectedValues[9],
                                                                                                          expectedValues[10],
                                                                                                          expectedValues[11],
                                                                                                          expectedValues[12],
                                                                                                          expectedValues[13],
                                                                                                          expectedValues[14],
                                                                                                          expectedValues[15],
                                                                                                          expectedValues[16],
                                                                                                          expectedValues[17],
                                                                                                          expectedValues[18],
                                                                                                          expectedValues[19],
                                                                                                          expectedValues[20],
                                                                                                          expectedValues[21],
                                                                                                          expectedValues[22],
                                                                                                          expectedValues[23],
                                                                                                          expectedValues[24],
                                                                                                          expectedValues[25],
                                                                                                          expectedValues[26],
                                                                                                          expectedValues[27],
                                                                                                          expectedValues[28],
                                                                                                          expectedValues[29],
                                                                                                          expectedValues[30],
                                                                                                          expectedValues[31],
                                                                                                          expectedValues[32],
                                                                                                          expectedValues[33],
                                                                                                          expectedValues[34],
                                                                                                          expectedValues[35],
                                                                                                          expectedValues[36],
                                                                                                          expectedValues[37],
                                                                                                          expectedValues[38],
                                                                                                          expectedValues[39],
                                                                                                          expectedValues[40]));

            long sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyFloatsManaged(float[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            float expectedSum = ManagedNativeVarargTests.TestPassingFloats(expectedValues.Length, __arglist(expectedValues[0],
                                                                                                            expectedValues[1],
                                                                                                            expectedValues[2],
                                                                                                            expectedValues[3],
                                                                                                            expectedValues[4],
                                                                                                            expectedValues[5],
                                                                                                            expectedValues[6],
                                                                                                            expectedValues[7],
                                                                                                            expectedValues[8],
                                                                                                            expectedValues[9],
                                                                                                            expectedValues[10],
                                                                                                            expectedValues[11],
                                                                                                            expectedValues[12],
                                                                                                            expectedValues[13],
                                                                                                            expectedValues[14],
                                                                                                            expectedValues[15],
                                                                                                            expectedValues[16],
                                                                                                            expectedValues[17],
                                                                                                            expectedValues[18],
                                                                                                            expectedValues[19],
                                                                                                            expectedValues[20],
                                                                                                            expectedValues[21],
                                                                                                            expectedValues[22],
                                                                                                            expectedValues[23],
                                                                                                            expectedValues[24],
                                                                                                            expectedValues[25],
                                                                                                            expectedValues[26],
                                                                                                            expectedValues[27],
                                                                                                            expectedValues[28],
                                                                                                            expectedValues[29],
                                                                                                            expectedValues[30],
                                                                                                            expectedValues[31],
                                                                                                            expectedValues[32],
                                                                                                            expectedValues[33],
                                                                                                            expectedValues[34],
                                                                                                            expectedValues[35],
                                                                                                            expectedValues[36],
                                                                                                            expectedValues[37],
                                                                                                            expectedValues[38],
                                                                                                            expectedValues[39],
                                                                                                            expectedValues[40]));

            float sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// The callee will simply loop over the arguments, compute the sum
        /// then return the value.
        ///
        /// Do a quick check on the value returned, and return whether they
        /// are equal.
        ///
        /// Notes:
        ///
        /// This is a particularly interesting test case because on every platform it
        /// will force spilling locals to the stack instead of just passing in registers.
        ///
        /// </summary>
        /// <param name="expectedValues"></param>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestPassingManyDoublesManaged(double[] expectedValues)
        {
            Debug.Assert(expectedValues.Length == 41);
            double expectedSum = ManagedNativeVarargTests.TestPassingDoubles(expectedValues.Length, __arglist(expectedValues[0],
                                                                                                              expectedValues[1],
                                                                                                              expectedValues[2],
                                                                                                              expectedValues[3],
                                                                                                              expectedValues[4],
                                                                                                              expectedValues[5],
                                                                                                              expectedValues[6],
                                                                                                              expectedValues[7],
                                                                                                              expectedValues[8],
                                                                                                              expectedValues[9],
                                                                                                              expectedValues[10],
                                                                                                              expectedValues[11],
                                                                                                              expectedValues[12],
                                                                                                              expectedValues[13],
                                                                                                              expectedValues[14],
                                                                                                              expectedValues[15],
                                                                                                              expectedValues[16],
                                                                                                              expectedValues[17],
                                                                                                              expectedValues[18],
                                                                                                              expectedValues[19],
                                                                                                              expectedValues[20],
                                                                                                              expectedValues[21],
                                                                                                              expectedValues[22],
                                                                                                              expectedValues[23],
                                                                                                              expectedValues[24],
                                                                                                              expectedValues[25],
                                                                                                              expectedValues[26],
                                                                                                              expectedValues[27],
                                                                                                              expectedValues[28],
                                                                                                              expectedValues[29],
                                                                                                              expectedValues[30],
                                                                                                              expectedValues[31],
                                                                                                              expectedValues[32],
                                                                                                              expectedValues[33],
                                                                                                              expectedValues[34],
                                                                                                              expectedValues[35],
                                                                                                              expectedValues[36],
                                                                                                              expectedValues[37],
                                                                                                              expectedValues[38],
                                                                                                              expectedValues[39],
                                                                                                              expectedValues[40]));

            double sum = 0;
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                sum += expectedValues[i];
            }

            return sum == expectedSum;
        }

        /// <summary>
        /// Given an input set create an arglist to pass to a vararg callee.
        ///
        /// This function will test passing struct through varargs.
        ///
        /// </summary>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingStructsManaged()
        {
            int success = 100;

            success = ReportFailure(TestPassingEightByteStructsManaged(), "TestPassingEightByteStructsManaged()", success, TestPassingEightByteStructsManaged());
            success = ReportFailure(TestPassingSixteenByteStructsManaged(), "TestPassingSixteenByteStructsManaged()", success, TestPassingSixteenByteStructsManaged());
            success = ReportFailure(TestPassingThirtyTwoByteStructsManaged(), "TestPassingThirtyTwoByteStructsManaged()", success, TestPassingThirtyTwoByteStructsManaged());
            success = ReportFailure(TestFour16ByteStructsManaged(), "TestFour16ByteStructsManaged()", success, TestFour16ByteStructsManaged());

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructsManaged
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingEightByteStructsManaged()
        {
            int success = 100;

            TwoIntStruct first = new TwoIntStruct();
            OneLongStruct second = new OneLongStruct();
            TwoFloatStruct third = new TwoFloatStruct();
            OneDoubleStruct fourth = new OneDoubleStruct();

            first.a = 20;
            first.b = -8;

            second.a = 4020120;

            third.a = 1.0f;
            third.b = 2.0f;

            fourth.a = 1.0;

            int firstExpectedValue = first.a + first.b;
            long secondExpectedValue = second.a;
            float thirdExpectedValue = third.a + third.b;
            double fourthExpectedValue = fourth.a;

            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 0, 0, 8, 1, firstExpectedValue, first)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 0, 0, 8, 1, firstExpectedValue, first)) == 0", success, 46);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 0, 0, 8, 1, secondExpectedValue, second)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 0, 0, 8, 1, secondExpectedValue, second)) == 0", success, 47);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 1, 0, 8, 1, thirdExpectedValue, third)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 1, 0, 8, 1, thirdExpectedValue, third)) == 0", success, 48);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 1, 0, 8, 1, fourthExpectedValue, fourth)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 1, 0, 8, 1, fourthExpectedValue, fourth)) == 0", success, 49);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructsManaged
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingSixteenByteStructsManaged()
        {
            int success = 100;

            TwoLongStruct first = new TwoLongStruct();
            FourIntStruct second = new FourIntStruct();
            TwoDoubleStruct third = new TwoDoubleStruct();
            FourFloatStruct fourth = new FourFloatStruct();

            first.a = 30;
            first.b = -20;

            second.a = 10;
            second.b = 50;
            second.c = 80;
            second.b = 12000;

            third.a = 10.223;
            third.b = 10331.1;

            fourth.a = 1.0f;
            fourth.b = 2.0f;
            fourth.c = 3.0f;
            fourth.d = 4.0f;

            long firstExpectedValue = first.a + first.b;
            int secondExpectedValue = second.a + second.b + second.c + second.d;
            double thirdExpectedValue = third.a + third.b;
            float fourthExpectedValue = fourth.a + fourth.b + fourth.c + fourth.d;

            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 0, 0, 16, 1, firstExpectedValue, first)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 0, 0, 16, 1, firstExpectedValue, first)) == 0", success, 50);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 0, 0, 16, 1, secondExpectedValue, second)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 0, 0, 16, 1, secondExpectedValue, second)) == 0", success, 51);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 1, 0, 16, 1, thirdExpectedValue, third)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 1, 0, 16, 1, thirdExpectedValue, third)) == 0", success, 52);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 1, 0, 16, 1, fourthExpectedValue, fourth)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(1, 1, 0, 16, 1, fourthExpectedValue, fourth)) == 0", success, 53);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructsManaged
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestPassingThirtyTwoByteStructsManaged()
        {
            int success = 100;

            FourLongStruct first = new FourLongStruct();
            FourDoubleStruct second = new FourDoubleStruct();

            first.a = 20241231;
            first.b = -8213123;
            first.c = 1202;
            first.c = 1231;

            second.a = 10.102;
            second.b = 50.55;
            second.c = 80.341;
            second.b = 12000.00000000001;

            long firstExpectedValue = first.a + first.b + first.c + first.d;
            double secondExpectedValue = second.a + second.b + second.c + second.d;

            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 0, 0, 32, 1, firstExpectedValue, first)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 0, 0, 32, 1, firstExpectedValue, first)) == 0", success, 54);
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 1, 0, 32, 1, secondExpectedValue, second)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(6, __arglist(0, 1, 0, 32, 1, secondExpectedValue, second)) == 0", success, 55);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructsManaged
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestMany16ByteStructsManaged()
        {
            int success = 100;

            TwoLongStruct s = new TwoLongStruct();

            s.a = 30;
            s.b = -20;

            long expectedValue = (s.a + s.b) * 5;

            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(11, __arglist(0, 0, 0, 16, 5, expectedValue, s, s, s, s, s)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(11, __arglist(0, 0, 0, 16, 5, expectedValue, s, s, s, s, s)) == 0", success, 56);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructsManaged
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestFour16ByteStructsManaged()
        {
            int success = 100;

            TwoLongStruct s = new TwoLongStruct();
            TwoLongStruct s2 = new TwoLongStruct();
            TwoLongStruct s3 = new TwoLongStruct();
            TwoLongStruct s4 = new TwoLongStruct();

            s.a = 1;
            s.b = 2;

            s2.a = 3;
            s2.b = 4;

            s3.a = 5;
            s3.b = 6;

            s4.a = 7;
            s4.b = 8;

            long expectedValue = s.a + s.b + s2.a + s2.b + s3.a + s3.b + s4.a + s4.b;
            success = ReportFailure(ManagedNativeVarargTests.CheckPassingFourSixteenByteStructs(5, __arglist(expectedValue, s, s2, s3, s4)) == 0, "ManagedNativeVarargTests.CheckPassingFourSixteenByteStructs(5, __arglist(expectedValue, s, s2, s3, s4)) == 0", success, 57);

            return success;
        }

        /// <summary>
        /// This is a helper for TestPassingStructsManaged
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestMany32ByteStructsManaged()
        {
            int success = 100;

            FourLongStruct s = new FourLongStruct();

            s.a = 30;
            s.b = -20;
            s.c = 100;
            s.d = 200;

            long expectedValue = (s.a + s.b + s.c + s.d) * 5;

            success = ReportFailure(ManagedNativeVarargTests.CheckPassingStruct(11, __arglist(0, 0, 0, 32, 5, expectedValue, s, s, s, s, s)) == 0, "ManagedNativeVarargTests.CheckPassingStruct(11, __arglist(0, 0, 0, 32, 5, expectedValue, s, s, s, s, s)) == 0", success, 58);

            return success;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingIntsNoVarargsManaged()
        {
            int sum = ManagedNativeVarargTests.TestPassingIntsNoVarargs(1,
                                                                        2,
                                                                        3,
                                                                        4,
                                                                        5,
                                                                        6,
                                                                        7,
                                                                        8,
                                                                        9,
                                                                        __arglist());

            return sum == 45;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingLongsNoVarargsManaged()
        {
            long sum = ManagedNativeVarargTests.TestPassingLongsNoVarargs(1,
                                                                          2,
                                                                          3,
                                                                          4,
                                                                          5,
                                                                          6,
                                                                          7,
                                                                          8,
                                                                          9,
                                                                          __arglist());

            return sum == 45;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFloatsNoVarargsManaged()
        {
            float sum = ManagedNativeVarargTests.TestPassingFloatsNoVarargs(1.0f,
                                                                            2.0f,
                                                                            3.0f,
                                                                            4.0f,
                                                                            5.0f,
                                                                            6.0f,
                                                                            7.0f,
                                                                            8.0f,
                                                                            9.0f,
                                                                            __arglist());

            return sum == 45.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingDoublesNoVarargsManaged()
        {
            double sum = ManagedNativeVarargTests.TestPassingDoublesNoVarargs(1.0,
                                                                              2.0,
                                                                              3.0,
                                                                              4.0,
                                                                              5.0,
                                                                              6.0,
                                                                              7.0,
                                                                              8.0,
                                                                              9.0,
                                                                              __arglist());

            return sum == 45.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingIntAndFloatsNoVarargsManaged()
        {
            float sum = ManagedNativeVarargTests.TestPassingIntAndFloatsNoVarargs(1,
                                                                                  2,
                                                                                  3,
                                                                                  4,
                                                                                  5,
                                                                                  6,
                                                                                  7,
                                                                                  8,
                                                                                  9,
                                                                                  10.0f,
                                                                                  11.0f,
                                                                                  12.0f,
                                                                                  13.0f,
                                                                                  14.0f,
                                                                                  15.0f,
                                                                                  16.0f,
                                                                                  17.0f,
                                                                                  18.0f,
                                                                                  __arglist());

            return sum == 171.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFloatsAndIntNoVarargsManaged()
        {
            float sum = ManagedNativeVarargTests.TestPassingFloatsAndIntNoVarargs(1.0f,
                                                                                  2.0f,
                                                                                  3.0f,
                                                                                  4.0f,
                                                                                  5.0f,
                                                                                  6.0f,
                                                                                  7.0f,
                                                                                  8.0f,
                                                                                  9.0f,
                                                                                  10,
                                                                                  11,
                                                                                  12,
                                                                                  13,
                                                                                  14,
                                                                                  15,
                                                                                  16,
                                                                                  17,
                                                                                  18,
                                                                                  __arglist());

            return sum == 171.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingIntAndDoublesNoVarargsManaged()
        {
            double sum = ManagedNativeVarargTests.TestPassingIntAndDoublesNoVarargs(1,
                                                                                    2,
                                                                                    3,
                                                                                    4,
                                                                                    5,
                                                                                    6,
                                                                                    7,
                                                                                    8,
                                                                                    9,
                                                                                    10.0,
                                                                                    11.0,
                                                                                    12.0,
                                                                                    13.0,
                                                                                    14.0,
                                                                                    15.0,
                                                                                    16.0,
                                                                                    17.0,
                                                                                    18.0,
                                                                                    __arglist());

            return sum == 171.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingDoublesAndIntNoVarargsManaged()
        {
            double sum = ManagedNativeVarargTests.TestPassingDoublesAndIntNoVarargs(1.0,
                                                                                    2.0,
                                                                                    3.0,
                                                                                    4.0,
                                                                                    5.0,
                                                                                    6.0,
                                                                                    7.0,
                                                                                    8.0,
                                                                                    9.0,
                                                                                    10,
                                                                                    11,
                                                                                    12,
                                                                                    13,
                                                                                    14,
                                                                                    15,
                                                                                    16,
                                                                                    17,
                                                                                    18,
                                                                                    __arglist());

            return sum == 171.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingLongAndFloatsNoVarargsManaged()
        {
            float sum = ManagedNativeVarargTests.TestPassingLongAndFloatsNoVarargs(1,
                                                                                   2,
                                                                                   3,
                                                                                   4,
                                                                                   5,
                                                                                   6,
                                                                                   7,
                                                                                   8,
                                                                                   9,
                                                                                   10.0f,
                                                                                   11.0f,
                                                                                   12.0f,
                                                                                   13.0f,
                                                                                   14.0f,
                                                                                   15.0f,
                                                                                   16.0f,
                                                                                   17.0f,
                                                                                   18.0f,
                                                                                   __arglist());

            return sum == 171.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFloatsAndlongNoVarargsManaged()
        {
            float sum = ManagedNativeVarargTests.TestPassingFloatsAndlongNoVarargs(1.0f,
                                                                                   2.0f,
                                                                                   3.0f,
                                                                                   4.0f,
                                                                                   5.0f,
                                                                                   6.0f,
                                                                                   7.0f,
                                                                                   8.0f,
                                                                                   9.0f,
                                                                                   10,
                                                                                   11,
                                                                                   12,
                                                                                   13,
                                                                                   14,
                                                                                   15,
                                                                                   16,
                                                                                   17,
                                                                                   18,
                                                                                   __arglist());


            return sum == 171.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassinglongAndDoublesNoVarargsManaged()
        {
            double sum = ManagedNativeVarargTests.TestPassinglongAndDoublesNoVarargs(1,
                                                                                     2,
                                                                                     3,
                                                                                     4,
                                                                                     5,
                                                                                     6,
                                                                                     7,
                                                                                     8,
                                                                                     9,
                                                                                     10.0,
                                                                                     11.0,
                                                                                     12.0,
                                                                                     13.0,
                                                                                     14.0,
                                                                                     15.0,
                                                                                     16.0,
                                                                                     17.0,
                                                                                     18.0,
                                                                                     __arglist());

            return sum == 171.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingDoublesAndlongNoVarargsManaged()
        {
            double sum = ManagedNativeVarargTests.TestPassingDoublesAndlongNoVarargs(1.0,
                                                                                     2.0,
                                                                                     3.0,
                                                                                     4.0,
                                                                                     5.0,
                                                                                     6.0,
                                                                                     7.0,
                                                                                     8.0,
                                                                                     9.0,
                                                                                     10,
                                                                                     11,
                                                                                     12,
                                                                                     13,
                                                                                     14,
                                                                                     15,
                                                                                     16,
                                                                                     17,
                                                                                     18,
                                                                                     __arglist());

            return sum == 171.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoIntStructsNoVarargsManaged()
        {
            TwoIntStruct one = new TwoIntStruct();
            TwoIntStruct two = new TwoIntStruct();
            TwoIntStruct three = new TwoIntStruct();
            TwoIntStruct four = new TwoIntStruct();
            TwoIntStruct five = new TwoIntStruct();
            TwoIntStruct six = new TwoIntStruct();
            TwoIntStruct seven = new TwoIntStruct();
            TwoIntStruct eight = new TwoIntStruct();
            TwoIntStruct nine = new TwoIntStruct();
            TwoIntStruct ten = new TwoIntStruct();

            one.a = 1;
            one.b = 2;

            two.a = 3;
            two.b = 4;


            three.a = 5;
            three.b = 6;

            four.a = 7;
            four.b = 8;

            five.a = 9;
            five.b = 10;

            six.a = 11;
            six.b = 12;

            seven.a = 13;
            seven.b = 14;

            eight.a = 15;
            eight.b = 16;

            nine.a = 17;
            nine.b = 18;

            ten.a = 19;
            ten.b = 20;

            long sum = ManagedNativeVarargTests.TestPassingTwoIntStructsNoVarargs(one,
                                                                                  two,
                                                                                  three,
                                                                                  four,
                                                                                  five,
                                                                                  six,
                                                                                  seven,
                                                                                  eight,
                                                                                  nine,
                                                                                  ten,
                                                                                  __arglist());

            return sum == 210;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFourIntStructsNoVarargsManaged()
        {
            FourIntStruct one = new FourIntStruct();
            FourIntStruct two = new FourIntStruct();
            FourIntStruct three = new FourIntStruct();
            FourIntStruct four = new FourIntStruct();
            FourIntStruct five = new FourIntStruct();
            FourIntStruct six = new FourIntStruct();
            FourIntStruct seven = new FourIntStruct();
            FourIntStruct eight = new FourIntStruct();
            FourIntStruct nine = new FourIntStruct();
            FourIntStruct ten = new FourIntStruct();

            one.a = 1;
            one.b = 2;
            one.c = 3;
            one.d = 4;

            two.a = 5;
            two.b = 6;
            two.c = 7;
            two.d = 8;


            three.a = 9;
            three.b = 10;
            three.c = 11;
            three.d = 12;

            four.a = 13;
            four.b = 14;
            four.c = 15;
            four.d = 16;

            five.a = 17;
            five.b = 18;
            five.c = 19;
            five.d = 20;

            six.a = 21;
            six.b = 22;
            six.c = 23;
            six.d = 24;

            seven.a = 25;
            seven.b = 26;
            seven.c = 27;
            seven.d = 28;

            eight.a = 29;
            eight.b = 30;
            eight.c = 31;
            eight.d = 32;

            nine.a = 33;
            nine.b = 34;
            nine.c = 35;
            nine.d = 36;

            ten.a = 37;
            ten.b = 38;
            ten.c = 39;
            ten.d = 40;

            int sum = ManagedNativeVarargTests.TestPassingFourIntStructsNoVarargs(one,
                                                                                  two,
                                                                                  three,
                                                                                  four,
                                                                                  five,
                                                                                  six,
                                                                                  seven,
                                                                                  eight,
                                                                                  nine,
                                                                                  ten,
                                                                                  __arglist());

            return sum == 820;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoLongStructsNoVarargsManaged()
        {
            TwoLongStruct one = new TwoLongStruct();
            TwoLongStruct two = new TwoLongStruct();
            TwoLongStruct three = new TwoLongStruct();
            TwoLongStruct four = new TwoLongStruct();
            TwoLongStruct five = new TwoLongStruct();
            TwoLongStruct six = new TwoLongStruct();
            TwoLongStruct seven = new TwoLongStruct();
            TwoLongStruct eight = new TwoLongStruct();
            TwoLongStruct nine = new TwoLongStruct();
            TwoLongStruct ten = new TwoLongStruct();

            one.a = 1;
            one.b = 2;

            two.a = 3;
            two.b = 4;

            three.a = 5;
            three.b = 6;

            four.a = 7;
            four.b = 8;

            five.a = 9;
            five.b = 10;

            six.a = 11;
            six.b = 12;

            seven.a = 13;
            seven.b = 14;

            eight.a = 15;
            eight.b = 16;

            nine.a = 17;
            nine.b = 18;

            ten.a = 19;
            ten.b = 20;

            long sum = ManagedNativeVarargTests.TestPassingTwoLongStructsNoVarargs(one,
                                                                                   two,
                                                                                   three,
                                                                                   four,
                                                                                   five,
                                                                                   six,
                                                                                   seven,
                                                                                   eight,
                                                                                   nine,
                                                                                   ten,
                                                                                   __arglist());

            return sum == 210;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoLongStructsWithIntAndLongNoVarargsManaged()
        {
            TwoLongStruct one = new TwoLongStruct();
            TwoLongStruct two = new TwoLongStruct();
            TwoLongStruct three = new TwoLongStruct();
            TwoLongStruct four = new TwoLongStruct();
            TwoLongStruct five = new TwoLongStruct();
            TwoLongStruct six = new TwoLongStruct();
            TwoLongStruct seven = new TwoLongStruct();
            TwoLongStruct eight = new TwoLongStruct();
            TwoLongStruct nine = new TwoLongStruct();
            TwoLongStruct ten = new TwoLongStruct();

            one.a = 1;
            one.b = 2;

            two.a = 3;
            two.b = 4;

            three.a = 5;
            three.b = 6;

            four.a = 7;
            four.b = 8;

            five.a = 9;
            five.b = 10;

            six.a = 11;
            six.b = 12;

            seven.a = 13;
            seven.b = 14;

            eight.a = 15;
            eight.b = 16;

            nine.a = 17;
            nine.b = 18;

            ten.a = 19;
            ten.b = 20;

            bool passed = ManagedNativeVarargTests.TestPassingTwoLongStructsNoVarargs(5,
                                                                                      210,
                                                                                      one,
                                                                                      two,
                                                                                      three,
                                                                                      four,
                                                                                      five,
                                                                                      six,
                                                                                      seven,
                                                                                      eight,
                                                                                      nine,
                                                                                      ten,
                                                                                      __arglist());

            return passed;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoLongStructsAndIntNoVarargsManaged()
        {
            TwoLongStruct one = new TwoLongStruct();
            TwoLongStruct two = new TwoLongStruct();
            TwoLongStruct three = new TwoLongStruct();
            TwoLongStruct four = new TwoLongStruct();
            TwoLongStruct five = new TwoLongStruct();
            TwoLongStruct six = new TwoLongStruct();
            TwoLongStruct seven = new TwoLongStruct();
            TwoLongStruct eight = new TwoLongStruct();
            TwoLongStruct nine = new TwoLongStruct();
            TwoLongStruct ten = new TwoLongStruct();

            one.a = 1;
            one.b = 2;

            two.a = 3;
            two.b = 4;


            three.a = 5;
            three.b = 6;

            four.a = 7;
            four.b = 8;

            five.a = 9;
            five.b = 10;

            six.a = 11;
            six.b = 12;

            seven.a = 13;
            seven.b = 14;

            eight.a = 15;
            eight.b = 16;

            nine.a = 17;
            nine.b = 18;

            ten.a = 19;
            ten.b = 20;

            long sum = ManagedNativeVarargTests.TestPassingTwoLongStructsAndIntNoVarargs(21,
                                                                                         one,
                                                                                         two,
                                                                                         three,
                                                                                         four,
                                                                                         five,
                                                                                         six,
                                                                                         seven,
                                                                                         eight,
                                                                                         nine,
                                                                                         ten,
                                                                                         __arglist());

            return sum == 231;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFourLongStructsNoVarargsManaged()
        {
            FourLongStruct one = new FourLongStruct();
            FourLongStruct two = new FourLongStruct();
            FourLongStruct three = new FourLongStruct();
            FourLongStruct four = new FourLongStruct();
            FourLongStruct five = new FourLongStruct();
            FourLongStruct six = new FourLongStruct();
            FourLongStruct seven = new FourLongStruct();
            FourLongStruct eight = new FourLongStruct();
            FourLongStruct nine = new FourLongStruct();
            FourLongStruct ten = new FourLongStruct();

            one.a = 1;
            one.b = 2;
            one.c = 3;
            one.d = 4;

            two.a = 5;
            two.b = 6;
            two.c = 7;
            two.d = 8;


            three.a = 9;
            three.b = 10;
            three.c = 11;
            three.d = 12;

            four.a = 13;
            four.b = 14;
            four.c = 15;
            four.d = 16;

            five.a = 17;
            five.b = 18;
            five.c = 19;
            five.d = 20;

            six.a = 21;
            six.b = 22;
            six.c = 23;
            six.d = 24;

            seven.a = 25;
            seven.b = 26;
            seven.c = 27;
            seven.d = 28;

            eight.a = 29;
            eight.b = 30;
            eight.c = 31;
            eight.d = 32;

            nine.a = 33;
            nine.b = 34;
            nine.c = 35;
            nine.d = 36;

            ten.a = 37;
            ten.b = 38;
            ten.c = 39;
            ten.d = 40;

            long sum = ManagedNativeVarargTests.TestPassingFourLongStructsNoVarargs(one,
                                                                                    two,
                                                                                    three,
                                                                                    four,
                                                                                    five,
                                                                                    six,
                                                                                    seven,
                                                                                    eight,
                                                                                    nine,
                                                                                    ten,
                                                                                    __arglist());

            return sum == 820;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoFloatStructsNoVarargsManaged()
        {
            TwoFloatStruct one = new TwoFloatStruct();
            TwoFloatStruct two = new TwoFloatStruct();
            TwoFloatStruct three = new TwoFloatStruct();
            TwoFloatStruct four = new TwoFloatStruct();
            TwoFloatStruct five = new TwoFloatStruct();
            TwoFloatStruct six = new TwoFloatStruct();
            TwoFloatStruct seven = new TwoFloatStruct();
            TwoFloatStruct eight = new TwoFloatStruct();
            TwoFloatStruct nine = new TwoFloatStruct();
            TwoFloatStruct ten = new TwoFloatStruct();

            one.a = 1.0f;
            one.b = 2.0f;

            two.a = 3.0f;
            two.b = 4.0f;


            three.a = 5.0f;
            three.b = 6.0f;

            four.a = 7.0f;
            four.b = 8.0f;

            five.a = 9.0f;
            five.b = 10.0f;

            six.a = 11.0f;
            six.b = 12.0f;

            seven.a = 13.0f;
            seven.b = 14.0f;

            eight.a = 15.0f;
            eight.b = 16.0f;

            nine.a = 17.0f;
            nine.b = 18.0f;

            ten.a = 19.0f;
            ten.b = 20.0f;

            float sum = ManagedNativeVarargTests.TestPassingTwoFloatStructsNoVarargs(one,
                                                                                     two,
                                                                                     three,
                                                                                     four,
                                                                                     five,
                                                                                     six,
                                                                                     seven,
                                                                                     eight,
                                                                                     nine,
                                                                                     ten,
                                                                                     __arglist());

            return sum == 210.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFourFloatStructsNoVarargsManaged()
        {
            FourFloatStruct one = new FourFloatStruct();
            FourFloatStruct two = new FourFloatStruct();
            FourFloatStruct three = new FourFloatStruct();
            FourFloatStruct four = new FourFloatStruct();
            FourFloatStruct five = new FourFloatStruct();
            FourFloatStruct six = new FourFloatStruct();
            FourFloatStruct seven = new FourFloatStruct();
            FourFloatStruct eight = new FourFloatStruct();
            FourFloatStruct nine = new FourFloatStruct();
            FourFloatStruct ten = new FourFloatStruct();

            one.a = 1.0f;
            one.b = 2.0f;
            one.c = 3.0f;
            one.d = 4.0f;

            two.a = 5.0f;
            two.b = 6.0f;
            two.c = 7.0f;
            two.d = 8.0f;


            three.a = 9.0f;
            three.b = 10.0f;
            three.c = 11.0f;
            three.d = 12.0f;

            four.a = 13.0f;
            four.b = 14.0f;
            four.c = 15.0f;
            four.d = 16.0f;

            five.a = 17.0f;
            five.b = 18.0f;
            five.c = 19.0f;
            five.d = 20.0f;

            six.a = 21.0f;
            six.b = 22.0f;
            six.c = 23.0f;
            six.d = 24.0f;

            seven.a = 25.0f;
            seven.b = 26.0f;
            seven.c = 27.0f;
            seven.d = 28.0f;

            eight.a = 29.0f;
            eight.b = 30.0f;
            eight.c = 31.0f;
            eight.d = 32.0f;

            nine.a = 33.0f;
            nine.b = 34.0f;
            nine.c = 35.0f;
            nine.d = 36.0f;

            ten.a = 37.0f;
            ten.b = 38.0f;
            ten.c = 39.0f;
            ten.d = 40.0f;

            float sum = ManagedNativeVarargTests.TestPassingFourFloatStructsNoVarargs(one,
                                                                                     two,
                                                                                     three,
                                                                                     four,
                                                                                     five,
                                                                                     six,
                                                                                     seven,
                                                                                     eight,
                                                                                     nine,
                                                                                     ten,
                                                                                     __arglist());

            return sum == 820.0f;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoDoubleStructsNoVarargsManaged()
        {
            TwoDoubleStruct one = new TwoDoubleStruct();
            TwoDoubleStruct two = new TwoDoubleStruct();
            TwoDoubleStruct three = new TwoDoubleStruct();
            TwoDoubleStruct four = new TwoDoubleStruct();
            TwoDoubleStruct five = new TwoDoubleStruct();
            TwoDoubleStruct six = new TwoDoubleStruct();
            TwoDoubleStruct seven = new TwoDoubleStruct();
            TwoDoubleStruct eight = new TwoDoubleStruct();
            TwoDoubleStruct nine = new TwoDoubleStruct();
            TwoDoubleStruct ten = new TwoDoubleStruct();

            one.a = 1.0;
            one.b = 2.0;

            two.a = 3.0;
            two.b = 4.0;


            three.a = 5.0;
            three.b = 6.0;

            four.a = 7.0;
            four.b = 8.0;

            five.a = 9.0;
            five.b = 10.0;

            six.a = 11.0;
            six.b = 12.0;

            seven.a = 13.0;
            seven.b = 14.0;

            eight.a = 15.0;
            eight.b = 16.0;

            nine.a = 17.0;
            nine.b = 18.0;

            ten.a = 19.0;
            ten.b = 20.0;

            double sum = ManagedNativeVarargTests.TestPassingTwoDoubleStructsNoVarargs(one,
                                                                                       two,
                                                                                       three,
                                                                                       four,
                                                                                       five,
                                                                                       six,
                                                                                       seven,
                                                                                       eight,
                                                                                       nine,
                                                                                       ten,
                                                                                       __arglist());

            return sum == 210.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoLongStructsAndFloatNoVarargsManaged()
        {
            TwoDoubleStruct one = new TwoDoubleStruct();
            TwoDoubleStruct two = new TwoDoubleStruct();
            TwoDoubleStruct three = new TwoDoubleStruct();
            TwoDoubleStruct four = new TwoDoubleStruct();
            TwoDoubleStruct five = new TwoDoubleStruct();
            TwoDoubleStruct six = new TwoDoubleStruct();
            TwoDoubleStruct seven = new TwoDoubleStruct();
            TwoDoubleStruct eight = new TwoDoubleStruct();
            TwoDoubleStruct nine = new TwoDoubleStruct();
            TwoDoubleStruct ten = new TwoDoubleStruct();

            one.a = 1.0;
            one.b = 2.0;

            two.a = 3.0;
            two.b = 4.0;


            three.a = 5.0;
            three.b = 6.0;

            four.a = 7.0;
            four.b = 8.0;

            five.a = 9.0;
            five.b = 10.0;

            six.a = 11.0;
            six.b = 12.0;

            seven.a = 13.0;
            seven.b = 14.0;

            eight.a = 15.0;
            eight.b = 16.0;

            nine.a = 17.0;
            nine.b = 18.0;

            ten.a = 19.0;
            ten.b = 20.0;

            double sum = ManagedNativeVarargTests.TestPassingTwoDoubleStructsAndFloatNoVarargs(21,
                                                                                              one,
                                                                                              two,
                                                                                              three,
                                                                                              four,
                                                                                              five,
                                                                                              six,
                                                                                              seven,
                                                                                              eight,
                                                                                              nine,
                                                                                              ten,
                                                                                              __arglist());

            return sum == 231.0;
        }

        /// <summary>
        /// Test passing using the vararg calling convention; however, not passing
        /// and varargs. This is to assure that the non-variadic arguments
        /// are passing correctly.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingFourDoubleStructsNoVarargsManaged()
        {
            FourDoubleStruct one = new FourDoubleStruct();
            FourDoubleStruct two = new FourDoubleStruct();
            FourDoubleStruct three = new FourDoubleStruct();
            FourDoubleStruct four = new FourDoubleStruct();
            FourDoubleStruct five = new FourDoubleStruct();
            FourDoubleStruct six = new FourDoubleStruct();
            FourDoubleStruct seven = new FourDoubleStruct();
            FourDoubleStruct eight = new FourDoubleStruct();
            FourDoubleStruct nine = new FourDoubleStruct();
            FourDoubleStruct ten = new FourDoubleStruct();

            one.a = 1.0;
            one.b = 2.0;
            one.c = 3.0;
            one.d = 4.0;

            two.a = 5.0;
            two.b = 6.0;
            two.c = 7.0;
            two.d = 8.0;


            three.a = 9.0;
            three.b = 10.0;
            three.c = 11.0;
            three.d = 12.0;

            four.a = 13.0;
            four.b = 14.0;
            four.c = 15.0;
            four.d = 16.0;

            five.a = 17.0;
            five.b = 18.0;
            five.c = 19.0;
            five.d = 20.0;

            six.a = 21.0;
            six.b = 22.0;
            six.c = 23.0;
            six.d = 24.0;

            seven.a = 25.0;
            seven.b = 26.0;
            seven.c = 27.0;
            seven.d = 28.0;

            eight.a = 29.0;
            eight.b = 30.0;
            eight.c = 31.0;
            eight.d = 32.0;

            nine.a = 33.0;
            nine.b = 34.0;
            nine.c = 35.0;
            nine.d = 36.0;

            ten.a = 37.0;
            ten.b = 38.0;
            ten.c = 39.0;
            ten.d = 40.0;

            double sum = ManagedNativeVarargTests.TestPassingFourDoubleStructsNoVarargs(one,
                                                                                        two,
                                                                                        three,
                                                                                        four,
                                                                                        five,
                                                                                        six,
                                                                                        seven,
                                                                                        eight,
                                                                                        nine,
                                                                                        ten,
                                                                                        __arglist());

            return sum == 820.0;
        }

        /// <summary>
        /// Test passing using the regular calling convention ten eight byte
        /// structs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTenEightBytes()
        {
            EightByteStruct one = new EightByteStruct();
            EightByteStruct two = new EightByteStruct();
            EightByteStruct three = new EightByteStruct();
            EightByteStruct four = new EightByteStruct();
            EightByteStruct five = new EightByteStruct();
            EightByteStruct six = new EightByteStruct();
            EightByteStruct seven = new EightByteStruct();
            EightByteStruct eight = new EightByteStruct();
            EightByteStruct nine = new EightByteStruct();
            EightByteStruct ten = new EightByteStruct();

            long cookie = 1010;

            one.one = 1;
            one.two = 2;
            one.three = 3;
            one.four = 4;
            one.five = 5;
            one.six = 6;
            one.seven = 7;
            one.eight = 8;

            two.one = 9;
            two.two = 10;
            two.three = 11;
            two.four = 12;
            two.five = 13;
            two.six = 14;
            two.seven = 15;
            two.eight = 16;

            three.one = 17;
            three.two = 18;
            three.three = 19;
            three.four = 20;
            three.five = 21;
            three.six = 22;
            three.seven = 23;
            three.eight = 24;

            four.one = 25;
            four.two = 26;
            four.three = 27;
            four.four = 28;
            four.five = 29;
            four.six = 30;
            four.seven = 31;
            four.eight = 32;

            five.one = 33;
            five.two = 34;
            five.three = 35;
            five.four = 36;
            five.five = 37;
            five.six = 38;
            five.seven = 39;
            five.eight = 40;

            six.one = 41;
            six.two = 42;
            six.three = 43;
            six.four = 44;
            six.five = 45;
            six.six = 46;
            six.seven = 47;
            six.eight = 48;

            seven.one = 49;
            seven.two = 50;
            seven.three = 51;
            seven.four = 52;
            seven.five = 53;
            seven.six = 54;
            seven.seven = 55;
            seven.eight = 56;

            eight.one = 57;
            eight.two = 58;
            eight.three = 59;
            eight.four = 60;
            eight.five = 61;
            eight.six = 62;
            eight.seven = 63;
            eight.eight = 64;

            nine.one = 65;
            nine.two = 66;
            nine.three = 67;
            nine.four = 68;
            nine.five = 69;
            nine.six = 70;
            nine.seven = 71;
            nine.eight = 72;

            ten.one = 73;
            ten.two = 74;
            ten.three = 75;
            ten.four = 76;
            ten.five = 77;
            ten.six = 78;
            ten.seven = 79;
            ten.eight = 80;

            int sum = TestPassingTenEightBytesHelper(cookie, one, two, three, four, five, six, seven, eight, nine, ten);

            return sum == 3240;
        }

        /// <summary>
        /// Test passing using the regular calling convention ten sixteen byte
        /// structs
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTenSixteenBytes()
        {
            SixteenByteStruct one = new SixteenByteStruct();
            SixteenByteStruct two = new SixteenByteStruct();
            SixteenByteStruct three = new SixteenByteStruct();
            SixteenByteStruct four = new SixteenByteStruct();
            SixteenByteStruct five = new SixteenByteStruct();
            SixteenByteStruct six = new SixteenByteStruct();
            SixteenByteStruct seven = new SixteenByteStruct();
            SixteenByteStruct eight = new SixteenByteStruct();
            SixteenByteStruct nine = new SixteenByteStruct();
            SixteenByteStruct ten = new SixteenByteStruct();

            long cookie = 1010;

            one.one = 1;
            one.two = 2;
            one.three = 3;
            one.four = 4;
            one.five = 5;
            one.six = 6;
            one.seven = 7;
            one.eight = 8;
            one.nine = 9;
            one.ten = 10;
            one.eleven = 11;
            one.twelve = 12;
            one.thirteen = 13;
            one.fourteen = 14;
            one.fifteen = 15;
            one.sixteen = 16;

            two.one = 17;
            two.two = 18;
            two.three = 19;
            two.four = 20;
            two.five = 21;
            two.six = 22;
            two.seven = 23;
            two.eight = 24;
            two.nine = 25;
            two.ten = 26;
            two.eleven = 27;
            two.twelve = 28;
            two.thirteen = 29;
            two.fourteen = 30;
            two.fifteen = 31;
            two.sixteen = 32;

            three.one = 33;
            three.two = 34;
            three.three = 35;
            three.four = 36;
            three.five = 37;
            three.six = 38;
            three.seven = 39;
            three.eight = 40;
            three.nine = 41;
            three.ten = 42;
            three.eleven = 43;
            three.twelve = 44;
            three.thirteen = 45;
            three.fourteen = 46;
            three.fifteen = 47;
            three.sixteen = 48;

            four.one = 49;
            four.two = 50;
            four.three = 51;
            four.four = 52;
            four.five = 53;
            four.six = 54;
            four.seven = 55;
            four.eight = 56;
            four.nine = 57;
            four.ten = 58;
            four.eleven = 59;
            four.twelve = 60;
            four.thirteen = 61;
            four.fourteen = 62;
            four.fifteen = 63;
            four.sixteen = 64;

            five.one = 65;
            five.two = 66;
            five.three = 67;
            five.four = 68;
            five.five = 69;
            five.six = 70;
            five.seven = 71;
            five.eight = 72;
            five.nine = 73;
            five.ten = 74;
            five.eleven = 75;
            five.twelve = 76;
            five.thirteen = 77;
            five.fourteen = 78;
            five.fifteen = 79;
            five.sixteen = 80;

            six.one = 81;
            six.two = 82;
            six.three = 83;
            six.four = 84;
            six.five = 85;
            six.six = 86;
            six.seven = 87;
            six.eight = 88;
            six.nine = 89;
            six.ten = 90;
            six.eleven = 91;
            six.twelve = 92;
            six.thirteen = 93;
            six.fourteen = 94;
            six.fifteen = 95;
            six.sixteen = 96;

            seven.one = 97;
            seven.two = 98;
            seven.three = 99;
            seven.four = 100;
            seven.five = 101;
            seven.six = 102;
            seven.seven = 103;
            seven.eight = 104;
            seven.nine = 105;
            seven.ten = 106;
            seven.eleven = 107;
            seven.twelve = 108;
            seven.thirteen = 109;
            seven.fourteen = 110;
            seven.fifteen = 111;
            seven.sixteen = 112;

            eight.one = 113;
            eight.two = 114;
            eight.three = 115;
            eight.four = 116;
            eight.five = 117;
            eight.six = 118;
            eight.seven = 119;
            eight.eight = 120;
            eight.nine = 121;
            eight.ten = 122;
            eight.eleven = 123;
            eight.twelve = 124;
            eight.thirteen = 125;
            eight.fourteen = 126;
            eight.fifteen = 127;
            eight.sixteen = 128;

            nine.one = 129;
            nine.two = 130;
            nine.three = 131;
            nine.four = 132;
            nine.five = 133;
            nine.six = 134;
            nine.seven = 135;
            nine.eight = 136;
            nine.nine = 137;
            nine.ten = 138;
            nine.eleven = 139;
            nine.twelve = 140;
            nine.thirteen = 141;
            nine.fourteen = 142;
            nine.fifteen = 143;
            nine.sixteen = 144;

            ten.one = 145;
            ten.two = 146;
            ten.three = 147;
            ten.four = 148;
            ten.five = 149;
            ten.six = 150;
            ten.seven = 151;
            ten.eight = 152;
            ten.nine = 153;
            ten.ten = 154;
            ten.eleven = 155;
            ten.twelve = 156;
            ten.thirteen = 157;
            ten.fourteen = 158;
            ten.fifteen = 159;
            ten.sixteen = 160;

            int sum = TestPassingTenSixteenBytesHelper(cookie, one, two, three, four, five, six, seven, eight, nine, ten);

            return sum == 12880;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestPassingTenEightBytesHelper(long cookie,
                                                         EightByteStruct one,
                                                         EightByteStruct two,
                                                         EightByteStruct three,
                                                         EightByteStruct four,
                                                         EightByteStruct five,
                                                         EightByteStruct six,
                                                         EightByteStruct seven,
                                                         EightByteStruct eight,
                                                         EightByteStruct nine,
                                                         EightByteStruct ten)
        {
            int sum = 0;

            sum += one.one + one.two + one.three + one.four + one.five + one.six + one.seven + one.eight;
            sum += two.one + two.two + two.three + two.four + two.five + two.six + two.seven + two.eight;
            sum += three.one + three.two + three.three + three.four + three.five + three.six + three.seven + three.eight;
            sum += four.one + four.two + four.three + four.four + four.five + four.six + four.seven + four.eight;
            sum += five.one + five.two + five.three + five.four + five.five + five.six + five.seven + five.eight;
            sum += six.one + six.two + six.three + six.four + six.five + six.six + six.seven + six.eight;
            sum += seven.one + seven.two + seven.three + seven.four + seven.five + seven.six + seven.seven + seven.eight;
            sum += eight.one + eight.two + eight.three + eight.four + eight.five + eight.six + eight.seven + eight.eight;
            sum += nine.one + nine.two + nine.three + nine.four + nine.five + nine.six + nine.seven + nine.eight;
            sum += ten.one + ten.two + ten.three + ten.four + ten.five + ten.six + ten.seven + ten.eight;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestPassingTenSixteenBytesHelper(long cookie,
                                                           SixteenByteStruct one,
                                                           SixteenByteStruct two,
                                                           SixteenByteStruct three,
                                                           SixteenByteStruct four,
                                                           SixteenByteStruct five,
                                                           SixteenByteStruct six,
                                                           SixteenByteStruct seven,
                                                           SixteenByteStruct eight,
                                                           SixteenByteStruct nine,
                                                           SixteenByteStruct ten)
        {
            int sum = 0;

            sum += one.one + one.two + one.three + one.four + one.five + one.six + one.seven + one.eight + one.nine + one.ten + one.eleven + one.twelve + one.thirteen + one.fourteen + one.fifteen + one.sixteen;
            sum += two.one + two.two + two.three + two.four + two.five + two.six + two.seven + two.eight + two.nine + two.ten + two.eleven + two.twelve + two.thirteen + two.fourteen + two.fifteen + two.sixteen;
            sum += three.one + three.two + three.three + three.four + three.five + three.six + three.seven + three.eight + three.nine + three.ten + three.eleven + three.twelve + three.thirteen + three.fourteen + three.fifteen + three.sixteen;
            sum += four.one + four.two + four.three + four.four + four.five + four.six + four.seven + four.eight + four.nine + four.ten + four.eleven + four.twelve + four.thirteen + four.fourteen + four.fifteen + four.sixteen;
            sum += five.one + five.two + five.three + five.four + five.five + five.six + five.seven + five.eight + five.nine + five.ten + five.eleven + five.twelve + five.thirteen + five.fourteen + five.fifteen + five.sixteen;
            sum += six.one + six.two + six.three + six.four + six.five + six.six + six.seven + six.eight + six.nine + six.ten + six.eleven + six.twelve + six.thirteen + six.fourteen + six.fifteen + six.sixteen;
            sum += seven.one + seven.two + seven.three + seven.four + seven.five + seven.six + seven.seven + seven.eight + seven.nine + seven.ten + seven.eleven + seven.twelve + seven.thirteen + seven.fourteen + seven.fifteen + seven.sixteen;
            sum += eight.one + eight.two + eight.three + eight.four + eight.five + eight.six + eight.seven + eight.eight + eight.nine + eight.ten + eight.eleven + eight.twelve + eight.thirteen + eight.fourteen + eight.fifteen + eight.sixteen;
            sum += nine.one + nine.two + nine.three + nine.four + nine.five + nine.six + nine.seven + nine.eight + nine.nine + nine.ten + nine.eleven + nine.twelve + nine.thirteen + nine.fourteen + nine.fifteen + nine.sixteen;
            sum += ten.one + ten.two + ten.three + ten.four + ten.five + ten.six + ten.seven + ten.eight + ten.nine + ten.ten + ten.eleven + ten.twelve + ten.thirteen + ten.fourteen + ten.fifteen + ten.sixteen;

            return sum;
        }

        ////////////////////////////////////////////////////////////////////////
        // Echo Tests
        //
        // Notes:
        //
        //  Simple tests which confirm that what is passed to the method/function
        //  is the same when it is returned.
        //
        ////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoByteNoVararg(byte arg)
        {
            byte returnValue = echo_byte(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoCharNoVararg(char arg)
        {
            char returnValue = echo_char(arg, __arglist());

            return returnValue == arg;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoShortNoVararg(short arg)
        {
            short returnValue = echo_short(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoIntNoVararg(int arg)
        {
            int returnValue = echo_int(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoLongNoVararg(long arg)
        {
            long returnValue = echo_int64(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFloatNoVararg(float arg)
        {
            float returnValue = echo_float(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoDoubleNoVararg(double arg)
        {
            double returnValue = echo_double(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneIntStructNoVararg()
        {
            OneIntStruct arg = new OneIntStruct();
            arg.a = 1;

            OneIntStruct returnValue = echo_one_int_struct(arg, __arglist());

            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoIntStructNoVararg()
        {
            TwoIntStruct arg = new TwoIntStruct();
            arg.a = 1;
            arg.b = 2;

            TwoIntStruct returnValue = echo_two_int_struct(arg, __arglist());

            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneLongStructNoVararg()
        {
            OneLongStruct arg = new OneLongStruct();
            arg.a = 1;

            OneLongStruct returnValue = echo_one_long_struct(arg, __arglist());

            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoLongStructNoVararg()
        {
            TwoLongStruct arg = new TwoLongStruct();
            arg.a = 1;
            arg.b = 2;

            TwoLongStruct returnValue = echo_two_long_struct(arg, __arglist());

            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoEightByteStructStructNoVararg()
        {
            EightByteStruct arg = new EightByteStruct();
            arg.one = 1;
            arg.two = 2;
            arg.three = 3;
            arg.four = 4;
            arg.five = 5;
            arg.six = 6;
            arg.seven = 7;
            arg.eight = 8;

            EightByteStruct returnValue = echo_eight_byte_struct(arg, __arglist());

            bool equal = arg.one == returnValue.one &&
                         arg.two == returnValue.two &&
                         arg.three == returnValue.three &&
                         arg.four == returnValue.four &&
                         arg.five == returnValue.five &&
                         arg.six == returnValue.six &&
                         arg.seven == returnValue.seven &&
                         arg.eight == returnValue.eight;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourIntStructNoVararg()
        {
            FourIntStruct arg = new FourIntStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourIntStruct returnValue = echo_four_int_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoSixteenByteStructNoVararg()
        {
            SixteenByteStruct arg = new SixteenByteStruct();
            arg.one = 1;
            arg.two = 2;
            arg.three = 3;
            arg.four = 4;
            arg.five = 5;
            arg.six = 6;
            arg.seven = 7;
            arg.eight = 8;
            arg.nine = 9;
            arg.ten = 10;
            arg.eleven = 11;
            arg.twelve = 12;
            arg.thirteen = 13;
            arg.fourteen = 14;
            arg.fifteen = 15;
            arg.sixteen = 16;

            SixteenByteStruct returnValue = echo_sixteen_byte_struct(arg, __arglist());

            bool equal = arg.one == returnValue.one &&
                         arg.two == returnValue.two &&
                         arg.three == returnValue.three &&
                         arg.four == returnValue.four &&
                         arg.five == returnValue.five &&
                         arg.six == returnValue.six &&
                         arg.seven == returnValue.seven &&
                         arg.eight == returnValue.eight &&
                         arg.nine == returnValue.nine &&
                         arg.ten == returnValue.ten &&
                         arg.eleven == returnValue.eleven &&
                         arg.twelve == returnValue.twelve &&
                         arg.thirteen == returnValue.thirteen &&
                         arg.fourteen == returnValue.fourteen &&
                         arg.fifteen == returnValue.fifteen &&
                         arg.sixteen == returnValue.sixteen;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourLongStruct()
        {
            FourLongStruct arg = new FourLongStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourLongStruct returnValue = echo_four_long_struct(arg);
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourLongStructNoVararg()
        {
            FourLongStruct arg = new FourLongStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourLongStruct returnValue = echo_four_long_struct_with_vararg(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneFloatStructNoVararg()
        {
            OneFloatStruct arg = new OneFloatStruct();
            arg.a = 1.0f;

            OneFloatStruct returnValue = echo_one_float_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoFloatStructNoVararg()
        {
            TwoFloatStruct arg = new TwoFloatStruct();
            arg.a = 1.0f;
            arg.b = 2.0f;

            TwoFloatStruct returnValue = echo_two_float_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneDoubleStructNoVararg()
        {
            OneDoubleStruct arg = new OneDoubleStruct();
            arg.a = 1.0;

            OneDoubleStruct returnValue = echo_one_double_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoDoubleStructNoVararg()
        {
            TwoDoubleStruct arg = new TwoDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;

            TwoDoubleStruct returnValue = echo_two_double_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoThreeDoubleStructNoVararg()
        {
            ThreeDoubleStruct arg = new ThreeDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;

            ThreeDoubleStruct returnValue = echo_three_double_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b && arg.c == returnValue.c;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourFloatStructNoVararg()
        {
            FourFloatStruct arg = new FourFloatStruct();
            arg.a = 1.0f;
            arg.b = 2.0f;
            arg.c = 3.0f;
            arg.d = 4.0f;

            FourFloatStruct returnValue = echo_four_float_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourDoubleStructNoVararg()
        {
            FourDoubleStruct arg = new FourDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;
            arg.d = 4.0;

            FourDoubleStruct returnValue = echo_four_double_struct(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        ////////////////////////////////////////////////////////////////////////
        // Echo Tests
        //
        // Notes:
        //
        //  Simple tests which confirm that what is passed to the method/function
        //  is the same when it is returned.
        //
        //  These tests, are the managed to managed calls.
        //
        ////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoByteManagedNoVararg(byte arg)
        {
            byte returnValue = ManagedNativeVarargTests.TestEchoByteManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoCharManagedNoVararg(char arg)
        {
            char returnValue = ManagedNativeVarargTests.TestEchoCharManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoShortManagedNoVararg(short arg)
        {
            short returnValue = ManagedNativeVarargTests.TestEchoShortManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoIntManagedNoVararg(int arg)
        {
            int returnValue = ManagedNativeVarargTests.TestEchoIntManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoLongManagedNoVararg(long arg)
        {
            long returnValue = ManagedNativeVarargTests.TestEchoLongManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFloatManagedNoVararg(float arg)
        {
            float returnValue = ManagedNativeVarargTests.TestEchoFloatManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoDoubleManagedNoVararg(double arg)
        {
            double returnValue = ManagedNativeVarargTests.TestEchoDoubleManagedNoVararg(arg, __arglist());

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneIntStructManagedNoVararg()
        {
            OneIntStruct arg = new OneIntStruct();
            arg.a = 1;

            OneIntStruct returnValue = ManagedNativeVarargTests.TestEchoOneIntStructManagedNoVararg(arg, __arglist());

            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoIntStructManagedNoVararg()
        {
            TwoIntStruct arg = new TwoIntStruct();
            arg.a = 1;
            arg.b = 2;

            TwoIntStruct returnValue = ManagedNativeVarargTests.TestEchoTwoIntStructManagedNoVararg(arg, __arglist());

            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneLongStructManagedNoVararg()
        {
            OneLongStruct arg = new OneLongStruct();
            arg.a = 1;

            OneLongStruct returnValue = ManagedNativeVarargTests.TestEchoOneLongStructManagedNoVararg(arg, __arglist());

            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoLongStructManagedNoVararg()
        {
            TwoLongStruct arg = new TwoLongStruct();
            arg.a = 1;
            arg.b = 2;

            TwoLongStruct returnValue = ManagedNativeVarargTests.TestEchoTwoLongStructManagedNoVararg(arg, __arglist());

            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoEightByteStructStructManagedNoVararg()
        {
            EightByteStruct arg = new EightByteStruct();
            arg.one = 1;
            arg.two = 2;
            arg.three = 3;
            arg.four = 4;
            arg.five = 5;
            arg.six = 6;
            arg.seven = 7;
            arg.eight = 8;

            EightByteStruct returnValue = ManagedNativeVarargTests.TestEchoEightByteStructStructManagedNoVararg(arg, __arglist());

            bool equal = arg.one == returnValue.one &&
                         arg.two == returnValue.two &&
                         arg.three == returnValue.three &&
                         arg.four == returnValue.four &&
                         arg.five == returnValue.five &&
                         arg.six == returnValue.six &&
                         arg.seven == returnValue.seven &&
                         arg.eight == returnValue.eight;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourIntStructManagedNoVararg()
        {
            FourIntStruct arg = new FourIntStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourIntStruct returnValue = ManagedNativeVarargTests.TestEchoFourIntStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoSixteenByteStructManagedNoVararg()
        {
            SixteenByteStruct arg = new SixteenByteStruct();
            arg.one = 1;
            arg.two = 2;
            arg.three = 3;
            arg.four = 4;
            arg.five = 5;
            arg.six = 6;
            arg.seven = 7;
            arg.eight = 8;
            arg.nine = 9;
            arg.ten = 10;
            arg.eleven = 11;
            arg.twelve = 12;
            arg.thirteen = 13;
            arg.fourteen = 14;
            arg.fifteen = 15;
            arg.sixteen = 16;

            SixteenByteStruct returnValue = ManagedNativeVarargTests.TestEchoSixteenByteStructManagedNoVararg(arg, __arglist());

            bool equal = arg.one == returnValue.one &&
                         arg.two == returnValue.two &&
                         arg.three == returnValue.three &&
                         arg.four == returnValue.four &&
                         arg.five == returnValue.five &&
                         arg.six == returnValue.six &&
                         arg.seven == returnValue.seven &&
                         arg.eight == returnValue.eight &&
                         arg.nine == returnValue.nine &&
                         arg.ten == returnValue.ten &&
                         arg.eleven == returnValue.eleven &&
                         arg.twelve == returnValue.twelve &&
                         arg.thirteen == returnValue.thirteen &&
                         arg.fourteen == returnValue.fourteen &&
                         arg.fifteen == returnValue.fifteen &&
                         arg.sixteen == returnValue.sixteen;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourLongStructManagedNoVararg()
        {
            FourLongStruct arg = new FourLongStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourLongStruct returnValue = ManagedNativeVarargTests.TestEchoFourLongStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneFloatStructManagedNoVararg()
        {
            OneFloatStruct arg = new OneFloatStruct();
            arg.a = 1.0f;

            OneFloatStruct returnValue = ManagedNativeVarargTests.TestEchoOneFloatStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoFloatStructManagedNoVararg()
        {
            TwoFloatStruct arg = new TwoFloatStruct();
            arg.a = 1.0f;
            arg.b = 2.0f;

            TwoFloatStruct returnValue = ManagedNativeVarargTests.TestEchoTwoFloatStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneDoubleStructManagedNoVararg()
        {
            OneDoubleStruct arg = new OneDoubleStruct();
            arg.a = 1.0;

            OneDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoOneDoubleStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoDoubleStructManagedNoVararg()
        {
            TwoDoubleStruct arg = new TwoDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;

            TwoDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoTwoDoubleStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoThreeDoubleStructManagedNoVararg()
        {
            ThreeDoubleStruct arg = new ThreeDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;

            ThreeDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoThreeDoubleStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b && arg.c == returnValue.c;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourFloatStructManagedNoVararg()
        {
            FourFloatStruct arg = new FourFloatStruct();
            arg.a = 1.0f;
            arg.b = 2.0f;
            arg.c = 3.0f;
            arg.d = 4.0f;

            FourFloatStruct returnValue = ManagedNativeVarargTests.TestEchoFourFloatStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourDoubleStructManagedNoVararg()
        {
            FourDoubleStruct arg = new FourDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;
            arg.d = 4.0;

            FourDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoFourDoubleStructManagedNoVararg(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoByteManaged(byte arg)
        {
            byte returnValue = ManagedNativeVarargTests.TestEchoByteManaged(arg, __arglist(arg));

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoCharManaged(char arg)
        {
            char returnValue = ManagedNativeVarargTests.TestEchoCharManaged(arg, __arglist(arg));

            return returnValue == arg;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoShortManaged(short arg)
        {
            short returnValue = ManagedNativeVarargTests.TestEchoShortManaged(arg, __arglist(arg));

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoIntManaged(int arg)
        {
            int returnValue = ManagedNativeVarargTests.TestEchoIntManaged(arg, __arglist(arg));

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoLongManaged(long arg)
        {
            long returnValue = ManagedNativeVarargTests.TestEchoLongManaged(arg, __arglist(arg));

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFloatManaged(float arg)
        {
            float returnValue = ManagedNativeVarargTests.TestEchoFloatManaged(arg, __arglist(arg));

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoDoubleManaged(double arg)
        {
            double returnValue = ManagedNativeVarargTests.TestEchoDoubleManaged(arg, __arglist(arg));

            return returnValue == arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneIntStructManaged()
        {
            OneIntStruct arg = new OneIntStruct();
            arg.a = 1;

            OneIntStruct returnValue = ManagedNativeVarargTests.TestEchoOneIntStructManaged(arg, __arglist(arg));

            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoIntStructManaged()
        {
            TwoIntStruct arg = new TwoIntStruct();
            arg.a = 1;
            arg.b = 2;

            TwoIntStruct returnValue = ManagedNativeVarargTests.TestEchoTwoIntStructManaged(arg, __arglist(arg));

            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneLongStructManaged()
        {
            OneLongStruct arg = new OneLongStruct();
            arg.a = 1;

            OneLongStruct returnValue = ManagedNativeVarargTests.TestEchoOneLongStructManaged(arg, __arglist(arg));

            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoLongStructManaged()
        {
            TwoLongStruct arg = new TwoLongStruct();
            arg.a = 1;
            arg.b = 2;

            TwoLongStruct returnValue = ManagedNativeVarargTests.TestEchoTwoLongStructManaged(arg, __arglist(arg));

            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoEightByteStructStructManaged()
        {
            EightByteStruct arg = new EightByteStruct();
            arg.one = 1;
            arg.two = 2;
            arg.three = 3;
            arg.four = 4;
            arg.five = 5;
            arg.six = 6;
            arg.seven = 7;
            arg.eight = 8;

            EightByteStruct returnValue = ManagedNativeVarargTests.TestEchoEightByteStructStructManaged(arg, __arglist(arg));

            bool equal = arg.one == returnValue.one &&
                         arg.two == returnValue.two &&
                         arg.three == returnValue.three &&
                         arg.four == returnValue.four &&
                         arg.five == returnValue.five &&
                         arg.six == returnValue.six &&
                         arg.seven == returnValue.seven &&
                         arg.eight == returnValue.eight;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourIntStructManaged()
        {
            FourIntStruct arg = new FourIntStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourIntStruct returnValue = ManagedNativeVarargTests.TestEchoFourIntStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoSixteenByteStructManaged()
        {
            SixteenByteStruct arg = new SixteenByteStruct();
            arg.one = 1;
            arg.two = 2;
            arg.three = 3;
            arg.four = 4;
            arg.five = 5;
            arg.six = 6;
            arg.seven = 7;
            arg.eight = 8;
            arg.nine = 9;
            arg.ten = 10;
            arg.eleven = 11;
            arg.twelve = 12;
            arg.thirteen = 13;
            arg.fourteen = 14;
            arg.fifteen = 15;
            arg.sixteen = 16;

            SixteenByteStruct returnValue = ManagedNativeVarargTests.TestEchoSixteenByteStructManaged(arg, __arglist(arg));

            bool equal = arg.one == returnValue.one &&
                         arg.two == returnValue.two &&
                         arg.three == returnValue.three &&
                         arg.four == returnValue.four &&
                         arg.five == returnValue.five &&
                         arg.six == returnValue.six &&
                         arg.seven == returnValue.seven &&
                         arg.eight == returnValue.eight &&
                         arg.nine == returnValue.nine &&
                         arg.ten == returnValue.ten &&
                         arg.eleven == returnValue.eleven &&
                         arg.twelve == returnValue.twelve &&
                         arg.thirteen == returnValue.thirteen &&
                         arg.fourteen == returnValue.fourteen &&
                         arg.fifteen == returnValue.fifteen &&
                         arg.sixteen == returnValue.sixteen;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourLongStructManaged()
        {
            FourLongStruct arg = new FourLongStruct();
            arg.a = 1;
            arg.b = 2;
            arg.c = 3;
            arg.d = 4;

            FourLongStruct returnValue = ManagedNativeVarargTests.TestEchoFourLongStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneFloatStructManaged()
        {
            OneFloatStruct arg = new OneFloatStruct();
            arg.a = 1.0f;

            OneFloatStruct returnValue = ManagedNativeVarargTests.TestEchoOneFloatStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoFloatStructManaged()
        {
            TwoFloatStruct arg = new TwoFloatStruct();
            arg.a = 1.0f;
            arg.b = 2.0f;

            TwoFloatStruct returnValue = ManagedNativeVarargTests.TestEchoTwoFloatStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoOneDoubleStructManaged()
        {
            OneDoubleStruct arg = new OneDoubleStruct();
            arg.a = 1.0;

            OneDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoOneDoubleStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoTwoDoubleStructManaged()
        {
            TwoDoubleStruct arg = new TwoDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;

            TwoDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoTwoDoubleStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoThreeDoubleStructManaged()
        {
            ThreeDoubleStruct arg = new ThreeDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;

            ThreeDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoThreeDoubleStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a && arg.b == returnValue.b && arg.c == returnValue.c;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourFloatStructManaged()
        {
            FourFloatStruct arg = new FourFloatStruct();
            arg.a = 1.0f;
            arg.b = 2.0f;
            arg.c = 3.0f;
            arg.d = 4.0f;

            FourFloatStruct returnValue = ManagedNativeVarargTests.TestEchoFourFloatStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourDoubleStructManaged()
        {
            FourDoubleStruct arg = new FourDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;
            arg.d = 4.0;

            FourDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoFourDoubleStructManaged(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestShortInByteOutNoVararg(short arg)
        {
            byte returnValue = short_in_byte_out(arg, __arglist());

            return returnValue == (byte)arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestByteInShortOutNoVararg(byte arg)
        {
            short returnValue = byte_in_short_out(arg, __arglist());

            return returnValue == (short)arg;
        }

        // Tests that take the address of a parameter of a vararg method

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourDoubleStructManagedViaAddress()
        {
            FourDoubleStruct arg = new FourDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;
            arg.d = 4.0;

            FourDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoFourDoubleStructManagedViaAddress(arg, __arglist(arg));
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        // Miscellaneous tests

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TestEchoFourDoubleStructViaParameterAssign()
        {
            FourDoubleStruct arg = new FourDoubleStruct();
            arg.a = 1.0;
            arg.b = 2.0;
            arg.c = 3.0;
            arg.d = 4.0;

            FourDoubleStruct returnValue = ManagedNativeVarargTests.TestEchoFourDoubleStructViaParameterAssign(arg, __arglist());
            bool equal = arg.a == returnValue.a &&
                         arg.b == returnValue.b &&
                         arg.c == returnValue.c &&
                         arg.d == returnValue.d;

            return equal;
        }

        ////////////////////////////////////////////////////////////////////////
        // Report Failure
        ////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ReportFailure(bool success, string name, int old_val, int new_val)
        {
            ++m_testCount;
            if (!success)
            {
                printf("Failure: %s\n", __arglist(name));

                ++m_failCount;
                return new_val;
            }
            else
            {
                printf("Passed: %s\n", __arglist(name));
                ++m_passCount;
            }

            return old_val;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ReportFailure(bool success, string name, bool old_val, bool new_val)
        {
            ++m_testCount;
            if (!success)
            {
                printf("Failure: %s\n", __arglist(name));

                ++m_failCount;
                return new_val;
            }
            else
            {
                printf("Passed: %s\n", __arglist(name));
                ++m_passCount;
            }

            return old_val;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ReportFailure(int success, string name, bool old_val, bool new_val)
        {
            ++m_testCount;
            if (success != 100)
            {
                printf("Failure: %s\n", __arglist(name));

                ++m_failCount;
                return new_val;
            }
            else
            {
                printf("Passed: %s\n", __arglist(name));
                ++m_passCount;
            }

            return old_val;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ReportFailure(int success, string name, int old_val, int new_val)
        {
            ++m_testCount;
            if (success != 100)
            {
                printf("Failure: %s\n", __arglist(name));

                ++m_failCount;
                return new_val;
            }
            else
            {
                printf("Passed: %s\n", __arglist(name));
                ++m_passCount;
            }

            return old_val;
        }

        ////////////////////////////////////////////////////////////////////////////
        // Main test driver
        ////////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int TestEntryPoint()
        {
            int success = 100;
            m_testCount = 0;

            success = ReportFailure(TestPassingIntsManaged(new int[] { 100, 299, -100, 50 }), "TestPassingIntsManaged(new int[] { 100, 299, -100, 50 })", success, 30);

            TestFour16ByteStructs();

            // !Varargs
            success = ReportFailure(TestPassingTenEightBytes(), "TestPassingTenEightBytes", success, 81);
            success = ReportFailure(TestPassingTenSixteenBytes(), "TestPassingTenSixteenBytes", success, 82);

            success = ReportFailure(TestPassingIntsNoVarargsManaged(), "TestPassingIntsNoVarargsManaged", success, 59);
            success = ReportFailure(TestPassingLongsNoVarargsManaged(), "TestPassingLongsNoVarargsManaged", success, 60);

            success = ReportFailure(TestPassingFloatsNoVarargsManaged(), "TestPassingFloatsNoVarargsManaged", success, 61);
            success = ReportFailure(TestPassingDoublesNoVarargsManaged(), "TestPassingDoublesNoVarargsManaged", success, 62);

            success = ReportFailure(TestPassingIntAndFloatsNoVarargsManaged(), "TestPassingIntAndFloatsNoVarargsManaged", success, 63);
            success = ReportFailure(TestPassingFloatsAndIntNoVarargsManaged(), "TestPassingFloatsAndIntNoVarargsManaged", success, 64);

            success = ReportFailure(TestPassingIntAndDoublesNoVarargsManaged(), "TestPassingIntAndDoublesNoVarargsManaged", success, 65);
            success = ReportFailure(TestPassingDoublesAndIntNoVarargsManaged(), "TestPassingDoublesAndIntNoVarargsManaged", success, 66);

            success = ReportFailure(TestPassingLongAndFloatsNoVarargsManaged(), "TestPassingLongAndFloatsNoVarargsManaged()", success, 67);
            success = ReportFailure(TestPassingFloatsAndlongNoVarargsManaged(), "TestPassingFloatsAndlongNoVarargsManaged()", success, 68);

            success = ReportFailure(TestPassinglongAndDoublesNoVarargsManaged(), "TestPassinglongAndDoublesNoVarargsManaged()", success, 69);
            success = ReportFailure(TestPassingDoublesAndlongNoVarargsManaged(), "TestPassingDoublesAndlongNoVarargsManaged()", success, 70);

            success = ReportFailure(TestPassingTwoIntStructsNoVarargsManaged(), "TestPassingTwoIntStructsNoVarargsManaged()", success, 71);
            success = ReportFailure(TestPassingFourIntStructsNoVarargsManaged(), "TestPassingFourIntStructsNoVaragsManaged()", success, 72);

            success = ReportFailure(TestPassingTwoLongStructsNoVarargsManaged(), "TestPassingTwoLongStructsNoVarargsManaged()", success, 73);
            success = ReportFailure(TestPassingTwoLongStructsWithIntAndLongNoVarargsManaged(), "TestPassingTwoLongStructsWithIntAndLongNoVarargsManaged()", success, 83);
            success = ReportFailure(TestPassingTwoLongStructsAndIntNoVarargsManaged(), "TestPassingTwoLongStructsAndIntNoVarargsManaged()", success, 74);

            success = ReportFailure(TestPassingFourLongStructsNoVarargsManaged(), "TestPassingFourLongStructsNoVarargsManaged()", success, 75);
            success = ReportFailure(TestPassingTwoFloatStructsNoVarargsManaged(), "TestPassingTwoFloatStructsNoVarargsManaged()", success, 76);

            success = ReportFailure(TestPassingFourFloatStructsNoVarargsManaged(), "TestPassingFourFloatStructsNoVarargsManaged()", success, 77);
            success = ReportFailure(TestPassingTwoDoubleStructsNoVarargsManaged() , "TestPassingTwoDoubleStructsNoVarargsManaged() ", success, 78);

            success = ReportFailure(TestPassingTwoLongStructsAndFloatNoVarargsManaged(), "TestPassingTwoLongStructsAndFloatNoVarargsManaged()", success, 79);
            success = ReportFailure(TestPassingFourDoubleStructsNoVarargsManaged(), "TestPassingFourDoubleStructsNoVarargsManaged()", success, 80);

            success = ReportFailure(TestPassingIntsManaged(new int[] { 100, 299, -100, 50 }), "TestPassingIntsManaged(new int[] { 100, 299, -100, 50 })", success, 30);
            success = ReportFailure(TestPassingLongsManaged(new long[] { 100L, 299L, -100L, 50L }), "TestPassingLongsManaged(new long[] { 100L, 299L, -100L, 50L })", success, 31);
            success = ReportFailure(TestPassingFloatsManaged(new float[] { 100.0f, 299.0f, -100.0f, 50.0f }), "TestPassingFloatsManaged(new float[] { 100.0f, 299.0f, -100.0f, 50.0f })", success, 32);
            success = ReportFailure(TestPassingDoublesManaged(new double[] { 100.0d, 299.0d, -100.0d, 50.0d }), "TestPassingDoublesManaged(new double[] { 100.0d, 299.0d, -100.0d, 50.0d })", success, 33);

            success = ReportFailure(TestPassingManyIntsManaged(new int[]
            {
                1002,
                40,
                39,
                12,
                14,
                -502,
                -13,
                11,
                98,
                45,
                3,
                80,
                7,
                -1,
                48,
                66,
                23,
                62,
                1092,
                -890,
                -20,
                -41,
                88,
                98,
                1,
                2,
                3,
                4012,
                16,
                673,
                873,
                45,
                85,
                -3041,
                22,
                62,
                401,
                901,
                501,
                1001,
                1002
            }), "TestPassingManyIntsManaged", success, 34);

            success = ReportFailure(TestPassingManyLongsManaged(new long[]
            {
                1002L,
                40L,
                39L,
                12L,
                14L,
                -502L,
                -13L,
                11L,
                98L,
                45L,
                3L,
                80L,
                7L,
                -1L,
                48L,
                66L,
                23L,
                62L,
                1092L,
                -890L,
                -20L,
                -41L,
                88L,
                98L,
                1L,
                2L,
                3L,
                4012L,
                16L,
                673L,
                873L,
                45L,
                85L,
                -3041L,
                22L,
                62L,
                401L,
                901L,
                501L,
                1001L,
                1002L
            }), "TestPassingManyLongsManaged", success, 35);

            success = ReportFailure(TestPassingManyFloatsManaged(new float[]
            {
                1002,
                40,
                39,
                12,
                14,
                -502,
                -13,
                11,
                98,
                45,
                3,
                80,
                7,
                -1,
                48,
                66,
                23,
                62,
                1092,
                -890,
                -20,
                -41,
                88,
                98,
                1,
                2,
                3,
                4012,
                16,
                673,
                873,
                45,
                85,
                -3041,
                22,
                62,
                401,
                901,
                501,
                1001,
                1002
            }), "TestPassingManyFloatsManaged", success, 36);

            success = ReportFailure(TestPassingManyDoublesManaged(new double[]
            {
                1002,
                40,
                39,
                12,
                14,
                -502,
                -13,
                11,
                98,
                45,
                3,
                80,
                7,
                -1,
                48,
                66,
                23,
                62,
                1092,
                -890,
                -20,
                -41,
                88,
                98,
                1,
                2,
                3,
                4012,
                16,
                673,
                873,
                45,
                85,
                -3041,
                22,
                62,
                401,
                901,
                501,
                1001,
                1002
            }), "TestPassingManyDoublesManaged", success, 37);

            success = ReportFailure(TestPassingIntsAndLongsManaged(new int[] { 100, 200 }, new long[] { 102312131L, 91239191L }), "TestPassingIntsAndLongsManaged(new int[] { 100, 200 }, new long[] { 102312131L, 91239191L })", success, 38);
            success = ReportFailure(TestPassingFloatsAndDoublesManaged(new float[] { 100.0F, 200.0F }, new double[] { 12.1231321, 441.2332132335342321 }), "TestPassingFloatsAndDoublesManaged(new float[] { 100.0F, 200.0F }, new double[] { 12.1231321, 441.2332132335342321 })", success, 39);

            success = ReportFailure(TestPassingIntsAndFloatsManaged(), "TestPassingIntsAndFloatsManaged()", success, 40);
            success = ReportFailure(TestPassingLongsAndDoublesManaged(), "TestPassingLongsAndDoublesManaged()", success, 41);

            // Try passing empty varargs.
            success = ReportFailure(TestPassingEmptyIntsManaged(new int[] { }), "TestPassingEmptyIntsManaged(new int[] { })", success, 42);
            success = ReportFailure(TestPassingEmptyLongsManaged(new long[] { }), "TestPassingEmptyLongsManaged(new long[] { })", success, 43);
            success = ReportFailure(TestPassingEmptyFloatsManaged(new float[] { }), "TestPassingEmptyFloatsManaged(new float[] { })", success, 44);
            success = ReportFailure(TestPassingEmptyDoubleManaged(new double[] { }), "TestPassingEmptyDoubleManaged(new double[] { })", success, 45);

            success = ReportFailure(TestPassingStructsManaged(), "TestPassingStructsManaged()", success, TestPassingStructsManaged());

            ////////////////////////////////////////////////////////////////////
            // PInvoke Tests
            ////////////////////////////////////////////////////////////////////

            success = ReportFailure(TestPassingInts(new int[] { 100, 299, -100, 50 }), "TestPassingInts(new int[] { 100, 299, -100, 50 })", success, 1);
            success = ReportFailure(TestPassingLongs(new long[] { 100L, 299L, -100L, 50L }), "TestPassingLongs(new long[] { 100L, 299L, -100L, 50L })", success, 2);
            success = ReportFailure(TestPassingFloats(new float[] { 100.0f, 299.0f, -100.0f, 50.0f }), "TestPassingFloats(new float[] { 100.0f, 299.0f, -100.0f, 50.0f })", success, 3);
            success = ReportFailure(TestPassingDoubles(new double[] { 100.0d, 299.0d, -100.0d, 50.0d }), "TestPassingDoubles(new double[] { 100.0d, 299.0d, -100.0d, 50.0d })", success, 4);

            success = ReportFailure(TestPassingManyInts(new int[]
            {
                1002,
                40,
                39,
                12,
                14,
                -502,
                -13,
                11,
                98,
                45,
                3,
                80,
                7,
                -1,
                48,
                66,
                23,
                62,
                1092,
                -890,
                -20,
                -41,
                88,
                98,
                1,
                2,
                3,
                4012,
                16,
                673,
                873,
                45,
                85,
                -3041,
                22,
                62,
                401,
                901,
                501,
                1001,
                1002
            }), "TestPassingManyInts", success, 5);

            success = ReportFailure(TestPassingManyLongs(new long[]
            {
                1002L,
                40L,
                39L,
                12L,
                14L,
                -502L,
                -13L,
                11L,
                98L,
                45L,
                3L,
                80L,
                7L,
                -1L,
                48L,
                66L,
                23L,
                62L,
                1092L,
                -890L,
                -20L,
                -41L,
                88L,
                98L,
                1L,
                2L,
                3L,
                4012L,
                16L,
                673L,
                873L,
                45L,
                85L,
                -3041L,
                22L,
                62L,
                401L,
                901L,
                501L,
                1001L,
                1002L
            }), "TestPassingManyLongs", success, 6);

            // Passing doubles to native method.
            success = ReportFailure(TestPassingManyFloats(new double[]
            {
                1002,
                40,
                39,
                12,
                14,
                -502,
                -13,
                11,
                98,
                45,
                3,
                80,
                7,
                -1,
                48,
                66,
                23,
                62,
                1092,
                -890,
                -20,
                -41,
                88,
                98,
                1,
                2,
                3,
                4012,
                16,
                673,
                873,
                45,
                85,
                -3041,
                22,
                62,
                401,
                901,
                501,
                1001,
                1002
            }), "TestPassingManyFloats", success, 7);

            success = ReportFailure(TestPassingManyDoubles(new double[]
            {
                1002,
                40,
                39,
                12,
                14,
                -502,
                -13,
                11,
                98,
                45,
                3,
                80,
                7,
                -1,
                48,
                66,
                23,
                62,
                1092,
                -890,
                -20,
                -41,
                88,
                98,
                1,
                2,
                3,
                4012,
                16,
                673,
                873,
                45,
                85,
                -3041,
                22,
                62,
                401,
                901,
                501,
                1001,
                1002
            }), "TestPassingManyDoubles", success, 8);

            success = ReportFailure(TestPassingIntsAndLongs(new int[] { 100, 200 }, new long[] { 102312131L, 91239191L }), "TestPassingIntsAndLongs(new int[] { 100, 200 }, new long[] { 102312131L, 91239191L })", success, 9);
            success = ReportFailure(TestPassingFloatsAndDoubles(new float[] { 100.0F, 200.0F }, new double[] { 12.1231321, 441.2332132335342321 }), "TestPassingFloatsAndDoubles(new float[] { 100.0F, 200.0F }, new double[] { 12.1231321, 441.2332132335342321 })", success, 10);

            success = ReportFailure(TestPassingIntsAndFloats(), "TestPassingIntsAndFloats()", success, 28);
            success = ReportFailure(TestPassingLongsAndDoubles(), "TestPassingLongsAndDoubles()", success, 29);

            // Try passing empty varargs.
            success = ReportFailure(TestPassingEmptyInts(new int[] { }), "TestPassingEmptyInts(new int[] { })", success, 11);
            success = ReportFailure(TestPassingEmptyLongs(new long[] { }), "TestPassingEmptyLongs(new long[] { })", success, 12);
            success = ReportFailure(TestPassingEmptyFloats(new float[] { }), "TestPassingEmptyFloats(new float[] { })", success, 13);
            success = ReportFailure(TestPassingEmptyDouble(new double[] { }), "TestPassingEmptyDouble(new double[] { })", success, 14);

            success = ReportFailure(TestPassingStructs(), "TestPassingStructs()", success, TestPassingStructs());

            success = ReportFailure(TestPassingTwentyFourByteStructs(), "TestPassingTwentyFourByteStructs()", success, 108);

            // Managed to managed Echo types.
            // return passed fixed arg
            success = ReportFailure(TestEchoByteManagedNoVararg(1), "TestEchoByteManagedNoVararg(1)", success, 109);
            success = ReportFailure(TestEchoCharManagedNoVararg('c'), "TestEchoCharManagedNoVararg(1)", success, 110);
            success = ReportFailure(TestEchoShortManagedNoVararg(2), "TestEchoShortManagedNoVararg(2)", success, 111);
            success = ReportFailure(TestEchoIntManagedNoVararg(3), "TestEchoIntManagedNoVararg(3)", success, 112);
            success = ReportFailure(TestEchoLongManagedNoVararg(4), "TestEchoLongManagedNoVararg(4)", success, 113);
            success = ReportFailure(TestEchoFloatManagedNoVararg(5.0f), "TestEchoFloatManagedNoVararg(5.0f)", success, 114);
            success = ReportFailure(TestEchoDoubleManagedNoVararg(6.0), "TestEchoDoubleManagedNoVararg(6.0)", success, 115);
            success = ReportFailure(TestEchoOneIntStructManagedNoVararg(), "TestEchoOneIntStructManagedNoVararg()", success, 116);
            success = ReportFailure(TestEchoTwoIntStructManagedNoVararg(), "TestEchoTwoIntStructManagedNoVararg()", success, 117);
            success = ReportFailure(TestEchoOneLongStructManagedNoVararg(), "TestEchoOneLongStructManagedNoVararg()", success, 118);
            success = ReportFailure(TestEchoTwoLongStructManagedNoVararg(), "TestEchoTwoLongStructManagedNoVararg()", success, 119);
            success = ReportFailure(TestEchoEightByteStructStructManagedNoVararg(), "TestEchoEightByteStructStructManagedNoVararg()", success, 120);
            success = ReportFailure(TestEchoFourIntStructManagedNoVararg(), "TestEchoFourIntStructManagedNoVararg()", success, 121);
            success = ReportFailure(TestEchoSixteenByteStructManagedNoVararg(), "TestEchoSixteenByteStructManagedNoVararg()", success, 122);
            success = ReportFailure(TestEchoFourLongStruct(), "TestEchoFourLongStruct()", success, 123);
            success = ReportFailure(TestEchoFourLongStructManagedNoVararg(), "TestEchoFourLongStructManagedNoVararg()", success, 124);
            success = ReportFailure(TestEchoOneFloatStructManagedNoVararg(), "TestEchoOneFloatStructManagedNoVararg()", success, 125);
            success = ReportFailure(TestEchoTwoFloatStructManagedNoVararg(), "TestEchoTwoFloatStructManagedNoVararg()", success, 126);
            success = ReportFailure(TestEchoOneDoubleStructManagedNoVararg(), "TestEchoOneDoubleStructManagedNoVararg()", success, 127);
            success = ReportFailure(TestEchoTwoDoubleStructManagedNoVararg(), "TestEchoTwoDoubleStructManagedNoVararg()", success, 128);
            success = ReportFailure(TestEchoThreeDoubleStructManagedNoVararg(), "TestEchoThreeDoubleStructManagedNoVararg()", success, 129);
            success = ReportFailure(TestEchoFourFloatStructManagedNoVararg(), "TestEchoFourFloatStructManagedNoVararg()", success, 130);
            success = ReportFailure(TestEchoFourDoubleStructManagedNoVararg(), "TestEchoFourDoubleStructManagedNoVararg()", success, 131);

            // Managed to managed Echo types.
            // return passed vararg
            success = ReportFailure(TestEchoByteManaged(1), "TestEchoByteManaged(1)", success, 132);
            success = ReportFailure(TestEchoCharManaged('c'), "TestEchoCharManaged(1)", success, 133);
            success = ReportFailure(TestEchoShortManaged(2), "TestEchoShortManaged(2)", success, 134);
            success = ReportFailure(TestEchoIntManaged(3), "TestEchoIntManaged(3)", success, 135);
            success = ReportFailure(TestEchoLongManaged(4), "TestEchoLongManaged(4)", success, 136);
            success = ReportFailure(TestEchoFloatManaged(5.0f), "TestEchoFloatManaged(5.0f)", success, 137);
            success = ReportFailure(TestEchoDoubleManaged(6.0), "TestEchoDoubleManaged(6.0)", success, 138);
            success = ReportFailure(TestEchoOneIntStructManaged(), "TestEchoOneIntStructManaged()", success, 139);
            success = ReportFailure(TestEchoTwoIntStructManaged(), "TestEchoTwoIntStructManaged()", success, 140);
            success = ReportFailure(TestEchoOneLongStructManaged(), "TestEchoOneLongStructManaged()", success, 141);
            success = ReportFailure(TestEchoTwoLongStructManaged(), "TestEchoTwoLongStructManaged()", success, 142);
            success = ReportFailure(TestEchoEightByteStructStructManaged(), "TestEchoEightByteStructStructManaged()", success, 143);
            success = ReportFailure(TestEchoFourIntStructManaged(), "TestEchoFourIntStructManaged()", success, 144);
            success = ReportFailure(TestEchoSixteenByteStructManaged(), "TestEchoSixteenByteStructManaged()", success, 145);
            success = ReportFailure(TestEchoFourLongStruct(), "TestEchoFourLongStruct()", success, 146);
            success = ReportFailure(TestEchoFourLongStructManaged(), "TestEchoFourLongStructManaged()", success, 147);
            success = ReportFailure(TestEchoOneFloatStructManaged(), "TestEchoOneFloatStructManaged()", success, 148);
            success = ReportFailure(TestEchoTwoFloatStructManaged(), "TestEchoTwoFloatStructManaged()", success, 149);
            success = ReportFailure(TestEchoOneDoubleStructManaged(), "TestEchoOneDoubleStructManaged()", success, 150);
            success = ReportFailure(TestEchoTwoDoubleStructManaged(), "TestEchoTwoDoubleStructManaged()", success, 151);
            success = ReportFailure(TestEchoThreeDoubleStructManaged(), "TestEchoThreeDoubleStructManaged()", success, 152);
            success = ReportFailure(TestEchoFourFloatStructManaged(), "TestEchoFourFloatStructManaged()", success, 153);
            success = ReportFailure(TestEchoFourDoubleStructManaged(), "TestEchoFourDoubleStructManaged()", success, 154);

            // Echo types.
            success = ReportFailure(TestEchoByteNoVararg(1), "TestEchoByteNoVararg(1)", success, 85);
            success = ReportFailure(TestEchoCharNoVararg('c'), "TestEchoCharNoVararg(1)", success, 86);
            success = ReportFailure(TestEchoShortNoVararg(2), "TestEchoShortNoVararg(2)", success, 87);
            success = ReportFailure(TestEchoIntNoVararg(3), "TestEchoIntNoVararg(3)", success, 88);
            success = ReportFailure(TestEchoLongNoVararg(4), "TestEchoLongNoVararg(4)", success, 89);
            success = ReportFailure(TestEchoFloatNoVararg(5.0f), "TestEchoFloatNoVararg(5.0f)", success, 90);
            success = ReportFailure(TestEchoDoubleNoVararg(6.0), "TestEchoDoubleNoVararg(6.0)", success, 91);
            success = ReportFailure(TestEchoOneIntStructNoVararg(), "TestEchoOneIntStructNoVararg()", success, 92);
            success = ReportFailure(TestEchoTwoIntStructNoVararg(), "TestEchoTwoIntStructNoVararg()", success, 93);
            success = ReportFailure(TestEchoOneLongStructNoVararg(), "TestEchoOneLongStructNoVararg()", success, 94);
            success = ReportFailure(TestEchoTwoLongStructNoVararg(), "TestEchoTwoLongStructNoVararg()", success, 95);
            success = ReportFailure(TestEchoEightByteStructStructNoVararg(), "TestEchoEightByteStructStructNoVararg()", success, 96);
            success = ReportFailure(TestEchoFourIntStructNoVararg(), "TestEchoFourIntStructNoVararg()", success, 97);
            success = ReportFailure(TestEchoSixteenByteStructNoVararg(), "TestEchoSixteenByteStructNoVararg()", success, 98);
            success = ReportFailure(TestEchoFourLongStruct(), "TestEchoFourLongStruct()", success, 108);
            success = ReportFailure(TestEchoFourLongStructNoVararg(), "TestEchoFourLongStructNoVararg()", success, 99);
            success = ReportFailure(TestEchoOneFloatStructNoVararg(), "TestEchoOneFloatStructNoVararg()", success, 101);
            success = ReportFailure(TestEchoTwoFloatStructNoVararg(), "TestEchoTwoFloatStructNoVararg()", success, 102);
            success = ReportFailure(TestEchoOneDoubleStructNoVararg(), "TestEchoOneDoubleStructNoVararg()", success, 103);
            success = ReportFailure(TestEchoTwoDoubleStructNoVararg(), "TestEchoTwoDoubleStructNoVararg()", success, 104);
            success = ReportFailure(TestEchoThreeDoubleStructNoVararg(), "TestEchoThreeDoubleStructNoVararg()", success, 105);
            success = ReportFailure(TestEchoFourFloatStructNoVararg(), "TestEchoFourFloatStructNoVararg()", success, 106);
            success = ReportFailure(TestEchoFourDoubleStructNoVararg(), "TestEchoFourDoubleStructNoVararg()", success, 107);

            success = ReportFailure(TestShortInByteOutNoVararg(7), "TestShortInByteOutNoVararg(7)", success, 108);
            success = ReportFailure(TestByteInShortOutNoVararg(8), "TestByteInShortOutNoVararg(8)", success, 109);

            // Parameter address tests
            success = ReportFailure(TestEchoFourDoubleStructManagedViaAddress(), "TestEchoFourDoubleStructManagedViaAddress()", success, 155);

            // Miscellaneous tests
            success = ReportFailure(TestEchoFourDoubleStructViaParameterAssign(), "TestEchoFourDoubleStructViaParameterAssign()", success, 156);

            printf("\n", __arglist());
            printf("%d Tests run. %d Passed, %d Failed.\n", __arglist(m_testCount, m_passCount, m_failCount));

            return success;
        }
    }
}
