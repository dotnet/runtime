// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Lowering value & ((1 << k) - 1) to BZHI must keep the IL shift semantics for every k:
// the IL shift masks its count to (operand size - 1), so the mask is 0 when k >= the
// operand size, while BZHI would keep the value unchanged for such counts. The reference
// methods are compiled with NoOptimization so they can never take the BZHI path themselves.

namespace Runtime_129368;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_129368
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32(uint v, int k) => v & ((1u << k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32Commuted(uint v, int k) => ((1u << k) - 1) & v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32CommutedDecrement(uint v, int k) => ((1u << k) - 1) & (v - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Mask32Signed(int v, int k) => v & ((1 << k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32Premasked(uint v, int k) => v & ((1u << (k & 31)) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32NarrowMask(uint v, int k) => v & ((1u << (k & 15)) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Mask32Checked(int v, int k) => v & checked((1 << k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32ConstantCount(uint v) => v & ((1u << 5) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Mask32MultiUse(uint v, int k)
    {
        uint m = (1u << k) - 1;
        return (v & m) ^ m;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mask32Rmw(uint[] a, int k) => a[0] &= (1u << k) - 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Mask64(ulong v, int k) => v & ((1ul << k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Mask64LongCount(ulong v, long k) => v & ((1ul << (int)k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Mask64Premasked(ulong v, int k) => v & ((1ul << (k & 31)) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Mask64Checked(long v, int k) => v & checked((1L << k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BlsrChecked(int x) => x & checked(x + (-1));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BlsmskChecked(int x) => x ^ checked(x + (-1));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long BlsrChecked64(long x) => x & checked(x + (-1L));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long BlsmskChecked64(long x) => x ^ checked(x + (-1L));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static uint Reference32(uint v, int k) => v & ((1u << k) - 1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static uint ReferenceMask32(int k) => (1u << k) - 1;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static ulong Reference64(ulong v, int k) => v & ((1ul << k) - 1);

    [Fact]
    public static void Test32Bit()
    {
        uint[] values = { 0u, 1u, 0xAA55AA55u, 0x80000000u, ~0u };

        for (int k = -4; k <= 36; k++)
        {
            foreach (uint v in values)
            {
                uint expected = Reference32(v, k);
                Assert.Equal(expected, Mask32(v, k));
                Assert.Equal(expected, Mask32Commuted(v, k));
                Assert.Equal(Reference32(v - 1, k), Mask32CommutedDecrement(v, k));
                Assert.Equal((int)expected, Mask32Signed((int)v, k));
                Assert.Equal(Reference32(v, k & 31), Mask32Premasked(v, k));
                Assert.Equal(Reference32(v, k & 15), Mask32NarrowMask(v, k));
                Assert.Equal(expected ^ ReferenceMask32(k), Mask32MultiUse(v, k));

                // The overflow-checked mask must still throw when (1 << k) is int.MinValue
                if ((k & 31) == 31)
                {
                    int value = (int)v;
                    Assert.Throws<OverflowException>(() => Mask32Checked(value, k));
                }
                else
                {
                    Assert.Equal((int)expected, Mask32Checked((int)v, k));
                }

                uint[] a = { v };
                Mask32Rmw(a, k);
                Assert.Equal(expected, a[0]);
            }

            Assert.Equal(Reference32(0xAA55AA55u, 5), Mask32ConstantCount(0xAA55AA55u));
        }

        // The blsr/blsmsk patterns must also keep their overflow checks
        Assert.Equal(0x30 & 0x2F, BlsrChecked(0x30));
        Assert.Equal(0x30 ^ 0x2F, BlsmskChecked(0x30));
        Assert.Throws<OverflowException>(() => BlsrChecked(int.MinValue));
        Assert.Throws<OverflowException>(() => BlsmskChecked(int.MinValue));
    }

    [Fact]
    public static void Test64Bit()
    {
        ulong[] values = { 0ul, 1ul, 0xAA55AA55AA55AA55ul, 0x8000000000000000ul, ~0ul };

        for (int k = -4; k <= 68; k++)
        {
            foreach (ulong v in values)
            {
                ulong expected = Reference64(v, k);
                Assert.Equal(expected, Mask64(v, k));
                Assert.Equal(expected, Mask64LongCount(v, k));
                Assert.Equal(Reference64(v, k & 31), Mask64Premasked(v, k));

                if ((k & 63) == 63)
                {
                    long value = (long)v;
                    Assert.Throws<OverflowException>(() => Mask64Checked(value, k));
                }
                else
                {
                    Assert.Equal((long)expected, Mask64Checked((long)v, k));
                }
            }
        }

        Assert.Equal(0x30L & 0x2FL, BlsrChecked64(0x30L));
        Assert.Equal(0x30L ^ 0x2FL, BlsmskChecked64(0x30L));
        Assert.Throws<OverflowException>(() => BlsrChecked64(long.MinValue));
        Assert.Throws<OverflowException>(() => BlsmskChecked64(long.MinValue));
    }
}
