// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for compare AND chains of length 2.

using System;
using System.Runtime.CompilerServices;

public class ComparisonTestAnd2Chains
{
    // Using bitwise AND to ensure compare chains are generated.

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_byte_2(byte a1, byte a2) => a1 == 10 & a2 == 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_short_2(short a1, short a2) => a1 == 10 & a2 == 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_int_2(int a1, int a2) => a1 == 10 & a2 == 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_long_2(long a1, long a2) => a1 == 10 & a2 == 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_ushort_2(ushort a1, ushort a2) => a1 == 10 & a2 == 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_uint_2(uint a1, uint a2) => a1 == 10 & a2 == 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_ulong_2(ulong a1, ulong a2) => a1 == 10 & a2 == 11;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_byte_2(byte a1, byte a2) => a1 != 5 & a2 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_short_2(short a1, short a2) => a1 != 5 & a2 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_int_2(int a1, int a2) => a1 != 5 & a2 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_long_2(long a1, long a2) => a1 != 5 & a2 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_ushort_2(ushort a1, ushort a2) => a1 != 5 & a2 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_uint_2(uint a1, uint a2) => a1 != 5 & a2 != 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_ulong_2(ulong a1, ulong a2) => a1 != 5 & a2 != 5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_byte_2(byte a1, byte a2) => a1 < 5 & a2 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_short_2(short a1, short a2) => a1 < 5 & a2 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_int_2(int a1, int a2) => a1 < 5 & a2 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_long_2(long a1, long a2) => a1 < 5 & a2 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_ushort_2(ushort a1, ushort a2) => a1 < 5 & a2 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_uint_2(uint a1, uint a2) => a1 < 5 & a2 < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_ulong_2(ulong a1, ulong a2) => a1 < 5 & a2 < 5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_byte_2(byte a1, byte a2) => a1 <= 5 & a2 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_short_2(short a1, short a2) => a1 <= 5 & a2 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_int_2(int a1, int a2) => a1 <= 5 & a2 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_long_2(long a1, long a2) => a1 <= 5 & a2 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_ushort_2(ushort a1, ushort a2) => a1 <= 5 & a2 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_uint_2(uint a1, uint a2) => a1 <= 5 & a2 <= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_ulong_2(ulong a1, ulong a2) => a1 <= 5 & a2 <= 5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_byte_2(byte a1, byte a2) => a1 > 5 & a2 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_short_2(short a1, short a2) => a1 > 5 & a2 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_int_2(int a1, int a2) => a1 > 5 & a2 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_long_2(long a1, long a2) => a1 > 5 & a2 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_ushort_2(ushort a1, ushort a2) => a1 > 5 & a2 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_uint_2(uint a1, uint a2) => a1 > 5 & a2 > 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_ulong_2(ulong a1, ulong a2) => a1 > 5 & a2 > 5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_byte_2(byte a1, byte a2) => a1 >= 5 & a2 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_short_2(short a1, short a2) => a1 >= 5 & a2 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_int_2(int a1, int a2) => a1 >= 5 & a2 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_long_2(long a1, long a2) => a1 >= 5 & a2 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_ushort_2(ushort a1, ushort a2) => a1 >= 5 & a2 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_uint_2(uint a1, uint a2) => a1 >= 5 & a2 >= 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_ulong_2(ulong a1, ulong a2) => a1 >= 5 & a2 >= 5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Main()
    {
        if (!Eq_byte_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_byte_2(10, 11) failed");
            return 101;
        }
        if (!Eq_short_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_short_2(10, 11) failed");
            return 101;
        }
        if (!Eq_int_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_int_2(10, 11) failed");
            return 101;
        }
        if (!Eq_long_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_long_2(10, 11) failed");
            return 101;
        }
        if (!Eq_ushort_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_ushort_2(10, 11) failed");
            return 101;
        }
        if (!Eq_uint_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_uint_2(10, 11) failed");
            return 101;
        }
        if (!Eq_ulong_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_ulong_2(10, 11) failed");
            return 101;
        }

        if (!Ne_byte_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_byte_2(10, 11) failed");
            return 101;
        }
        if (!Ne_short_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_short_2(10, 11) failed");
            return 101;
        }
        if (!Ne_int_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_int_2(10, 11) failed");
            return 101;
        }
        if (!Ne_long_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_long_2(10, 11) failed");
            return 101;
        }
        if (!Ne_ushort_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_ushort_2(10, 11) failed");
            return 101;
        }
        if (!Ne_uint_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_uint_2(10, 11) failed");
            return 101;
        }
        if (!Ne_ulong_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_ulong_2(10, 11) failed");
            return 101;
        }

        if (!Lt_byte_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_byte_2(3, 4) failed");
            return 101;
        }
        if (!Lt_short_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_short_2(3, 4) failed");
            return 101;
        }
        if (!Lt_int_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_int_2(3, 4) failed");
            return 101;
        }
        if (!Lt_long_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_long_2(3, 4) failed");
            return 101;
        }
        if (!Lt_ushort_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_ushort_2(3, 4) failed");
            return 101;
        }
        if (!Lt_uint_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_uint_2(3, 4) failed");
            return 101;
        }
        if (!Lt_ulong_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_ulong_2(3, 4) failed");
            return 101;
        }

        if (!Le_byte_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_byte_2(3, 4) failed");
            return 101;
        }
        if (!Le_short_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_short_2(3, 4) failed");
            return 101;
        }
        if (!Le_int_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_int_2(3, 4) failed");
            return 101;
        }
        if (!Le_long_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_long_2(3, 4) failed");
            return 101;
        }
        if (!Le_ushort_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_ushort_2(3, 4) failed");
            return 101;
        }
        if (!Le_uint_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_uint_2(3, 4) failed");
            return 101;
        }
        if (!Le_ulong_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_ulong_2(3, 4) failed");
            return 101;
        }

        if (!Gt_byte_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_byte_2(10, 11) failed");
            return 101;
        }
        if (!Gt_short_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_short_2(10, 11) failed");
            return 101;
        }
        if (!Gt_int_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_int_2(10, 11) failed");
            return 101;
        }
        if (!Gt_long_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_long_2(10, 11) failed");
            return 101;
        }
        if (!Gt_ushort_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_ushort_2(10, 11) failed");
            return 101;
        }
        if (!Gt_uint_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_uint_2(10, 11) failed");
            return 101;
        }
        if (!Gt_ulong_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_ulong_2(10, 11) failed");
            return 101;
        }

        if (!Ge_byte_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_byte_2(10, 11) failed");
            return 101;
        }
        if (!Ge_short_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_short_2(10, 11) failed");
            return 101;
        }
        if (!Ge_int_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_int_2(10, 11) failed");
            return 101;
        }
        if (!Ge_long_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_long_2(10, 11) failed");
            return 101;
        }
        if (!Ge_ushort_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_ushort_2(10, 11) failed");
            return 101;
        }
        if (!Ge_uint_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_uint_2(10, 11) failed");
            return 101;
        }
        if (!Ge_ulong_2(10, 11))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_ulong_2(10, 11) failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
