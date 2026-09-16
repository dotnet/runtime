// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class CarryChainTests
{
    public struct Words
    {
        public ulong A, B, C, D;
        public Words(ulong a, ulong b, ulong c, ulong d) { A = a; B = b; C = c; D = d; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Add(ulong a, ulong b, ulong carry, out ulong next)
    {
        ulong sum = a + b;
        ulong c1 = sum < a ? 1UL : 0;
        ulong result = sum + carry;
        next = c1 + (result < sum ? 1UL : 0);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Sub(ulong a, ulong b, ulong borrow, out ulong next)
    {
        ulong difference = a - b;
        ulong b1 = difference > a ? 1UL : 0;
        ulong result = difference - borrow;
        next = b1 + (result > difference ? 1UL : 0);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words AddChain(Words a, Words b)
    {
        ulong w0 = Add(a.A, b.A, 0, out ulong carry);
        ulong w1 = Add(a.B, b.B, carry, out carry);
        ulong w2 = Add(a.C, b.C, carry, out carry);
        ulong w3 = a.D + b.D + carry;
        return new Words(w0, w1, w2, w3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words SubChain(Words a, Words b)
    {
        ulong w0 = Sub(a.A, b.A, 0, out ulong borrow);
        ulong w1 = Sub(a.B, b.B, borrow, out borrow);
        ulong w2 = Sub(a.C, b.C, borrow, out borrow);
        ulong w3 = a.D - b.D - borrow;
        return new Words(w0, w1, w2, w3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Clobber(ulong value) => (value * 37) ^ (value >> 3);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words Interrupted(Words a, Words b, out ulong observed)
    {
        ulong w0 = Add(a.A, b.A, 0, out ulong carry);
        observed = Clobber(carry);
        ulong w1 = Add(a.B, b.B, carry, out carry);
        ulong w2 = Add(a.C, b.C, carry, out carry);
        return new Words(w0, w1, w2, a.D + b.D + carry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words ExtraConsumer(Words a, Words b, out ulong observed)
    {
        ulong w0 = Sub(a.A, b.A, 0, out ulong borrow);
        ulong saved = borrow;
        ulong w1 = Sub(a.B, b.B, borrow, out borrow);
        ulong w2 = Sub(a.C, b.C, borrow, out borrow);
        observed = saved;
        return new Words(w0, w1, w2, a.D - b.D - borrow);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words FullLimbCarry(Words a, Words b, ulong carry)
    {
        ulong w0 = Add(a.A, b.A, carry, out carry);
        ulong w1 = Add(a.B, b.B, carry, out carry);
        ulong w2 = Add(a.C, b.C, carry, out carry);
        return new Words(w0, w1, w2, a.D + b.D + carry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Words TwoCarrySeed(Words a, Words b)
    {
        // Each comparison is a bit, but their sum need not be. This local
        // must not acquire a one-bit proof merely because its inputs have one.
        ulong carry = (a.A + b.A < a.A ? 1UL : 0) + (a.B + b.B < a.B ? 1UL : 0);
        ulong w0 = Add(a.A, b.A, carry, out carry);
        ulong w1 = Add(a.B, b.B, carry, out carry);
        ulong w2 = Add(a.C, b.C, carry, out carry);
        return new Words(w0, w1, w2, a.D + b.D + carry);
    }

    private static BigInteger Value(Words value) => value.A | ((BigInteger)value.B << 64) |
        ((BigInteger)value.C << 128) | ((BigInteger)value.D << 192);

    [Fact]
    public static void TestEntryPoint()
    {
        Random random = new Random(80674);
        byte[] bytes = new byte[64];
        BigInteger mask = (BigInteger.One << 256) - 1;
        void Equal(Words actual, BigInteger expected)
        {
            if (Value(actual) != (expected & mask)) throw new Exception("Incorrect carry chain");
        }
        for (int i = 0; i < 3000; i++)
        {
            random.NextBytes(bytes);
            Words a = new Words(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8), BitConverter.ToUInt64(bytes, 16), BitConverter.ToUInt64(bytes, 24));
            Words b = new Words(BitConverter.ToUInt64(bytes, 32), BitConverter.ToUInt64(bytes, 40), BitConverter.ToUInt64(bytes, 48), BitConverter.ToUInt64(bytes, 56));
            if (i < 4)
            {
                a = new Words(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
                b = i == 0 ? a : new Words(1, 0, 0, 0);
                if (i == 2) a = default;
                if (i == 3) b = default;
            }
            BigInteger x = Value(a), y = Value(b);
            Equal(AddChain(a, b), x + y);
            Equal(SubChain(a, b), x - y);
            Equal(Interrupted(a, b, out ulong observed), x + y);
            if (observed != Clobber(a.A + b.A < a.A ? 1UL : 0)) throw new Exception("Lost carry consumer");
            Equal(ExtraConsumer(a, b, out observed), x - y);
            if (observed != (a.A < b.A ? 1UL : 0)) throw new Exception("Lost borrow consumer");
            Equal(FullLimbCarry(a, b, 2), x + y + 2);
            Equal(FullLimbCarry(a, b, ulong.MaxValue), x + y + ulong.MaxValue);
            ulong seed = (a.A + b.A < a.A ? 1UL : 0) + (a.B + b.B < a.B ? 1UL : 0);
            Equal(TwoCarrySeed(a, b), x + y + seed);
        }
    }
}
