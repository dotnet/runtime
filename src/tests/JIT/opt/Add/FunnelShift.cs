// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class FunnelShiftTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shift1(ulong lo, ulong hi)
    {
        // X64: shrd {{.*}}, 1
        // ARM64: extr {{.*}}, #1
        return (lo >> 1) | (hi << 63);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shift17(ulong lo, ulong hi)
    {
        // X64: shrd {{.*}}, 17
        // ARM64: extr {{.*}}, #17
        return (hi << 47) | (lo >> 17);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shift32(ulong lo, ulong hi)
    {
        // X64: shrd {{.*}}, 32
        // ARM64: extr {{.*}}, #32
        return (lo >> 32) | (hi << 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shift63(ulong lo, ulong hi)
    {
        // X64: shrd {{.*}}, 63
        // ARM64: extr {{.*}}, #63
        return (lo >> 63) | (hi << 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shift13(uint lo, uint hi)
    {
        // X64: shrd {{.*}}, 13
        // ARM64: extr {{.*}}, #13
        return (lo >> 13) | (hi << 19);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shift31(uint lo, uint hi)
    {
        // X64: shrd {{.*}}, 31
        // ARM64: extr {{.*}}, #31
        return (hi << 1) | (lo >> 31);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Masked(ulong lo, ulong hi) => (lo >> 65) | (hi << -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong KeepInputs(ulong lo, ulong hi, out ulong result)
    {
        result = (lo >> 17) | (hi << 47);
        return lo ^ hi;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsZero(ulong lo, ulong hi) => ((lo >> 17) | (hi << 47)) == 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Overlap(ulong lo, ulong hi) => (lo >> 17) | (hi << 46);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Gap(ulong lo, ulong hi) => (lo >> 17) | (hi << 48);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ZeroCount(ulong lo, ulong hi) => (lo >> 64) | (hi << 64);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Signed(long lo, long hi) => (lo >> 17) | (hi << 47);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Redefine(ulong lo, ulong hi) => (lo >> 17) | ((lo = hi) << 47);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Aliased(ulong[] values)
    {
        ulong shifted = values[0] >> 17;
        values[0] = ~values[0];
        return shifted | (values[0] << 47);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 SquareStep(ulong a, ulong b, ulong old, UInt128 carry, out ulong low)
    {
        // X64: shrd {{.*}}, 1
        // X64: shrd {{.*}}, 63
        // ARM64: extr {{.*}}, #1
        // ARM64: extr {{.*}}, #63
        UInt128 digit1 = old + carry;
        UInt128 digit2 = (UInt128)a * b;
        low = (ulong)(digit1 + (digit2 << 1));
        return (digit2 + (digit1 >> 1)) >> 63;
    }

    // Construct the expected result one bit at a time, independently of the
    // complementary-shift pattern under test.
    private static ulong Expected(ulong lo, ulong hi, int count, int width = 64)
    {
        ulong result = 0;
        for (int bit = 0; bit < width; bit++)
        {
            int sourceBit = bit + count;
            ulong source = sourceBit < width ? lo : hi;
            result |= ((source >> (sourceBit % width)) & 1) << bit;
        }
        return result;
    }

    private static void Check(ulong lo, ulong hi)
    {
        if (Shift1(lo, hi) != Expected(lo, hi, 1) ||
            Shift17(lo, hi) != Expected(lo, hi, 17) ||
            Shift32(lo, hi) != Expected(lo, hi, 32) ||
            Shift63(lo, hi) != Expected(lo, hi, 63) ||
            Shift13((uint)lo, (uint)hi) != Expected((uint)lo, (uint)hi, 13, 32) ||
            Shift31((uint)lo, (uint)hi) != Expected((uint)lo, (uint)hi, 31, 32) ||
            Masked(lo, hi) != Expected(lo, hi, 1) ||
            KeepInputs(lo, hi, out ulong result) != (lo ^ hi) || result != Expected(lo, hi, 17) ||
            IsZero(lo, hi) != (Expected(lo, hi, 17) == 0) ||
            Overlap(lo, hi) != ((lo >> 17) | (hi << 46)) ||
            Gap(lo, hi) != ((lo >> 17) | (hi << 48)) ||
            ZeroCount(lo, hi) != (lo | hi) ||
            Signed((long)lo, (long)hi) != (((long)lo >> 17) | ((long)hi << 47)) ||
            Redefine(lo, hi) != Expected(lo, hi, 17) ||
            Aliased(new[] { lo }) != Expected(lo, ~lo, 17))
        {
            throw new Exception($"Funnel shift failed: {lo:X16}, {hi:X16}");
        }

        // The square kernel carries 65 bits between limbs. Use a wider oracle
        // so the doubled product's 129th bit is retained independently.
        UInt128 carry = ((UInt128)(hi & 1) << 64) | lo;
        BigInteger full = (BigInteger)lo * hi * 2 + hi + (BigInteger)carry;
        UInt128 nextCarry = SquareStep(lo, hi, hi, carry, out ulong low);
        if (low != (ulong)(full & ulong.MaxValue) || nextCarry != (UInt128)(full >> 64))
        {
            throw new Exception($"Square carry failed: {lo:X16}, {hi:X16}");
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        ulong[] edges = { 0, 1, ulong.MaxValue, 1UL << 63, (1UL << 63) - 1, 0xAAAAAAAAAAAAAAAA, 0x5555555555555555 };
        foreach (ulong lo in edges)
        {
            foreach (ulong hi in edges)
            {
                Check(lo, hi);
            }
        }

        for (int lo = 0; lo < 64; lo++)
        {
            for (int hi = 0; hi < 64; hi++)
            {
                Check(1UL << lo, 1UL << hi);
            }
        }

        Random random = new Random(42);
        byte[] bytes = new byte[16];
        for (int i = 0; i < 2000; i++)
        {
            random.NextBytes(bytes);
            Check(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8));
        }
    }
}
