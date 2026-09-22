// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class DivisionTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong RemainderNonZero(ulong value, ulong divisor)
    {
        // X64: div
        // X64: ret
        divisor |= 1;
        return Math.DivRem(value, divisor).Remainder;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddWithHighMemory(ulong low, ulong value, ref uint high)
    {
        // X64: add
        // X64-NEXT: adc
        // ARM64: adds
        // ARM64-NEXT: adc
        ulong sum = low + value;
        ulong upper = high;
        return upper + (sum < low ? 1UL : 0UL);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void IncrementHigh(ref uint high) => high++;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddWithChangedHighMemory(ulong low, ulong value, ref uint high)
    {
        ulong sum = low + value;
        IncrementHigh(ref high);
        ulong upper = high;
        return upper + (sum < low ? 1UL : 0UL);
    }

    private static void CheckDecimalArithmetic()
    {
        BigInteger max = (BigInteger.One << 96) - 1;
        (BigInteger Coefficient, int Scale) Parts(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            BigInteger coefficient = (uint)bits[0] | ((BigInteger)(uint)bits[1] << 32) |
                ((BigInteger)(uint)bits[2] << 64);
            return (bits[3] < 0 ? -coefficient : coefficient, (bits[3] >> 16) & 255);
        }

        decimal Make(BigInteger coefficient, int scale) => new decimal(
            (int)(uint)(BigInteger.Abs(coefficient) & uint.MaxValue),
            (int)(uint)((BigInteger.Abs(coefficient) >> 32) & uint.MaxValue),
            (int)(uint)(BigInteger.Abs(coefficient) >> 64), coefficient.Sign < 0, (byte)scale);

        void Check(decimal a, decimal b)
        {
            var (ca, sa) = Parts(a);
            var (cb, sb) = Parts(b);
            int scale = Math.Max(sa, sb);
            ca *= BigInteger.Pow(10, scale - sa);
            cb *= BigInteger.Pow(10, scale - sb);
            foreach (bool subtract in new[] { false, true })
            {
                BigInteger exact = subtract ? ca - cb : ca + cb;
                bool fits = false;
                for (int drop = 0; drop <= scale; drop++)
                {
                    BigInteger divisor = BigInteger.Pow(10, drop);
                    BigInteger q = BigInteger.DivRem(BigInteger.Abs(exact), divisor, out BigInteger r);
                    if (r * 2 > divisor || (r * 2 == divisor && !q.IsEven))
                    {
                        q++;
                    }
                    if (q <= max)
                    {
                        decimal expected = Make(exact.Sign < 0 ? -q : q, scale - drop);
                        decimal actual = subtract ? a - b : a + b;
                        if (actual != expected)
                        {
                            throw new Exception("Decimal addition/subtraction oracle");
                        }
                        fits = true;
                        break;
                    }
                }
                if (!fits)
                {
                    Throws<OverflowException>(() => { _ = subtract ? a - b : a + b; });
                }
            }
            if (cb != 0)
            {
                BigInteger remainder = ca % cb;
                int remainderScale = scale;
                while (BigInteger.Abs(remainder) > max && remainderScale > 0 && remainder % 10 == 0)
                {
                    remainder /= 10;
                    remainderScale--;
                }
                decimal actualRemainder = a % b;
                decimal expectedRemainder = Make(remainder, remainderScale);
                if (actualRemainder != expectedRemainder)
                {
                    throw new Exception($"Decimal remainder oracle: {a} % {b} = {actualRemainder}, expected {expectedRemainder}");
                }
            }
        }

        decimal[] edges = { 0m, 1m, -1m, decimal.MaxValue, decimal.MinValue,
            new decimal(-1, -1, 0, false, 0), new decimal(0, 0, 1, false, 0),
            new decimal(-1, -1, -1, false, 1), new decimal(-1, -1, -1, true, 28),
            0.0000000000000000000000000001m, 0.5m, 1.5m, 10m, uint.MaxValue };
        foreach (decimal a in edges)
        {
            foreach (decimal b in edges)
            {
                Check(a, b);
            }
        }
        Random random = new Random(789);
        byte[] bytes = new byte[24];
        for (int i = 0; i < 1500; i++)
        {
            random.NextBytes(bytes);
            int scale = random.Next(29);
            decimal a = new decimal(BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4),
                BitConverter.ToInt32(bytes, 8), (i & 1) != 0, (byte)scale);
            decimal b = new decimal(BitConverter.ToInt32(bytes, 12), i % 3 == 0 ? 0 : BitConverter.ToInt32(bytes, 16),
                i % 3 == 0 ? 0 : BitConverter.ToInt32(bytes, 20), (i & 2) != 0,
                (byte)(i % 2 == 0 ? scale : random.Next(29)));
            Check(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SubtractWithWidenedHigh(ulong low, ulong subLow, uint high, ulong subHigh, out ulong result)
    {
        // X64: sub
        // X64-NEXT: sbb
        // ARM64: subs
        // ARM64-NEXT: sbc
        ulong difference = low - subLow;
        long upper = (long)high - (long)subHigh;
        upper -= difference > low ? 1L : 0L;
        result = difference;
        return upper;
    }

    private static void CheckDecimalDivisionHelpers()
    {
        Type calculator = typeof(decimal).GetNestedType("DecCalc", BindingFlags.NonPublic);
        Type buf12 = calculator.GetNestedType("Buf12", BindingFlags.NonPublic);
        Type buf16 = calculator.GetNestedType("Buf16", BindingFlags.NonPublic);
        MethodInfo divide96 = calculator.GetMethod("Div96By32", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo divide128 = calculator.GetMethod("Div128By96", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo add32 = calculator.GetMethod("Add32To96", BindingFlags.NonPublic | BindingFlags.Static);

        object Pack(Type type, BigInteger value, int limbs)
        {
            object buffer = Activator.CreateInstance(type);
            for (int limb = 0; limb < limbs; limb++)
            {
                type.GetField($"U{limb}").SetValue(buffer, (uint)(value & uint.MaxValue));
                value >>= 32;
            }
            return buffer;
        }

        BigInteger Unpack(object buffer, int limbs)
        {
            BigInteger value = 0;
            for (int limb = limbs - 1; limb >= 0; limb--)
            {
                value = (value << 32) | (uint)buffer.GetType().GetField($"U{limb}").GetValue(buffer);
            }
            return value;
        }

        int correctionsSeen = 0;
        void Check128(BigInteger numerator, BigInteger denominator)
        {
            BigInteger expected = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            BigInteger estimate = (numerator >> 64) / (denominator >> 64);
            int corrections = (int)(estimate - expected);
            if (corrections < 0 || corrections > 2)
            {
                throw new Exception("Invalid normalized division test vector");
            }
            correctionsSeen |= 1 << corrections;
            object[] args = { Pack(buf16, numerator, 4), Pack(buf12, denominator, 3) };
            uint quotient = (uint)divide128.Invoke(null, args);
            if (quotient != expected || Unpack(args[0], 3) != remainder)
            {
                throw new Exception($"Decimal 128/96 division, corrections {corrections}");
            }
        }

        BigInteger normalized = (BigInteger.One << 95) | ulong.MaxValue;
        foreach (uint q in new uint[] { 0, 1, 2, 0x7FFFFFFF, 0xFFFFFFFD })
        {
            foreach (BigInteger remainder in new[] { BigInteger.Zero, BigInteger.One, normalized - 1 })
            {
                Check128(normalized * q + remainder, normalized);
            }
        }

        Random random = new Random(456);
        byte[] bytes = new byte[16];
        for (int i = 0; i < 1500; i++)
        {
            random.NextBytes(bytes);
            BigInteger numerator = new BigInteger(bytes, isUnsigned: true);
            random.NextBytes(bytes);
            BigInteger denominator = (new BigInteger(bytes, isUnsigned: true) & ((BigInteger.One << 96) - 1)) |
                (BigInteger.One << 95);
            // The helper requires the highest dividend limb to be below the
            // highest divisor limb, so its quotient estimate fits in 32 bits.
            Check128(numerator % ((denominator >> 64) << 96), denominator);

            uint divisor = (i % 4) switch { 0 => 1U, 1 => uint.MaxValue, 2 => 10U, _ => (uint)random.Next(1, int.MaxValue) };
            numerator &= (BigInteger.One << 96) - 1;
            if (i % 3 == 0)
            {
                numerator &= ulong.MaxValue;
            }
            BigInteger expected = BigInteger.DivRem(numerator, divisor, out BigInteger remainder);
            object[] args = { Pack(buf12, numerator, 3), divisor };
            uint actualRemainder = (uint)divide96.Invoke(null, args);
            if (Unpack(args[0], 3) != expected || actualRemainder != remainder)
            {
                throw new Exception("Decimal 96/32 division");
            }
            BigInteger max = (BigInteger.One << 96) - 1;
            if (i % 5 == 0)
            {
                numerator = max - i % 3;
            }
            args = new object[] { Pack(buf12, numerator, 3), divisor };
            bool fits = (bool)add32.Invoke(null, args);
            BigInteger sum = numerator + divisor;
            if (fits != (sum <= max) || Unpack(args[0], 3) != (sum & max))
            {
                throw new Exception("Decimal add32 carry/overflow");
            }
        }
        if (correctionsSeen != 7)
        {
            throw new Exception("Decimal tests must exercise zero, one, and two quotient corrections");
        }
    }

    private static void CheckDecimalRounding()
    {
        Random random = new Random(123);
        byte[] bytes = new byte[12];
        for (int i = 0; i < 1200; i++)
        {
            random.NextBytes(bytes);
            BigInteger coefficient = new BigInteger(bytes, isUnsigned: true);
            if (i % 3 == 0)
            {
                coefficient &= ulong.MaxValue;
            }
            int scale = i % 29;
            int decimals = random.Next(scale + 1);
            BigInteger divisor = BigInteger.Pow(10, scale - decimals);
            // Include exact midpoints, their neighbors, zero, and maximal limbs.
            if (i % 7 == 0)
            {
                coefficient = divisor / 2 + i % 3 - 1;
                coefficient = BigInteger.Max(coefficient, BigInteger.Zero);
            }
            else if (i % 11 == 0)
            {
                coefficient = (BigInteger.One << 96) - 1;
            }
            bool negative = (i & 1) != 0;
            decimal value = new decimal((int)(uint)(coefficient & uint.MaxValue),
                (int)(uint)((coefficient >> 32) & uint.MaxValue),
                (int)(uint)(coefficient >> 64), negative, (byte)scale);
            BigInteger quotient = BigInteger.DivRem(coefficient, divisor, out BigInteger remainder);
            foreach (MidpointRounding mode in Enum.GetValues<MidpointRounding>())
            {
                bool increment = mode switch
                {
                    MidpointRounding.ToEven => remainder * 2 > divisor ||
                        (remainder * 2 == divisor && !quotient.IsEven),
                    MidpointRounding.AwayFromZero => remainder * 2 >= divisor,
                    MidpointRounding.ToNegativeInfinity => negative && !remainder.IsZero,
                    MidpointRounding.ToPositiveInfinity => !negative && !remainder.IsZero,
                    _ => false,
                };
                BigInteger expected = quotient + (increment ? 1 : 0);
                int[] bits = decimal.GetBits(decimal.Round(value, decimals, mode));
                BigInteger actual = (uint)bits[0] | ((BigInteger)(uint)bits[1] << 32) |
                    ((BigInteger)(uint)bits[2] << 64);
                int expectedFlags = (decimals << 16) | (negative ? int.MinValue : 0);
                if (actual != expected || bits[3] != expectedFlags)
                {
                    throw new Exception($"Decimal rounding: vector {i}, mode {mode}");
                }
            }
        }
    }

    private static int s_calls;
    private delegate nuint WideningDivide(nuint hi, nuint lo, nuint divisor, out nuint remainder);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Unsigned(ulong x, ulong y, out ulong remainder)
    {
        // X64: div
        // X64-NOT: imul
        // X64-NOT: sub
        // X64: ret
        ulong q = x / y;
        remainder = x - q * y;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Signed(long x, long y, out long remainder)
    {
        // X64: idiv
        // X64-NOT: imul
        // X64-NOT: sub
        // X64: ret
        long q = x / y;
        remainder = x - y * q;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Unsigned32(uint x, uint y, out uint remainder)
    {
        // X64: div
        // X64-NOT: imul
        // X64: ret
        // X86: div
        // X86-NOT: imul
        // X86: ret
        uint q = x / y;
        remainder = x - q * y;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Signed32(int x, int y, out int remainder)
    {
        // X64: idiv
        // X64-NOT: imul
        // X64: ret
        // X86: idiv
        // X86-NOT: imul
        // X86: ret
        int q = x / y;
        remainder = x - q * y;
        return q;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong RemainderOnly(ulong x, ulong y)
    {
        return x - (x / y) * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong InlinedRemainder(ulong x, ulong y) => Math.DivRem(x, y).Remainder;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Touch(ulong x) => x + 3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AcrossCall(ulong x, ulong y)
    {
        ulong q = x / y;
        ulong z = Touch(x);
        return z ^ q ^ (x - q * y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong KeepQuotient(ulong x, ulong y, ulong z, out ulong remainder)
    {
        ulong q = x / y;
        s_calls++;
        remainder = x - q * y;
        return q ^ z;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Changed(ulong x, ulong y)
    {
        ulong q = x / y;
        x += 17;
        y += 3;
        q += 2;
        return x - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Aliased(ulong[] values, ulong y)
    {
        ulong q = values[0] / y;
        values[0] += 17;
        return values[0] - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mutate(ref ulong x) => x += 17;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ChangedByRef(ulong x, ulong y)
    {
        ulong q = x / y;
        Mutate(ref x);
        return x - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong QuotientChangedByRef(ulong x, ulong y)
    {
        ulong q = x / y;
        Mutate(ref q);
        return x - q * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong EmbeddedAssignment(ulong x, ulong y) => x - ((x += 17) / y) * y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CheckedProduct(ulong x, ulong y, ulong z)
    {
        ulong q = x / y;
        return checked(x - checked(q * z));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt128 Unsigned128(UInt128 x, UInt128 y, out UInt128 r)
    {
        var result = UInt128.DivRem(x, y);
        r = result.Remainder;
        return result.Quotient;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 Signed128(Int128 x, Int128 y, out Int128 r)
    {
        var result = Int128.DivRem(x, y);
        r = result.Remainder;
        return result.Quotient;
    }

    private static void Check128(UInt128 a, UInt128 b)
    {
        if (b == 0)
        {
            return;
        }
        UInt128 q = Unsigned128(a, b, out UInt128 r);
        if ((BigInteger)q * (BigInteger)b + (BigInteger)r != (BigInteger)a || r >= b)
        {
            throw new Exception("UInt128 division");
        }

        Int128 x = (Int128)a, y = (Int128)b;
        if (x == Int128.MinValue && y == -1)
        {
            return;
        }
        Int128 sq = Signed128(x, y, out Int128 sr);
        if ((BigInteger)sq * (BigInteger)y + (BigInteger)sr != (BigInteger)x ||
            BigInteger.Abs((BigInteger)sr) >= BigInteger.Abs((BigInteger)y) ||
            (sr != 0 && Int128.IsNegative(sr) != Int128.IsNegative(x)) || x % y != sr || x / y != sq)
        {
            throw new Exception("Int128 division");
        }
    }

    private static void CheckScalar(ulong x, ulong y)
    {
        if (RemainderNonZero(x, y) != x % (y | 1))
        {
            throw new Exception("The unused quotient must not remove the remainder's division");
        }
        uint memoryHigh = (uint)y;
        ulong carry = (ulong)(((BigInteger)x + y) >> 64);
        if (AddWithHighMemory(x, y, ref memoryHigh) != (ulong)memoryHigh + carry)
        {
            throw new Exception("Carry with widened memory input");
        }
        ulong changed = AddWithChangedHighMemory(x, y, ref memoryHigh);
        if (changed != (ulong)(uint)(y + 1) + carry || memoryHigh != (uint)(y + 1))
        {
            throw new Exception("Carry must observe the intervening memory write");
        }
        long high = SubtractWithWidenedHigh(x, y, (uint)y, x, out ulong low);
        BigInteger expected = ((((BigInteger)(uint)y << 64) | x) - (((BigInteger)x << 64) | y)) &
            ((BigInteger.One << 128) - 1);
        if ((((BigInteger)(ulong)high << 64) | low) != expected)
        {
            throw new Exception("Subtraction with widened high word");
        }
        if (y == 0)
        {
            return;
        }
        ulong q = Unsigned(x, y, out ulong r);
        if ((BigInteger)q * y + r != x || r >= y || RemainderOnly(x, y) != r ||
            InlinedRemainder(x, y) != r || AcrossCall(x, y) != ((x + 3) ^ q ^ r))
        {
            throw new Exception("unsigned scalar");
        }
        int calls = s_calls;
        if (KeepQuotient(x, y, ~x, out ulong r2) != (q ^ ~x) || r2 != r || s_calls != calls + 1)
        {
            throw new Exception("quotient lifetime");
        }
        if (Changed(x, y) != unchecked((x + 17) - (q + 2) * (y + 3)) ||
            Aliased(new[] { x }, y) != unchecked(x + 17 - q * y) ||
            ChangedByRef(x, y) != unchecked(x + 17 - q * y) ||
            QuotientChangedByRef(x, y) != unchecked(x - (q + 17) * y) ||
            EmbeddedAssignment(x, y) != unchecked(x - ((x + 17) / y) * y))
        {
            throw new Exception("changed operands");
        }

        long sx = (long)x, sy = (long)y;
        if (!(sx == long.MinValue && sy == -1))
        {
            long sq = Signed(sx, sy, out long sr);
            if ((BigInteger)sq * sy + sr != sx || BigInteger.Abs(sr) >= BigInteger.Abs(sy) ||
                (sr != 0 && (sr < 0) != (sx < 0)))
            {
                throw new Exception("signed scalar");
            }
        }

        uint ux = (uint)x, uy = (uint)y;
        if (uy == 0)
        {
            return;
        }
        uint uq = Unsigned32(ux, uy, out uint ur);
        if ((ulong)uq * uy + ur != ux || ur >= uy)
        {
            throw new Exception("uint division");
        }
        int ix = (int)x, iy = (int)y;
        if (!(ix == int.MinValue && iy == -1))
        {
            int iq = Signed32(ix, iy, out int ir);
            if ((long)iq * iy + ir != ix || Math.Abs((long)ir) >= Math.Abs((long)iy) ||
                (ir != 0 && (ir < 0) != (ix < 0)))
            {
                throw new Exception("int division");
            }
        }
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception($"Expected {typeof(T)}");
    }

    [Fact]
    public static void TestEntryPoint()
    {
        ulong[] edges = { 0, 1, 2, 3, uint.MaxValue, (ulong)uint.MaxValue + 1, 1UL << 63, (1UL << 63) - 1, ulong.MaxValue };
        foreach (ulong x in edges)
        {
            foreach (ulong y in edges)
            {
                CheckScalar(x, y);
                Check128(x, y);
                Check128(((UInt128)x << 64) | y, ((UInt128)y << 64) | x);
            }
        }
        Throws<DivideByZeroException>(() => Unsigned(123, 0, out _));
        Throws<DivideByZeroException>(() => Signed(123, 0, out _));
        Throws<DivideByZeroException>(() => Unsigned32(123, 0, out _));
        Throws<DivideByZeroException>(() => Signed32(123, 0, out _));
        int callsBeforeException = s_calls;
        Throws<DivideByZeroException>(() => KeepQuotient(123, 0, 456, out _));
        if (s_calls != callsBeforeException)
        {
            throw new Exception("division exception order");
        }
        Throws<OverflowException>(() => Signed(long.MinValue, -1, out _));
        Throws<OverflowException>(() => Signed32(int.MinValue, -1, out _));
        Throws<DivideByZeroException>(() => Signed128(123, 0, out _));
        Throws<OverflowException>(() => Signed128(Int128.MinValue, -1, out _));
        Throws<OverflowException>(() => { _ = Int128.MinValue % -1; });
        Throws<OverflowException>(() => CheckedProduct(ulong.MaxValue, 1, 2));

        Type calculator = typeof(BigInteger).Assembly.GetType("System.Numerics.BigIntegerCalculator", throwOnError: true);
        MethodInfo widening = calculator.GetMethod("DivRem", BindingFlags.Static | BindingFlags.NonPublic,
            binder: null, new[] { typeof(nuint), typeof(nuint), typeof(nuint), typeof(nuint).MakeByRefType() }, modifiers: null);
        WideningDivide divide = widening.CreateDelegate<WideningDivide>();
        Throws<DivideByZeroException>(() => divide(0, 123, 0, out _));
        foreach (ulong edge in edges)
        {
            nuint d = (nuint)edge;
            if (d == 0)
            {
                continue;
            }
            foreach (nuint hi in new nuint[] { 0, d - 1 })
            {
                foreach (nuint lo in new nuint[] { 0, 1, nuint.MaxValue })
                {
                    nuint q = divide(hi, lo, d, out nuint r);
                    BigInteger value = ((BigInteger)(ulong)hi << (IntPtr.Size * 8)) | (ulong)lo;
                    if ((BigInteger)(ulong)q * (ulong)d + (ulong)r != value || r >= d)
                    {
                        throw new Exception("widening division");
                    }
                }
            }
        }

        CheckDecimalRounding();
        CheckDecimalDivisionHelpers();
        CheckDecimalArithmetic();

        Random random = new Random(42);
        byte[] bytes = new byte[32];
        for (int i = 0; i < 3000; i++)
        {
            random.NextBytes(bytes);
            ulong lo = BitConverter.ToUInt64(bytes, 0), hi = BitConverter.ToUInt64(bytes, 8);
            ulong dlo = BitConverter.ToUInt64(bytes, 16), dhi = BitConverter.ToUInt64(bytes, 24);
            CheckScalar(lo, dlo);
            UInt128 a = ((UInt128)hi << 64) | lo;
            Check128(a, ((UInt128)dhi << 64) | dlo);
            Check128(a, dlo);
        }

        // Exercise both the single-limb and multi-limb BigInteger division paths.
        for (int length = 1; length <= 160; length++)
        {
            byte[] input = new byte[length * IntPtr.Size];
            random.NextBytes(input);
            BigInteger a = new BigInteger(input, isUnsigned: true);
            foreach (ulong d in edges)
            {
                if (d == 0)
                {
                    continue;
                }
                BigInteger q = BigInteger.DivRem(a, d, out BigInteger r);
                if (q * d + r != a || r < 0 || r >= d)
                {
                    throw new Exception("BigInteger limb division");
                }
            }
            byte[] divisor = new byte[Math.Max(1, input.Length / 2)];
            random.NextBytes(divisor);
            divisor[0] |= 1;
            BigInteger b = new BigInteger(divisor, isUnsigned: true);
            BigInteger quotient = BigInteger.DivRem(a, b, out BigInteger remainder);
            if (quotient * b + remainder != a || remainder < 0 || remainder >= b)
            {
                throw new Exception("BigInteger multi-limb division");
            }
        }
    }
}
