// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Rows of widening multiply-accumulates as in Montgomery multiplication, kept in CF/OF by
// TryLowerMultiplyCarryRows, including a reused product variable and high register pressure.
public class MontgomeryRowTests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MulAdd(ulong m, ulong x, ulong t, ulong c, out ulong lo)
    {
        ulong hi = Math.BigMul(m, x, out ulong l);
        l += t;
        hi += l < t ? 1UL : 0UL;
        l += c;
        hi += l < c ? 1UL : 0UL;
        lo = l;
        return hi;
    }

    // A product variable reused for a second row; the first products are used as they are.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ReusedProducts(ulong a0, ulong a1, ulong b, ulong c, ulong t0, ulong t1, out ulong r0, out ulong r1, out ulong plain)
    {
        UInt128 p = Math.BigMul(a0, b);
        UInt128 q = Math.BigMul(a1, b);
        plain = (ulong)p ^ (ulong)(p >> 64) ^ (ulong)q ^ (ulong)(q >> 64);
        p = Math.BigMul(a0, c);
        q = Math.BigMul(a1, c);
        ulong lo = (ulong)p + t0;
        ulong hi = (ulong)(p >> 64) + (lo < t0 ? 1UL : 0UL);
        r0 = lo;
        ulong lo1 = (ulong)q + t1;
        ulong hi1 = (ulong)(q >> 64) + (lo1 < t1 ? 1UL : 0UL);
        ulong lo2 = lo1 + hi;
        hi1 += lo2 < hi ? 1UL : 0UL;
        r1 = lo2;
        return hi1;
    }

    // Two six-lane rows with many values live across them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong PressureRows(ulong[] a, ulong[] t, ulong m0, ulong m1, ulong[] r)
    {
        ulong x0 = a[0], x1 = a[1], x2 = a[2], x3 = a[3], x4 = a[4], x5 = a[5];
        ulong t0 = t[0], t1 = t[1], t2 = t[2], t3 = t[3], t4 = t[4], t5 = t[5];
        ulong c = MulAdd(m0, x0, t0, 0, out t0);
        c = MulAdd(m0, x1, t1, c, out t1);
        c = MulAdd(m0, x2, t2, c, out t2);
        c = MulAdd(m0, x3, t3, c, out t3);
        c = MulAdd(m0, x4, t4, c, out t4);
        c = MulAdd(m0, x5, t5, c, out t5);
        ulong t6 = c;
        c = MulAdd(m1, x5, t0, 0, out t0);
        c = MulAdd(m1, x4, t1, c, out t1);
        c = MulAdd(m1, x3, t2, c, out t2);
        c = MulAdd(m1, x2, t3, c, out t3);
        c = MulAdd(m1, x1, t4, c, out t4);
        c = MulAdd(m1, x0, t5, c, out t5);
        r[0] = t0; r[1] = t1; r[2] = t2; r[3] = t3; r[4] = t4; r[5] = t5;
        return t6 ^ c ^ x0 ^ x1 ^ x2 ^ x3 ^ x4 ^ x5 ^ m0 ^ m1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var random = new Random(134039);
        ulong Next() => random.Next(4) switch { 0 => ulong.MaxValue, 1 => 0, _ => (ulong)random.NextInt64() * 3 + (ulong)random.Next() };
        BigInteger W = BigInteger.One << 64;
        for (int i = 0; i < 20000; i++)
        {
            ulong a0 = Next(), a1 = Next(), b = Next(), c = Next(), t0 = Next(), t1 = Next();
            ulong carry = ReusedProducts(a0, a1, b, c, t0, t1, out ulong r0, out ulong r1, out ulong plain);
            BigInteger p0 = (BigInteger)a0 * b, q0 = (BigInteger)a1 * b;
            ulong expectedPlain = (ulong)(p0 % W) ^ (ulong)(p0 / W) ^ (ulong)(q0 % W) ^ (ulong)(q0 / W);
            BigInteger sum = (BigInteger)a0 * c + t0 + ((BigInteger)a1 * c + t1) * W;
            if (plain != expectedPlain || r0 != (ulong)(sum % W) || r1 != (ulong)(sum / W % W) || carry != (ulong)(sum / W / W))
                throw new Exception("ReusedProducts mismatch");

            ulong[] a = { Next(), Next(), Next(), Next(), Next(), Next() };
            ulong[] t = { Next(), Next(), Next(), Next(), Next(), Next() };
            ulong m0 = Next(), m1 = Next();
            ulong[] r = new ulong[6];
            ulong mix = PressureRows(a, t, m0, m1, r);
            BigInteger acc = 0;
            for (int j = 5; j >= 0; j--) acc = acc * W + t[j];
            BigInteger row0 = 0;
            for (int j = 5; j >= 0; j--) row0 = row0 * W + a[j];
            acc += row0 * m0;
            BigInteger t6 = acc / BigInteger.Pow(W, 6);
            acc %= BigInteger.Pow(W, 6);
            BigInteger row1 = 0;
            for (int j = 0; j < 6; j++) row1 = row1 * W + a[j];
            acc += row1 * m1;
            BigInteger c1 = acc / BigInteger.Pow(W, 6);
            for (int j = 0; j < 6; j++) { if (r[j] != (ulong)(acc % W)) throw new Exception("PressureRows limb mismatch"); acc /= W; }
            ulong expectedMix = (ulong)t6 ^ (ulong)c1 ^ a[0] ^ a[1] ^ a[2] ^ a[3] ^ a[4] ^ a[5] ^ m0 ^ m1;
            if (mix != expectedMix) throw new Exception("PressureRows carry mismatch");
        }
    }
}
