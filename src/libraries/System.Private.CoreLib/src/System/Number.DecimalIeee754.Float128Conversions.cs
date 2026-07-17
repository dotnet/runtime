// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System;

internal static partial class Number
{
    // Compact conversions between a BID decimal coefficient and the software binary128 (`ux`) engine.
    //
    // Intel evaluates Decimal64/Decimal128 transcendentals in binary128, converting the operand in and
    // the result out. Its own conversions (`bid*_to_binary128` / `binary128_to_bid*`) are table-driven
    // and pull in ~840 KB of precomputed multiplier/breakpoint tables spanning decimal exponents
    // -5000..+5000. Rather than embed those in CoreLib, these conversions reuse the per-format
    // `UInt{64,128}Powers10` tables already present for parsing/formatting and build the required power
    // of ten on the fly by chunked multiply/divide in the engine. A coefficient (< 2^113) loads into a
    // binary128 significand exactly, and every 10^k with k below the format precision is exact in the
    // 128-bit `ux` fraction (5^34 < 2^114), so the only rounding is the final round-to-nearest-even
    // extraction of the P-digit result. That keeps the transcendental cores bit-faithful to Intel while
    // the conversion stays within the <= 1 ulp faithful target; the extended-precision table path is a
    // documented later refinement.

    /// <summary>
    /// Builds a normalized <see cref="Float128"/> holding the exact value of the non-zero magnitude
    /// <paramref name="coefficient"/> (which must be below 2^128) with the given <paramref name="sign"/>.
    /// </summary>
    private static Float128 Float128FromUInt128(UInt128 coefficient, uint sign)
    {
        Debug.Assert(coefficient != UInt128.Zero);

        int leadingZeros = (int)UInt128.LeadingZeroCount(coefficient);
        UInt128 fraction = coefficient << leadingZeros; // most significant bit at bit 127

        // value = fraction * 2^(exponent - 128); with fraction = coefficient << leadingZeros this is
        // exactly coefficient, so exponent = 128 - leadingZeros.
        return new Float128(sign, 128 - leadingZeros, (ulong)(fraction >> 64), (ulong)fraction);
    }

    /// <summary>
    /// Scales <paramref name="value"/> by <c>10^<paramref name="power"/></c> in the engine, multiplying
    /// for a non-negative power and dividing for a negative one. The power of ten is assembled from the
    /// format's existing pow10 table in chunks of at most <c>Precision - 1</c> digits, each an exact
    /// binary128 multiplier.
    /// </summary>
    private static Float128 Float128ScaleByPow10<TDecimal, TValue>(Float128 value, int power)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        int remaining = int.Abs(power);
        int maxChunk = TDecimal.Precision - 1;

        while (remaining > 0)
        {
            int chunk = int.Min(remaining, maxChunk);
            Float128 pow = Float128FromUInt128(UInt128.CreateTruncating(TDecimal.Power10(chunk)), 0);

            if (power > 0)
            {
                Float128Multiply(ref value, ref pow, out value);
                Float128Normalize(ref value);
            }
            else
            {
                Float128Divide(value, pow, Float128FullPrecision, out value);
            }

            remaining -= chunk;
        }

