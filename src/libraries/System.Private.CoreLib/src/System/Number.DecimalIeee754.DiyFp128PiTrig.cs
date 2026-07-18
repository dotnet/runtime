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
    // integer/fractional split is exact in binary128 for every non-integer decimal (its magnitude is
    // below 2^113), so the pi-scaled reduction avoids the large-argument cancellation that motivates a
    // dedicated *Pi routine. The inverse variants are the radian result divided by pi.

    private static readonly DiyFp128 UxQuarter = new DiyFp128(0, -1, UxMsb, 0);
    private static readonly DiyFp128 UxHalf = new DiyFp128(0, 0, UxMsb, 0);
    private static readonly DiyFp128 UxThreeQuarter = new DiyFp128(0, 0, 0xC000000000000000, 0);
    private static readonly DiyFp128 UxOne = new DiyFp128(0, 1, UxMsb, 0);

    // 0, 1/4, 1/2, 3/4, 1 -- InvTrigConstants (0, pi/4, pi/2, 3pi/4, pi) divided by pi, for the exact
    // signed-zero/infinity quadrant results of the inverse *Pi variants.
    private static readonly DiyFp128[] PiFractionConstants = new DiyFp128[]
    {
        new DiyFp128(0, UxZeroExponent, 0, 0), // 0
        UxQuarter,                             // 1/4
        UxHalf,                                // 1/2
        UxThreeQuarter,                        // 3/4
        UxOne,                                 // 1
    };

    private static bool DiyFp128IsZero(in DiyFp128 value) => (value._hi | value._lo) == 0;

    // Compares the magnitudes of two normalized non-negative DiyFp128 values (returns a <= b).
    private static bool DiyFp128MagnitudeLessOrEqual(in DiyFp128 a, in DiyFp128 b)
    {
        if (DiyFp128IsZero(a))
        {
            return true;
        }

        if (DiyFp128IsZero(b))
        {
            return false;
        }

        if (a._exponent != b._exponent)
        {
            return a._exponent < b._exponent;
        }

        if (a._hi != b._hi)
        {
            return a._hi < b._hi;
        }

        return a._lo <= b._lo;
    }

    // Splits |value| (assumed normalized) into its fractional part in [0, 1); reports whether floor(|value|)
    // is odd and whether the value is an exact integer.
    private static DiyFp128 DiyFp128SplitInteger(in DiyFp128 value, out bool oddInteger, out bool isInteger)
    {
        if (DiyFp128IsZero(value))
        {
            oddInteger = false;
            isInteger = true;
            return default;
        }

        int exponent = value._exponent;

        if (exponent <= 0)
        {
            // |value| < 1, so the whole value is fractional and floor is 0 (even).
            oddInteger = false;
            isInteger = false;
            DiyFp128 fraction = value;
            fraction._sign = 0;
            return fraction;
        }

        if (exponent >= 128)
        {
            // The 128-bit significand has no fractional bits; the value is an even integer (a power-of-two scale).
            oddInteger = false;
            isInteger = true;
            return default;
        }

        UInt128 significand = ((UInt128)value._hi << 64) | value._lo;
        int shift = 128 - exponent;
        UInt128 integer = significand >> shift;
        UInt128 fractionBits = significand & ((UInt128.One << shift) - UInt128.One);
        oddInteger = (integer & UInt128.One) != UInt128.Zero;

        if (fractionBits == UInt128.Zero)
        {
            isInteger = true;
            return default;
        }

        isInteger = false;
        DiyFp128 result = new DiyFp128(0, exponent, (ulong)(fractionBits >> 64), (ulong)fractionBits);
        DiyFp128Normalize(ref result);
        return result;
    }

    private static DiyFp128 DiyFp128Product(in DiyFp128 a, in DiyFp128 b)
    {
        DiyFp128 x = a;
        DiyFp128 y = b;
        DiyFp128Multiply(ref x, ref y, out DiyFp128 z);
        DiyFp128Normalize(ref z);
        return z;
    }

    // reduced (in [0, 1/4]) * pi -> a small angle in [0, pi/4].
    private static DiyFp128 DiyFp128TimesPi(in DiyFp128 reduced) => DiyFp128Product(reduced, InvTrigConstants[4]);

    private static DiyFp128 DiyFp128Difference(in DiyFp128 a, in DiyFp128 b)
    {
        Span<DiyFp128> result = stackalloc DiyFp128[1];
        DiyFp128AddSub(a, b, UxSub, result);
        return result[0];
    }

    private static DiyFp128 DiyFp128WithSignFlipped(DiyFp128 value, uint sign)
    {
        value._sign ^= sign;
        return value;
    }

    /// <summary>Computes <c>sin(pi * x)</c> for a finite non-zero binary128 argument.</summary>
    private static DiyFp128 DiyFp128SinPi(in DiyFp128 x)
    {
        DiyFp128 magnitude = x;
        magnitude._sign = 0;
        DiyFp128 fraction = DiyFp128SplitInteger(magnitude, out bool oddInteger, out bool isInteger);

        if (isInteger)
        {
            // sin(pi * n) = +/-0, keeping the sign of x.
            return new DiyFp128(x._sign, UxZeroExponent, 0, 0);
        }

        uint sign = x._sign ^ (oddInteger ? UxSignBit : 0u);
        DiyFp128 result;

        if (DiyFp128MagnitudeLessOrEqual(fraction, UxQuarter))
        {
            result = DiyFp128Sin(DiyFp128TimesPi(fraction));
        }
        else if (DiyFp128MagnitudeLessOrEqual(fraction, UxHalf))
        {
            result = DiyFp128Cos(DiyFp128TimesPi(DiyFp128Difference(UxHalf, fraction)));
        }
        else if (DiyFp128MagnitudeLessOrEqual(fraction, UxThreeQuarter))
        {
            result = DiyFp128Cos(DiyFp128TimesPi(DiyFp128Difference(fraction, UxHalf)));
        }
        else
        {
            result = DiyFp128Sin(DiyFp128TimesPi(DiyFp128Difference(UxOne, fraction)));
        }

        return DiyFp128WithSignFlipped(result, sign);
    }

    /// <summary>Computes <c>cos(pi * x)</c> for a finite non-zero binary128 argument.</summary>
    private static DiyFp128 DiyFp128CosPi(in DiyFp128 x)
    {
        DiyFp128 magnitude = x;
        magnitude._sign = 0;
        DiyFp128 fraction = DiyFp128SplitInteger(magnitude, out bool oddInteger, out bool isInteger);

        if (isInteger)
        {
            // cos(pi * n) = (-1)^n.
            return DiyFp128WithSignFlipped(UxOne, oddInteger ? UxSignBit : 0u);
        }

        uint sign = oddInteger ? UxSignBit : 0u;
        DiyFp128 result;

        if (DiyFp128MagnitudeLessOrEqual(fraction, UxQuarter))
        {
            result = DiyFp128Cos(DiyFp128TimesPi(fraction));
        }
        else if (DiyFp128MagnitudeLessOrEqual(fraction, UxHalf))
        {
            result = DiyFp128Sin(DiyFp128TimesPi(DiyFp128Difference(UxHalf, fraction)));
        }
        else if (DiyFp128MagnitudeLessOrEqual(fraction, UxThreeQuarter))
        {
            result = DiyFp128WithSignFlipped(DiyFp128Sin(DiyFp128TimesPi(DiyFp128Difference(fraction, UxHalf))), UxSignBit);
        }
        else
        {
            result = DiyFp128WithSignFlipped(DiyFp128Cos(DiyFp128TimesPi(DiyFp128Difference(UxOne, fraction))), UxSignBit);
        }

        return DiyFp128WithSignFlipped(result, sign);
    }

    /// <summary>Computes <c>sin(pi * x)</c> and <c>cos(pi * x)</c> for a finite non-zero binary128 argument.</summary>
    private static void DiyFp128SinCosPi(in DiyFp128 x, out DiyFp128 sin, out DiyFp128 cos)
    {
        sin = DiyFp128SinPi(x);
        cos = DiyFp128CosPi(x);
    }
}
