using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class WideArithmeticTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 Increment(UInt128 value) => value + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 Decrement(UInt128 value)
    {
        // X64: sub
        // X64-NEXT: sbb
        // ARM64: subs
        // ARM64-NEXT: sbc
        return value - 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 Negate(Int128 value)
    {
        // X64: neg
        // X64-NEXT: sbb
        // ARM64: negs
        // ARM64-NEXT: sbc {{.*}}, xzr,
        return -value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SubtractSeven(ulong value, ulong high, out ulong low)
    {
        // X64: sub
        // X64-NOT: cmp
        // X64: sbb
        // ARM64: subs
        // ARM64-NEXT: sbc
        // ARM64-NEXT: str {{x[0-9]+}}, [{{x[0-9]+}}]
        // ARM64-NOT: mov
        ulong difference = value - 7;
        ulong result = high - (difference > value ? 1UL : 0UL);
        low = difference;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint SubtractSeven32(uint value, uint high, out uint low)
    {
        // X64: sub
        // X64-NOT: cmp
        // X64: sbb
        // ARM64: subs
        // ARM64-NEXT: sbc
        // ARM64-NEXT: str {{w[0-9]+}}, [{{x[0-9]+}}]
        // ARM64-NOT: mov
        uint difference = value - 7;
        uint result = high - (difference > value ? 1U : 0U);
        low = difference;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SubtractHalfRange(ulong value, ulong high, out ulong low)
    {
        ulong difference = value - (1UL << 63);
        ulong result = high - (difference > value ? 1UL : 0UL);
        low = difference;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ComplementBorrow(ulong value, out ulong low)
    {
        ulong difference = value - 7;
        ulong result = difference <= value ? 1UL : 0UL;
        low = difference;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NegatedProductIsZero(long a, long b, out long product)
    {
        // The returned condition must not displace the still-live product.
        // X64: imul
        // X64-NEXT: neg
        // X64-NEXT: setae
        // X64-NEXT: movzx
        // X64-NEXT: mov {{qword ptr \[[^]]+\]}},
        // A contained ARM64 MNEG must not be used as a flags producer.
        long value = -(a * b);
        bool zero = value == 0;
        product = value;
        return zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CorrectQuotient(ulong high, ulong low, ulong extra, out ulong sum)
    {
        // X64: add
        // X64-NEXT: adc
        // ARM64: adds
        // ARM64-NEXT: adc
        ulong middle = low + extra;
        high += middle < extra ? 1UL : 0UL;
        sum = middle;
        return high;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ClearAndReturnFalse(out object value)
    {
        // The return constant can reuse the register already zeroed for the store.
        // X64: xor
        // X64-NEXT: mov
        // X64-NOT: xor
        // X64: ret
        value = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 Multiply(UInt128 a, UInt128 b, out UInt128 low) => UInt128.BigMul(a, b, out low);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 MultiplySigned(Int128 a, Int128 b, out Int128 low) => Int128.BigMul(a, b, out low);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 CheckedAdd(Int128 a, Int128 b) => checked(a + b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 CheckedSubtract(Int128 a, Int128 b) => checked(a - b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 CheckedNegate(Int128 value) => checked(-value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 CheckedMultiply(Int128 a, Int128 b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Negate32(uint value, uint high, out uint low)
    {
        uint difference = 0 - value;
        uint result = (0 - high) - (difference != 0 ? 1U : 0U);
        low = difference;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 CheckedDecrement(UInt128 value) => checked(value - 1);

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new Exception("Wide arithmetic mismatch");
        }
    }

    private static void CheckOverflow(Func<Int128> operation, BigInteger expected)
    {
        bool overflow = expected < (BigInteger)Int128.MinValue || expected > (BigInteger)Int128.MaxValue;
        try
        {
            Int128 actual = operation();
            Require(!overflow && (BigInteger)actual == expected);
        }
        catch (OverflowException)
        {
            Require(overflow);
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        ulong[] edges = { 0, 1, 6, 7, 8, uint.MaxValue, 1UL << 32, 1UL << 63, ulong.MaxValue - 1, ulong.MaxValue };
        var random = new Random(80674);
        byte[] bytes = new byte[32];
        BigInteger mask = (BigInteger.One << 128) - 1;
        Require(!ClearAndReturnFalse(out object cleared) && cleared == null);
        for (int i = 0; i < 2500; i++)
        {
            random.NextBytes(bytes);
            UInt128 a = ((UInt128)BitConverter.ToUInt64(bytes, 0) << 64) | BitConverter.ToUInt64(bytes, 8);
            UInt128 b = ((UInt128)BitConverter.ToUInt64(bytes, 16) << 64) | BitConverter.ToUInt64(bytes, 24);
            if (i < edges.Length * edges.Length)
            {
                a = ((UInt128)edges[i / edges.Length] << 64) | edges[i % edges.Length];
                b = UInt128.MaxValue - a;
            }
            BigInteger product = (BigInteger)a * (BigInteger)b;
            UInt128 high = Multiply(a, b, out UInt128 low);
            Require((BigInteger)high == product >> 128 && (BigInteger)low == (product & mask));
            product = (BigInteger)(Int128)a * (BigInteger)(Int128)b;
            Int128 signedHigh = MultiplySigned((Int128)a, (Int128)b, out Int128 signedLow);
            Require((BigInteger)signedHigh == product >> 128 && (BigInteger)(UInt128)signedLow == (product & mask));
            Require((BigInteger)Increment(a) == (((BigInteger)a + 1) & mask));
            Require((BigInteger)Decrement(a) == (((BigInteger)a - 1) & mask));
            Require((BigInteger)(UInt128)Negate((Int128)a) == (-(BigInteger)a & mask));
            CheckOverflow(() => CheckedAdd((Int128)a, (Int128)b), (BigInteger)(Int128)a + (BigInteger)(Int128)b);
            CheckOverflow(() => CheckedSubtract((Int128)a, (Int128)b), (BigInteger)(Int128)a - (BigInteger)(Int128)b);
            CheckOverflow(() => CheckedNegate((Int128)a), -(BigInteger)(Int128)a);
            if (i < edges.Length * edges.Length)
            {
                CheckOverflow(() => CheckedMultiply((Int128)a, (Int128)b), product);
            }
            try
            {
                Require(CheckedDecrement(a) == Decrement(a) && a != 0);
            }
            catch (OverflowException)
            {
                Require(a == 0);
            }
            ulong x = (ulong)a, h = (ulong)(a >> 64);
            ulong pair32 = ((ulong)(uint)h << 32) | (uint)x;
            uint negatedHigh = Negate32((uint)x, (uint)h, out uint negatedLow);
            Require((((ulong)negatedHigh << 32) | negatedLow) == unchecked(0UL - pair32));
            Require(SubtractSeven(x, h, out ulong l) == unchecked(h - (x < 7 ? 1UL : 0)) && l == unchecked(x - 7));
            Require(SubtractSeven32((uint)x, (uint)h, out uint l32) == unchecked((uint)h - ((uint)x < 7 ? 1U : 0)) && l32 == unchecked((uint)x - 7));
            Require(SubtractHalfRange(x, h, out l) == unchecked(h - (x < (1UL << 63) ? 1UL : 0)) && l == unchecked(x - (1UL << 63)));
            Require(ComplementBorrow(x, out l) == (x >= 7 ? 1UL : 0) && l == unchecked(x - 7));
            bool zero = NegatedProductIsZero((long)x, (long)h, out long negated);
            Require(negated == unchecked(-((long)x * (long)h)) && zero == (negated == 0));
            UInt128 sum = (UInt128)x + h;
            Require(CorrectQuotient((ulong)b, x, h, out l) == unchecked((ulong)b + (ulong)(sum >> 64)) && l == (ulong)sum);

            // Exercise the calculator's multiply-by-limb and quotient correction
            // through public APIs, using a known quotient and remainder.
            int bits = IntPtr.Size * 8;
            BigInteger divisor = ((BigInteger)a << bits) + (ulong)b + 1;
            BigInteger quotient = (ulong)(nuint)x;
            BigInteger remainder = divisor >> 1;
            BigInteger dividend = divisor * quotient + remainder;
            Require(BigInteger.DivRem(dividend, divisor, out BigInteger actualRemainder) == quotient && actualRemainder == remainder);
            if (b != 0)
            {
                Require((BigInteger)(a / b) == (BigInteger)a / (BigInteger)b && (BigInteger)(a % b) == (BigInteger)a % (BigInteger)b);
            }
        }
    }
}