        return value;
    }

    /// <summary>
    /// Converts the decoded finite, non-zero BID decimal <c>(sign, unbiasedExponent, significand)</c> to
    /// the engine's binary128 form.
    /// </summary>
    private static Float128 DecimalToFloat128<TDecimal, TValue>(bool signed, int unbiasedExponent, TValue significand)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        Debug.Assert(!TValue.IsZero(significand));

        Float128 value = Float128FromUInt128(UInt128.CreateTruncating(significand), signed ? UxSignBit : 0);
        return Float128ScaleByPow10<TDecimal, TValue>(value, unbiasedExponent);
    }

    /// <summary>
    /// Rounds the finite <paramref name="value"/> to a <c>P</c>-digit decimal coefficient (round-to-
    /// nearest, ties-to-even) and returns the encoded BID bit pattern, mapping over/underflow to the
    /// format's infinity, subnormal, or zero as required. <paramref name="value"/> is assumed positive-
    /// magnitude in <paramref name="value"/><c>._hi/_lo</c>; the sign is taken from <c>value._sign</c>.
    /// </summary>
    private static TValue Float128ToDecimal<TDecimal, TValue>(Float128 value)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        bool signed = value.IsNegative;
        Float128Normalize(ref value);

        if ((value._hi | value._lo) == 0)
        {
            // Exact zero result: encode the sign-preserving canonical decimal zero at exponent 0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, TValue.Zero, 0);
        }

        int precision = TDecimal.Precision;

        // value in [2^(exp-1), 2^exp), so floor(log10(value)) is floor((exp-1)*log10(2)) or one greater.
        // Target q = d - (P-1) puts value/10^q in [10^(P-1), 10^P); the loop corrects the +/-1 estimate.
        int binaryExponent = value._exponent - 1;
        int d = (int)double.Floor(binaryExponent * 0.30102999566398119521);
        int q = d - (precision - 1);

        UInt128 pow10P = UInt128.CreateTruncating(TDecimal.MaxSignificand) + UInt128.One; // 10^P
        UInt128 pow10Pm1 = UInt128.CreateTruncating(TDecimal.Power10(precision - 1));     // 10^(P-1)

        UInt128 coefficient;

        while (true)
        {
            Float128 scaled = Float128ScaleByPow10<TDecimal, TValue>(value, -q);
            coefficient = Float128RoundToUInt128(scaled);

            if (coefficient >= pow10P)
            {
                // Rounded up past P digits; shift one decimal place and retry at the higher exponent.
                q++;
                continue;
            }

            if ((coefficient != UInt128.Zero) && (coefficient < pow10Pm1))
            {
                // Under-shot (estimate was one high); pull in another decimal place.
                q--;
                continue;
            }

            break;
        }

        return EncodeDecimalFromUInt128<TDecimal, TValue>(signed, coefficient, q);
    }

    /// <summary>
    /// Rounds a finite <see cref="Float128"/> whose magnitude is below 2^128 to the nearest integer
    /// (ties-to-even) and returns it. The value is assumed normalized with a fractional part (i.e. its
    /// binary exponent is below 128), which holds for the in-range decimal coefficients this is used for.
    /// </summary>
    private static UInt128 Float128RoundToUInt128(Float128 value)
    {
        int shift = 128 - value._exponent;
        Debug.Assert(shift is > 0 and < 128);

        UInt128 fraction = ((UInt128)value._hi << 64) | value._lo;
        UInt128 integer = fraction >> shift;
        UInt128 remainder = fraction & ((UInt128.One << shift) - UInt128.One);
        UInt128 half = UInt128.One << (shift - 1);

        if ((remainder > half) || ((remainder == half) && ((integer & UInt128.One) != UInt128.Zero)))
        {
            integer++;
        }

        return integer;
    }

    /// <summary>
    /// Encodes <c>(sign, coefficient, exponent)</c> into the BID bit pattern, reducing the coefficient to
    /// a representable subnormal (ties-to-even) when the exponent is below the minimum and returning the
    /// format's infinity when it is above the maximum.
    /// </summary>
    private static TValue EncodeDecimalFromUInt128<TDecimal, TValue>(bool signed, UInt128 coefficient, int exponent)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (coefficient == UInt128.Zero)
        {
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, TValue.Zero, TDecimal.MinAdjustedExponent);
        }

        if (exponent > TDecimal.MaxAdjustedExponent)
        {
            return signed ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        if (exponent < TDecimal.MinAdjustedExponent)
        {
            // Fold the extra magnitude into the coefficient as a subnormal, rounding ties-to-even.
            int deficit = TDecimal.MinAdjustedExponent - exponent;

            if (deficit > 39)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, TValue.Zero, TDecimal.MinAdjustedExponent);
            }

            UInt128 power = Pow10ToUInt128(deficit);
            UInt128 quotient = coefficient / power;
            UInt128 remainder = coefficient - (quotient * power);
            UInt128 half = power >> 1; // 10^deficit is even, so this is an exact half

            if ((remainder > half) || ((remainder == half) && ((quotient & UInt128.One) != UInt128.Zero)))
            {
                quotient++;
            }

            coefficient = quotient;
            exponent = TDecimal.MinAdjustedExponent;

            if (coefficient == UInt128.Zero)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, TValue.Zero, exponent);
            }
        }

        return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, TValue.CreateTruncating(coefficient), exponent);
    }

    /// <summary>
    /// Computes <c>10^<paramref name="exponent"/></c> as a <see cref="UInt128"/> for a small exponent
    /// (used only on the rare subnormal-folding path).
    /// </summary>
    private static UInt128 Pow10ToUInt128(int exponent)
    {
        UInt128 result = UInt128.One;
        UInt128 ten = new UInt128(0, 10);

        for (int i = 0; i < exponent; i++)
        {
            result *= ten;
        }

        return result;
    }
}
