// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for compare AND chains of length 2.

using System;
using System.Runtime.CompilerServices;
using Xunit;

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
    public static bool Eq_float_2(float a1, float a2) => a1 == 10.5f & a2 == 11.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Eq_double_2(double a1, double a2) => a1 == 10.5 & a2 == 11.5;


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
    public static bool Ne_float_2(float a1, float a2) => a1 != 5.5f & a2 != 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ne_double_2(double a1, double a2) => a1 != 5.5 & a2 != 5.5;


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
    public static bool Lt_float_2(float a1, float a2) => a1 < 5.5f & a2 < 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Lt_double_2(double a1, double a2) => a1 < 5.5 & a2 < 5.5;


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
    public static bool Le_float_2(float a1, float a2) => a1 <= 5.5f & a2 <= 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Le_double_2(double a1, double a2) => a1 <= 5.5 & a2 <= 5.5;


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
    public static bool Gt_float_2(float a1, float a2) => a1 > 5.5f & a2 > 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Gt_double_2(double a1, double a2) => a1 > 5.5 & a2 > 5.5;


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
    public static bool Ge_float_2(float a1, float a2) => a1 >= 5.5f & a2 >= 5.5f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Ge_double_2(double a1, double a2) => a1 >= 5.5 & a2 >= 5.5;


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void consume<T>(T a1, T a2) {}

    // If conditions that are consumed.

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Lt_byte_2_consume(byte a1, byte a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #10
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #11, nc, {{ge|lt}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        if (a1 < 10 || a2 < 11) { a1 = 10; }
        consume<byte>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(12, 13)]
    public static void Le_short_2_consume(short a1, short a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #10
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #12, 0, {{gt|le}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        if (a1 <= 10 && a2 <= 12) { a1 = 10; }
        consume<short>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(14, 15)]
    public static void Gt_int_2_consume(int a1, int a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #10
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #13, 0, {{le|gt}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{le|gt}}
        if (a1 > 10 || a2 > 13) { a1 = 10; }
        consume<int>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(16, 17)]
    public static void Ge_long_2_consume(long a1, long a2) {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #10
        //ARM64-FULL-LINE-NEXT: ccmp {{x[0-9]+}}, #14, nc, {{lt|ge}}
        //ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{lt|ge}}
        if (a1 >= 10 && a2 >= 14) { a1 = 10; }
        consume<long>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(18, 19)]
    public static void Eq_ushort_2_consume(ushort a1, ushort a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #10
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #15, z, {{ne|eq}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ne|eq}}
        if (a1 == 10 || a2 == 15) { a1 = 10; }
        consume<ushort>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(20, 21)]
    public static void Ne_uint_2_consume(uint a1, uint a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #10
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #16, z, {{eq|ne}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{eq|ne}}
        if (a1 != 10 && a2 != 16) { a1 = 10; }
        consume<uint>(a1, a2);
    }

    /* If/Else conditions that consume. */

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    [InlineData(12, 13)]
    public static void Le_else_byte_2_consume(byte a1, byte a2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #11
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #22, nzc, {{gt|le}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        if (a1 <= 11 || a2 <= 22) { a1 = 20; } else { a1 = 200; }
        consume<byte>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(14, 15)]
    public static void Gt_else_short_2_consume(short a1, short a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #11
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #23, nzc, {{le|gt}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{le|gt}}
        if (a1 > 11 && a2 > 23) { a1 = 20; } else { a1 = 200; }
        consume<short>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(16, 17)]
    public static void Ge_else_int_2_consume(int a1, int a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #11
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #24, z, {{lt|ge}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{lt|ge}}
        if (a1 >= 11 || a2 >= 24) { a1 = 20; } else { a1 = 200; }
        consume<int>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(18, 19)]
    public static void Eq_else_long_2_consume(long a1, long a2) {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #11
        //ARM64-FULL-LINE-NEXT: ccmp {{x[0-9]+}}, #25, 0, {{ne|eq}}
        //ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{ne|eq}}
        if (a1 == 11 && a2 == 25) { a1 = 20; } else { a1 = 200; }
        consume<long>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(20, 21)]
    public static void Ne_else_ushort_2_consume(ushort a1, ushort a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #11
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #26, 0, {{eq|ne}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{eq|ne}}
        if (a1 != 11 || a2 != 26) { a1 = 20; } else { a1 = 200; }
        consume<ushort>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(22, 23)]
    public static void Lt_else_uint_2_consume(uint a1, uint a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #11
        //ARM64-FULL-LINE-NEXT: ccmp {{w[0-9]+}}, #27, c, {{hs|lo}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{hs|lo}}
        if (a1 < 11 && a2 < 27) { a1 = 20; } else { a1 = 200; }
        consume<uint>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static int TestEntryPoint()
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
        if (!Eq_float_2(10.5f, 11.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_float_2(10.5, 11.5) failed");
            return 101;
        }
        if (!Eq_double_2(10.5, 11.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Eq_ulong_2(10.5, 11.5) failed");
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
        if (!Ne_float_2(10.5f, 11.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_float_2(10.5, 11.5) failed");
            return 101;
        }
        if (!Ne_double_2(10.5, 11.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ne_double_2(10.5, 11.5) failed");
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
        if (!Lt_float_2(3f, 4f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_float_2(3.5, 4.5) failed");
            return 101;
        }
        if (!Lt_double_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Lt_double_2(3.5, 4.5) failed");
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
        if (!Le_float_2(3f, 4f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_float_2(3.5, 4.5) failed");
            return 101;
        }
        if (!Le_double_2(3, 4))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Le_double_2(3.5, 4.5) failed");
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
        if (!Gt_float_2(10.5f, 11.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_float_2(10.5, 11.5) failed");
            return 101;
        }
        if (!Gt_double_2(10.5, 11.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Gt_ulong_2(10.5, 11.5) failed");
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
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_ulong_2(10, 11) failed");
            return 101;
        }
        if (!Ge_float_2(10.5f, 11.5f))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_float_2(10.5, 11.5) failed");
            return 101;
        }
        if (!Ge_double_2(10.5, 11.5))
        {
            Console.WriteLine("ComparisonTestAnd2Chains:Ge_double_2(10.5, 11.5) failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
