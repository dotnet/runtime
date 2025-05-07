// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class SingleBit
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set(int a, int b) => a | (1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SetPow2(int a, int b) => a | (0b100 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set10(int a) => a | (1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set11(int a) => a | (1 << 11);


    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear(int a, int b) => a & ~(1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClearPow2(int a, int b) => a & ~(0b100 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear10(int a) => a & ~(1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear11(int a) => a & ~(1 << 11);


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractShift(int a, int b) => ((a >> b) & 1) == 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractPow2Shift(int a, int b) => ((a >> b) & 0b100) == 0b100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract10Shift(int a) => ((a >> 10) & 1) == 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract11Shift(int a) => ((a >> 11) & 1) == 1;


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractDoubleShift(int a, int b) => ((a & (1 << b)) >> b) == 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractPow2DoubleShift(int a, int b) => ((a & (0b100 << b)) >> b) == 0b100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract10DoubleShift(int a) => ((a & (1 << 10)) >> 10) == 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract11DoubleShift(int a) => ((a & (1 << 11)) >> 11) == 1;


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractNotEqual(int a, int b) => (a & (1 << b)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractPow2NotEqual(int a, int b) => (a & (0b100 << b)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract10NotEqual(int a) => (a & (1 << 10)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract11NotEqual(int a) => (a & (1 << 11)) != 0;


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractEqual(int a, int b) => (a & (1 << b)) == (1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExtractPow2Equal(int a, int b) => (a & (0b100 << b)) == (0b100 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract10Equal(int a) => (a & (1 << 10)) == (1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Extract11Equal(int a) => (a & (1 << 11)) == (1 << 11);


    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert(int a, int b) => a ^ (1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int InvertPow2(int a, int b) => a ^ (0b100 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert10(int a) => a ^ (1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert11(int a) => a ^ (1 << 11);


    [Fact]
    public static void Test()
    {
        Assert.Equal(0x12345478, Set(0x12345078, 10));
        Assert.Equal(0x12345878, Set(0x12345078, 11));
        Assert.Equal(0x12345478, SetPow2(0x12345078, 8));
        Assert.Equal(0x12345878, SetPow2(0x12345078, 9));
        Assert.Equal(0x12345478, Set10(0x12345078));
        Assert.Equal(0x12345878, Set11(0x12345078));

        Assert.Equal(0x12345078, Clear(0x12345478, 10));
        Assert.Equal(0x12345078, Clear(0x12345878, 11));
        Assert.Equal(0x12345078, ClearPow2(0x12345478, 8));
        Assert.Equal(0x12345078, ClearPow2(0x12345878, 9));
        Assert.Equal(0x12345078, Clear10(0x12345478));
        Assert.Equal(0x12345078, Clear11(0x12345878));

        Assert.False(ExtractShift(0x12345878, 10));
        Assert.True (ExtractShift(0x12345878, 11));
        Assert.False(ExtractPow2Shift(0x12345878, 8));
        Assert.True (ExtractPow2Shift(0x12345878, 9));
        Assert.False(Extract10Shift(0x12345878));
        Assert.True (Extract11Shift(0x12345878));

        Assert.False(ExtractDoubleShift(0x12345878, 10));
        Assert.True (ExtractDoubleShift(0x12345878, 11));
        Assert.False(ExtractPow2DoubleShift(0x12345878, 8));
        Assert.True (ExtractPow2DoubleShift(0x12345878, 9));
        Assert.False(Extract10DoubleShift(0x12345878));
        Assert.True (Extract11DoubleShift(0x12345878));

        Assert.False(ExtractNotEqual(0x12345878, 10));
        Assert.True (ExtractNotEqual(0x12345878, 11));
        Assert.False(ExtractPow2NotEqual(0x12345878, 8));
        Assert.True (ExtractPow2NotEqual(0x12345878, 9));
        Assert.False(Extract10NotEqual(0x12345878));
        Assert.True (Extract11NotEqual(0x12345878));

        Assert.False(ExtractEqual(0x12345878, 10));
        Assert.True (ExtractEqual(0x12345878, 11));
        Assert.False(ExtractPow2Equal(0x12345878, 8));
        Assert.True (ExtractPow2Equal(0x12345878, 9));
        Assert.False(Extract10Equal(0x12345878));
        Assert.True (Extract11Equal(0x12345878));

        Assert.Equal(0x12345478, Invert(0x12345078, 10));
        Assert.Equal(0x12345078, Invert(0x12345478, 10));
        Assert.Equal(0x12345878, Invert(0x12345078, 11));
        Assert.Equal(0x12345078, Invert(0x12345878, 11));
        Assert.Equal(0x12345478, InvertPow2(0x12345078, 8));
        Assert.Equal(0x12345078, InvertPow2(0x12345478, 8));
        Assert.Equal(0x12345878, InvertPow2(0x12345078, 9));
        Assert.Equal(0x12345078, InvertPow2(0x12345878, 9));
        Assert.Equal(0x12345478, Invert10(0x12345078));
        Assert.Equal(0x12345078, Invert10(0x12345478));
        Assert.Equal(0x12345878, Invert11(0x12345078));
        Assert.Equal(0x12345078, Invert11(0x12345878));
    }
}