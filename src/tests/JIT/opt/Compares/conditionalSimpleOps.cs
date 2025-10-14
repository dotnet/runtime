// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ConditionalSimpleOpConstantTest
{
    [Theory]
    [InlineData(12, 10)]
    [InlineData(45, 5)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_left(byte op1, int expected)
    {
        int result = op1 < 42 ? 10 : 5;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, -13)]
    [InlineData(45, -25)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_right_arithmetic(byte op1, int expected)
    {
        int result = op1 > 42 ? -25 : -13;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, 0x7FFF_FFF3)]
    [InlineData(45, -25)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_right_logic(byte op1, int expected)
    {
        int result = op1 < 42 ? 0x7FFF_FFF3 : -25;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, 0x7FFF_FFFF_FFFF_FFF3ul)]
    [InlineData(45, 0xFFFF_FFFF_FFFF_FFE7ul)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_right_logic_ulong(byte op1, ulong expected)
    {
        ulong result = op1 < 42 ? 0x7FFF_FFFF_FFFF_FFF3ul : 0xFFFF_FFFF_FFFF_FFE7ul;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, 0x7FFF_FFF3)]
    [InlineData(45, 0xFFFF_FFE7)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_right_logic_long_32(byte op1, long expected)
    {
        long result = op1 > 42 ? 0xFFFF_FFE7 : 0x7FFF_FFF3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, 64)]
    [InlineData(45, 0)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void pow2_or_zero(byte op1, int expected)
    {
        int result = op1 < 42 ? 64 : 0;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, long.MinValue)]
    [InlineData(45, 0)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void pow2_or_zero_long(byte op1, long expected)
    {
        long result = op1 >= 42 ? 0 : long.MinValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, 0xFFFF_FFFF_8000_0000ul)]
    [InlineData(45, 0ul)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void pow2_or_zero_ulong_32(byte op1, ulong expected)
    {
        ulong result = op1 < 42 ? 0xFFFF_FFFF_8000_0000ul : 0ul;
        Assert.Equal(expected, result);
    }
}

public class ConditionalSimpleOpVariableTest
{
    [Theory]
    [InlineData(11, 12)]
    [InlineData(12, 13)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void add_var(int a, int expected)
    {
        a = a < 42 ? a + 1 : a;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(11, 12)]
    [InlineData(12, 13)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void add_var_no_else(int a, int expected)
    {
        if (a < 42)
            a++;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(11, 12)]
    [InlineData(12, 13)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void add_var_reversed(int a, int expected)
    {
        a = a > 42 ? a : ++a;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(12, 13)]
    [InlineData(13, 13)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void or_var(int a, int expected)
    {
        a = a < 42 ? a | 1 : a;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(12, 13)]
    [InlineData(13, 13)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void or_var_no_else(int a, int expected)
    {
        if (a < 42)
            a = a | 1;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(11, 10)]
    [InlineData(12, 11)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void sub_var(int a, int expected)
    {
        a = a < 42 ? a - 1 : a;
        Assert.Equal(expected, a);
    }

    public static int globVar = 0;
    [Theory]
    [InlineData(11, 10)]
    [InlineData(12, 11)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void sub_var_globref(int a, int expected)
    {
        a = (a + globVar) < 42 ? a - 1 : a;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(11, 10)]
    [InlineData(12, 11)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void sub_var_globref_no_else(int a, int expected)
    {
        if ((a + globVar) < 42)
            --a;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(12, 13)]
    [InlineData(13, 12)]
    [InlineData(45, 45)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void xor_var(int a, int expected)
    {
        a = a < 42 ? a ^ 1 : a;
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData(-12, -24)]
    [InlineData(12, 24)]
    [InlineData(43, 43)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_left_var(int a, int expected)
    {
        long result = a > 42 ? a : a * 2;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-12, -24)]
    [InlineData(12, 24)]
    [InlineData(43, 43)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_left_var_no_else(int a, int expected)
    {
        long result = a;
        if (a <= 42)
            result *= 2;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-12, -24)]
    [InlineData(12, 24)]
    [InlineData(43, 3)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_left_var_no_else_different_var(long a, long expected)
    {
        long result = 3;
        if (a <= 42)
            result = a * 2;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12, 6)]
    [InlineData(-25, -13)]
    [InlineData(45, 45)]
    [InlineData(-4000_000_000_000l, -2000_000_000_000l)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_right_arithmetic_var(long a, long expected)
    {
        long result = a > 42 ? a : a >> 1;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(43, 21)]
    [InlineData(0x8000_0000, 0x4000_0000)]
    [InlineData(12, 12)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void shift_right_logic_var(uint a, uint expected)
    {
        uint result = a > 42 ? a >> 1 : a;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(44, 0)]
    [InlineData(43, 1)]
    [InlineData(11, 0)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void and_or_zero_var(int a, int expected)
    {
        int result = a > 42 ? a & 1 : 0;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(44, 0)]
    [InlineData(43, 0)]
    [InlineData(11, 1)]
    [InlineData(10, 0)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void and_or_zero_var_globref_reversed(uint a, uint expected)
    {
        uint result = (a ^ globVar) > 42 ? 0 : a & 1;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(4, 16)]
    [InlineData(6, 64)]
    [InlineData(43, 0)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void pow2_or_zero_var(int a, int expected)
    {
        int result = a > 42 ? 0 : 1 << a;
        Assert.Equal(expected, result);
    }
}
