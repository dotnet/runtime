// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Constant-time selects t ^ ((t ^ s) & (0 - bit)) after carry and borrow chains, which
// LowerMaskSelects turns into CMOVs reading the flags, and shapes it must leave alone.
public class MaskSelectTests
{
    public struct Words
    {
        public ulong A, B, C, D;
        public Words(ulong a, ulong b, ulong c, ulong d) { A = a; B = b; C = c; D = d; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong AddCarry(ulong a, ulong b, ref ulong carry)
    {
        ulong sum = a + b;
        ulong c1 = sum < a ? 1UL : 0UL;
        ulong result = sum + carry;
        carry = c1 + (result < sum ? 1UL : 0UL);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong SubBorrow(ulong a, ulong b, ref ulong borrow)
    {
        ulong difference = a - b;
        ulong b1 = difference > a ? 1UL : 0UL;
        ulong result = difference - borrow;
        borrow = b1 + (result > difference ? 1UL : 0UL);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Select(ulong whenSet, ulong whenClear, ulong mask) => whenClear ^ ((whenClear ^ whenSet) & mask);

    // t + k, or t itself when that overflows 256 bits.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words AddUnlessOverflow(Words t, Words k)
    {
        ulong s0 = t.A + k.A;
        ulong carry = s0 < t.A ? 1UL : 0UL;
        ulong s1 = AddCarry(t.B, k.B, ref carry);
        ulong s2 = AddCarry(t.C, k.C, ref carry);
        ulong s3 = AddCarry(t.D, k.D, ref carry);
        ulong mask = 0UL - (carry & 1UL);
        return new Words(Select(t.A, s0, mask), Select(t.B, s1, mask), Select(t.C, s2, mask), Select(t.D, s3, mask));
    }

    // The same with flag-writing arithmetic between the chain and the selects.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words AddUnlessOverflowClobbered(Words t, Words k, ulong x, ulong y, out ulong unrelated)
    {
        ulong s0 = t.A + k.A;
        ulong carry = s0 < t.A ? 1UL : 0UL;
        ulong s1 = AddCarry(t.B, k.B, ref carry);
        ulong s2 = AddCarry(t.C, k.C, ref carry);
        ulong s3 = AddCarry(t.D, k.D, ref carry);
        ulong mask = 0UL - carry;
        ulong r0 = Select(t.A, s0, mask);
        unrelated = (x + y) ^ (x - y);
        ulong r1 = Select(t.B, s1, mask) + unrelated;
        return new Words(r0, r1 - unrelated, Select(t.C, s2, mask), Select(t.D, s3, mask));
    }

    // The mask also escapes, so it must stay materialized.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words AddUnlessOverflowKeepMask(Words t, Words k, out ulong mask)
    {
        ulong s0 = t.A + k.A;
        ulong carry = s0 < t.A ? 1UL : 0UL;
        ulong s1 = AddCarry(t.B, k.B, ref carry);
        ulong s2 = AddCarry(t.C, k.C, ref carry);
        ulong s3 = AddCarry(t.D, k.D, ref carry);
        mask = 0UL - carry;
        return new Words(Select(t.A, s0, mask), Select(t.B, s1, mask), Select(t.C, s2, mask), Select(t.D, s3, mask));
    }

    // t - k, or t itself on a borrow: the conditional subtraction of modular arithmetic.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words SubUnlessBorrow(Words t, Words k)
    {
        ulong d0 = t.A - k.A;
        ulong borrow = d0 > t.A ? 1UL : 0UL;
        ulong d1 = SubBorrow(t.B, k.B, ref borrow);
        ulong d2 = SubBorrow(t.C, k.C, ref borrow);
        ulong d3 = SubBorrow(t.D, k.D, ref borrow);
        ulong mask = 0UL - borrow;
        return new Words(Select(t.A, d0, mask), Select(t.B, d1, mask), Select(t.C, d2, mask), Select(t.D, d3, mask));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Flip(ref ulong value) => value = ~value;

    // t is address-exposed, so its read can stay ahead of the masked XOR.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddUnlessOverflowExposed(ulong t0, ulong t1, ulong k0, ulong k1)
    {
        ulong s0 = t0 + k0;
        ulong carry = s0 < t0 ? 1UL : 0UL;
        ulong s1 = AddCarry(t1, k1, ref carry);
        ulong mask = 0UL - carry;
        ulong r0 = Select(t0, s0, mask);
        ulong r1 = Select(t1, s1, mask);
        Flip(ref t1);
        return r0 ^ r1 ^ t1;
    }

    // A select of a select on the same mask.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong NestedSelect(ulong t0, ulong t1, ulong k0, ulong k1, ulong c)
    {
        ulong s0 = t0 + k0;
        ulong carry = s0 < t0 ? 1UL : 0UL;
        ulong s1 = AddCarry(t1, k1, ref carry);
        ulong mask = 0UL - carry;
        return Select(Select(t1, s1, mask), c, mask) ^ Select(t0, s0, mask);
    }

    // The carry has another reader.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddUnlessOverflowKeepCarry(ulong t0, ulong t1, ulong k0, ulong k1, out ulong carryOut)
    {
        ulong s0 = t0 + k0;
        ulong carry = s0 < t0 ? 1UL : 0UL;
        ulong s1 = AddCarry(t1, k1, ref carry);
        ulong mask = 0UL - carry;
        ulong result = Select(t0, s0, mask) ^ Select(t1, s1, mask);
        carryOut = carry;
        return result;
    }

    // A handler observes the mask.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MaskInHandler(ulong t0, ulong t1, ulong k0, ulong k1, ulong[] sink)
    {
        ulong mask = 0;
        try
        {
            ulong s0 = t0 + k0;
            ulong carry = s0 < t0 ? 1UL : 0UL;
            ulong s1 = AddCarry(t1, k1, ref carry);
            mask = 0UL - carry;
            sink[0] = Select(t0, s0, mask) ^ Select(t1, s1, mask);
            mask = 0;
        }
        catch (IndexOutOfRangeException)
        {
            return mask;
        }
        return sink[0];
    }

    private static BigInteger Value(Words w) =>
        w.A | ((BigInteger)w.B << 64) | ((BigInteger)w.C << 128) | ((BigInteger)w.D << 192);

    [Fact]
    public static void TestEntryPoint()
    {
        var random = new Random(80675);
        ulong Next() => random.Next(4) switch { 0 => ulong.MaxValue, 1 => 0, 2 => 1, _ => (ulong)random.NextInt64() * 3 + (ulong)random.Next() };
        BigInteger W256 = BigInteger.One << 256;
        for (int i = 0; i < 20000; i++)
        {
            Words t = new Words(Next(), Next(), Next(), Next());
            Words k = new Words(Next(), Next(), Next(), Next());
            BigInteger tv = Value(t), kv = Value(k);
            BigInteger addExpected = tv + kv >= W256 ? tv : tv + kv;

            if (Value(AddUnlessOverflow(t, k)) != addExpected)
                throw new Exception("AddUnlessOverflow");

            ulong x = Next(), y = Next();
            Words clobbered = AddUnlessOverflowClobbered(t, k, x, y, out ulong unrelated);
            if (unrelated != ((x + y) ^ (x - y)) || Value(clobbered) != addExpected)
                throw new Exception("AddUnlessOverflowClobbered");

            Words kept = AddUnlessOverflowKeepMask(t, k, out ulong mask);
            if (mask != (tv + kv >= W256 ? ulong.MaxValue : 0) || Value(kept) != addExpected)
                throw new Exception("AddUnlessOverflowKeepMask");

            if (Value(SubUnlessBorrow(t, k)) != (tv < kv ? tv : tv - kv))
                throw new Exception("SubUnlessBorrow");

            UInt128 t2 = new UInt128(t.B, t.A), sum2 = t2 + new UInt128(k.B, k.A);
            bool overflow = sum2 < t2;
            ulong r0 = overflow ? t.A : (ulong)sum2, r1 = overflow ? t.B : (ulong)(sum2 >> 64);
            if (AddUnlessOverflowExposed(t.A, t.B, k.A, k.B) != (r0 ^ r1 ^ ~t.B))
                throw new Exception("AddUnlessOverflowExposed");

            if (NestedSelect(t.A, t.B, k.A, k.B, t.C) != ((overflow ? t.B : t.C) ^ r0))
                throw new Exception("NestedSelect");

            if (AddUnlessOverflowKeepCarry(t.A, t.B, k.A, k.B, out ulong carry) != (r0 ^ r1) || carry != (overflow ? 1UL : 0UL))
                throw new Exception("AddUnlessOverflowKeepCarry");

            if (MaskInHandler(t.A, t.B, k.A, k.B, new ulong[1]) != (r0 ^ r1) ||
                MaskInHandler(t.A, t.B, k.A, k.B, Array.Empty<ulong>()) != (overflow ? ulong.MaxValue : 0))
                throw new Exception("MaskInHandler");
        }
    }
}
