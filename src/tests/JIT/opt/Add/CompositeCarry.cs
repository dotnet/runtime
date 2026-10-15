// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// Several independent chains in one method exceed the old candidate and
// method-size allowances. Preserve both each row's carry and the final words.
public class CompositeCarryTests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Add(ulong a, ulong b, ref ulong carry)
    {
        ulong s = a + b, c = s < a ? 1UL : 0UL;
        ulong r = s + carry;
        carry = c + (r < s ? 1UL : 0UL);
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Sub(ulong a, ulong b, ref ulong borrow)
    {
        ulong s = a - b, c = a < b ? 1UL : 0UL;
        ulong r = s - borrow;
        borrow = c + (s < borrow ? 1UL : 0UL);
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddRows(ulong[] a, ulong[] b, ulong[] result)
    {
        ulong r0 = a[0], r1 = a[1], r2 = a[2], r3 = a[3], r4 = a[4], r5 = a[5];
        ulong b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3], b4 = b[4], b5 = b[5];
        ulong total = 0;
        ulong c0 = 0;
        r0 = Add(r0, b0, ref c0);
        r1 = Add(r1, b1, ref c0);
        r2 = Add(r2, b2, ref c0);
        r3 = Add(r3, b3, ref c0);
        r4 = Add(r4, b4, ref c0);
        r5 = Add(r5, b5, ref c0);
        total += c0;
        ulong c1 = 0;
        r0 = Add(r0, b0, ref c1);
        r1 = Add(r1, b1, ref c1);
        r2 = Add(r2, b2, ref c1);
        r3 = Add(r3, b3, ref c1);
        r4 = Add(r4, b4, ref c1);
        r5 = Add(r5, b5, ref c1);
        total += c1;
        ulong c2 = 0;
        r0 = Add(r0, b0, ref c2);
        r1 = Add(r1, b1, ref c2);
        r2 = Add(r2, b2, ref c2);
        r3 = Add(r3, b3, ref c2);
        r4 = Add(r4, b4, ref c2);
        r5 = Add(r5, b5, ref c2);
        total += c2;
        ulong c3 = 0;
        r0 = Add(r0, b0, ref c3);
        r1 = Add(r1, b1, ref c3);
        r2 = Add(r2, b2, ref c3);
        r3 = Add(r3, b3, ref c3);
        r4 = Add(r4, b4, ref c3);
        r5 = Add(r5, b5, ref c3);
        total += c3;
        ulong c4 = 0;
        r0 = Add(r0, b0, ref c4);
        r1 = Add(r1, b1, ref c4);
        r2 = Add(r2, b2, ref c4);
        r3 = Add(r3, b3, ref c4);
        r4 = Add(r4, b4, ref c4);
        r5 = Add(r5, b5, ref c4);
        total += c4;
        ulong c5 = 0;
        r0 = Add(r0, b0, ref c5);
        r1 = Add(r1, b1, ref c5);
        r2 = Add(r2, b2, ref c5);
        r3 = Add(r3, b3, ref c5);
        r4 = Add(r4, b4, ref c5);
        r5 = Add(r5, b5, ref c5);
        total += c5;
        ulong c6 = 0;
        r0 = Add(r0, b0, ref c6);
        r1 = Add(r1, b1, ref c6);
        r2 = Add(r2, b2, ref c6);
        r3 = Add(r3, b3, ref c6);
        r4 = Add(r4, b4, ref c6);
        r5 = Add(r5, b5, ref c6);
        total += c6;
        ulong c7 = 0;
        r0 = Add(r0, b0, ref c7);
        r1 = Add(r1, b1, ref c7);
        r2 = Add(r2, b2, ref c7);
        r3 = Add(r3, b3, ref c7);
        r4 = Add(r4, b4, ref c7);
        r5 = Add(r5, b5, ref c7);
        total += c7;
        ulong c8 = 0;
        r0 = Add(r0, b0, ref c8);
        r1 = Add(r1, b1, ref c8);
        r2 = Add(r2, b2, ref c8);
        r3 = Add(r3, b3, ref c8);
        r4 = Add(r4, b4, ref c8);
        r5 = Add(r5, b5, ref c8);
        total += c8;
        ulong c9 = 0;
        r0 = Add(r0, b0, ref c9);
        r1 = Add(r1, b1, ref c9);
        r2 = Add(r2, b2, ref c9);
        r3 = Add(r3, b3, ref c9);
        r4 = Add(r4, b4, ref c9);
        r5 = Add(r5, b5, ref c9);
        total += c9;
        ulong c10 = 0;
        r0 = Add(r0, b0, ref c10);
        r1 = Add(r1, b1, ref c10);
        r2 = Add(r2, b2, ref c10);
        r3 = Add(r3, b3, ref c10);
        r4 = Add(r4, b4, ref c10);
        r5 = Add(r5, b5, ref c10);
        total += c10;
        ulong c11 = 0;
        r0 = Add(r0, b0, ref c11);
        r1 = Add(r1, b1, ref c11);
        r2 = Add(r2, b2, ref c11);
        r3 = Add(r3, b3, ref c11);
        r4 = Add(r4, b4, ref c11);
        r5 = Add(r5, b5, ref c11);
        total += c11;
        ulong c12 = 0;
        r0 = Add(r0, b0, ref c12);
        r1 = Add(r1, b1, ref c12);
        r2 = Add(r2, b2, ref c12);
        r3 = Add(r3, b3, ref c12);
        r4 = Add(r4, b4, ref c12);
        r5 = Add(r5, b5, ref c12);
        total += c12;
        ulong c13 = 0;
        r0 = Add(r0, b0, ref c13);
        r1 = Add(r1, b1, ref c13);
        r2 = Add(r2, b2, ref c13);
        r3 = Add(r3, b3, ref c13);
        r4 = Add(r4, b4, ref c13);
        r5 = Add(r5, b5, ref c13);
        total += c13;
        ulong c14 = 0;
        r0 = Add(r0, b0, ref c14);
        r1 = Add(r1, b1, ref c14);
        r2 = Add(r2, b2, ref c14);
        r3 = Add(r3, b3, ref c14);
        r4 = Add(r4, b4, ref c14);
        r5 = Add(r5, b5, ref c14);
        total += c14;
        ulong c15 = 0;
        r0 = Add(r0, b0, ref c15);
        r1 = Add(r1, b1, ref c15);
        r2 = Add(r2, b2, ref c15);
        r3 = Add(r3, b3, ref c15);
        r4 = Add(r4, b4, ref c15);
        r5 = Add(r5, b5, ref c15);
        total += c15;
        result[0] = r0; result[1] = r1; result[2] = r2; result[3] = r3; result[4] = r4; result[5] = r5;
        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SubRows(ulong[] a, ulong[] b, ulong[] result)
    {
        ulong r0 = a[0], r1 = a[1], r2 = a[2], r3 = a[3], r4 = a[4], r5 = a[5];
        ulong b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3], b4 = b[4], b5 = b[5];
        ulong total = 0;
        ulong c0 = 0;
        r0 = Sub(r0, b0, ref c0);
        r1 = Sub(r1, b1, ref c0);
        r2 = Sub(r2, b2, ref c0);
        r3 = Sub(r3, b3, ref c0);
        r4 = Sub(r4, b4, ref c0);
        r5 = Sub(r5, b5, ref c0);
        total += c0;
        ulong c1 = 0;
        r0 = Sub(r0, b0, ref c1);
        r1 = Sub(r1, b1, ref c1);
        r2 = Sub(r2, b2, ref c1);
        r3 = Sub(r3, b3, ref c1);
        r4 = Sub(r4, b4, ref c1);
        r5 = Sub(r5, b5, ref c1);
        total += c1;
        ulong c2 = 0;
        r0 = Sub(r0, b0, ref c2);
        r1 = Sub(r1, b1, ref c2);
        r2 = Sub(r2, b2, ref c2);
        r3 = Sub(r3, b3, ref c2);
        r4 = Sub(r4, b4, ref c2);
        r5 = Sub(r5, b5, ref c2);
        total += c2;
        ulong c3 = 0;
        r0 = Sub(r0, b0, ref c3);
        r1 = Sub(r1, b1, ref c3);
        r2 = Sub(r2, b2, ref c3);
        r3 = Sub(r3, b3, ref c3);
        r4 = Sub(r4, b4, ref c3);
        r5 = Sub(r5, b5, ref c3);
        total += c3;
        ulong c4 = 0;
        r0 = Sub(r0, b0, ref c4);
        r1 = Sub(r1, b1, ref c4);
        r2 = Sub(r2, b2, ref c4);
        r3 = Sub(r3, b3, ref c4);
        r4 = Sub(r4, b4, ref c4);
        r5 = Sub(r5, b5, ref c4);
        total += c4;
        ulong c5 = 0;
        r0 = Sub(r0, b0, ref c5);
        r1 = Sub(r1, b1, ref c5);
        r2 = Sub(r2, b2, ref c5);
        r3 = Sub(r3, b3, ref c5);
        r4 = Sub(r4, b4, ref c5);
        r5 = Sub(r5, b5, ref c5);
        total += c5;
        ulong c6 = 0;
        r0 = Sub(r0, b0, ref c6);
        r1 = Sub(r1, b1, ref c6);
        r2 = Sub(r2, b2, ref c6);
        r3 = Sub(r3, b3, ref c6);
        r4 = Sub(r4, b4, ref c6);
        r5 = Sub(r5, b5, ref c6);
        total += c6;
        ulong c7 = 0;
        r0 = Sub(r0, b0, ref c7);
        r1 = Sub(r1, b1, ref c7);
        r2 = Sub(r2, b2, ref c7);
        r3 = Sub(r3, b3, ref c7);
        r4 = Sub(r4, b4, ref c7);
        r5 = Sub(r5, b5, ref c7);
        total += c7;
        ulong c8 = 0;
        r0 = Sub(r0, b0, ref c8);
        r1 = Sub(r1, b1, ref c8);
        r2 = Sub(r2, b2, ref c8);
        r3 = Sub(r3, b3, ref c8);
        r4 = Sub(r4, b4, ref c8);
        r5 = Sub(r5, b5, ref c8);
        total += c8;
        ulong c9 = 0;
        r0 = Sub(r0, b0, ref c9);
        r1 = Sub(r1, b1, ref c9);
        r2 = Sub(r2, b2, ref c9);
        r3 = Sub(r3, b3, ref c9);
        r4 = Sub(r4, b4, ref c9);
        r5 = Sub(r5, b5, ref c9);
        total += c9;
        ulong c10 = 0;
        r0 = Sub(r0, b0, ref c10);
        r1 = Sub(r1, b1, ref c10);
        r2 = Sub(r2, b2, ref c10);
        r3 = Sub(r3, b3, ref c10);
        r4 = Sub(r4, b4, ref c10);
        r5 = Sub(r5, b5, ref c10);
        total += c10;
        ulong c11 = 0;
        r0 = Sub(r0, b0, ref c11);
        r1 = Sub(r1, b1, ref c11);
        r2 = Sub(r2, b2, ref c11);
        r3 = Sub(r3, b3, ref c11);
        r4 = Sub(r4, b4, ref c11);
        r5 = Sub(r5, b5, ref c11);
        total += c11;
        ulong c12 = 0;
        r0 = Sub(r0, b0, ref c12);
        r1 = Sub(r1, b1, ref c12);
        r2 = Sub(r2, b2, ref c12);
        r3 = Sub(r3, b3, ref c12);
        r4 = Sub(r4, b4, ref c12);
        r5 = Sub(r5, b5, ref c12);
        total += c12;
        ulong c13 = 0;
        r0 = Sub(r0, b0, ref c13);
        r1 = Sub(r1, b1, ref c13);
        r2 = Sub(r2, b2, ref c13);
        r3 = Sub(r3, b3, ref c13);
        r4 = Sub(r4, b4, ref c13);
        r5 = Sub(r5, b5, ref c13);
        total += c13;
        ulong c14 = 0;
        r0 = Sub(r0, b0, ref c14);
        r1 = Sub(r1, b1, ref c14);
        r2 = Sub(r2, b2, ref c14);
        r3 = Sub(r3, b3, ref c14);
        r4 = Sub(r4, b4, ref c14);
        r5 = Sub(r5, b5, ref c14);
        total += c14;
        ulong c15 = 0;
        r0 = Sub(r0, b0, ref c15);
        r1 = Sub(r1, b1, ref c15);
        r2 = Sub(r2, b2, ref c15);
        r3 = Sub(r3, b3, ref c15);
        r4 = Sub(r4, b4, ref c15);
        r5 = Sub(r5, b5, ref c15);
        total += c15;
        result[0] = r0; result[1] = r1; result[2] = r2; result[3] = r3; result[4] = r4; result[5] = r5;
        return total;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var rng = new Random(97531);
        ulong[] a = new ulong[6], b = new ulong[6], result = new ulong[6];
        BigInteger radix = BigInteger.One << 384;
        for (int sample = 0; sample < 2000; sample++)
        {
            rng.NextBytes(MemoryMarshal.AsBytes(a.AsSpan()));
            rng.NextBytes(MemoryMarshal.AsBytes(b.AsSpan()));
            if (sample < 4)
            {
                Array.Fill(a, (sample & 1) == 0 ? 0UL : ulong.MaxValue);
                Array.Fill(b, (sample & 2) == 0 ? 0UL : ulong.MaxValue);
            }
            BigInteger av = new BigInteger(MemoryMarshal.AsBytes(a.AsSpan()), true);
            BigInteger bv = new BigInteger(MemoryMarshal.AsBytes(b.AsSpan()), true);
            BigInteger expected = av + 16 * bv;
            ulong carries = AddRows(a, b, result);
            BigInteger actual = new BigInteger(MemoryMarshal.AsBytes(result.AsSpan()), true);
            if (actual != expected % radix || carries != (ulong)(expected / radix)) return 101;
            expected = av - 16 * bv;
            ulong borrows = SubRows(a, b, result);
            actual = new BigInteger(MemoryMarshal.AsBytes(result.AsSpan()), true);
            BigInteger normalized = (expected % radix + radix) % radix;
            if (actual != normalized || borrows != (ulong)((normalized - expected) / radix)) return 102;
            // Overlapping output with an input must retain the entry values.
            ulong[] alias = (ulong[])a.Clone();
            if (AddRows(alias, b, alias) != carries ||
                new BigInteger(MemoryMarshal.AsBytes(alias.AsSpan()), true) != (av + 16 * bv) % radix) return 103;
        }
        return 100;
    }
}
