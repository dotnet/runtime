// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public static class CarryTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Count64(ulong a, ulong b, ulong count, out ulong sum)
    {
        // X64: add
        // X64-NEXT: adc
        // ARM64: adds
        // ARM64-NEXT: adc
        ulong s = a + b;
        count += s < a ? 1UL : 0UL;
        sum = s;
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Count32(uint a, uint b, uint count, out uint sum)
    {
        // X64: add
        // X64-NEXT: adc
        // ARM64: adds
        // ARM64-NEXT: adc
        uint s = a + b;
        count += b > s ? 1U : 0U;
        sum = s;
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Increment(ulong a, ulong count, out ulong sum)
    {
        // X64-NOT: {{^ +}}inc
        // X64: add
        // X64-NEXT: adc
        // X64-NOT: {{^ +}}inc
        ulong s = a + 1;
        count += s < a ? 1UL : 0UL;
        sum = s;
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddWithCarry(ulong a, ulong b, ulong carryIn, out ulong carryOut)
    {
        ulong t = a + b;
        ulong s = t + carryIn;
        carryOut = (t < a ? 1UL : 0UL) + (s < t ? 1UL : 0UL);
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 Add128(ulong aLo, ulong aHi, ulong bLo, ulong bHi)
    {
        // X64: add
        // X64: adc
        // ARM64: adds
        // ARM64: adc
        ulong lo = aLo + bLo;
        ulong hi = aHi + bHi + (lo < aLo ? 1UL : 0UL);
        return ((UInt128)hi << 64) | lo;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 DirectAdd128(UInt128 a, UInt128 b)
    {
        // X64: add
        // X64-NEXT: adc
        // ARM64: adds
        // ARM64-NEXT: adc
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddMemoryAfterStore(ulong a, ulong b, ref ulong source, ref ulong destination, ulong replacement)
    {
        // X64: add
        // X64-NEXT: setb
        // ARM64: adds
        // ARM64-NEXT: cset
        ulong lo = a + b;
        ulong carry = lo < a ? 1UL : 0UL;
        destination = replacement;
        return source + carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 MultiplyAdd(ulong a, ulong b, ulong existing, ulong carry)
    {
        // X64: add
        // X64-NEXT: adc
        // X64-NEXT: add
        // X64-NEXT: adc
        // ARM64: adds
        // ARM64-NEXT: adc
        // ARM64-NEXT: adds
        // ARM64-NEXT: adc
        return (UInt128)a * b + existing + carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CountColumn(ulong a, ulong b, ulong c, ulong d, out ulong sum)
    {
        ulong s0 = a + b;
        ulong count = s0 < a ? 1UL : 0UL;
        ulong s1 = s0 + c;
        count += s1 < s0 ? 1UL : 0UL;
        ulong s2 = s1 + d;
        count += s2 < s1 ? 1UL : 0UL;
        sum = s2;
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Branch(ulong a, ulong b, out ulong sum)
    {
        // X64: add
        // X64-NOT: cmp
        // X64: j{{b|ae}}
        // ARM64: adds
        // ARM64-NOT: cmp
        // ARM64: b{{lo|hs}}
        ulong s = a + b;
        if (s < a)
        {
            sum = s;
            return true;
        }
        sum = s + 1;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Signed(long a, long b)
    {
        long sum = a + b;
        return sum < a ? 1UL : 0UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Reload(ulong a, ulong b, ref ulong sum, ref ulong alias)
    {
        sum = a + b;
        alias = 0;
        return sum < a ? 1UL : 0UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Checked(ulong a, ulong b)
    {
        ulong sum = checked(a + b);
        return sum < a ? 1UL : 0UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 TwoConsumers(ulong a, ulong b, ulong count)
    {
        // X64: add
        // X64-NEXT: setb
        // ARM64: adds
        // ARM64-NEXT: cset
        ulong sum = a + b;
        ulong carry = sum < a ? 1UL : 0UL;
        return ((UInt128)unchecked(count + carry) << 64) | carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AcrossCall(ulong a, ulong b, ulong count)
    {
        ulong sum = a + b;
        Mutate(ref count);
        return count + (sum < a ? 1UL : 0UL);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mutate(ref ulong value) => value = ~value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint AddLimbWithCarry(nuint a, nuint b, nuint carryIn, out nuint carryOut)
    {
        if (nint.Size == 8)
        {
            nuint sum1 = a + b;
            nuint c1 = (sum1 < a) ? 1 : (nuint)0;
            nuint sum2 = sum1 + carryIn;
            nuint c2 = (sum2 < sum1) ? 1 : (nuint)0;
            carryOut = c1 + c2;
            return sum2;
        }
        else
        {
            ulong sum = (ulong)a + b + carryIn;
            carryOut = (uint)(sum >> 32);
            return (uint)sum;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoop(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
    {
        // X64: adc {{.*}}, qword ptr
        // X64-NEXT: mov
        // X64-NEXT: lea
        // X64-NEXT: dec
        // X64-NEXT: jne
        // ARM64: ldr
        // ARM64-NEXT: ldr
        // ARM64-NEXT: adcs
        // ARM64-NEXT: str
        // ARM64-NEXT: add
        // ARM64-NEXT: sub
        // ARM64-NEXT: cbnz
        // Establish cross-span length relationships so the JIT can
        // elide bounds checks for left[i] and bits[i] in the loop.
        _ = left[right.Length - 1];
        _ = bits[right.Length];

        nuint carry = 0;

        for (int i = 0; i < right.Length; i++)
        {
            bits[i] = AddLimbWithCarry(left[i], right[i], carry, out carry);
        }

        bits[right.Length] = carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoopWithCarry(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits, nuint initialCarry)
    {
        // Establish cross-span length relationships so the JIT can
        // elide bounds checks for left[i] and bits[i] in the loop.
        _ = left[right.Length - 1];
        _ = bits[right.Length];

        nuint carry = initialCarry;

        for (int i = 0; i < right.Length; i++)
        {
            bits[i] = AddLimbWithCarry(left[i], right[i], carry, out carry);
        }

        bits[right.Length] = carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoopDiscardCarry(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
    {
        // Establish cross-span length relationships so the JIT can
        // elide bounds checks for left[i] and bits[i] in the loop.
        _ = left[right.Length - 1];
        _ = bits[right.Length];

        nuint carry = 0;

        for (int i = 0; i < right.Length; i++)
        {
            bits[i] = AddLimbWithCarry(left[i], right[i], carry, out carry);
        }

    }

    private static int s_loopSignal;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoopVolatile(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
    {
        // Establish cross-span length relationships so the JIT can
        // elide bounds checks for left[i] and bits[i] in the loop.
        _ = left[right.Length - 1];
        _ = bits[right.Length];

        nuint carry = 0;

        for (int i = 0; i < right.Length; i++)
        {
            bits[i] = AddLimbWithCarry(left[i], right[i], carry, out carry);
            System.Threading.Volatile.Write(ref s_loopSignal, i);
        }

        bits[right.Length] = carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoopOne(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
    {
        // Establish cross-span length relationships so the JIT can
        // elide bounds checks for left[i] and bits[i] in the loop.
        _ = left[right.Length - 1];
        _ = bits[right.Length];

        nuint carry = 1;

        for (int i = 0; i < right.Length; i++)
        {
            bits[i] = AddLimbWithCarry(left[i], right[i], carry, out carry);
        }

        bits[right.Length] = carry;
    }
    private static BigInteger LimbsToBigInteger(ReadOnlySpan<nuint> limbs)
    {
        BigInteger value = 0;
        for (int i = limbs.Length - 1; i >= 0; i--)
            value = (value << (IntPtr.Size * 8)) | (ulong)limbs[i];
        return value;
    }

    private static void CheckLimbLoops()
    {
        Random random = new Random(125799);
        byte[] bytes = new byte[8];
        for (int test = 0; test < 1000; test++)
        {
            int length = 1 + test % 129;
            nuint[] left = new nuint[length + 1];
            nuint[] right = new nuint[length];
            nuint[] result = new nuint[length + 1];
            for (int i = 0; i < length; i++)
            {
                random.NextBytes(bytes);
                left[i] = test % 4 == 0 ? nuint.MaxValue : (nuint)BitConverter.ToUInt64(bytes);
                random.NextBytes(bytes);
                right[i] = test % 4 == 0 ? nuint.MaxValue : (nuint)BitConverter.ToUInt64(bytes);
            }
            BigInteger expected = LimbsToBigInteger(left.AsSpan(0, length)) + LimbsToBigInteger(right);
            AddLimbLoop(left.AsSpan(0, length), right, result);
            if (LimbsToBigInteger(result) != expected) throw new Exception("AddLimbLoop");
            AddLimbLoopOne(left.AsSpan(0, length), right, result);
            if (LimbsToBigInteger(result) != expected + 1) throw new Exception("AddLimbLoopOne");
            AddLimbLoopVolatile(left.AsSpan(0, length), right, result);
            if (LimbsToBigInteger(result) != expected || s_loopSignal != length - 1) throw new Exception("AddLimbLoopVolatile");
            AddLimbLoopDiscardCarry(left.AsSpan(0, length), right, result);
            if (LimbsToBigInteger(result.AsSpan(0, length)) != (expected & ((BigInteger.One << (length * IntPtr.Size * 8)) - 1)))
                throw new Exception("AddLimbLoopDiscardCarry");
            foreach (nuint carry in new nuint[] { 0, 1, 2, nuint.MaxValue })
            {
                AddLimbLoopWithCarry(left.AsSpan(0, length), right, result, carry);
                if (LimbsToBigInteger(result) != expected + (ulong)carry) throw new Exception("AddLimbLoopWithCarry");
            }
            AddLimbLoop(left.AsSpan(0, length), right, left);
            if (LimbsToBigInteger(left) != expected) throw new Exception("AddLimbLoop alias");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SelectCarry(ulong a, ulong b, ulong x, ulong y, out ulong sum)
    {
        // X64-NOT: {{^ +}}set
        // X64: cmov
        // ARM64-NOT: {{^ +}}cset
        // ARM64: cs{{el|inc}}
        ulong s = unchecked(a + b);
        sum = s;
        return s >= a ? x + 1 : y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoop32(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
    {
        // X64: adc {{.*}}, dword ptr
        // X64-NEXT: mov
        // X64-NEXT: lea
        // X64-NEXT: dec
        // X64-NEXT: jne
        // ARM64: adcs w
        // ARM64-NEXT: str w
        // ARM64-NEXT: add
        // ARM64-NEXT: sub
        // ARM64-NEXT: cbnz
        _ = left[right.Length - 1];
        _ = bits[right.Length];
        uint carry = 0;
        for (int i = 0; i < right.Length; i++)
        {
            uint a = left[i];
            uint sum1 = unchecked(a + right[i]);
            uint c1 = sum1 < a ? 1U : 0U;
            uint sum2 = unchecked(sum1 + carry);
            uint c2 = sum2 < sum1 ? 1U : 0U;
            carry = c1 + c2;
            bits[i] = sum2;
        }
        bits[right.Length] = carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddLimbLoop32WithIndex(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits, Span<uint> indices)
    {
        // X64: adc {{.*}}, dword ptr
        // X64: lea e
        // ARM64: adcs w
        // ARM64: add w
        _ = left[right.Length - 1];
        _ = bits[right.Length];
        _ = indices[right.Length - 1];
        uint index = 0;
        uint carry = 0;
        for (int i = 0; i < right.Length; i++)
        {
            uint a = left[i];
            uint sum1 = unchecked(a + right[i]);
            uint c1 = sum1 < a ? 1U : 0U;
            uint sum2 = unchecked(sum1 + carry);
            uint c2 = sum2 < sum1 ? 1U : 0U;
            carry = c1 + c2;
            bits[i] = sum2;
            index = unchecked(index + 7);
            indices[i] = index;
        }
        bits[right.Length] = carry;
    }

    private static BigInteger Limbs32ToBigInteger(ReadOnlySpan<uint> limbs)
    {
        BigInteger value = 0;
        for (int i = limbs.Length - 1; i >= 0; i--)
        {
            value = (value << 32) | limbs[i];
        }
        return value;
    }

    private static void CheckLimbLoops32()
    {
        Random random = new Random(125799);
        for (int trial = 0; trial < 1000; trial++)
        {
            int length = 1 + trial % 129;
            uint[] left = new uint[length + 1];
            uint[] right = new uint[length + 1];
            uint[] result = new uint[length + 1];
            uint[] indices = new uint[length];
            for (int i = 0; i < length; i++)
            {
                left[i] = trial % 4 == 0 ? uint.MaxValue : (uint)random.NextInt64(1L << 32);
                right[i] = trial % 4 == 1 ? uint.MaxValue : (uint)random.NextInt64(1L << 32);
            }
            BigInteger expected = Limbs32ToBigInteger(left) + Limbs32ToBigInteger(right);
            AddLimbLoop32WithIndex(left.AsSpan(0, length), right.AsSpan(0, length), result, indices);
            if (Limbs32ToBigInteger(result) != expected) throw new Exception("AddLimbLoop32WithIndex");
            for (int i = 0; i < length; i++)
            {
                if (indices[i] != (uint)(7 * (i + 1))) throw new Exception("AddLimbLoop32WithIndex index");
            }
            AddLimbLoop32(left.AsSpan(0, length), right.AsSpan(0, length), result);
            if (Limbs32ToBigInteger(result) != expected) throw new Exception("AddLimbLoop32");
            AddLimbLoop32(left.AsSpan(0, length), right.AsSpan(0, length), left);
            if (Limbs32ToBigInteger(left) != expected) throw new Exception("AddLimbLoop32 alias left");
            left = (uint[])result.Clone();
            expected = Limbs32ToBigInteger(left.AsSpan(0, length)) + Limbs32ToBigInteger(right);
            AddLimbLoop32(left.AsSpan(0, length), right.AsSpan(0, length), right);
            if (Limbs32ToBigInteger(right) != expected) throw new Exception("AddLimbLoop32 alias right");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NoCarry(ulong a, ulong b)
    {
        // X64: add
        // X64-NEXT: setae
        // ARM64: adds
        // ARM64-NEXT: cset {{.*}}, lo
        return a <= unchecked(a + b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ZeroOr(int x, int y)
    {
        // X64: {{^ +}}or
        // X64-NOT: {{^ +}}set
        // X64-NOT: test
        // X64: cmov
        return (x | y) == 0 ? 1 : 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ZeroAdd(int x, int y)
    {
        // X64: add
        // X64-NOT: {{^ +}}set
        // X64-NOT: test
        // X64: cmov
        return unchecked(x + y) == 0 ? 1 : 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ZeroShift(int x)
    {
        // X64: shl
        // X64-NOT: {{^ +}}set
        // X64-NOT: test
        // X64: cmov
        return (x << 3) == 0 ? 1 : 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UInt128 WideningProduct(ulong a, ulong b)
    {
        if (System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported)
        {
            ulong high = System.Runtime.Intrinsics.Arm.ArmBase.Arm64.MultiplyHigh(a, b);
            return ((UInt128)high << 64) | unchecked(a * b);
        }
        return Math.BigMul(a, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint UnrolledMultiplyAdd(Span<nuint> result, ReadOnlySpan<nuint> left, nuint multiplier)
    {
        // X64-NOT: {{^ +}}setb
        // X64: mul{{x| }}
        // X64-NOT: {{^ +}}setb
        // X64: adc
        // X64-NOT: {{^ +}}setb
        // X64: ret
        // ARM64-NOT: {{^ +}}cset
        // ARM64: umulh
        // ARM64-NOT: {{^ +}}cset
        // ARM64: adc
        // ARM64-NOT: {{^ +}}cset
        // ARM64: ret

        int length = left.Length;
        result = result.Slice(0, length);
        int i = 0;
        nuint carry = 0;

        if (nint.Size == 8)
        {
            // Unroll by 4: mulx has 3-5 cycle latency but 1 cycle throughput,
            // so issuing 4 multiplies allows the CPU to pipeline them while
            // carry chains complete sequentially behind.
            for (; i < length - 3; i += 4)
            {
                UInt128 p0 = WideningProduct((ulong)left[i], (ulong)multiplier) + (ulong)result[i] + (ulong)carry;
                result[i] = (nuint)(ulong)p0;

                UInt128 p1 = WideningProduct((ulong)left[i + 1], (ulong)multiplier) + (ulong)result[i + 1] + (ulong)(p0 >> 64);
                result[i + 1] = (nuint)(ulong)p1;

                UInt128 p2 = WideningProduct((ulong)left[i + 2], (ulong)multiplier) + (ulong)result[i + 2] + (ulong)(p1 >> 64);
                result[i + 2] = (nuint)(ulong)p2;

                UInt128 p3 = WideningProduct((ulong)left[i + 3], (ulong)multiplier) + (ulong)result[i + 3] + (ulong)(p2 >> 64);
                result[i + 3] = (nuint)(ulong)p3;

                carry = (nuint)(ulong)(p3 >> 64);
            }

            for (; i < length; i++)
            {
                UInt128 product = WideningProduct((ulong)left[i], (ulong)multiplier) + (ulong)result[i] + (ulong)carry;
                result[i] = (nuint)(ulong)product;
                carry = (nuint)(ulong)(product >> 64);
            }
        }
        else
        {
            for (; i < length; i++)
            {
                ulong product = (ulong)left[i] * multiplier
                                + result[i] + carry;
                result[i] = (uint)product;
                carry = (uint)(product >> 32);
            }
        }

        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SignedWideningProduct(long a, long b, out long low) => Math.BigMul(a, b, out low);

    private static void CheckUnrolledMultiplyAdd()
    {
        Random random = new Random(80674);
        for (int trial = 0; trial < 1000; trial++)
        {
            int length = 1 + trial % 65;
            nuint[] left = new nuint[length];
            nuint[] result = new nuint[length];
            for (int i = 0; i < length; i++)
            {
                left[i] = trial % 4 == 0 ? nuint.MaxValue : (nuint)random.NextInt64();
                result[i] = trial % 4 == 0 ? nuint.MaxValue : (nuint)random.NextInt64();
            }
            nuint multiplier = trial % 4 == 0 ? nuint.MaxValue : (nuint)random.NextInt64();
            BigInteger expected = LimbsToBigInteger(left) * (ulong)multiplier + LimbsToBigInteger(result);
            nuint carry = UnrolledMultiplyAdd(result, left, multiplier);
            if (LimbsToBigInteger(result) + ((BigInteger)(ulong)carry << (length * IntPtr.Size * 8)) != expected)
                throw new Exception("UnrolledMultiplyAdd");
            expected = LimbsToBigInteger(left) * ((BigInteger)(ulong)multiplier + 1);
            carry = UnrolledMultiplyAdd(left, left, multiplier);
            if (LimbsToBigInteger(left) + ((BigInteger)(ulong)carry << (length * IntPtr.Size * 8)) != expected)
                throw new Exception("UnrolledMultiplyAdd alias");
            long a = unchecked((long)(ulong)multiplier);
            long b = trial % 2 == 0 ? long.MinValue : random.NextInt64();
            long high = SignedWideningProduct(a, b, out long low);
            if (((BigInteger)high << 64) + (ulong)low != (BigInteger)a * b)
                throw new Exception("SignedWideningProduct");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ScalarMultiplyAdd(Span<ulong> result, ReadOnlySpan<ulong> left, ulong multiplier, ulong carry)
    {
        // X64: mul{{x| }}
        // X64: ad{{cx|d}}
        // X64: ad{{ox|c}}
        // ARM64: umulh
        // ARM64: adc
        for (int i = 0; i < left.Length; i++)
        {
            UInt128 product = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)product;
            carry = (ulong)(product >> 64);
        }
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong VolatileMultiplyAdd(Span<ulong> result, ReadOnlySpan<ulong> left, ulong multiplier, ulong carry)
    {
        // X64-NOT: adcx
        // X64-NOT: adox
        // X64: ret
        // Volatile loads must not become part of an ADX loop.
        for (int i = 0; i < left.Length; i++)
        {
            UInt128 product = WideningProduct(left[i], multiplier) + System.Threading.Volatile.Read(ref result[i]) + carry;
            result[i] = (ulong)product;
            carry = (ulong)(product >> 64);
        }
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong GuardedUnrolledMultiplyAdd(Span<ulong> result, ReadOnlySpan<ulong> left, ulong multiplier, ulong carry)
    {
        // X64: mul{{x| }}
        // X64: ad{{cx|d}}
        // X64: ad{{ox|c}}
        // ARM64: umulh
        // ARM64: adcs
        // ARM64-NEXT: adc
        // ARM64-NEXT: adds
        // ARM64: adcs
        // ARM64-NEXT: adc
        // ARM64-NEXT: adds
        // ARM64: adcs
        // ARM64-NEXT: adc
        // ARM64-NEXT: adds
        // ARM64: adcs
        // ARM64-NEXT: adc
        // ARM64-NEXT: adds
        // ARM64: cbnz
        // ARM64: adc
        int length = left.Length;
        result = result.Slice(0, length);
        int i = 0;
        for (; i < length - 3; i += 4)
        {
            UInt128 p0 = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)p0;
            UInt128 p1 = WideningProduct(left[i + 1], multiplier) + result[i + 1] + (ulong)(p0 >> 64);
            result[i + 1] = (ulong)p1;
            UInt128 p2 = WideningProduct(left[i + 2], multiplier) + result[i + 2] + (ulong)(p1 >> 64);
            result[i + 2] = (ulong)p2;
            UInt128 p3 = WideningProduct(left[i + 3], multiplier) + result[i + 3] + (ulong)(p2 >> 64);
            result[i + 3] = (ulong)p3;
            carry = (ulong)(p3 >> 64);
        }
        for (; i < left.Length; i++)
        {
            UInt128 product = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)product;
            carry = (ulong)(product >> 64);
        }
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ObservableCarryUnrolledMultiplyAdd(Span<ulong> result, ReadOnlySpan<ulong> left, ulong multiplier, ulong carry, out ulong observed)
    {
        // The intermediate high word is observable and must include both carries.
        // ARM64-NOT: adcs
        // ARM64: ret
        observed = 0;
        int length = left.Length;
        result = result.Slice(0, length);
        int i = 0;
        for (; i < length - 3; i += 4)
        {
            UInt128 p0 = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)p0;
            observed = (ulong)(p0 >> 64);
            UInt128 p1 = WideningProduct(left[i + 1], multiplier) + result[i + 1] + (ulong)(p0 >> 64);
            result[i + 1] = (ulong)p1;
            UInt128 p2 = WideningProduct(left[i + 2], multiplier) + result[i + 2] + (ulong)(p1 >> 64);
            result[i + 2] = (ulong)p2;
            UInt128 p3 = WideningProduct(left[i + 3], multiplier) + result[i + 3] + (ulong)(p2 >> 64);
            result[i + 3] = (ulong)p3;
            carry = (ulong)(p3 >> 64);
        }
        for (; i < left.Length; i++)
        {
            UInt128 product = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)product;
            carry = (ulong)(product >> 64);
        }
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong InterveningStoreUnrolledMultiplyAdd(Span<ulong> result, ReadOnlySpan<ulong> left, ulong multiplier, ulong carry)
    {
        // X64: mul{{x| }}
        // X64: ad{{cx|d}}
        // X64: ad{{ox|c}}
        // ARM64: umulh
        // ARM64: adc
        int length = left.Length;
        result = result.Slice(0, length);
        int i = 0;
        for (; i < length - 3; i += 4)
        {
            // The saved load must remain before this potentially aliasing store.
            ulong saved = result[i];
            result[i + 1] = multiplier;
            UInt128 p0 = WideningProduct(left[i], multiplier) + saved + carry;
            result[i] = (ulong)p0;
            UInt128 p1 = WideningProduct(left[i + 1], multiplier) + result[i + 1] + (ulong)(p0 >> 64);
            result[i + 1] = (ulong)p1;
            UInt128 p2 = WideningProduct(left[i + 2], multiplier) + result[i + 2] + (ulong)(p1 >> 64);
            result[i + 2] = (ulong)p2;
            UInt128 p3 = WideningProduct(left[i + 3], multiplier) + result[i + 3] + (ulong)(p2 >> 64);
            result[i + 3] = (ulong)p3;
            carry = (ulong)(p3 >> 64);
        }
        for (; i < left.Length; i++)
        {
            UInt128 product = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)product;
            carry = (ulong)(product >> 64);
        }
        return carry;
    }

    private static void CheckObservableCarry()
    {
        Random random = new Random(125802);
        for (int trial = 0; trial < 256; trial++)
        {
            int length = trial % 66;
            ulong[] left = new ulong[length];
            ulong[] result = new ulong[length];
            random.NextBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(left.AsSpan()));
            random.NextBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(result.AsSpan()));
            ulong multiplier = trial % 3 == 0 ? ulong.MaxValue : (ulong)random.NextInt64();
            ulong carry = trial % 2 == 0 ? ulong.MaxValue : 0;
            ulong[] expected = (ulong[])result.Clone();
            ulong expectedCarry = carry;
            ulong expectedObserved = 0;
            for (int i = 0; i < length; i++)
            {
                BigInteger product = (BigInteger)left[i] * multiplier + expected[i] + expectedCarry;
                expected[i] = (ulong)(product & ulong.MaxValue);
                expectedCarry = (ulong)(product >> 64);
                if ((i & 3) == 0 && i < length - 3)
                {
                    expectedObserved = expectedCarry;
                }
            }
            ulong actualCarry = ObservableCarryUnrolledMultiplyAdd(result, left, multiplier, carry, out ulong observed);
            if (actualCarry != expectedCarry || observed != expectedObserved || !result.AsSpan().SequenceEqual(expected))
            {
                throw new Exception("ObservableCarryUnrolledMultiplyAdd");
            }
        }
    }
    private static void CheckInterveningStore()
    {
        Random random = new Random(125801);
        for (int trial = 0; trial < 256; trial++)
        {
            int length = trial % 66;
            int source = 2;
            int destination = (trial % 4) switch { 0 => length + 4, 1 => 2, 2 => 3, _ => 1 };
            ulong[] data = new ulong[length * 2 + 8];
            random.NextBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(data.AsSpan()));
            ulong multiplier = trial % 3 == 0 ? ulong.MaxValue : data[0];
            ulong carry = trial % 2 == 0 ? ulong.MaxValue : 0;
            ulong[] expected = (ulong[])data.Clone();
            ulong expectedCarry = carry;
            for (int i = 0; i < length; i++)
            {
                ulong saved = expected[destination + i];
                if (((i & 3) == 0) && (i < length - 3))
                {
                    expected[destination + i + 1] = multiplier;
                }
                BigInteger product = (BigInteger)expected[source + i] * multiplier + saved + expectedCarry;
                expected[destination + i] = (ulong)(product & ulong.MaxValue);
                expectedCarry = (ulong)(product >> 64);
            }
            ulong actualCarry = InterveningStoreUnrolledMultiplyAdd(data.AsSpan(destination, length),
                data.AsSpan(source, length), multiplier, carry);
            if (actualCarry != expectedCarry || !data.AsSpan().SequenceEqual(expected))
            {
                throw new Exception("InterveningStoreUnrolledMultiplyAdd");
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ChangingMultiplierUnrolledMultiplyAdd(Span<ulong> result, ReadOnlySpan<ulong> left, ulong multiplier, ulong carry)
    {
        // The multiplier changes on the backedge and must not be hoisted.
        // X64: mul{{x| }}
        // X64: ad{{cx|d}}
        // X64: ad{{ox|c}}
        // ARM64: umulh
        // ARM64: adc
        int length = left.Length;
        result = result.Slice(0, length);
        int i = 0;
        for (; i < length - 3; i += 4)
        {
            UInt128 p0 = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)p0;
            UInt128 p1 = WideningProduct(left[i + 1], multiplier) + result[i + 1] + (ulong)(p0 >> 64);
            result[i + 1] = (ulong)p1;
            UInt128 p2 = WideningProduct(left[i + 2], multiplier) + result[i + 2] + (ulong)(p1 >> 64);
            result[i + 2] = (ulong)p2;
            UInt128 p3 = WideningProduct(left[i + 3], multiplier) + result[i + 3] + (ulong)(p2 >> 64);
            result[i + 3] = (ulong)p3;
            carry = (ulong)(p3 >> 64);
            multiplier = unchecked(multiplier + 1);
        }
        for (; i < left.Length; i++)
        {
            UInt128 product = WideningProduct(left[i], multiplier) + result[i] + carry;
            result[i] = (ulong)product;
            carry = (ulong)(product >> 64);
        }
        return carry;
    }

    private static void CheckChangingMultiplier()
    {
        Random random = new Random(125800);
        for (int trial = 0; trial < 256; trial++)
        {
            int length = trial % 66;
            ulong[] left = new ulong[length];
            ulong[] result = new ulong[length];
            random.NextBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(left.AsSpan()));
            random.NextBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(result.AsSpan()));
            ulong multiplier = trial % 3 == 0 ? ulong.MaxValue : (ulong)random.NextInt64();
            ulong carry = trial % 2 == 0 ? ulong.MaxValue : 0;
            ulong[] expected = (ulong[])result.Clone();
            ulong expectedMultiplier = multiplier;
            ulong expectedCarry = carry;
            for (int i = 0; i < length; i++)
            {
                BigInteger product = (BigInteger)left[i] * expectedMultiplier + expected[i] + expectedCarry;
                expected[i] = (ulong)(product & ulong.MaxValue);
                expectedCarry = (ulong)(product >> 64);
                if ((i & 3) == 3)
                {
                    expectedMultiplier = unchecked(expectedMultiplier + 1);
                }
            }
            ulong actualCarry = ChangingMultiplierUnrolledMultiplyAdd(result, left, multiplier, carry);
            if (actualCarry != expectedCarry || !result.AsSpan().SequenceEqual(expected))
            {
                throw new Exception("ChangingMultiplierUnrolledMultiplyAdd");
            }
        }
    }

    private static void CheckScalarMultiplyAdd()
    {
        Random random = new Random(125799);
        BigInteger mask = (BigInteger.One << 64) - 1;
        for (int trial = 0; trial < 12000; trial++)
        {
            int length = trial % 129;
            int source = 2;
            int destination = (trial % 4) switch { 0 => length + 4, 1 => 2, 2 => 3, _ => 1 };
            ulong[] input = new ulong[length * 2 + 8];
            random.NextBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
            if (trial % 13 == 0)
                Array.Fill(input, ulong.MaxValue);
            if (trial % 17 == 0)
                Array.Fill(input, 0UL);
            ulong multiplier = trial % 7 == 0 ? ulong.MaxValue : input[0];
            ulong initialCarry = trial % 11 == 0 ? ulong.MaxValue : input[^1];
            ulong[] expected = (ulong[])input.Clone();
            BigInteger expectedCarry = initialCarry;
            // The sequential oracle also covers overlap in either direction.
            for (int i = 0; i < length; i++)
            {
                BigInteger product = (BigInteger)expected[source + i] * multiplier + expected[destination + i] + expectedCarry;
                expected[destination + i] = (ulong)(product & mask);
                expectedCarry = product >> 64;
            }
            for (int variant = 0; variant < 3; variant++)
            {
                ulong[] actual = (ulong[])input.Clone();
                ulong carry = variant switch
                {
                    0 => ScalarMultiplyAdd(actual.AsSpan(destination, length), actual.AsSpan(source, length), multiplier, initialCarry),
                    1 => VolatileMultiplyAdd(actual.AsSpan(destination, length), actual.AsSpan(source, length), multiplier, initialCarry),
                    _ => GuardedUnrolledMultiplyAdd(actual.AsSpan(destination, length), actual.AsSpan(source, length), multiplier, initialCarry)
                };
                if (carry != (ulong)expectedCarry || !actual.AsSpan().SequenceEqual(expected))
                    throw new Exception($"ScalarMultiplyAdd trial {trial}, variant {variant}");
            }
        }

        // A short destination must still throw after storing only the valid prefix.
        for (int length = 0; length < 8; length++)
        {
            ulong[] left = new ulong[length + 1];
            ulong[] result = new ulong[length + 2];
            Array.Fill(left, ulong.MaxValue);
            Array.Fill(result, ulong.MaxValue);
            ulong[] expected = (ulong[])result.Clone();
            BigInteger carry = ulong.MaxValue;
            for (int i = 0; i < length; i++)
            {
                BigInteger product = (BigInteger)left[i] * ulong.MaxValue + expected[i] + carry;
                expected[i] = (ulong)(product & mask);
                carry = product >> 64;
            }
            try
            {
                ScalarMultiplyAdd(result.AsSpan(0, length), left, ulong.MaxValue, ulong.MaxValue);
                throw new Exception("Missing bounds exception");
            }
            catch (IndexOutOfRangeException)
            {
                if (!result.AsSpan().SequenceEqual(expected))
                    throw new Exception("MultiplyAdd exception store order");
            }
        }

        // The upfront slice must reject a short destination before any stores.
        for (int length = 0; length < 12; length++)
        {
            ulong[] left = new ulong[length + 4];
            ulong[] result = new ulong[length + 4];
            Array.Fill(left, ulong.MaxValue);
            Array.Fill(result, ulong.MaxValue);
            ulong[] expected = (ulong[])result.Clone();
            try
            {
                GuardedUnrolledMultiplyAdd(result.AsSpan(0, length), left, ulong.MaxValue, ulong.MaxValue);
                throw new Exception("Missing guarded bounds exception");
            }
            catch (ArgumentOutOfRangeException)
            {
                if (!result.AsSpan().SequenceEqual(expected))
                    throw new Exception("Guarded multiply-add exception store order");
            }
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        CheckLimbLoops();
        CheckUnrolledMultiplyAdd();
        CheckChangingMultiplier();
        CheckInterveningStore();
        CheckObservableCarry();
        CheckScalarMultiplyAdd();
        CheckLimbLoops32();
        ulong[] values = { 0, 1, 2, uint.MaxValue, 1UL << 32, (1UL << 63) - 1, 1UL << 63, ulong.MaxValue - 1, ulong.MaxValue };
        foreach (ulong a in values)
        {
            foreach (ulong b in values)
            {
                foreach (ulong c in values)
                {
                    Check(a, b, c, ~c);
                }
            }
        }

        Random random = new Random(80674);
        byte[] bytes = new byte[32];
        for (int i = 0; i < 10000; i++)
        {
            random.NextBytes(bytes);
            Check(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8),
                  BitConverter.ToUInt64(bytes, 16), BitConverter.ToUInt64(bytes, 24));
        }

        ulong shared = 42;
        if (Reload(1, 2, ref shared, ref shared) != 1 || shared != 0)
        {
            throw new Exception("Aliased reload");
        }
        try
        {
            Checked(ulong.MaxValue, 1);
            throw new Exception("Missing overflow exception");
        }
        catch (OverflowException)
        {
        }
    }

    private static void Check(ulong a, ulong b, ulong c, ulong d)
    {
        int x = (int)a;
        int y = (int)b;
        if (ZeroOr(x, y) != ((x | y) == 0 ? 1 : 2) ||
            ZeroAdd(x, y) != (unchecked(x + y) == 0 ? 1 : 2) ||
            ZeroShift(x) != ((x << 3) == 0 ? 1 : 2))
        {
            throw new Exception("Zero flags");
        }
        UInt128 wide = (UInt128)a + b;
        if (Count64(a, b, c, out ulong sum) != unchecked(c + (ulong)(wide >> 64)) || sum != (ulong)wide)
            throw new Exception("Count64");

        if (NoCarry(a, b) != (wide <= ulong.MaxValue))
        {
            throw new Exception("NoCarry");
        }
        ulong selected = SelectCarry(a, b, c, d, out ulong selectedSum);
        if (selectedSum != (ulong)wide || selected != (wide <= ulong.MaxValue ? unchecked(c + 1) : d))
        {
            throw new Exception("SelectCarry");
        }
        ulong bit = (ulong)(wide >> 64);
        if (TwoConsumers(a, b, c) != (((UInt128)unchecked(c + bit) << 64) | bit))
            throw new Exception("TwoConsumers");

        if (AcrossCall(a, b, c) != unchecked(~c + bit))
            throw new Exception("AcrossCall");

        ulong wide32 = (ulong)(uint)a + (uint)b;
        if (Count32((uint)a, (uint)b, (uint)c, out uint sum32) != unchecked((uint)c + (uint)(wide32 >> 32)) || sum32 != (uint)wide32)
            throw new Exception("Count32");

        wide = (UInt128)a + 1;
        if (Increment(a, c, out sum) != unchecked(c + (ulong)(wide >> 64)) || sum != (ulong)wide)
            throw new Exception("Increment");

        wide = (UInt128)a + b + c;
        if (AddWithCarry(a, b, c, out ulong carry) != (ulong)wide || carry != (ulong)(wide >> 64))
            throw new Exception("AddWithCarry");

        UInt128 left = ((UInt128)c << 64) | a;
        UInt128 right = ((UInt128)d << 64) | b;
        if (Add128(a, c, b, d) != unchecked(left + right))
            throw new Exception("Add128");

        BigInteger sum128 = ((BigInteger)left + (BigInteger)right) & ((BigInteger.One << 128) - 1);
        if ((BigInteger)DirectAdd128(left, right) != sum128)
            throw new Exception("DirectAdd128");

        ulong memory = c;
        if (AddMemoryAfterStore(a, b, ref memory, ref memory, d) != unchecked(d + bit) || memory != d)
            throw new Exception("AddMemoryAfterStore alias");
        ulong destination = 0;
        if (AddMemoryAfterStore(a, b, ref memory, ref destination, c) != unchecked(d + bit) || destination != c)
            throw new Exception("AddMemoryAfterStore distinct");

        BigInteger product = (BigInteger)a * b + c + d;
        if ((BigInteger)MultiplyAdd(a, b, c, d) != product)
            throw new Exception("MultiplyAdd");

        wide = (UInt128)a + b + c + d;
        if (CountColumn(a, b, c, d, out sum) != (ulong)(wide >> 64) || sum != (ulong)wide)
            throw new Exception("CountColumn");

        wide = (UInt128)a + b;
        bool overflow = wide > ulong.MaxValue;
        if (Branch(a, b, out sum) != overflow || sum != unchecked((ulong)wide + (overflow ? 0UL : 1UL)))
            throw new Exception("Branch");

        long signedSum = unchecked((long)a + (long)b);
        if (Signed((long)a, (long)b) != (signedSum < (long)a ? 1UL : 0UL))
            throw new Exception("Signed");
    }
}
