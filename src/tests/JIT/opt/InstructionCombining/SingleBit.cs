// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class SingleBit
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set(int a, int b)
    {
        // X64: bts {{[a-z]+}}, {{[a-z]+}}
        return a | (1 << b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SetSwap(int a, int b)
    {
        // X64: bts {{[a-z]+}}, {{[a-z]+}}
        return (1 << b) | a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SetLong(long a, int b)
    {
        // X64: bts {{[a-z]+}}, {{[a-z]+}}
        return a | (1L << b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set10(int a)
    {
        // A constant bit index folds to a plain 'or' with an immediate; it must not use 'bts'.
        // X64-NOT: bts
        // X64: or {{[a-z]+}}, 0x400
        return a | (1 << 10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set11(int a) => a | (1 << 11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Set31(int a) => a | (1 << 31);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SetNegatedBit(int a, int b) => ~(1 << a) | (1 << b);


    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Clear(int a, int b)
    {
        // X64: btr {{[a-z]+}}, {{[a-z]+}}
        return a & ~(1 << b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClearSwap(int a, int b)
    {
        // X64: btr {{[a-z]+}}, {{[a-z]+}}
        return ~(1 << b) & a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ClearLong(long a, int b)
    {
        // X64: btr {{[a-z]+}}, {{[a-z]+}}
        return a & ~(1L << b);
    }

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
    static int Invert(int a, int b)
    {
        // X64: btc {{[a-z]+}}, {{[a-z]+}}
        return a ^ (1 << b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int InvertSwap(int a, int b)
    {
        // X64: btc {{[a-z]+}}, {{[a-z]+}}
        return (1 << b) ^ a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long InvertLong(long a, int b)
    {
        // X64: btc {{[a-z]+}}, {{[a-z]+}}
        return a ^ (1L << b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert10(int a) => a ^ (1 << 10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert11(int a) => a ^ (1 << 11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Invert31(int a) => a ^ (1 << 31);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int InvertNegatedBit(int a, int b) => ~(1 << a) ^ (1 << b);

    // Bit-test recognition: testing a single bit to feed a branch should become 'bt'. Both the
    // '(x >> y) & 1' and 'x & (1 << y)' shapes select bit 'y', and 'bt' masks the index modulo the
    // operand size, matching the C# masked-shift semantics even for an out-of-range 'y'.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBitShr(int a, int b)
    {
        // X64: bt {{[a-z]+}}, {{[a-z]+}}
        if (((a >> b) & 1) != 0)
        {
            return 100;
        }
        return 200;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBitShrEq(int a, int b)
    {
        // X64: bt {{[a-z]+}}, {{[a-z]+}}
        if (((a >> b) & 1) == 0)
        {
            return 100;
        }
        return 200;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBitShrLong(long a, int b)
    {
        // X64: bt {{[a-z]+}}, {{[a-z]+}}
        if (((a >> b) & 1) != 0)
        {
            return 100;
        }
        return 200;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBitMask(int a, int b)
    {
        // X64: bt {{[a-z]+}}, {{[a-z]+}}
        if ((a & (1 << b)) != 0)
        {
            return 100;
        }
        return 200;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBitShrConst(int a)
    {
        // A constant bit index keeps the shift folded into a 'test' with an immediate; 'bt' has no
        // immediate form here so it must not be used.
        // X64-NOT: bt
        // X64: test {{[a-z]+}}, {{(32|0x20)}}
        if (((a >> 5) & 1) != 0)
        {
            return 100;
        }
        return 200;
    }

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

        // Long variants (exercise the 64-bit bts/btr/btc forms). The reg,reg encoding masks the
        // bit index modulo 64, matching the C# masked-shift semantics even for out-of-range indices.
        Assert.Equal(0x1_00000000L, SetLong(0, 32));
        Assert.Equal(0x1_00000000L, SetLong(0, 32 + 64));
        Assert.Equal(unchecked((long)0x8000000000000000UL), SetLong(0, 63));
        Assert.Equal(0x12345078L, ClearLong(0x1_12345078L, 32));
        Assert.Equal(0L, ClearLong(unchecked((long)0x8000000000000000UL), 63));
        Assert.Equal(0x1_00000000L, InvertLong(0, 32));
        Assert.Equal(0L, InvertLong(0x1_00000000L, 32));
        Assert.Equal(unchecked((long)0x8000000000000000UL), InvertLong(0, 63));

        // Bit-test recognition. Exercise a set and a clear bit, plus an out-of-range (masked) index.
        Assert.Equal(100, TestBitShr(0b1000, 3));
        Assert.Equal(200, TestBitShr(0b1000, 2));
        Assert.Equal(100, TestBitShr(0b1000, 3 + 32));
        Assert.Equal(200, TestBitShrEq(0b1000, 3));
        Assert.Equal(100, TestBitShrEq(0b1000, 2));
        Assert.Equal(100, TestBitShrLong(0x1_00000000L, 32));
        Assert.Equal(200, TestBitShrLong(0x1_00000000L, 33));
        Assert.Equal(100, TestBitShrLong(0x1_00000000L, 32 + 64));
        Assert.Equal(100, TestBitMask(0b1000, 3));
        Assert.Equal(200, TestBitMask(0b1000, 2));
        Assert.Equal(100, TestBitMask(0b1000, 3 + 32));
        Assert.Equal(100, TestBitShrConst(0b100000));
        Assert.Equal(200, TestBitShrConst(0b010000));
    }
}