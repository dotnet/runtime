// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ConditionalIncrementTest
{

    [Theory]
    [InlineData(72, 6)]
    [InlineData(32, 5)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_byte(byte op1, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #42
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 42 ? 6: 5;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, byte.MinValue)]
    [InlineData(32, byte.MaxValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_byte_min_max(byte op1, byte expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #43
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, wzr, {{w[0-9]+}}, {{ge|lt}}
        byte result = op1 >= 43 ? byte.MinValue : byte.MaxValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, byte.MinValue)]
    [InlineData(32, byte.MaxValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinv_byte_min_max(byte op1, byte expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #43
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        byte result = (byte) (op1 >= 43 ? byte.MinValue : ~byte.MinValue);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(74, 5)]
    [InlineData(34, 6)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_short(short op1, short expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #44
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        short result = (short) (op1 <= 44 ? 6 : 5);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(76, short.MinValue)]
    [InlineData(-35, short.MaxValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_short_min_max(short op1, short expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #45
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        short result = op1 > 45 ? short.MinValue : short.MaxValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(76, 6)]
    [InlineData(36, 5)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_int(int op1, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #46
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 46 ? 6 : 5;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(77, int.MinValue)]
    [InlineData(37, int.MaxValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_int_min_max(int op1, int expected)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #47
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        int result = op1 >= 47 ? int.MinValue : int.MaxValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(78, 5)]
    [InlineData(38, 6)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_long(long op1, long expected)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #48
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        //ARM64-FULL-LINE-NEXT: sxtw {{x[0-9]+}}, {{w[0-9]+}}
        long result = op1 < 48 ? 6 : 5;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(79, long.MaxValue)]
    [InlineData(39, long.MinValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_long_min_max(long op1, long expected)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #49
        //ARM64-FULL-LINE-NEXT: cinc {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        long result = op1 < 49 ? long.MinValue : long.MaxValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(79, long.MaxValue)]
    [InlineData(39, long.MinValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinv_long_min_max(long op1, long expected)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #49
        //ARM64-FULL-LINE-NEXT: cinc {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        long result = op1 < 49 ? long.MinValue : ~long.MinValue;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(80.0f, 6)]
    [InlineData(30.0f, 5)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_float(float op1, int expected)
    {
        //ARM64-FULL-LINE: fcmp {{s[0-9]+}}, {{s[0-9]+}}
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        int result = op1 > 50.0f ? 6 : 5;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(80.0, 5)]
    [InlineData(30.0, 6)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void cinc_double(double op1, int expected)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{hs|lo}}
        int result = op1 < 51.0 ? 6 : 5;
        Assert.Equal(expected, result);
    }
}
