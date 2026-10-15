// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// Integer NOT preserves carry flags. Its operand may move only when the
// closed load/expression tree is invariant; aliasing stores and a dependency
// on the previous sum must keep it at its original position.
public class ComplementedCarryTests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Add(ulong a, ulong b, ref ulong carry)
    {
        ulong s = a + b;
        ulong c0 = s < a ? 1UL : 0UL;
        ulong r = s + carry;
        ulong c1 = r < s ? 1UL : 0UL;
        carry = c0 + c1;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Add(uint a, uint b, ref uint carry)
    {
        uint s = a + b;
        uint c0 = s < a ? 1U : 0U;
        uint r = s + carry;
        uint c1 = r < s ? 1U : 0U;
        carry = c0 + c1;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Words(ulong[] a, ulong[] b, ulong[] result, bool dependent)
    {
        ulong x0 = a[0], x1 = a[1], x2 = a[2];
        ulong y0 = b[0], y1 = b[1], y2 = b[2];
        ulong seed = x0 + 1;
        ulong r0 = seed + ~y0;
        ulong carry = r0 < seed ? 1UL : 0UL;
        // The second variant depends on the just-produced results, so neither
        // complement may be hoisted ahead of the flags producer that defines it.
        ulong r1 = Add(x1, ~(dependent ? r0 : y1), ref carry);
        ulong r2 = Add(x2, ~(dependent ? r1 : y2), ref carry);
        result[0] = r0; result[1] = r1; result[2] = r2;
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Words32(uint b0, uint b1, uint b2, out uint r0, out uint r1, out uint r2)
    {
        const uint x0 = 0xfeffeffe, x1 = 0x80000001, x2 = 0x7fffffff;
        r0 = (x0 + 1) + ~b0;
        uint carry = r0 < (x0 + 1) ? 1U : 0U;
        r1 = Add(x1, ~b1, ref carry);
        r2 = Add(x2, ~b2, ref carry);
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Aliased(ulong[] a, ReadOnlySpan<ulong> right, Span<ulong> output)
    {
        ulong x0 = a[0], x1 = a[1], x2 = a[2];
        ref ulong b = ref MemoryMarshal.GetReference(right);
        ref ulong r = ref MemoryMarshal.GetReference(output);
        ulong seed = x0 + 1;
        ulong r0 = seed + ~b;
        ulong carry = r0 < seed ? 1UL : 0UL;
        r = r0;
        ulong r1 = Add(x1, ~Unsafe.Add(ref b, 1), ref carry);
        Unsafe.Add(ref r, 1) = r1;
        ulong r2 = Add(x2, ~Unsafe.Add(ref b, 2), ref carry);
        Unsafe.Add(ref r, 2) = r2;
        return carry;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var random = new Random(134041);
        ulong Next() => random.Next(5) switch
        {
            0 => 0, 1 => ulong.MaxValue, 2 => 0x8000000000000000UL,
            _ => (ulong)random.NextInt64() * 3 + (ulong)random.Next()
        };
        BigInteger radix = BigInteger.One << 64;
        BigInteger mask = radix - 1;
        BigInteger radix32 = BigInteger.One << 32;
        BigInteger mask32 = radix32 - 1;
        for (int i = 0; i < 5000; i++)
        {
            // Absorbing the initial carry into x0+1 must not overflow.
            ulong[] a = { Next() >> 1, Next(), Next() };
            ulong[] b = { Next(), Next(), Next() };
            ulong[] r = new ulong[3];
            foreach (bool dependent in new[] { false, true })
            {
                ulong actualCarry = Words(a, b, r, dependent);
                ulong expectedCarry = 1;
                ulong[] expected = new ulong[3];
                for (int j = 0; j < 3; j++)
                {
                    ulong operand = dependent && j != 0 ? expected[j - 1] : b[j];
                    BigInteger sum = (BigInteger)a[j] + (mask - operand) + expectedCarry;
                    expected[j] = (ulong)(sum & mask);
                    expectedCarry = (ulong)(sum >> 64);
                    if (r[j] != expected[j]) throw new Exception("Complemented carry limb mismatch");
                }
                if (actualCarry != expectedCarry) throw new Exception("Complemented carry mismatch");
            }

            uint b0 = (uint)Next(), b1 = (uint)Next(), b2 = (uint)Next();
            uint carry32 = Words32(b0, b1, b2, out uint r0, out uint r1, out uint r2);
            uint[] x32 = { 0xfeffeffe, 0x80000001, 0x7fffffff };
            uint[] y32 = { b0, b1, b2 }, z32 = { r0, r1, r2 };
            uint expected32 = 1;
            for (int j = 0; j < 3; j++)
            {
                BigInteger sum = (BigInteger)x32[j] + (mask32 - y32[j]) + expected32;
                if (z32[j] != (uint)(sum & mask32)) throw new Exception("32-bit complement mismatch");
                expected32 = (uint)(sum >> 32);
            }
            if (carry32 != expected32) throw new Exception("32-bit complement carry mismatch");

            foreach (int offset in new[] { 0, 1 })
            {
                ulong[] buffer = { b[0], b[1], b[2], Next() };
                ulong[] expected = (ulong[])buffer.Clone();
                ulong expectedCarry = 1;
                for (int j = 0; j < 3; j++)
                {
                    BigInteger sum = (BigInteger)a[j] + (mask - expected[j]) + expectedCarry;
                    expected[offset + j] = (ulong)(sum & mask);
                    expectedCarry = (ulong)(sum >> 64);
                }
                ulong actualCarry = Aliased(a, buffer.AsSpan(0, 3), buffer.AsSpan(offset, 3));
                if (actualCarry != expectedCarry) throw new Exception("Aliased complement carry mismatch");
                for (int j = 0; j < 4; j++)
                    if (buffer[j] != expected[j]) throw new Exception("Aliased complement load moved across store");
            }
        }
    }
}
