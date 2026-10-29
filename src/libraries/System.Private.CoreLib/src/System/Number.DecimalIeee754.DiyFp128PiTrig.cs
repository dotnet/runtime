// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // The forward *Pi variants (sinPi/cosPi/tanPi) evaluate on top of the validated ux radian engine.
    //
    // This mirrors the interval-reduction structure of the binary64 double.SinPi/CosPi (based on
    // `sinpi`/`cospi`/`tanpi` from amd/aocl-libm-ose, BSD 3-Clause; see THIRD-PARTY-NOTICES.TXT): the
    // magnitude is split exactly into an integer and a fractional part in [0, 1), the fraction folds by
    // quarter turns, and a small ux sin/cos of (reduced * pi) with reduced in [0, 1/4] is evaluated. The
    // reduction is performed in decimal before conversion so small distances from integers and
    // half-integers are preserved. The inverse variants are the radian result divided by pi.

    // 0, 1/4, 1/2, 3/4, 1 -- InvTrigConstants (0, pi/4, pi/2, 3pi/4, pi) divided by pi, for the exact
    // signed-zero/infinity quadrant results of the inverse *Pi variants.
    private static DiyFp128 GetPiFractionConstant(int index)
    {
        ReadOnlySpan<int> exponents = [UxZeroExponent, -1, 0, 0, 1];
        ReadOnlySpan<ulong> significands = [0, UxMsb, UxMsb, 0xC000000000000000, UxMsb];

        return new DiyFp128(0, exponents[index], significands[index], 0);
    }

    private static bool DiyFp128IsZero(in DiyFp128 value) => (value._hi | value._lo) == 0;

    private static DiyFp128 ReduceDecimalIeee754Pi<TDecimal, TValue>(
        in DecodedDecimalIeee754<TValue> decoded, out int octant)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        int exponent = decoded.UnbiasedExponent;
        TValue coefficient = decoded.Significand;
        octant = 0;

        if (exponent >= 0)
        {
            octant = ((exponent == 0) && TValue.IsOddInteger(coefficient)) ? 4 : 0;
            return new DiyFp128(decoded.Signed ? UxSignBit : 0, UxZeroExponent, 0, 0);
        }

        if (-exponent > TDecimal.Precision)
        {
            // |x| < 1/10: no reduction is needed, and 10^-exponent need not fit in TValue.
            return DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, exponent, coefficient);
        }

        int scale = -exponent;
        TValue one = (scale == TDecimal.Precision) ? TDecimal.MaxSignificand + TValue.One : TDecimal.Power10(scale);
        TValue integer = coefficient / one;
        TValue fraction = coefficient - (integer * one);
        octant = TValue.IsOddInteger(integer) ? 4 : 0;

        TValue fourFraction = fraction << 2;
        if (fourFraction <= one)
        {
            coefficient = fraction;
        }
        else if ((fraction << 1) <= one)
        {
            octant += 1;
            coefficient = (one >> 1) - fraction;
        }
        else if (fourFraction <= (one + (one << 1)))
        {
            octant += 2;
            coefficient = fraction - (one >> 1);
        }
        else
        {
            octant += 3;
            coefficient = one - fraction;
        }

        return TValue.IsZero(coefficient)
            ? new DiyFp128(decoded.Signed ? UxSignBit : 0, UxZeroExponent, 0, 0)
            : DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, exponent, coefficient);
    }

    // reduced (in [0, 1/4]) * pi -> a small angle in [0, pi/4].
    private static DiyFp128 DiyFp128TimesPi(DiyFp128 reduced)
    {
        reduced._sign = 0;
        DiyFp128 pi = GetInvTrigConstant(4);
        DiyFp128Multiply(ref reduced, ref pi, out DiyFp128 result);
        DiyFp128Normalize(ref result);
        return result;
    }

    private static DiyFp128 DiyFp128Difference(in DiyFp128 a, in DiyFp128 b)
    {
        DiyFp128 result = default;
        DiyFp128AddSub(a, b, UxSub, new Span<DiyFp128>(ref result));
        return result;
    }

    private static DiyFp128 DiyFp128EvaluatePiTrig(in DiyFp128 reduced, bool cosine)
    {
        // Decimal reduction already bounds the angle to [0, pi/4], so no radian reduction is needed.
        DiyFp128 angle = DiyFp128TimesPi(reduced);
        Span<DiyFp128> results = [default, default];
        DiyFp128EvaluateRational(angle, cosine ? default : TrigSinCoefficients, 1,
            cosine ? TrigCosCoefficients : default, 1, TrigSinCosDegree,
            TrigSkip | (cosine ? TrigCosPolyFlags : TrigSinPolyFlags), results);
        return results[0];
    }

    /// <summary>Computes <c>sin(pi * x)</c> from its decimal-reduced argument and octant.</summary>
    private static DiyFp128 DiyFp128SinPi(in DiyFp128 reduced, int octant)
    {
        bool useCosine = (octant & 3) is 1 or 2;
        DiyFp128 result;

        if (DiyFp128IsZero(reduced))
        {
            if (!useCosine)
            {
                // sin(pi * n) = +/-0, keeping the sign of x.
                return reduced;
            }

            result = DiyFp128One;
        }
        else
        {
            result = DiyFp128EvaluatePiTrig(reduced, useCosine);
        }

        result._sign = reduced._sign ^ (((octant & 4) != 0) ? UxSignBit : 0u);
        return result;
    }

    /// <summary>Computes <c>cos(pi * x)</c> from its decimal-reduced argument and octant.</summary>
    private static DiyFp128 DiyFp128CosPi(in DiyFp128 reduced, int octant)
    {
        bool useCosine = (octant & 3) is not (1 or 2);
        DiyFp128 result = DiyFp128IsZero(reduced)
            ? (useCosine ? DiyFp128One : new DiyFp128(0, UxZeroExponent, 0, 0))
            : DiyFp128EvaluatePiTrig(reduced, useCosine);

        // cos(pi * (n + 1/2)) is exactly +0; the reduced result is +0 and must not take the odd-integer sign.
        if (DiyFp128IsZero(result))
        {
            return new DiyFp128(0, UxZeroExponent, 0, 0);
        }

        result._sign = (((octant + 2) & 4) != 0) ? UxSignBit : 0u;
        return result;
    }

    /// <summary>Computes <c>sin(pi * x)</c> and <c>cos(pi * x)</c> from their decimal-reduced argument and octant.</summary>
    private static void DiyFp128SinCosPi(in DiyFp128 reduced, int octant, out DiyFp128 sin, out DiyFp128 cos)
    {
        if (DiyFp128IsZero(reduced))
        {
            sin = reduced;
            cos = DiyFp128One;
        }
        else
        {
            DiyFp128 angle = DiyFp128TimesPi(reduced);
            Span<DiyFp128> results = [default, default];
            DiyFp128EvaluateRational(angle, TrigSinCoefficients, 1, TrigCosCoefficients, 1, TrigSinCosDegree,
                TrigSinPolyFlags | TrigCosPolyFlags | TrigNoDivide, results);
            sin = results[0];
            cos = results[1];
        }

        if ((octant & 3) is 1 or 2)
        {
            (sin, cos) = (cos, sin);
        }

        // Integer sine zeros retain the input sign; half-integer cosine zeros are always positive.
        sin._sign = reduced._sign ^ ((!DiyFp128IsZero(sin) && ((octant & 4) != 0)) ? UxSignBit : 0u);
        cos._sign = (!DiyFp128IsZero(cos) && (((octant + 2) & 4) != 0)) ? UxSignBit : 0u;
    }
}
