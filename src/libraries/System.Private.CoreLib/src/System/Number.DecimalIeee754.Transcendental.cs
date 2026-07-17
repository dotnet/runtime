// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // Format-agnostic dispatch for the decimal IEEE 754 transcendental surface. Following Intel's
    // reference, Decimal32 evaluates in binary64 (`double`) while Decimal64/Decimal128 evaluate in the
    // software binary128 (`ux`) engine so the wider formats keep their full precision. The `typeof`
    // guard is a JIT-time constant, so each concrete instantiation compiles to a single path.

    private static bool DecimalIeee754UsesDouble<TValue>() => typeof(TValue) == typeof(uint);

    /// <summary>Computes <c>e^x</c> (Intel routes Decimal32 through <c>double</c>, wider formats through
    /// the binary128 engine).</summary>
    internal static TValue ExpDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // exp(+inf) = +inf, exp(-inf) = +0.
            return TDecimal.IsNegative(x) ? TDecimal.Zero : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // exp(+/-0) = 1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Exp(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Exp(argument));
    }

    /// <summary>Computes <c>2^x</c>.</summary>
    internal static TValue Exp2DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // exp2(+inf) = +inf, exp2(-inf) = +0.
            return TDecimal.IsNegative(x) ? TDecimal.Zero : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // exp2(+/-0) = 1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Exp2(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Exp2(argument));
    }

    /// <summary>Computes <c>10^x</c>.</summary>
    internal static TValue Exp10DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // exp10(+inf) = +inf, exp10(-inf) = +0.
            return TDecimal.IsNegative(x) ? TDecimal.Zero : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // exp10(+/-0) = 1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Exp10(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Exp10(argument));
    }

    /// <summary>Computes <c>e^x - 1</c>.</summary>
    internal static TValue ExpM1DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // expm1(+inf) = +inf, expm1(-inf) = -1.
            return TDecimal.IsNegative(x)
                ? DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: true, TValue.One, 0)
                : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // expm1(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.ExpM1(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128ExpM1(argument));
    }

    /// <summary>Computes <c>2^x - 1</c>.</summary>
    internal static TValue Exp2M1DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // exp2m1(+inf) = +inf, exp2m1(-inf) = -1.
            return TDecimal.IsNegative(x)
                ? DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: true, TValue.One, 0)
                : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // exp2m1(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Exp2M1(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Exp2M1(argument));
    }

    /// <summary>Computes <c>10^x - 1</c>.</summary>
    internal static TValue Exp10M1DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // exp10m1(+inf) = +inf, exp10m1(-inf) = -1.
            return TDecimal.IsNegative(x)
                ? DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: true, TValue.One, 0)
                : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // exp10m1(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Exp10M1(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Exp10M1(argument));
    }

    /// <summary>Accurate binary64 <c>ln(1 + x)</c> (Kahan), avoiding the cancellation in the naive
    /// <c>Log(1 + x)</c> so the Decimal32 log1p family stays faithful to Intel's binary path.</summary>
    private static double DoubleLog1p(double x)
    {
        double u = 1.0 + x;

        if (u == 1.0)
        {
            return x;
        }

        return double.Log(u) * (x / (u - 1.0));
    }

    /// <summary>
    /// Returns whether the finite value <c>significand * 10^unbiasedExponent</c> has magnitude exactly
    /// one, testing in the decimal domain so it is exact for every format (a binary approximation would
    /// misclassify values a fraction of an ulp from one for Decimal128).
    /// </summary>
    private static bool DecimalIeee754MagnitudeIsOne<TDecimal, TValue>(int unbiasedExponent, TValue significand)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (unbiasedExponent > 0)
        {
            // significand >= 1 and a positive exponent give a value >= 10.
            return false;
        }

        if (unbiasedExponent == 0)
        {
            return significand == TValue.One;
        }

        int k = -unbiasedExponent;

        if (k > TDecimal.Precision - 1)
        {
            // 10^k exceeds the largest representable coefficient, so it cannot equal the significand.
            return false;
        }

        return significand == TDecimal.Power10(k);
    }

    /// <summary>Computes <c>ln(x)</c> (Intel routes Decimal32 through <c>double</c>, wider formats through
    /// the binary128 engine).</summary>
    internal static TValue LogDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // log(+inf) = +inf, log(-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.IsNegative(x) ? TDecimal.NaNMask : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // log(+/-0) = -inf.
            return TDecimal.NegativeInfinity;
        }

        if (decoded.Signed)
        {
            // log of a negative value is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Log(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Ln(argument));
    }

    /// <summary>Computes <c>log_newBase(x)</c> as <c>log(x) / log(newBase)</c>, mirroring the
    /// <c>double</c> special cases.</summary>
    internal static TValue LogDecimalIeee754<TDecimal, TValue>(TValue x, TValue newBase)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsNaN(newBase))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(newBase);
        }

        DecodedDecimalIeee754<TValue> decodedBase = UnpackDecimalIeee754<TDecimal, TValue>(newBase);
        bool baseIsOne = !TDecimal.IsInfinity(newBase) && !TDecimal.IsNegative(newBase)
                       && DecimalIeee754MagnitudeIsOne<TDecimal, TValue>(decodedBase.UnbiasedExponent, decodedBase.Significand);

        if (baseIsOne)
        {
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decodedX = UnpackDecimalIeee754<TDecimal, TValue>(x);
        bool xIsOne = !TDecimal.IsInfinity(x) && !TDecimal.IsNegative(x)
                    && DecimalIeee754MagnitudeIsOne<TDecimal, TValue>(decodedX.UnbiasedExponent, decodedX.Significand);
        bool baseIsZero = !TDecimal.IsInfinity(newBase) && TValue.IsZero(decodedBase.Significand);
        bool baseIsPositiveInfinity = TDecimal.IsInfinity(newBase) && !TDecimal.IsNegative(newBase);

        if (!xIsOne && (baseIsZero || baseIsPositiveInfinity))
        {
            return TDecimal.NaNMask;
        }

        TValue logX = LogDecimalIeee754<TDecimal, TValue>(x);
        TValue logBase = LogDecimalIeee754<TDecimal, TValue>(newBase);
        return DivideDecimalIeee754<TDecimal, TValue>(logX, logBase);
    }

    /// <summary>Computes <c>log2(x)</c>.</summary>
    internal static TValue Log2DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // log2(+inf) = +inf, log2(-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.IsNegative(x) ? TDecimal.NaNMask : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // log2(+/-0) = -inf.
            return TDecimal.NegativeInfinity;
        }

        if (decoded.Signed)
        {
            // log2 of a negative value is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Log2(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Log2(argument));
    }

    /// <summary>Computes <c>log10(x)</c>.</summary>
    internal static TValue Log10DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // log10(+inf) = +inf, log10(-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.IsNegative(x) ? TDecimal.NaNMask : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // log10(+/-0) = -inf.
            return TDecimal.NegativeInfinity;
        }

        if (decoded.Signed)
        {
            // log10 of a negative value is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Log10(value));
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Log10(argument));
    }

    /// <summary>Computes <c>ln(1 + x)</c>.</summary>
    internal static TValue LogP1DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        return Log1pDecimalIeee754<TDecimal, TValue>(x, LogBase.E);
    }

    /// <summary>Computes <c>log2(1 + x)</c>.</summary>
    internal static TValue Log2P1DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        return Log1pDecimalIeee754<TDecimal, TValue>(x, LogBase.Two);
    }

    /// <summary>Computes <c>log10(1 + x)</c>.</summary>
    internal static TValue Log10P1DecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        return Log1pDecimalIeee754<TDecimal, TValue>(x, LogBase.Ten);
    }

    private enum LogBase
    {
        E,
        Two,
        Ten,
    }

    /// <summary>Shared <c>log_b(1 + x)</c> dispatch for the log1p family.</summary>
    private static TValue Log1pDecimalIeee754<TDecimal, TValue>(TValue x, LogBase logBase)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // logP1(+inf) = +inf, logP1(-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.IsNegative(x) ? TDecimal.NaNMask : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // logP1(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = DoubleLog1p(value);

            result = logBase switch
            {
                LogBase.Two => result * 1.4426950408889634,  // 1 / ln(2)
                LogBase.Ten => result * 0.4342944819032518,  // 1 / ln(10)
                _ => result,
            };

            if (double.IsNaN(result))
            {
                // logP1(x < -1) is invalid; the double core yields a sign-carrying NaN, so canonicalize.
                return TDecimal.NaNMask;
            }

            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        // Guard the 1 + x domain in the binary128 engine (the double path gets this from IEEE): the
        // conversion error is always below the decimal granularity near x = -1, so 1 + x is exact here.
        Span<Float128> onePlus = stackalloc Float128[1];
        Float128AddSub(Float128One, argument, UxAdd, onePlus);

        if ((onePlus[0]._hi | onePlus[0]._lo) == 0)
        {
            // logP1(-1) = -inf.
            return TDecimal.NegativeInfinity;
        }

        if (onePlus[0]._sign != 0)
        {
            // logP1(x < -1) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        Float128 result128 = logBase switch
        {
            LogBase.Two => Float128Log2P1(argument),
            LogBase.Ten => Float128Log10P1(argument),
            _ => Float128Ln1p(argument),
        };

        return Float128ToDecimal<TDecimal, TValue>(result128);
    }

    /// <summary>
    /// Returns whether the finite value <c>significand * 10^unbiasedExponent</c> is an integer and, when
    /// it is, whether that integer is odd. Tested in the decimal domain so it is exact for every format.
    /// </summary>
    private static bool DecimalIeee754IsInteger<TDecimal, TValue>(int unbiasedExponent, TValue significand, out bool isOdd)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TValue.IsZero(significand))
        {
            // Zero is an even integer.
            isOdd = false;
            return true;
        }

        if (unbiasedExponent >= 0)
        {
            // A positive exponent multiplies in a factor of ten, so the value is odd only when the
            // exponent is zero and the significand itself is odd.
            isOdd = (unbiasedExponent == 0) && !TValue.IsZero(significand & TValue.One);
            return true;
        }

        int k = -unbiasedExponent;

        if (k >= TDecimal.Precision)
        {
            // 10^k exceeds the largest representable coefficient, so the value has a fractional part.
            isOdd = false;
            return false;
        }

        (TValue quotient, TValue remainder) = TValue.DivRem(significand, TDecimal.Power10(k));

        if (!TValue.IsZero(remainder))
        {
            isOdd = false;
            return false;
        }

        isOdd = !TValue.IsZero(quotient & TValue.One);
        return true;
    }

    /// <summary>
    /// Compares the magnitude of the finite, non-zero value <c>significand * 10^unbiasedExponent</c> to
    /// one, returning a negative value, zero, or a positive value. Tested in the decimal domain so the
    /// classification is exact for every format.
    /// </summary>
    private static int DecimalIeee754CompareMagnitudeToOne<TDecimal, TValue>(int unbiasedExponent, TValue significand)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (unbiasedExponent > 0)
        {
            // significand >= 1 scaled up by a positive power of ten is at least ten.
            return 1;
        }

        if (unbiasedExponent == 0)
        {
            return significand == TValue.One ? 0 : 1;
        }

        int k = -unbiasedExponent;

        if (k >= TDecimal.Precision)
        {
            // significand < 10^Precision <= 10^k, so the value is below one.
            return -1;
        }

        TValue power = TDecimal.Power10(k);

        if (significand == power)
        {
            return 0;
        }

        return significand > power ? 1 : -1;
    }

    /// <summary>Computes <c>x^y</c> (Intel routes Decimal32 through <c>double</c>, wider formats through
    /// the binary128 engine).</summary>
    internal static TValue PowDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        bool xNaN = TDecimal.IsNaN(x);
        bool yNaN = TDecimal.IsNaN(y);
        bool xInf = TDecimal.IsInfinity(x);
        bool yInf = TDecimal.IsInfinity(y);

        // The decoded fields are only read on the finite paths below.
        DecodedDecimalIeee754<TValue> dx = default;
        DecodedDecimalIeee754<TValue> dy = default;

        if (!xNaN && !xInf)
        {
            dx = UnpackDecimalIeee754<TDecimal, TValue>(x);
        }

        if (!yNaN && !yInf)
        {
            dy = UnpackDecimalIeee754<TDecimal, TValue>(y);
        }

        // pow(x, +/-0) = 1 for every x, including NaN.
        if (!yNaN && !yInf && TValue.IsZero(dy.Significand))
        {
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        // pow(+1, y) = 1 for every y, including NaN.
        if (!xNaN && !xInf && !dx.Signed
            && DecimalIeee754MagnitudeIsOne<TDecimal, TValue>(dx.UnbiasedExponent, dx.Significand))
        {
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (xNaN)
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (yNaN)
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(y);
        }

        bool yNegative = TDecimal.IsNegative(y);
        bool yIsOddInteger = false;
        bool yIsInteger = false;

        if (!yInf)
        {
            yIsInteger = DecimalIeee754IsInteger<TDecimal, TValue>(dy.UnbiasedExponent, dy.Significand, out yIsOddInteger);
        }

        // y is +/-Infinity: the result depends only on how |x| compares to one.
        if (yInf)
        {
            int cmp;

            if (xInf)
            {
                cmp = 1;
            }
            else if (TValue.IsZero(dx.Significand))
            {
                cmp = -1;
            }
            else
            {
                cmp = DecimalIeee754CompareMagnitudeToOne<TDecimal, TValue>(dx.UnbiasedExponent, dx.Significand);
            }

            if (cmp == 0)
            {
                // pow(+/-1, +/-inf) = 1.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
            }

            // |x| > 1 with +inf, or |x| < 1 with -inf, diverges to +inf; the complements go to +0.
            return ((cmp > 0) != yNegative)
                ? TDecimal.PositiveInfinity
                : DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.Zero, 0);
        }

        // x is +/-Infinity (y is finite and non-zero here).
        if (xInf)
        {
            bool resultNegative = TDecimal.IsNegative(x) && yIsOddInteger;

            if (!yNegative)
            {
                // pow(+/-inf, y > 0) = +/-inf.
                return resultNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            // pow(+/-inf, y < 0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultNegative, TValue.Zero, 0);
        }

        // x is +/-0 (y is finite and non-zero here).
        if (TValue.IsZero(dx.Significand))
        {
            bool resultNegative = dx.Signed && yIsOddInteger;

            if (!yNegative)
            {
                // pow(+/-0, y > 0) = +/-0.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultNegative, TValue.Zero, 0);
            }

            // pow(+/-0, y < 0) = +/-inf.
            return resultNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        // A finite negative base raised to a non-integer power is invalid.
        if (dx.Signed && !yIsInteger)
        {
            return TDecimal.NaNMask;
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double xValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double yValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(y);
            double result = double.Pow(xValue, yValue);

            if (double.IsNaN(result))
            {
                return TDecimal.NaNMask;
            }

            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        // The engine evaluates |x|^y; a negative base with an odd integer exponent carries the sign.
        Float128 baseValue = DecimalToFloat128<TDecimal, TValue>(signed: false, dx.UnbiasedExponent, dx.Significand);
        Float128 exponentValue = DecimalToFloat128<TDecimal, TValue>(dy.Signed, dy.UnbiasedExponent, dy.Significand);
        Float128 magnitude = Float128Pow(baseValue, exponentValue);

        if (dx.Signed && yIsOddInteger)
        {
            magnitude = new Float128(1u, magnitude._exponent, magnitude._hi, magnitude._lo);
        }

        return Float128ToDecimal<TDecimal, TValue>(magnitude);
    }

    /// <summary>Computes the cube root of <paramref name="x" />.</summary>
    internal static TValue CbrtDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // cbrt(+/-inf) = +/-inf.
            return x;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // cbrt(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Cbrt(value));
        }

        // The engine preserves the sign, so the cube root of a negative operand is handled directly.
        Float128 argument = DecimalToFloat128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Cbrt(argument));
    }

    /// <summary>Computes the hypotenuse (sqrt(<paramref name="x" />^2 + <paramref name="y" />^2)).</summary>
    internal static TValue HypotDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        // An infinite operand yields +inf even when the other operand is NaN.
        if (TDecimal.IsInfinity(x) || TDecimal.IsInfinity(y))
        {
            return TDecimal.PositiveInfinity;
        }

        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsNaN(y))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(y);
        }

        DecodedDecimalIeee754<TValue> dx = UnpackDecimalIeee754<TDecimal, TValue>(x);
        DecodedDecimalIeee754<TValue> dy = UnpackDecimalIeee754<TDecimal, TValue>(y);

        bool xZero = TValue.IsZero(dx.Significand);
        bool yZero = TValue.IsZero(dy.Significand);

        // hypot(x, +/-0) = |x| and hypot(+/-0, y) = |y| (which also covers hypot(+/-0, +/-0) = +0).
        if (yZero)
        {
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, dx.Significand, dx.UnbiasedExponent);
        }

        if (xZero)
        {
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, dy.Significand, dy.UnbiasedExponent);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double xValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double yValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(y);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Hypot(xValue, yValue));
        }

        // The result depends only on the magnitudes; the engine squares both operands.
        Float128 xMagnitude = DecimalToFloat128<TDecimal, TValue>(signed: false, dx.UnbiasedExponent, dx.Significand);
        Float128 yMagnitude = DecimalToFloat128<TDecimal, TValue>(signed: false, dy.UnbiasedExponent, dy.Significand);
        return Float128ToDecimal<TDecimal, TValue>(Float128Hypot(xMagnitude, yMagnitude));
    }

    /// <summary>Computes the <paramref name="n" />th root of <paramref name="x" />.</summary>
    internal static TValue RootNDecimalIeee754<TDecimal, TValue>(TValue x, int n)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        // rootn(x, 0) = NaN for every x, matching the binary surface.
        if (n == 0)
        {
            return TDecimal.NaNMask;
        }

        // rootn(x, 3) is the cube root, which handles a negative operand directly.
        if (n == 3)
        {
            return CbrtDecimalIeee754<TDecimal, TValue>(x);
        }

        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        bool nNegative = n < 0;
        bool nOdd = int.IsOddInteger(n);

        if (TDecimal.IsInfinity(x))
        {
            bool xNegative = TDecimal.IsNegative(x);

            // rootn(-inf, n) is real only for an odd n; the even case is invalid.
            if (xNegative && !nOdd)
            {
                return TDecimal.NaNMask;
            }

            if (!nNegative)
            {
                // rootn(+/-inf, n > 0) = +/-inf.
                return xNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            // rootn(+/-inf, n < 0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(xNegative, TValue.Zero, 0);
        }

        DecodedDecimalIeee754<TValue> dx = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(dx.Significand))
        {
            // rootn(+/-0, n): an odd n carries the sign, an even n normalizes to positive.
            bool resultNegative = dx.Signed && nOdd;

            if (!nNegative)
            {
                // rootn(+/-0, n > 0) = +/-0.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultNegative, TValue.Zero, 0);
            }

            // rootn(+/-0, n < 0) = +/-inf.
            return resultNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        // A finite negative base has a real root only for an odd n.
        if (dx.Signed && !nOdd)
        {
            return TDecimal.NaNMask;
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.RootN(value, n);

            if (double.IsNaN(result))
            {
                return TDecimal.NaNMask;
            }

            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        // The engine evaluates |x|^(1/n) with the reciprocal formed exactly in the binary128 domain;
        // a negative base only reaches here with an odd n, so it simply carries the sign.
        Float128 one = new Float128(0u, 1, 0x8000_0000_0000_0000, 0);
        Float128 degree = DecimalToFloat128<TDecimal, TValue>(nNegative, 0, TValue.CreateTruncating(int.Abs(n)));
        Float128Divide(one, degree, Float128FullPrecision, out Float128 exponent);

        Float128 baseValue = DecimalToFloat128<TDecimal, TValue>(signed: false, dx.UnbiasedExponent, dx.Significand);
        Float128 magnitude = Float128Pow(baseValue, exponent);

        if (dx.Signed)
        {
            magnitude = new Float128(1u, magnitude._exponent, magnitude._hi, magnitude._lo);
        }

        return Float128ToDecimal<TDecimal, TValue>(magnitude);
    }
}
