// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// Unsigned scalar products can use MULX between carry limbs. Signed products
// must retain their signed semantics. A doubled final operand may move only
// when its local inputs are independent of the flags producer.
public class ScalarProductCarryTests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Add(ulong a, ulong b, ref ulong carry)
    {
        ulong s = a + b, c = s < a ? 1UL : 0UL;
        ulong r = s + carry;
        carry = c + (r < s ? 1UL : 0UL);
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong UnsignedProducts(ulong[] a, ulong[] x, ulong[] result)
    {
        ulong a0 = a[0], a1 = a[1], a2 = a[2], a3 = a[3], a4 = a[4], a5 = a[5], a6 = a[6], a7 = a[7], a8 = a[8], a9 = a[9], a10 = a[10], a11 = a[11], a12 = a[12];
        ulong x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3], x4 = x[4], x5 = x[5];
        ulong carry = 0;
        ulong r0 = Add(a0, x0, ref carry);
        ulong high0 = Math.BigMul(x0, x0, out ulong low0);
        ulong r1 = Add(a1, low0, ref carry);
        ulong r2 = Add(a2, high0, ref carry);
        ulong high1 = Math.BigMul(x1, x1, out ulong low1);
        ulong r3 = Add(a3, low1, ref carry);
        ulong r4 = Add(a4, high1, ref carry);
        ulong high2 = Math.BigMul(x2, x2, out ulong low2);
        ulong r5 = Add(a5, low2, ref carry);
        ulong r6 = Add(a6, high2, ref carry);
        ulong high3 = Math.BigMul(x3, x3, out ulong low3);
        ulong r7 = Add(a7, low3, ref carry);
        ulong r8 = Add(a8, high3, ref carry);
        ulong high4 = Math.BigMul(x4, x4, out ulong low4);
        ulong r9 = Add(a9, low4, ref carry);
        ulong r10 = Add(a10, high4, ref carry);
        ulong high5 = Math.BigMul(x5, x5, out ulong low5);
        ulong r11 = Add(a11, low5, ref carry);
        ulong r12 = Add(a12, high5, ref carry);
        result[0] = r0; result[1] = r1; result[2] = r2; result[3] = r3; result[4] = r4; result[5] = r5; result[6] = r6; result[7] = r7; result[8] = r8; result[9] = r9; result[10] = r10; result[11] = r11; result[12] = r12;
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SignedProducts(ulong[] a, ulong[] x, ulong[] result)
    {
        ulong a0 = a[0], a1 = a[1], a2 = a[2], a3 = a[3], a4 = a[4], a5 = a[5], a6 = a[6];
        ulong x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3], x4 = x[4], x5 = x[5];
        ulong carry = 0;
        ulong r0 = Add(a0, x0, ref carry);
        long signedHigh0 = Math.BigMul((long)x0, (long)x1, out long signedLow0);
        ulong high0 = (ulong)signedHigh0, low0 = (ulong)signedLow0;
        ulong r1 = Add(a1, low0, ref carry);
        ulong r2 = Add(a2, high0, ref carry);
        long signedHigh1 = Math.BigMul((long)x2, (long)x3, out long signedLow1);
        ulong high1 = (ulong)signedHigh1, low1 = (ulong)signedLow1;
        ulong r3 = Add(a3, low1, ref carry);
        ulong r4 = Add(a4, high1, ref carry);
        long signedHigh2 = Math.BigMul((long)x4, (long)x5, out long signedLow2);
        ulong high2 = (ulong)signedHigh2, low2 = (ulong)signedLow2;
        ulong r5 = Add(a5, low2, ref carry);
        ulong r6 = Add(a6, high2, ref carry);
        result[0] = r0; result[1] = r1; result[2] = r2; result[3] = r3; result[4] = r4; result[5] = r5; result[6] = r6;
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong IndependentDouble(ulong a, ulong b, ulong tail, out ulong first)
    {
        ulong sum = a + b;
        ulong carry = sum < a ? (ulong)1 : 0;
        first = sum;
        return tail + tail + carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint IndependentDouble32(uint a, uint b, uint tail, out uint first)
    {
        uint sum = a + b;
        uint carry = sum < a ? (uint)1 : 0;
        first = sum;
        return tail + tail + carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DependentDouble(ulong a, ulong b, ulong tail, out ulong first)
    {
        ulong sum = a + b;
        ulong carry = sum < a ? (ulong)1 : 0;
        first = sum;
        return sum + sum + carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint DependentDouble32(uint a, uint b, uint tail, out uint first)
    {
        uint sum = a + b;
        uint carry = sum < a ? (uint)1 : 0;
        first = sum;
        return sum + sum + carry;
    }

    private static BigInteger Raw(ulong[] x, int words) =>
        new BigInteger(MemoryMarshal.AsBytes(x.AsSpan(0, words)), true);

    [Fact]
    public static int TestEntryPoint()
    {
        var rng = new Random(13579);
        ulong[] a = new ulong[13], x = new ulong[13], result = new ulong[13];
        BigInteger productMask = (BigInteger.One << 128) - 1;
        for (int sample = 0; sample < 2000; sample++)
        {
            rng.NextBytes(MemoryMarshal.AsBytes(a.AsSpan()));
            rng.NextBytes(MemoryMarshal.AsBytes(x.AsSpan()));
            if (sample < 4)
            {
                Array.Fill(a, (sample & 1) == 0 ? 0UL : ulong.MaxValue);
                Array.Fill(x, (sample & 2) == 0 ? 0UL : ulong.MaxValue);
            }
            if (sample == 4) { Array.Fill(a, ulong.MaxValue); Array.Fill(x, 1UL << 63); }
            for (int sign = 0; sign < 2; sign++)
            {
                int count = sign == 0 ? 6 : 3, words = 1 + 2 * count;
                BigInteger radix = BigInteger.One << (64 * words);
                BigInteger expected = Raw(a, words) + x[0];
                for (int i = 0; i < count; i++)
                {
                    BigInteger product = sign == 0 ? (BigInteger)x[i] * x[i]
                        : ((BigInteger)(long)x[2*i] * (long)x[2*i+1]) & productMask;
                    expected += product << (64 * (1 + 2*i));
                }
                ulong carry = sign == 0 ? UnsignedProducts(a, x, result) : SignedProducts(a, x, result);
                if (Raw(result, words) != expected % radix || carry != (ulong)(expected / radix)) return 101 + sign;
                ulong[] alias = (ulong[])a.Clone();
                ulong aliasCarry = sign == 0 ? UnsignedProducts(alias, x, alias) : SignedProducts(alias, x, alias);
                if (aliasCarry != carry || Raw(alias, words) != expected % radix) return 103 + sign;
                alias = (ulong[])x.Clone();
                aliasCarry = sign == 0 ? UnsignedProducts(a, alias, alias) : SignedProducts(a, alias, alias);
                if (aliasCarry != carry || Raw(alias, words) != expected % radix) return 105 + sign;
            }
            BigInteger sum = (BigInteger)a[0] + x[0], mask = (BigInteger.One << 64) - 1;
            ulong independent = IndependentDouble(a[0], x[0], a[1], out ulong first);
            if (first != (ulong)(sum & mask) || independent != (ulong)((2 * (BigInteger)a[1] + (sum >> 64)) & mask)) return 107;
            ulong dependent = DependentDouble(a[0], x[0], a[1], out first);
            if (first != (ulong)(sum & mask) || dependent != (ulong)((2 * (sum & mask) + (sum >> 64)) & mask)) return 108;
            sum = (BigInteger)(uint)a[0] + (uint)x[0]; mask = (BigInteger.One << 32) - 1;
            uint independent32 = IndependentDouble32((uint)a[0], (uint)x[0], (uint)a[1], out uint first32);
            if (first32 != (uint)(sum & mask) || independent32 != (uint)((2 * (BigInteger)(uint)a[1] + (sum >> 32)) & mask)) return 109;
            uint dependent32 = DependentDouble32((uint)a[0], (uint)x[0], (uint)a[1], out first32);
            if (first32 != (uint)(sum & mask) || dependent32 != (uint)((2 * (sum & mask) + (sum >> 32)) & mask)) return 110;
        }
        return 100;
    }
}
