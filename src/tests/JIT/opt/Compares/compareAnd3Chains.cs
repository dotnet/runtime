// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for compare AND chains of length 3.

using System;
using System.Runtime.CompilerServices;

public class ComparisonTestAnd3Chains
{
    // Using bitwise AND to ensure compare chains are generated.

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_byte_3(byte a1, byte a2, byte a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_short_3(short a1, short a2, short a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_int_3(int a1, int a2, int a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_long_3(long a1, long a2, long a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_ushort_3(ushort a1, ushort a2, ushort a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_uint_3(uint a1, uint a2, uint a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_ulong_3(ulong a1, ulong a2, ulong a3) => a1 == 10 & a2 == 11 & a3 == 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_float_3(float a1, float a2, float a3) => a1 == 10.5f & a2 == 11.5f & a3 == 12.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_double_3(double a1, double a2, double a3) => a1 == 10.5 & a2 == 11.5 & a3 == 12.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_byte_3(byte a1, byte a2, byte a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_short_3(short a1, short a2, short a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_int_3(int a1, int a2, int a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_long_3(long a1, long a2, long a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_ushort_3(ushort a1, ushort a2, ushort a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_uint_3(uint a1, uint a2, uint a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_ulong_3(ulong a1, ulong a2, ulong a3) => a1 != 5 & a2 != 5 & a3 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_float_3(float a1, float a2, float a3) => a1 != 5.5f & a2 != 5.5f & a3 != 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_double_3(double a1, double a2, double a3) => a1 != 5.5 & a2 != 5.5 & a3 != 5.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_byte_3(byte a1, byte a2, byte a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_short_3(short a1, short a2, short a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_int_3(int a1, int a2, int a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_long_3(long a1, long a2, long a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_ushort_3(ushort a1, ushort a2, ushort a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_uint_3(uint a1, uint a2, uint a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_ulong_3(ulong a1, ulong a2, ulong a3) => a1 < 5 & a2 < 5 & a3 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_float_3(float a1, float a2, float a3) => a1 < 5.5f & a2 < 5.5f & a3 < 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_double_3(double a1, double a2, double a3) => a1 < 5.5 & a2 < 5.5 & a3 < 5.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_byte_3(byte a1, byte a2, byte a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_short_3(short a1, short a2, short a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_int_3(int a1, int a2, int a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_long_3(long a1, long a2, long a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_ushort_3(ushort a1, ushort a2, ushort a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_uint_3(uint a1, uint a2, uint a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_ulong_3(ulong a1, ulong a2, ulong a3) => a1 <= 5 & a2 <= 5 & a3 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_float_3(float a1, float a2, float a3) => a1 <= 5.5f & a2 <= 5.5f & a3 <= 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_double_3(double a1, double a2, double a3) => a1 <= 5.5 & a2 <= 5.5 & a3 <= 5.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_byte_3(byte a1, byte a2, byte a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_short_3(short a1, short a2, short a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_int_3(int a1, int a2, int a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_long_3(long a1, long a2, long a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_ushort_3(ushort a1, ushort a2, ushort a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_uint_3(uint a1, uint a2, uint a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_ulong_3(ulong a1, ulong a2, ulong a3) => a1 > 5 & a2 > 5 & a3 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_float_3(float a1, float a2, float a3) => a1 > 5.5f & a2 > 5.5f & a3 > 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_double_3(double a1, double a2, double a3) => a1 > 5.5 & a2 > 5.5 & a3 > 5.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_byte_3(byte a1, byte a2, byte a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_short_3(short a1, short a2, short a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_int_3(int a1, int a2, int a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_long_3(long a1, long a2, long a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_ushort_3(ushort a1, ushort a2, ushort a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_uint_3(uint a1, uint a2, uint a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_ulong_3(ulong a1, ulong a2, ulong a3) => a1 >= 5 & a2 >= 5 & a3 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_float_3(float a1, float a2, float a3) => a1 >= 5.5f & a2 >= 5.5f & a3 >= 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_double_3(double a1, double a2, double a3) => a1 >= 5.5 & a2 >= 5.5 & a3 >= 5.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Main()
    {
        if (!Eq_byte_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_byte_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_short_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_short_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_int_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_int_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_long_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_long_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_ushort_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_ushort_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_uint_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_uint_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_ulong_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_ulong_3(10, 11, 12) failed");
            return 101;
        }
        if (!Eq_float_3(10.5f, 11.5f, 12.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_float_3(10.5, 11.5, 12.5) failed");
            return 101;
        }
        if (!Eq_double_3(10.5, 11.5, 12.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_double_3(10.5, 11.5, 12.5) failed");
            return 101;
        }

        if (!Ne_byte_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_byte_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_short_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_short_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_int_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_int_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_long_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_long_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_ushort_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_ushort_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_uint_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_uint_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_ulong_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_ulong_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ne_float_3(10.5f, 11.5f, 12.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_float_3(10.5, 11.5, 12.5) failed");
            return 101;
        }
        if (!Ne_double_3(10.5, 11.5, 12.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_double_3(10.5, 11.5, 12.5) failed");
            return 101;
        }

        if (!Lt_byte_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_byte_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_short_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_short_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_int_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_int_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_long_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_long_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_ushort_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_ushort_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_uint_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_uint_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_ulong_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_ulong_3(2, 3, 4) failed");
            return 101;
        }
        if (!Lt_float_3(2.5f, 3.5f, 4.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_float_3(2.5, 3.5, 4.5) failed");
            return 101;
        }
        if (!Lt_double_3(2.5, 3.5, 4.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_double_3(2.5, 3.5, 4.5) failed");
            return 101;
        }

        if (!Le_byte_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_byte_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_short_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_short_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_int_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_int_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_long_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_long_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_ushort_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_ushort_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_uint_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_uint_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_ulong_3(2, 3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_ulong_3(2, 3, 4) failed");
            return 101;
        }
        if (!Le_float_3(2.5f, 3.5f, 4.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_float_3(2.5, 3.5, 4.5) failed");
            return 101;
        }
        if (!Le_double_3(2.5, 3.5, 4.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_double_3(2.5, 3.5, 4.5) failed");
            return 101;
        }

        if (!Gt_byte_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_byte_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_short_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_short_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_int_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_int_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_long_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_long_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_ushort_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_ushort_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_uint_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_uint_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_ulong_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_ulong_3(10, 11, 12) failed");
            return 101;
        }
        if (!Gt_float_3(10.5f, 11.5f, 12.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_float_3(10.5, 11.5, 12.5) failed");
            return 101;
        }
        if (!Gt_double_3(10.5, 11.5, 12.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_double_3(10.5, 11.5, 12.5) failed");
            return 101;
        }

        if (!Ge_byte_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_byte_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_short_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_short_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_int_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_int_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_long_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_long_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_ushort_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_ushort_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_uint_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_uint_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_ulong_3(10, 11, 12))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_ulong_3(10, 11, 12) failed");
            return 101;
        }
        if (!Ge_float_3(10.5f, 11.5f, 12.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_float_3(10.5, 11.5, 12.5) failed");
            return 101;
        }
        if (!Ge_double_3(10.5, 11.5, 12.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_double_3(10.5, 11.5, 12.5) failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
