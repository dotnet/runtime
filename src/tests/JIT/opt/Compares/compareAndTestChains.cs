// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for compare AND chains that include a binary test.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ComparisonTestAndTestChains
{
    // Using bitwise AND to ensure compare chains are generated.

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_byte_bool(byte a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_short_bool(short a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_int_bool(int a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_long_bool(long a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_ushort_bool(ushort a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_uint_bool(uint a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_ulong_bool(ulong a1, bool a2) => (a1 == 10) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_float_bool(float a1, bool a2) => (a1 == 10.1f) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_double_bool(double a1, bool a2) => (a1 == 10.1) & !a2;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_byte_bool(byte a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_short_bool(short a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_int_bool(int a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_long_bool(long a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_ushort_bool(ushort a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_uint_bool(uint a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_ulong_bool(ulong a1, bool a2) => (a1 != 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_float_bool(float a1, bool a2) => (a1 != 5.1f) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_double_bool(double a1, bool a2) => (a1 != 5.1) & !a2;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_byte_bool(byte a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_short_bool(short a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_int_bool(int a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_long_bool(long a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_ushort_bool(ushort a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_uint_bool(uint a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_ulong_bool(ulong a1, bool a2) => (a1 < 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_float_bool(float a1, bool a2) => (a1 < 5.1f) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_double_bool(double a1, bool a2) => (a1 < 5.1) & !a2;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_byte_bool(byte a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_short_bool(short a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_int_bool(int a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_long_bool(long a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_ushort_bool(ushort a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_uint_bool(uint a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_ulong_bool(ulong a1, bool a2) => (a1 <= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_float_bool(float a1, bool a2) => (a1 <= 5.1f) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_double_bool(double a1, bool a2) => (a1 <= 5.1) & !a2;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_byte_bool(byte a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_short_bool(short a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_int_bool(int a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_long_bool(long a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_ushort_bool(ushort a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_uint_bool(uint a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_ulong_bool(ulong a1, bool a2) => (a1 > 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_float_bool(float a1, bool a2) => (a1 > 5.1f) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_double_bool(double a1, bool a2) => (a1 > 5.1) & !a2;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_byte_bool(byte a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_short_bool(short a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_int_bool(int a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_long_bool(long a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_ushort_bool(ushort a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_uint_bool(uint a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_ulong_bool(ulong a1, bool a2) => (a1 >= 5) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_float_bool(float a1, bool a2) => (a1 >= 5.1f) & !a2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_double_bool(double a1, bool a2) => (a1 >= 5.1) & !a2;


    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static int TestEntryPoint()
    {
        if (!Eq_byte_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_byte_bool(10, false) failed");
            return 101;
        }
        if (!Eq_short_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_short_bool(10, false) failed");
            return 101;
        }
        if (!Eq_int_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_int_bool(10, false) failed");
            return 101;
        }
        if (!Eq_long_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_long_bool(10, false) failed");
            return 101;
        }
        if (!Eq_ushort_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_ushort_bool(10, false) failed");
            return 101;
        }
        if (!Eq_uint_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_uint_bool(10, false) failed");
            return 101;
        }
        if (!Eq_ulong_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_ulong_bool(10, false) failed");
            return 101;
        }
        if (!Eq_float_bool(10.1f, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_float_bool(10.1, false) failed");
            return 101;
        }
        if (!Eq_double_bool(10.1, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Eq_double_bool(10.1, false) failed");
            return 101;
        }

        if (!Ne_byte_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_byte_bool(10, false) failed");
            return 101;
        }
        if (!Ne_short_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_short_bool(10, false) failed");
            return 101;
        }
        if (!Ne_int_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_int_bool(10, false) failed");
            return 101;
        }
        if (!Ne_long_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_long_bool(10, false) failed");
            return 101;
        }
        if (!Ne_ushort_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_ushort_bool(10, false) failed");
            return 101;
        }
        if (!Ne_uint_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_uint_bool(10, false) failed");
            return 101;
        }
        if (!Ne_ulong_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_ulong_bool(10, false) failed");
            return 101;
        }
        if (!Ne_float_bool(10.1f, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_float_bool(10.1, false) failed");
            return 101;
        }
        if (!Ne_double_bool(10.1, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ne_double_bool(10.1, false) failed");
            return 101;
        }

        if (!Lt_byte_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_byte_bool(3, false) failed");
            return 101;
        }
        if (!Lt_short_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_short_bool(3, false) failed");
            return 101;
        }
        if (!Lt_int_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_int_bool(3, false) failed");
            return 101;
        }
        if (!Lt_long_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_long_bool(3, false) failed");
            return 101;
        }
        if (!Lt_ushort_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_ushort_bool(3, false) failed");
            return 101;
        }
        if (!Lt_uint_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_uint_bool(3, false) failed");
            return 101;
        }
        if (!Lt_ulong_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_ulong_bool(3, false) failed");
            return 101;
        }
        if (!Lt_float_bool(3.1f, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_float_bool(3.1, false) failed");
            return 101;
        }
        if (!Lt_double_bool(3.1, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Lt_double_bool(3.1, false) failed");
            return 101;
        }

        if (!Le_byte_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_byte_bool(3, false) failed");
            return 101;
        }
        if (!Le_short_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_short_bool(3, false) failed");
            return 101;
        }
        if (!Le_int_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_int_bool(3, false) failed");
            return 101;
        }
        if (!Le_long_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_long_bool(3, false) failed");
            return 101;
        }
        if (!Le_ushort_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_ushort_bool(3, false) failed");
            return 101;
        }
        if (!Le_uint_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_uint_bool(3, false) failed");
            return 101;
        }
        if (!Le_ulong_bool(3, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_ulong_bool(3, false) failed");
            return 101;
        }
        if (!Le_float_bool(3.1f, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_float_bool(3.1, false) failed");
            return 101;
        }
        if (!Le_double_bool(3.1, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_double_bool(3.1, false) failed");
            return 101;
        }

        if (!Gt_byte_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_byte_bool(10, false) failed");
            return 101;
        }
        if (!Gt_short_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_short_bool(10, false) failed");
            return 101;
        }
        if (!Gt_int_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_int_bool(10, false) failed");
            return 101;
        }
        if (!Gt_long_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_long_bool(10, false) failed");
            return 101;
        }
        if (!Gt_ushort_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_ushort_bool(10, false) failed");
            return 101;
        }
        if (!Gt_uint_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_uint_bool(10, false) failed");
            return 101;
        }
        if (!Gt_ulong_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_ulong_bool(10, false) failed");
            return 101;
        }
        if (!Gt_float_bool(10.1f, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_float_bool(10.1, false) failed");
            return 101;
        }
        if (!Gt_double_bool(10.1, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Gt_double_bool(10.1, false) failed");
            return 101;
        }

        if (!Ge_byte_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_byte_bool(10, false) failed");
            return 101;
        }
        if (!Ge_short_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_short_bool(10, false) failed");
            return 101;
        }
        if (!Ge_int_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_int_bool(10, false) failed");
            return 101;
        }
        if (!Ge_long_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_long_bool(10, false) failed");
            return 101;
        }
        if (!Ge_ushort_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_ushort_bool(10, false) failed");
            return 101;
        }
        if (!Ge_uint_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_uint_bool(10, false) failed");
            return 101;
        }
        if (!Ge_ulong_bool(10, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Le_ulong_bool(10, false) failed");
            return 101;
        }
        if (!Ge_float_bool(10.1f, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_float_bool(10.1, false) failed");
            return 101;
        }
        if (!Ge_double_bool(10.1, false))
        {
            Console.WriteLine("ComparisonTestAndTestChains:Ge_double_bool(10.1, false) failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
