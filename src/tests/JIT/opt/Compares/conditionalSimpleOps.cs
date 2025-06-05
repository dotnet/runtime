// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ConditionalSimpleOpTest
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
        int result = op1 < 42 ? -13 : -25;
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
        long result = op1 < 42 ? 0x7FFF_FFF3 : 0xFFFF_FFE7;
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
        long result = op1 < 42 ? long.MinValue : 0;
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
