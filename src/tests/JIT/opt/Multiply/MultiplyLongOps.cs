// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for long multiply [add/sub/neg].

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MultiplyLongOpsTest
{

    [Theory]
    [InlineData(72, 6, 68L, 500L)]
    [InlineData(32, 5, 40L, 200L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smaddl_single_cast(int op1, int op2, long op3, long expected)
    {
        //ARM64-FULL-LINE: smaddl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        long result = ((long)op1 * op2) + op3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 68L, 500L)]
    [InlineData(32, 5, 40L, 200L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smaddl_double_cast(int op1, int op2, long op3, long expected)
    {
        //ARM64-FULL-LINE: smaddl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        long result = ((long)op1 * (long)op2) + op3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2000000000, 5, 68L, 10000000068L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smaddl_no_overflow(int op1, int op2, long op3, long expected)
    {
        //ARM64-FULL-LINE: smaddl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        long result = ((long)op1 * op2) + op3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 500L, 68L)]
    [InlineData(32, 5, 200L, 40L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smsubl_single_cast(int op1, int op2, long op3, long expected)
    {
        //ARM64-FULL-LINE: smsubl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        long result = op3 - ((long)op1 * op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 500L, 68L)]
    [InlineData(32, 5, 200L, 40L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smsubl_double_cast(int op1, int op2, long op3, long expected)
    {
        //ARM64-FULL-LINE: smsubl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        long result = op3 - ((long)op1 * (long)op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2000000000, 5, 10000000068L, 68L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smsubl_no_overflow(int op1, int op2, long op3, long expected)
    {
        //ARM64-FULL-LINE: smsubl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        long result = op3 - ((long)op1 * op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 68UL, 500UL)]
    [InlineData(32, 5, 40UL, 200UL)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void umaddl_single_cast(uint op1, uint op2, ulong op3, ulong expected)
    {
        //ARM64-FULL-LINE: umaddl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        ulong result = ((ulong)op1 * op2) + op3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 68UL, 500UL)]
    [InlineData(32, 5, 40UL, 200UL)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void umaddl_double_cast(uint op1, uint op2, ulong op3, ulong expected)
    {
        //ARM64-FULL-LINE: umaddl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        ulong result = ((ulong)op1 * (ulong)op2) + op3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2000000000, 5, 68UL, 10000000068UL)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void umaddl_no_overflow(uint op1, uint op2, ulong op3, ulong expected)
    {
        //ARM64-FULL-LINE: umaddl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        ulong result = ((ulong)op1 * op2) + op3;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 500UL, 68UL)]
    [InlineData(32, 5, 200UL, 40UL)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void umsubl_single_cast(uint op1, uint op2, ulong op3, ulong expected)
    {
        //ARM64-FULL-LINE: umsubl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        ulong result = op3 - ((ulong)op1 * op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, 500UL, 68UL)]
    [InlineData(32, 5, 200UL, 40UL)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void umsubl_double_cast(uint op1, uint op2, ulong op3, ulong expected)
    {
        //ARM64-FULL-LINE: umsubl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        ulong result = op3 - ((ulong)op1 * op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2000000000, 5, 10000000068UL, 68UL)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void umsubl_no_overflow(uint op1, uint op2, ulong op3, ulong expected)
    {
        //ARM64-FULL-LINE: umsubl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{x[0-9]+}}
        ulong result = op3 - ((ulong)op1 * op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, -432L)]
    [InlineData(32, 5, -160L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smnegl_single_cast(int op1, int op2, long expected)
    {
        //ARM64-FULL-LINE: smnegl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
        long result = -((long)op1 * op2);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(72, 6, -432L)]
    [InlineData(32, 5, -160L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void smnegl_double_cast(int op1, int op2, long expected)
    {
        //ARM64-FULL-LINE: smnegl {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
        long result = -((long)op1 * (long)op2);
        Assert.Equal(expected, result);
    }
}
