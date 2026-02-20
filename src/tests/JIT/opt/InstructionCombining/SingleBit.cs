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
    static int SetSwap(int a, int b) => (1 << b) | a ;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set10(int a) => a | (1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set11(int a) => a | (1 << 11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set31(int a) => a | (1 << 31);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SetNegatedBit(int a, int b) => ~(1 << a) | (1 << b);


    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear(int a, int b) => a & ~(1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClearSwap(int a, int b) => ~(1 << b) & a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear10(int a) => a & ~(1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear11(int a) => a & ~(1 << 11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear31(int a) => a & ~(1 << 31);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClearNegatedBit(int a, int b) => ~(1 << a) & ~(1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClearPositiveBit(int a, int b) => (1 << a) & ~(1 << b);


    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert(int a, int b) => a ^ (1 << b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int InvertSwap(int a, int b) => (1 << b) ^ a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert10(int a) => a ^ (1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert11(int a) => a ^ (1 << 11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert31(int a) => a ^ (1 << 31);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int InvertNegatedBit(int a, int b) => ~(1 << a) ^ (1 << b);

    [Fact]
    public static void Test()
    {
        Assert.Equal(0x12345478, Set(0x12345078, 10 + 32));
        Assert.Equal(0x12345878, Set(0x12345078, 11 + 32));
        Assert.Equal(0x12345478, SetSwap(0x12345078, 10 + 32));
        Assert.Equal(0x12345878, SetSwap(0x12345078, 11 + 32));
        Assert.Equal(0x12345478, Set10(0x12345078));
        Assert.Equal(0x12345878, Set11(0x12345078));
        Assert.Equal(int.MinValue, Set31(0));
        Assert.Equal(-1, SetNegatedBit(0, 0 + 32));

        Assert.Equal(0x12345078, Clear(0x12345478, 10 + 32));
        Assert.Equal(0x12345078, Clear(0x12345878, 11 + 32));
        Assert.Equal(0x12345078, ClearSwap(0x12345478, 10 + 32));
        Assert.Equal(0x12345078, ClearSwap(0x12345878, 11 + 32));
        Assert.Equal(0x12345078, Clear10(0x12345478));
        Assert.Equal(0x12345078, Clear11(0x12345878));
        Assert.Equal(0, Clear31(int.MinValue));
        Assert.Equal(-4, ClearNegatedBit(0, 1 + 32));
        Assert.Equal(0, ClearPositiveBit(0, 0 + 32));

        Assert.Equal(0x12345478, Invert(0x12345078, 10 + 32));
        Assert.Equal(0x12345078, Invert(0x12345478, 10 + 32));
        Assert.Equal(0x12345878, Invert(0x12345078, 11 + 32));
        Assert.Equal(0x12345078, Invert(0x12345878, 11 + 32));
        Assert.Equal(0x12345478, InvertSwap(0x12345078, 10 + 32));
        Assert.Equal(0x12345078, InvertSwap(0x12345478, 10 + 32));
        Assert.Equal(0x12345878, InvertSwap(0x12345078, 11 + 32));
        Assert.Equal(0x12345078, InvertSwap(0x12345878, 11 + 32));
        Assert.Equal(0x12345478, Invert10(0x12345078));
        Assert.Equal(0x12345078, Invert10(0x12345478));
        Assert.Equal(0x12345878, Invert11(0x12345078));
        Assert.Equal(0x12345078, Invert11(0x12345878));
        Assert.Equal(0, Invert31(int.MinValue));
        Assert.Equal(int.MinValue, Invert31(0));
        Assert.Equal(-1, InvertNegatedBit(0, 0 + 32));
        Assert.Equal(-4, InvertNegatedBit(0, 1 + 32));
    }
}