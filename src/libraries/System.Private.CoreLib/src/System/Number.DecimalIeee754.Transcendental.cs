// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // Format-agnostic dispatch for the decimal IEEE 754 transcendental surface. Intel's reference
    // evaluates Decimal32 in binary64 (`double`) and Decimal64/Decimal128 in the software binary128
    // (`ux`) engine. The `double` branch below preserves that faithful Decimal32 path, but it is
    // presently a measured pessimization for every format: reconstructing the decimal from the `double`
    // result runs through `ConvertFloatToDecimalIeee754`, which computes the full Dragon4 exact
    // expansion of the result before rounding -- far more work than the engine's direct
    // `DiyFp128ToDecimal` rounding. Until that conversion is replaced with a bounded correctly-rounded
    // form, routing every format through the engine is both faster and (for Decimal32) more accurate,
    // so the gate is disabled. The gate stays a single JIT-time-foldable call so re-enabling it is a
    // one-line change once the conversion cost is addressed.

    private static bool DecimalIeee754UsesDouble<TValue>() => false;

    /// <summary>Computes <c>e^x</c>.</summary>
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Exp(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Exp2(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Exp10(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128ExpM1(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Exp2M1(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Exp10M1(argument));
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

    /// <summary>Computes <c>ln(x)</c>.</summary>
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Ln(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Log2(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Log10(argument));
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

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        // Guard the 1 + x domain in the binary128 engine (the double path gets this from IEEE): the
        // conversion error is always below the decimal granularity near x = -1, so 1 + x is exact here.
        DiyFp128 onePlus = default;
        DiyFp128AddSub(DiyFp128One, argument, UxAdd, new Span<DiyFp128>(ref onePlus));

        if ((onePlus._hi | onePlus._lo) == 0)
        {
            // logP1(-1) = -inf.
            return TDecimal.NegativeInfinity;
        }

        if (onePlus._sign != 0)
        {
            // logP1(x < -1) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DiyFp128 result128 = logBase switch
        {
            LogBase.Two => DiyFp128Log2P1(argument),
            LogBase.Ten => DiyFp128Log10P1(argument),
            _ => DiyFp128Ln1p(argument),
        };

        return DiyFp128ToDecimal<TDecimal, TValue>(result128);
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

    /// <summary>Computes <c>x^y</c>.</summary>
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
        DiyFp128 baseValue = DecimalToDiyFp128<TDecimal, TValue>(signed: false, dx.UnbiasedExponent, dx.Significand);
        DiyFp128 exponentValue = DecimalToDiyFp128<TDecimal, TValue>(dy.Signed, dy.UnbiasedExponent, dy.Significand);
        DiyFp128 magnitude = DiyFp128Pow(baseValue, exponentValue);

        if (dx.Signed && yIsOddInteger)
        {
            magnitude = new DiyFp128(UxSignBit, magnitude._exponent, magnitude._hi, magnitude._lo);
        }

        return DiyFp128ToDecimal<TDecimal, TValue>(magnitude);
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
        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Cbrt(argument));
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
        DiyFp128 xMagnitude = DecimalToDiyFp128<TDecimal, TValue>(signed: false, dx.UnbiasedExponent, dx.Significand);
        DiyFp128 yMagnitude = DecimalToDiyFp128<TDecimal, TValue>(signed: false, dy.UnbiasedExponent, dy.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Hypot(xMagnitude, yMagnitude));
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
        // a negative base only reaches here with an odd n, so it simply carries the sign. `n` is taken
        // through `long` so `int.MinValue`'s magnitude does not overflow.
        DiyFp128 one = new DiyFp128(0u, 1, 0x8000_0000_0000_0000, 0);
        DiyFp128 degree = DecimalToDiyFp128<TDecimal, TValue>(nNegative, 0, TValue.CreateTruncating(long.Abs(n)));
        DiyFp128Divide(one, degree, DiyFp128FullPrecision, out DiyFp128 exponent);

        DiyFp128 baseValue = DecimalToDiyFp128<TDecimal, TValue>(signed: false, dx.UnbiasedExponent, dx.Significand);
        DiyFp128 magnitude = DiyFp128Pow(baseValue, exponent);

        if (dx.Signed)
        {
            magnitude = new DiyFp128(UxSignBit, magnitude._exponent, magnitude._hi, magnitude._lo);
        }

        return DiyFp128ToDecimal<TDecimal, TValue>(magnitude);
    }

    // Builds the radian range-reduction argument, choosing the decimal-domain reducer when the decimal
    // is >= 1 and does not convert to binary128 exactly (where the binary reducer would work on a value
    // whose low digits -- the ones that determine x mod 2*pi -- were lost to rounding).
    private static DiyFp128TrigReduceArg MakeRadianReduceArg<TDecimal, TValue>(in DecodedDecimalIeee754<TValue> decoded)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        DiyFp128 value = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        UInt128 coefficient = UInt128.CreateTruncating(decoded.Significand);
        uint sign = decoded.Signed ? UxSignBit : 0u;
        bool useDecimal = (value._exponent >= 1) && !DiyFp128DecimalReduceExact(coefficient, decoded.UnbiasedExponent);
        return new DiyFp128TrigReduceArg(value, coefficient, decoded.UnbiasedExponent, sign, useDecimal);
    }

    /// <summary>Computes <c>sin(x)</c> (x in radians).</summary>
    internal static TValue SinDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // sin(+/-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // sin(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Sin(value));
        }

        DiyFp128TrigReduceArg argument = MakeRadianReduceArg<TDecimal, TValue>(decoded);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Sin(argument));
    }

    /// <summary>Computes <c>cos(x)</c> (x in radians).</summary>
    internal static TValue CosDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // cos(+/-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // cos(+/-0) = 1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Cos(value));
        }

        DiyFp128TrigReduceArg argument = MakeRadianReduceArg<TDecimal, TValue>(decoded);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Cos(argument));
    }

    /// <summary>Computes <c>tan(x)</c> (x in radians).</summary>
    internal static TValue TanDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // tan(+/-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // tan(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Tan(value));
        }

        DiyFp128TrigReduceArg argument = MakeRadianReduceArg<TDecimal, TValue>(decoded);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Tan(argument));
    }

    /// <summary>Computes <c>sin(x)</c> and <c>cos(x)</c> in a single evaluation (x in radians).</summary>
    internal static (TValue Sin, TValue Cos) SinCosDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            TValue nan = CanonicalizeIfNaN<TDecimal, TValue>(x);
            return (nan, nan);
        }

        if (TDecimal.IsInfinity(x))
        {
            // sin/cos(+/-inf) are invalid and produce the canonical quiet NaN.
            return (TDecimal.NaNMask, TDecimal.NaNMask);
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // sincos(+/-0) = (+/-0, 1).
            TValue sinZero = DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
            TValue cosZero = DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
            return (sinZero, cosZero);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            (double sinValue, double cosValue) = double.SinCos(value);
            return (ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(sinValue),
                    ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(cosValue));
        }

        DiyFp128TrigReduceArg argument = MakeRadianReduceArg<TDecimal, TValue>(decoded);
        DiyFp128SinCosPair(argument, out DiyFp128 sin, out DiyFp128 cos);
        return (DiyFp128ToDecimal<TDecimal, TValue>(sin), DiyFp128ToDecimal<TDecimal, TValue>(cos));
    }

    /// <summary>Computes <c>atan(x)</c>, the result in radians.</summary>
    internal static TValue AtanDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        bool signed = (x & TDecimal.SignMask) != TValue.Zero;

        if (TDecimal.IsInfinity(x))
        {
            // atan(+/-inf) = +/- pi/2.
            if (DecimalIeee754UsesDouble<TValue>())
            {
                return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.CopySign(double.Pi / 2.0, signed ? -1.0 : 1.0));
            }

            DiyFp128 halfPi = InvTrigConstants[2];
            halfPi._sign = signed ? UxSignBit : 0;
            return DiyFp128ToDecimal<TDecimal, TValue>(halfPi);
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // atan(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Atan(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Atan(argument));
    }

    /// <summary>Computes <c>asin(x)</c>, the result in radians.</summary>
    internal static TValue AsinDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // asin(+/-inf) is outside the [-1, 1] domain and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // asin(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.Asin(value);
            // A domain error (|x| > 1) canonicalizes to the positive quiet NaN.
            return double.IsNaN(result) ? TDecimal.NaNMask : ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        if (DiyFp128MagnitudeExceedsOne(argument))
        {
            return TDecimal.NaNMask;
        }
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Asin(argument));
    }

    /// <summary>Computes <c>acos(x)</c>, the result in radians.</summary>
    internal static TValue AcosDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // acos(+/-inf) is outside the [-1, 1] domain and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // acos(+/-0) = pi/2.
            if (DecimalIeee754UsesDouble<TValue>())
            {
                return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Pi / 2.0);
            }
            return DiyFp128ToDecimal<TDecimal, TValue>(InvTrigConstants[2]);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.Acos(value);
            // A domain error (|x| > 1) canonicalizes to the positive quiet NaN.
            return double.IsNaN(result) ? TDecimal.NaNMask : ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        if (DiyFp128MagnitudeExceedsOne(argument))
        {
            return TDecimal.NaNMask;
        }
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Acos(argument));
    }

    /// <summary>Computes <c>atan2(y, x)</c>, the angle of the vector (x, y) in radians.</summary>
    internal static TValue Atan2DecimalIeee754<TDecimal, TValue>(TValue y, TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(y))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(y);
        }

        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            // binary64 atan2 already follows IEEE for the signed-zero and infinity quadrant cases.
            double yValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(y);
            double xValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Atan2(yValue, xValue));
        }

        DecodedDecimalIeee754<TValue> decodedY = UnpackDecimalIeee754<TDecimal, TValue>(y);
        DecodedDecimalIeee754<TValue> decodedX = UnpackDecimalIeee754<TDecimal, TValue>(x);

        bool yInfinity = TDecimal.IsInfinity(y);
        bool xInfinity = TDecimal.IsInfinity(x);
        bool yZero = !yInfinity && TValue.IsZero(decodedY.Significand);
        bool xZero = !xInfinity && TValue.IsZero(decodedX.Significand);

        // Signed-zero and infinity quadrant cases resolve to a signed multiple of pi.
        if (yInfinity || xInfinity || yZero || xZero)
        {
            DiyFp128 magnitude;
            if (yInfinity)
            {
                // atan2(+/-inf, +/-inf) = +/-3pi/4 or +/-pi/4; atan2(+/-inf, finite) = +/-pi/2.
                magnitude = xInfinity ? (decodedX.Signed ? InvTrigConstants[3] : InvTrigConstants[1]) : InvTrigConstants[2];
            }
            else if (xInfinity)
            {
                // atan2(+/-finite, -inf) = +/-pi; atan2(+/-finite, +inf) = +/-0.
                magnitude = decodedX.Signed ? InvTrigConstants[4] : InvTrigConstants[0];
            }
            else if (yZero)
            {
                // atan2(+/-0, x<0 or -0) = +/-pi; atan2(+/-0, x>=0) = +/-0.
                magnitude = decodedX.Signed ? InvTrigConstants[4] : InvTrigConstants[0];
            }
            else
            {
                // xZero, finite non-zero y: atan2(+/-y, +/-0) = +/-pi/2.
                magnitude = InvTrigConstants[2];
            }

            magnitude._sign = decodedY.Signed ? UxSignBit : 0;
            return DiyFp128ToDecimal<TDecimal, TValue>(magnitude);
        }

        DiyFp128 argumentY = DecimalToDiyFp128<TDecimal, TValue>(decodedY.Signed, decodedY.UnbiasedExponent, decodedY.Significand);
        DiyFp128 argumentX = DecimalToDiyFp128<TDecimal, TValue>(decodedX.Signed, decodedX.UnbiasedExponent, decodedX.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Atan2(argumentY, argumentX, haveX: true));
    }

    /// <summary>Computes <c>sin(pi * x)</c>.</summary>
    internal static TValue SinPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // sinPi(+/-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // sinPi(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.SinPi(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128SinPi(argument));
    }

    /// <summary>Computes <c>cos(pi * x)</c>.</summary>
    internal static TValue CosPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // cosPi(+/-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // cosPi(+/-0) = 1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.CosPi(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128CosPi(argument));
    }

    /// <summary>Computes <c>tan(pi * x)</c>.</summary>
    internal static TValue TanPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // tanPi(+/-inf) is invalid and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // tanPi(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.TanPi(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        DiyFp128SinCosPi(argument, out DiyFp128 sin, out DiyFp128 cos);

        if (DiyFp128IsZero(cos))
        {
            // A half-integer argument is a pole; tanPi returns a signed infinity matching sinPi's sign.
            return (sin._sign != 0) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        DiyFp128Divide(sin, cos, DiyFp128FullPrecision, out DiyFp128 tangent);
        return DiyFp128ToDecimal<TDecimal, TValue>(tangent);
    }

    /// <summary>Computes <c>sin(pi * x)</c> and <c>cos(pi * x)</c> in a single evaluation.</summary>
    internal static (TValue SinPi, TValue CosPi) SinCosPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            TValue nan = CanonicalizeIfNaN<TDecimal, TValue>(x);
            return (nan, nan);
        }

        if (TDecimal.IsInfinity(x))
        {
            // sinPi/cosPi(+/-inf) are invalid and produce the canonical quiet NaN.
            return (TDecimal.NaNMask, TDecimal.NaNMask);
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // sinCosPi(+/-0) = (+/-0, 1).
            TValue sinZero = DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
            TValue cosZero = DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
            return (sinZero, cosZero);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            (double sinValue, double cosValue) = double.SinCosPi(value);
            return (ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(sinValue),
                    ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(cosValue));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        DiyFp128SinCosPi(argument, out DiyFp128 sin, out DiyFp128 cos);
        return (DiyFp128ToDecimal<TDecimal, TValue>(sin), DiyFp128ToDecimal<TDecimal, TValue>(cos));
    }

    /// <summary>Computes <c>atan(x) / pi</c>.</summary>
    internal static TValue AtanPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        bool signed = (x & TDecimal.SignMask) != TValue.Zero;

        if (TDecimal.IsInfinity(x))
        {
            // atanPi(+/-inf) = +/-1/2.
            if (DecimalIeee754UsesDouble<TValue>())
            {
                return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.CopySign(0.5, signed ? -1.0 : 1.0));
            }

            DiyFp128 half = PiFractionConstants[2];
            half._sign = signed ? UxSignBit : 0;
            return DiyFp128ToDecimal<TDecimal, TValue>(half);
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // atanPi(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.AtanPi(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        DiyFp128Divide(DiyFp128Atan(argument), InvTrigConstants[4], DiyFp128FullPrecision, out DiyFp128 result);
        return DiyFp128ToDecimal<TDecimal, TValue>(result);
    }

    /// <summary>Computes <c>asin(x) / pi</c>.</summary>
    internal static TValue AsinPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // asinPi(+/-inf) is outside the [-1, 1] domain and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // asinPi(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.AsinPi(value);
            // A domain error (|x| > 1) canonicalizes to the positive quiet NaN.
            return double.IsNaN(result) ? TDecimal.NaNMask : ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        if (DiyFp128MagnitudeExceedsOne(argument))
        {
            return TDecimal.NaNMask;
        }

        DiyFp128Divide(DiyFp128Asin(argument), InvTrigConstants[4], DiyFp128FullPrecision, out DiyFp128 quotient);
        return DiyFp128ToDecimal<TDecimal, TValue>(quotient);
    }

    /// <summary>Computes <c>acos(x) / pi</c>.</summary>
    internal static TValue AcosPiDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // acosPi(+/-inf) is outside the [-1, 1] domain and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // acosPi(+/-0) = 1/2.
            if (DecimalIeee754UsesDouble<TValue>())
            {
                return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(0.5);
            }
            return DiyFp128ToDecimal<TDecimal, TValue>(PiFractionConstants[2]);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.AcosPi(value);
            // A domain error (|x| > 1) canonicalizes to the positive quiet NaN.
            return double.IsNaN(result) ? TDecimal.NaNMask : ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        if (DiyFp128MagnitudeExceedsOne(argument))
        {
            return TDecimal.NaNMask;
        }

        DiyFp128Divide(DiyFp128Acos(argument), InvTrigConstants[4], DiyFp128FullPrecision, out DiyFp128 quotient);
        return DiyFp128ToDecimal<TDecimal, TValue>(quotient);
    }

    /// <summary>Computes <c>atan2(y, x) / pi</c>.</summary>
    internal static TValue Atan2PiDecimalIeee754<TDecimal, TValue>(TValue y, TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(y))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(y);
        }

        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            // binary64 atan2Pi already follows IEEE for the signed-zero and infinity quadrant cases.
            double yValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(y);
            double xValue = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Atan2Pi(yValue, xValue));
        }

        DecodedDecimalIeee754<TValue> decodedY = UnpackDecimalIeee754<TDecimal, TValue>(y);
        DecodedDecimalIeee754<TValue> decodedX = UnpackDecimalIeee754<TDecimal, TValue>(x);

        bool yInfinity = TDecimal.IsInfinity(y);
        bool xInfinity = TDecimal.IsInfinity(x);
        bool yZero = !yInfinity && TValue.IsZero(decodedY.Significand);
        bool xZero = !xInfinity && TValue.IsZero(decodedX.Significand);

        // Signed-zero and infinity quadrant cases resolve to a signed multiple of pi, i.e. a signed
        // fraction of a half turn (atan2 result divided by pi).
        if (yInfinity || xInfinity || yZero || xZero)
        {
            DiyFp128 magnitude;
            if (yInfinity)
            {
                // atan2Pi(+/-inf, +/-inf) = +/-3/4 or +/-1/4; atan2Pi(+/-inf, finite) = +/-1/2.
                magnitude = xInfinity ? (decodedX.Signed ? PiFractionConstants[3] : PiFractionConstants[1]) : PiFractionConstants[2];
            }
            else if (xInfinity)
            {
                // atan2Pi(+/-finite, -inf) = +/-1; atan2Pi(+/-finite, +inf) = +/-0.
                magnitude = decodedX.Signed ? PiFractionConstants[4] : PiFractionConstants[0];
            }
            else if (yZero)
            {
                // atan2Pi(+/-0, x<0 or -0) = +/-1; atan2Pi(+/-0, x>=0) = +/-0.
                magnitude = decodedX.Signed ? PiFractionConstants[4] : PiFractionConstants[0];
            }
            else
            {
                // xZero, finite non-zero y: atan2Pi(+/-y, +/-0) = +/-1/2.
                magnitude = PiFractionConstants[2];
            }

            magnitude._sign = decodedY.Signed ? UxSignBit : 0;
            return DiyFp128ToDecimal<TDecimal, TValue>(magnitude);
        }

        DiyFp128 argumentY = DecimalToDiyFp128<TDecimal, TValue>(decodedY.Signed, decodedY.UnbiasedExponent, decodedY.Significand);
        DiyFp128 argumentX = DecimalToDiyFp128<TDecimal, TValue>(decodedX.Signed, decodedX.UnbiasedExponent, decodedX.Significand);
        DiyFp128Divide(DiyFp128Atan2(argumentY, argumentX, haveX: true), InvTrigConstants[4], DiyFp128FullPrecision, out DiyFp128 result);
        return DiyFp128ToDecimal<TDecimal, TValue>(result);
    }

    /// <summary>Computes <c>sinh(x)</c>.</summary>
    internal static TValue SinhDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // sinh(+/-inf) = +/-inf.
            return TDecimal.IsNegative(x) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // sinh(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Sinh(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Sinh(argument));
    }

    /// <summary>Computes <c>cosh(x)</c>.</summary>
    internal static TValue CoshDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // cosh(+/-inf) = +inf.
            return TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // cosh(+/-0) = 1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Cosh(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Cosh(argument));
    }

    /// <summary>Computes <c>tanh(x)</c>.</summary>
    internal static TValue TanhDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // tanh(+/-inf) = +/-1.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(TDecimal.IsNegative(x), TValue.One, 0);
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // tanh(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Tanh(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Tanh(argument));
    }

    /// <summary>Computes <c>asinh(x)</c>.</summary>
    internal static TValue AsinhDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // asinh(+/-inf) = +/-inf.
            return TDecimal.IsNegative(x) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // asinh(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            return ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(double.Asinh(value));
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Asinh(argument));
    }

    /// <summary>Computes <c>acosh(x)</c>.</summary>
    internal static TValue AcoshDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // acosh(+inf) = +inf; acosh(-inf) is outside the [1, inf) domain and produces the canonical quiet NaN.
            return TDecimal.IsNegative(x) ? TDecimal.NaNMask : TDecimal.PositiveInfinity;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.Acosh(value);
            // A domain error (x < 1, including negatives and zero) canonicalizes to the positive quiet NaN.
            return double.IsNaN(result) ? TDecimal.NaNMask : ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        // acosh is defined for x >= 1; negatives, zero, and any magnitude below 1 are a domain error.
        if (decoded.Signed)
        {
            return TDecimal.NaNMask;
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        if (!DiyFp128MagnitudeExceedsOne(argument) && !DiyFp128MagnitudeIsOne(argument))
        {
            return TDecimal.NaNMask;
        }
        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Acosh(argument));
    }

    /// <summary>Computes <c>atanh(x)</c>.</summary>
    internal static TValue AtanhDecimalIeee754<TDecimal, TValue>(TValue x)
        where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
        where TValue : unmanaged, IBinaryInteger<TValue>
    {
        if (TDecimal.IsNaN(x))
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(x);
        }

        if (TDecimal.IsInfinity(x))
        {
            // atanh(+/-inf) is outside the [-1, 1] domain and produces the canonical quiet NaN.
            return TDecimal.NaNMask;
        }

        DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(x);

        if (TValue.IsZero(decoded.Significand))
        {
            // atanh(+/-0) = +/-0.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(decoded.Signed, TValue.Zero, 0);
        }

        if (DecimalIeee754UsesDouble<TValue>())
        {
            double value = ConvertDecimalIeee754ToFloat<TDecimal, TValue, double>(x);
            double result = double.Atanh(value);
            // |x| > 1 is a domain error (canonical quiet NaN); |x| == 1 is the +/-inf pole (both from double.Atanh).
            return double.IsNaN(result) ? TDecimal.NaNMask : ConvertFloatToDecimalIeee754<double, TDecimal, TValue>(result);
        }

        DiyFp128 argument = DecimalToDiyFp128<TDecimal, TValue>(decoded.Signed, decoded.UnbiasedExponent, decoded.Significand);

        if (DiyFp128MagnitudeIsOne(argument))
        {
            // atanh(+/-1) = +/-inf (pole).
            return decoded.Signed ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
        }

        if (DiyFp128MagnitudeExceedsOne(argument))
        {
            // |x| > 1 is a domain error.
            return TDecimal.NaNMask;
        }

        return DiyFp128ToDecimal<TDecimal, TValue>(DiyFp128Atanh(argument));
    }
}
