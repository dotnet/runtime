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
}
