// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Repeated copies of a carry-row multiplier to RDX may be elided only while
// both registers retain the same value. Cover a 32-bit update, a call, and a
// new row under pressure; the emitter must stop at each of these boundaries.
public class MulxMultiplierCopyTests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MulAdd(ulong m, ulong x, ulong t, ulong c, out ulong lo)
    {
        ulong hi = Math.BigMul(m, x, out ulong low);
        low += t;
        hi += low < t ? 1UL : 0UL;
        low += c;
        hi += low < c ? 1UL : 0UL;
        lo = low;
        return hi;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AcrossCall(ulong m) => (m << 17) | (m >> 47);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Rows(ulong seed, ulong delta, ulong[] a, ulong[] t)
    {
        ulong x0 = a[0], x1 = a[1], x2 = a[2], x3 = a[3], x4 = a[4], x5 = a[5];
        ulong t0 = t[0], t1 = t[1], t2 = t[2], t3 = t[3], t4 = t[4], t5 = t[5];
        ulong q = seed * 0x89f3fffcfffcfffdUL;
        ulong mix = 0;
        ulong carry;
        carry = MulAdd(q, x0, t0, 0, out t0);
        carry = MulAdd(q, x1, t1, carry, out t1);
        carry = MulAdd(q, x2, t2, carry, out t2);
        carry = MulAdd(q, x3, t3, carry, out t3);
        carry = MulAdd(q, x4, t4, carry, out t4);
        carry = MulAdd(q, x5, t5, carry, out t5);
        mix ^= carry;
        q = (uint)(q + delta); // partial-width definition invalidates a previous copy
        carry = MulAdd(q, x0, t0, 0, out t0);
        carry = MulAdd(q, x1, t1, carry, out t1);
        carry = MulAdd(q, x2, t2, carry, out t2);
        carry = MulAdd(q, x3, t3, carry, out t3);
        carry = MulAdd(q, x4, t4, carry, out t4);
        carry = MulAdd(q, x5, t5, carry, out t5);
        mix ^= carry;
        q = AcrossCall(q ^ seed); // a call invalidates register-value knowledge
        carry = MulAdd(q, x0, t0, 0, out t0);
        carry = MulAdd(q, x1, t1, carry, out t1);
        carry = MulAdd(q, x2, t2, carry, out t2);
        carry = MulAdd(q, x3, t3, carry, out t3);
        carry = MulAdd(q, x4, t4, carry, out t4);
        carry = MulAdd(q, x5, t5, carry, out t5);
        mix ^= carry;
        t[0] = t0; t[1] = t1; t[2] = t2; t[3] = t3; t[4] = t4; t[5] = t5;
        return mix ^ q;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var random = new Random(134040);
        ulong Next() => random.Next(5) switch
        {
            0 => 0, 1 => ulong.MaxValue, 2 => 0xffffffffUL,
            _ => (ulong)random.NextInt64() * 3 + (ulong)random.Next()
        };
        BigInteger radix = BigInteger.One << 64;
        BigInteger modulus = BigInteger.One << 384;
        for (int i = 0; i < 5000; i++)
        {
            ulong seed = Next(), delta = Next();
            ulong[] a = { Next(), Next(), Next(), Next(), Next(), Next() };
            ulong[] t = { Next(), Next(), Next(), Next(), Next(), Next() };
            BigInteger x = 0, acc = 0;
            for (int j = 5; j >= 0; j--) { x = x * radix + a[j]; acc = acc * radix + t[j]; }
            ulong q = unchecked(seed * 0x89f3fffcfffcfffdUL);
            ulong mix = 0;
            for (int row = 0; row < 3; row++)
            {
                if (row == 1) q = unchecked((uint)(q + delta));
                if (row == 2) q = AcrossCall(q ^ seed);
                acc += x * q;
                mix ^= (ulong)(acc / modulus);
                acc %= modulus;
            }
            ulong actual = Rows(seed, delta, a, t);
            if (actual != (mix ^ q)) throw new Exception("Multiplier copy carry mismatch");
            for (int j = 0; j < 6; j++)
            {
                if (t[j] != (ulong)(acc % radix)) throw new Exception("Multiplier copy limb mismatch");
                acc /= radix;
            }
        }
    }
}
