// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class SeededMaskedCarryTests
{
    private const ulong K0 = 0xb9feffffffffaaab;
    private const ulong K1 = 0x1eabfffeb153ffff;
    private const ulong K2 = 0x6730d2a0f6b0f624;
    private static readonly BigInteger Limit = BigInteger.One << 192;
    private static readonly BigInteger Constant = K0 + ((BigInteger)K1 << 64) + ((BigInteger)K2 << 128);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Add(ulong a, ulong b, ref ulong carry)
    {
        ulong sum = a + b;
        ulong bit = sum < a ? 1UL : 0UL;
        ulong result = sum + carry;
        carry = bit + (result < sum ? 1UL : 0UL);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Sub(ulong a, ulong b, ref ulong borrow)
    {
        ulong difference = a - b;
        ulong bit = difference > a ? 1UL : 0UL;
        ulong result = difference - borrow;
        borrow = bit + (result > difference ? 1UL : 0UL);
        return result;
    }

    // Copy propagation gives the first carry a different local. The masks
    // between successive limbs must move before, never across, their inputs.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BigInteger Masked(ulong a, ulong b, ulong c, ulong mask, bool subtract)
    {
        ulong carry = 0;
        ulong x, y, z;
        if (subtract)
        {
            x = Sub(a, K0 & mask, ref carry);
            y = Sub(b, K1 & mask, ref carry);
            z = Sub(c, K2 & mask, ref carry);
        }
        else
        {
            x = Add(a, K0 & mask, ref carry);
            y = Add(b, K1 & mask, ref carry);
            z = Add(c, K2 & mask, ref carry);
        }
        return x + ((BigInteger)y << 64) + ((BigInteger)z << 128);
    }

    // The incoming word is deliberately not a bit. Do not fold its full adder.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong NonBit(ulong a, ulong b, ulong input, out ulong carry)
    {
        carry = input;
        return Add(a, b, ref carry);
    }

    // A mask depends on the previous result; it cannot move ahead of that
    // definition even if doing so would remove a SETCC/restore pair.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BigInteger Dependent(ulong a, ulong b, ulong c, ulong mask)
    {
        ulong carry = 0;
        ulong x = Add(a, K0 & mask, ref carry);
        mask ^= x;
        ulong y = Add(b, K1 & mask, ref carry);
        ulong z = Add(c, K2 & mask, ref carry);
        return x + ((BigInteger)y << 64) + ((BigInteger)z << 128);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MulAdd(ulong row, ulong limb, ulong addend, ulong carry, out ulong low)
    {
        ulong high = Math.BigMul(row, limb, out low);
        low += addend;
        high += low < addend ? 1UL : 0UL;
        low += carry;
        high += low < carry ? 1UL : 0UL;
        return high;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConstantRow(ulong row, ulong a, ulong b, ulong c, out ulong x, out ulong y, out ulong z, out ulong high)
    {
        ulong carry = MulAdd(row, K0, a, 0, out x);
        carry = MulAdd(row, K1, b, carry, out y);
        high = MulAdd(row, K2, c, carry, out z);
    }

    // Output stores can alias subsequent input loads. Carry forwarding must
    // preserve their order and handle the last limb without hoisting a load.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreAlias(ulong[] words, int offset)
    {
        ulong carry = 0;
        ref ulong first = ref words[0];
        Unsafe.Add(ref first, offset) = Add(first, K0, ref carry);
        Unsafe.Add(ref first, offset + 1) = Add(Unsafe.Add(ref first, 1), K1, ref carry);
        Unsafe.Add(ref first, offset + 2) = Add(Unsafe.Add(ref first, 2), K2, ref carry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MultiplyLoop(Span<ulong> destination, ReadOnlySpan<ulong> source, ulong row)
    {
        ref ulong dst = ref MemoryMarshal.GetReference(destination);
        ref ulong src = ref MemoryMarshal.GetReference(source);
        ulong carry = 0;
        for (int i = 0; i < source.Length; i++)
        {
            carry = MulAdd(row, Unsafe.Add(ref src, i), Unsafe.Add(ref dst, i), carry, out Unsafe.Add(ref dst, i));
        }
        return carry;
    }

    [Fact]
    public static int TestMultiplyLoop()
    {
        Random random = new Random(134039);
        byte[] bytes = new byte[8];
        for (int trial = 0; trial < 500; trial++)
        {
            int count = trial % 17;
            ulong[] destination = new ulong[count];
            ulong[] source = new ulong[count];
            BigInteger a = 0, b = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                random.NextBytes(bytes); destination[i] = BitConverter.ToUInt64(bytes, 0);
                random.NextBytes(bytes); source[i] = BitConverter.ToUInt64(bytes, 0);
                a = (a << 64) + destination[i];
                b = (b << 64) + source[i];
            }
            random.NextBytes(bytes); ulong row = BitConverter.ToUInt64(bytes, 0);
            BigInteger expected = a + b * row;
            ulong carry = MultiplyLoop(destination, source, row);
            BigInteger actual = carry;
            for (int i = count - 1; i >= 0; i--) actual = (actual << 64) + destination[i];
            if (actual != expected) return 6;
        }
        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Random random = new Random(2537);
        ulong[] edges = {0, 1, ulong.MaxValue, ulong.MaxValue - 1, K0, K1, K2};
        byte[] bytes = new byte[8];
        for (int i = 0; i < 10000; i++)
        {
            random.NextBytes(bytes); ulong a = i < edges.Length ? edges[i] : BitConverter.ToUInt64(bytes, 0);
            random.NextBytes(bytes); ulong b = BitConverter.ToUInt64(bytes, 0);
            random.NextBytes(bytes); ulong c = BitConverter.ToUInt64(bytes, 0);
            ulong mask = (i & 1) == 0 ? 0 : ulong.MaxValue;
            BigInteger value = a + ((BigInteger)b << 64) + ((BigInteger)c << 128);
            foreach (bool subtract in new[] {false, true})
            {
                BigInteger expected = (value + (subtract ? -1 : 1) * (mask == 0 ? 0 : Constant)) % Limit;
                if (expected.Sign < 0) expected += Limit;
                BigInteger actual = Masked(a, b, c, mask, subtract);
                if (actual != expected)
                {
                    Console.WriteLine($"i={i} subtract={subtract} a={a:x16} b={b:x16} c={c:x16} mask={mask:x16} actual={actual:x} expected={expected:x}");
                    return 1;
                }
            }
            ConstantRow(a, a, b, c, out ulong rx, out ulong ry, out ulong rz, out ulong rh);
            BigInteger rowExpected = value + (BigInteger)a * Constant;
            BigInteger rowActual = rx + ((BigInteger)ry << 64) + ((BigInteger)rz << 128) + ((BigInteger)rh << 192);
            if (rowActual != rowExpected) return 4;
            foreach (int offset in new[] {0, 1})
            {
                ulong[] words = {a, b, c, 0};
                ulong[] expectedWords = {a, b, c, 0};
                ulong referenceCarry = 0;
                ulong[] constants = {K0, K1, K2};
                for (int lane = 0; lane < 3; lane++)
                {
                    BigInteger total = (BigInteger)expectedWords[lane] + constants[lane] + referenceCarry;
                    expectedWords[offset + lane] = (ulong)(total & ulong.MaxValue);
                    referenceCarry = (ulong)(total >> 64);
                }
                StoreAlias(words, offset);
                for (int lane = 0; lane < 4; lane++) if (words[lane] != expectedWords[lane]) return 5;
            }
            ulong input = i % 3 == 0 ? ulong.MaxValue : (ulong)i;
            ulong sum = a + b;
            ulong r = sum + input;
            ulong expectedCarry = (sum < a ? 1UL : 0UL) + (r < sum ? 1UL : 0UL);
            if (NonBit(a, b, input, out ulong carry) != r || carry != expectedCarry) return 2;
            BigInteger first = (BigInteger)a + (K0 & mask);
            ulong x = (ulong)(first & ulong.MaxValue);
            mask ^= x;
            BigInteger second = (BigInteger)b + (K1 & mask) + (first >> 64);
            BigInteger third = (BigInteger)c + (K2 & mask) + (second >> 64);
            BigInteger expectedDependent = x + ((second & ulong.MaxValue) << 64) + ((third & ulong.MaxValue) << 128);
            if (Dependent(a, b, c, (i & 1) == 0 ? 0 : ulong.MaxValue) != expectedDependent) return 3;
        }
        return 100;
    }
}
