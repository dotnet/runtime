// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class WideDivisionTests
{
    private delegate nuint WideningDivide(nuint hi, nuint lo, nuint divisor, out nuint remainder);

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

    private static void CheckDecimalRemainder()
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
                Check128(x, y);
                Check128(((UInt128)x << 64) | y, ((UInt128)y << 64) | x);
            }
        }
        Throws<DivideByZeroException>(() => Signed128(123, 0, out _));
        Throws<OverflowException>(() => Signed128(Int128.MinValue, -1, out _));
        Throws<OverflowException>(() => { _ = Int128.MinValue % -1; });

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
        CheckDecimalRemainder();

        Random random = new Random(42);
        byte[] bytes = new byte[32];
        for (int i = 0; i < 3000; i++)
        {
            random.NextBytes(bytes);
            ulong lo = BitConverter.ToUInt64(bytes, 0), hi = BitConverter.ToUInt64(bytes, 8);
            ulong dlo = BitConverter.ToUInt64(bytes, 16), dhi = BitConverter.ToUInt64(bytes, 24);
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
