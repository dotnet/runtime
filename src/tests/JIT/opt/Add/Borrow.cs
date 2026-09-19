// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class BorrowTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 Sub128(UInt128 a, UInt128 b)
    {
        // X64: sub
        // X64: sbb
        // ARM64: subs
        // ARM64: sbc
        return a - b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 SubSigned128(Int128 a, Int128 b)
    {
        // X64: sub
        // X64: sbb
        // ARM64: subs
        // ARM64: sbc
        return unchecked(a - b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Borrow(ulong a, ulong b, ulong high, out ulong difference)
    {
        // X64: sub
        // X64: adc
        // ARM64: subs
        // ARM64: cinc
        ulong value = a - b;
        ulong result = high + (value > a ? 1UL : 0);
        difference = value;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong BorrowOnly(ulong a, ulong b, ulong high, out ulong difference)
    {
        // X64: sbb
        // ARM64: sbc {{.*}}, xzr
        ulong value = a - b;
        ulong result = high - (value > a ? 1UL : 0);
        difference = value;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Branch(ulong a, ulong b, out ulong difference)
    {
        difference = a - b;
        if (difference > a)
        {
            return 13;
        }
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Select(ulong a, ulong b, ulong x, ulong y, out ulong difference)
    {
        difference = a - b;
        return difference > a ? x : y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Sub(ulong a, ulong b, ulong borrow, out ulong next)
    {
        ulong d = a - b;
        ulong b1 = d > a ? 1UL : 0;
        ulong r = d - borrow;
        next = b1 + (r > d ? 1UL : 0);
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong LimbLoop(Span<ulong> result, ReadOnlySpan<ulong> left, ReadOnlySpan<ulong> right)
    {
        // X64: sbb
        // X64-NOT: setb
        // X64: jne
        // X64: setb
        // ARM64: sbcs
        // ARM64-NOT: cset
        // ARM64: cbnz
        // ARM64: cset
        result = result.Slice(0, left.Length);
        right = right.Slice(0, left.Length);
        ulong borrow = 0;
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = Sub(left[i], right[i], borrow, out borrow);
        }
        return borrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint LimbLoop32(Span<uint> result, ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        result = result.Slice(0, left.Length);
        right = right.Slice(0, left.Length);
        uint borrow = 0;
        for (int i = 0; i < left.Length; i++)
        {
            uint d = left[i] - right[i];
            uint b1 = d > left[i] ? 1U : 0;
            uint r = d - borrow;
            borrow = b1 + (r > d ? 1U : 0);
            result[i] = r;
        }
        return borrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ArbitraryBorrow(ulong a, ulong b, ulong carry, out ulong next) => Sub(a, b, carry, out next);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong VolatileLoop(Span<ulong> result, ReadOnlySpan<ulong> right)
    {
        ulong borrow = 0;
        for (int i = 0; i < right.Length; i++)
        {
            result[i] = Sub(System.Threading.Volatile.Read(ref result[i]), right[i], borrow, out borrow);
        }
        return borrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AcrossCall(ulong a, ulong b, ulong high)
    {
        ulong d = a - b;
        ulong borrow = d > a ? 1UL : 0;
        return Opaque(high) + borrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Opaque(ulong value) => ~value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SubMul(Span<ulong> result, ReadOnlySpan<ulong> right, ulong multiplier)
    {
        // X64: mul{{x| }}
        // X64: sub
        // X64-NEXT: adc
        // ARM64: umulh
        // ARM64: subs
        // ARM64-NEXT: cinc
        ulong carry = 0;
        int i = 0;
        for (; i + 3 < right.Length; i += 4)
        {
            UInt128 product0 = (UInt128)right[i + 0] * multiplier + carry;
            ulong low0 = (ulong)product0;
            ulong high0 = (ulong)(product0 >> 64);
            ulong original0 = result[i + 0];
            result[i + 0] = original0 - low0;
            high0 += original0 < low0 ? 1UL : 0;
            UInt128 product1 = (UInt128)right[i + 1] * multiplier + high0;
            ulong low1 = (ulong)product1;
            ulong high1 = (ulong)(product1 >> 64);
            ulong original1 = result[i + 1];
            result[i + 1] = original1 - low1;
            high1 += original1 < low1 ? 1UL : 0;
            UInt128 product2 = (UInt128)right[i + 2] * multiplier + high1;
            ulong low2 = (ulong)product2;
            ulong high2 = (ulong)(product2 >> 64);
            ulong original2 = result[i + 2];
            result[i + 2] = original2 - low2;
            high2 += original2 < low2 ? 1UL : 0;
            UInt128 product3 = (UInt128)right[i + 3] * multiplier + high2;
            ulong low3 = (ulong)product3;
            ulong high3 = (ulong)(product3 >> 64);
            ulong original3 = result[i + 3];
            result[i + 3] = original3 - low3;
            high3 += original3 < low3 ? 1UL : 0;
            carry = high3;
        }
        for (; i < right.Length; i++)
        {
            UInt128 product = (UInt128)right[i] * multiplier + carry;
            ulong low = (ulong)product;
            ulong high = (ulong)(product >> 64);
            ulong original = result[i];
            result[i] = original - low;
            high += original < low ? 1UL : 0;
            carry = high;
        }
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CompareRight(ulong a, ulong b) => unchecked(a - b) > b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CheckedSub(ulong a, ulong b) => checked(a - b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong LimbLoopWithTail(Span<ulong> left, ReadOnlySpan<ulong> right)
    {
        // X64: sbb
        // X64-NOT: setb
        // X64: jne
        // X64: setb
        // ARM64: sbcs
        // ARM64-NOT: cset
        // ARM64: cbnz
        // ARM64: cset
        if (right.Length != 0) _ = left[right.Length - 1];
        ulong borrow = 0;
        int i = 0;
        for (; i < right.Length; i++)
        {
            left[i] = Sub(left[i], right[i], borrow, out borrow);
        }
        for (; borrow != 0 && i < left.Length; i++)
        {
            ulong value = left[i];
            left[i] = value - borrow;
            borrow = value == 0 ? 1UL : 0;
        }
        return borrow;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Random random = new Random(125803);
        ulong[] boundary = { 0, 1, 2, 0x7FFFFFFFFFFFFFFF, 0x8000000000000000, ulong.MaxValue - 1, ulong.MaxValue };
        foreach (ulong a in boundary)
        foreach (ulong b in boundary)
        foreach (ulong c in boundary)
        {
            Check(a, b, c, ~c);
        }
        for (int trial = 0; trial < 10000; trial++)
        {
            ulong a = (ulong)random.NextInt64() << 1 | (uint)random.Next(2);
            ulong b = (ulong)random.NextInt64() << 1 | (uint)random.Next(2);
            Check(a, b, (ulong)random.NextInt64(), (ulong)random.NextInt64());
        }
        for (int trial = 0; trial < 512; trial++)
        {
            int n = trial % 65;
            int source = 2, rhs = n + 4;
            int dest = (trial % 4) switch { 0 => 2 * n + 6, 1 => 2, 2 => 3, _ => 1 };
            ulong[] data = new ulong[3 * n + 8];
            random.NextBytes(MemoryMarshal.AsBytes(data.AsSpan()));
            if (trial % 7 == 0) Array.Clear(data, source, n);
            ulong[] expected = (ulong[])data.Clone();
            ulong borrow = 0;
            for (int i = 0; i < n; i++)
            {
                BigInteger d = (BigInteger)expected[source + i] - expected[rhs + i] - borrow;
                expected[dest + i] = (ulong)(d & ulong.MaxValue);
                borrow = d.Sign < 0 ? 1UL : 0;
            }
            ulong[] multiply = (ulong[])data.Clone();
            ulong[] expectedMultiply = (ulong[])data.Clone();
            ulong multiplier = trial % 3 == 0 ? ulong.MaxValue : data[0];
            ulong highCarry = 0;
            for (int i = 0; i < n; i++)
            {
                BigInteger product = (BigInteger)expectedMultiply[source + i] * multiplier + highCarry;
                BigInteger difference = (BigInteger)expectedMultiply[dest + i] - (product & ulong.MaxValue);
                expectedMultiply[dest + i] = (ulong)(difference & ulong.MaxValue);
                highCarry = (ulong)(product >> 64) + (difference.Sign < 0 ? 1UL : 0);
            }
            ulong actualHigh = SubMul(multiply.AsSpan(dest, n), multiply.AsSpan(source, n), multiplier);
            if (actualHigh != highCarry || !multiply.AsSpan().SequenceEqual(expectedMultiply)) throw new Exception("SubMul");

            ulong actual = LimbLoop(data.AsSpan(dest, n), data.AsSpan(source, n), data.AsSpan(rhs, n));
            if (actual != borrow || !data.AsSpan().SequenceEqual(expected)) throw new Exception("LimbLoop");

            ulong[] tail = new ulong[n + 3], tailExpected = new ulong[n + 3];
            random.NextBytes(MemoryMarshal.AsBytes(tail.AsSpan()));
            if (trial % 3 == 0) Array.Clear(tail);
            borrow = 0;
            for (int i = 0; i < tail.Length; i++)
            {
                BigInteger d = (BigInteger)tail[i] - (i < n ? data[rhs + i] : 0UL) - borrow;
                tailExpected[i] = (ulong)(d & ulong.MaxValue);
                borrow = d.Sign < 0 ? 1UL : 0;
            }
            actual = LimbLoopWithTail(tail, data.AsSpan(rhs, n));
            if (actual != borrow || !tail.AsSpan().SequenceEqual(tailExpected)) throw new Exception("LimbLoopWithTail");

            uint[] left32 = new uint[n], right32 = new uint[n], result32 = new uint[n];
            random.NextBytes(MemoryMarshal.AsBytes(left32.AsSpan()));
            random.NextBytes(MemoryMarshal.AsBytes(right32.AsSpan()));
            uint actual32 = LimbLoop32(result32, left32, right32);
            long borrow32 = 0;
            for (int i = 0; i < n; i++)
            {
                long d = (long)left32[i] - right32[i] - borrow32;
                if (result32[i] != unchecked((uint)d)) throw new Exception("LimbLoop32 value");
                borrow32 = d < 0 ? 1 : 0;
            }
            if (actual32 != borrow32) throw new Exception("LimbLoop32 borrow");

            ulong[] volatileResult = new ulong[n], volatileExpected = new ulong[n];
            borrow = 0;
            for (int i = 0; i < n; i++)
            {
                BigInteger d = -(BigInteger)data[rhs + i] - borrow;
                volatileExpected[i] = (ulong)(d & ulong.MaxValue);
                borrow = d.Sign < 0 ? 1UL : 0;
            }
            actual = VolatileLoop(volatileResult, data.AsSpan(rhs, n));
            if (actual != borrow || !volatileResult.AsSpan().SequenceEqual(volatileExpected)) throw new Exception("VolatileLoop");
        }
    }

    private static void Check(ulong a, ulong b, ulong c, ulong d)
    {
        if (CompareRight(a, b) != (unchecked(a - b) > b)) throw new Exception("CompareRight");
        try
        {
            ulong checkedResult = CheckedSub(a, b);
            if (a < b || checkedResult != a - b) throw new Exception("CheckedSub");
        }
        catch (OverflowException)
        {
            if (a >= b) throw;
        }
        ulong bit = a < b ? 1UL : 0;
        ulong high = Borrow(a, b, c, out ulong diff);
        if (diff != unchecked(a - b) || high != unchecked(c + bit)) throw new Exception("Borrow");
        high = BorrowOnly(a, b, c, out diff);
        if (diff != unchecked(a - b) || high != unchecked(c - bit)) throw new Exception("BorrowOnly");
        UInt128 x = ((UInt128)c << 64) | a, y = ((UInt128)d << 64) | b;
        BigInteger expected = ((BigInteger)x - (BigInteger)y) & ((BigInteger.One << 128) - 1);
        if ((BigInteger)Sub128(x, y) != expected) throw new Exception("Sub128");
        if ((BigInteger)unchecked((UInt128)SubSigned128((Int128)x, (Int128)y)) != expected) throw new Exception("SubSigned128");
        if (Branch(a, b, out diff) != (bit != 0 ? 13UL : 29UL) || diff != unchecked(a - b)) throw new Exception("Branch");
        if (Select(a, b, c, d, out diff) != (bit != 0 ? c : d) || diff != unchecked(a - b)) throw new Exception("Select");
        ulong result = ArbitraryBorrow(a, b, c, out ulong next);
        BigInteger wide = (BigInteger)a - b - c;
        ulong expectedBorrow = (ulong)(-(wide >> 64));
        if (result != (ulong)(wide & ulong.MaxValue) || next != expectedBorrow) throw new Exception("ArbitraryBorrow");
        if (AcrossCall(a, b, c) != unchecked(~c + bit)) throw new Exception("AcrossCall");
    }
}
