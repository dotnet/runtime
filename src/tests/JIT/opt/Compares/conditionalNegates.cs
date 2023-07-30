// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ConditionalNegateTest
{

    [Theory]
    [InlineData(72, 13, 13)]
    [InlineData(32, 13, 224)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_byte(byte op1, byte op2, byte expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #42
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        byte result = (byte) (op1 > 42 ? op2: -op1);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(74, 13, 74)]
    [InlineData(34, 13, -13)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_short(short op1, short op2, short expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #43
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        short result = (short) (op1 <= 43 ? -op2 : op1);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(75, -short.MaxValue)]
    [InlineData(-35, short.MaxValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_short_min_max(short op1, short expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #44
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        short result = (short) (op1 > 44 ? -short.MaxValue : short.MaxValue);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(76, 17, 17)]
    [InlineData(36, 17, -36)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_int(int op1, int op2, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #45
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 45 ? op2 : -op1;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(77, int.MaxValue)]
    [InlineData(37, -int.MaxValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_int_min_max(int op1, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #46
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        int result = op1 >= 46 ? int.MaxValue : -int.MaxValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(78, 23, 78)]
    [InlineData(38, 23, -23)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_long(long op1, long op2, long expected)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #47
        //ARM64-FULL-LINE-NEXT: csneg {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        long result = op1 < 47 ? -op2 : op1;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(80.0f, 29, 29)]
    [InlineData(30.0f, 29, -29)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_float(float op1, int op2, int expected)
    {
        //ARM64-FULL-LINE: fcmp {{s[0-9]+}}, {{s[0-9]+}}
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 48.0f ? op2 : -op2;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(60.0, 31, -31)]
    [InlineData(30.0, 31, 31)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_double(double op1, int op2, int expected)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 49.0 ? -op2 : op2;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(81, 21, 21)]
    [InlineData(31, 21, -62)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_shifted_false_oper(int op1, int op2, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #50
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 50 ? op2 : -(op1 << 1);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(82, 22, 22)]
    [InlineData(32, 22, -4)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cneg_shifted_true_oper(int op1, int op2, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #51
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        int result = op1 < 51 ? -(op1 >> 3) : op2;
        Assert.Equal(expected, result);
    }
}
