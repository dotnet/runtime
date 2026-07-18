// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the logarithm evaluation from the Intel(R) Decimal Floating-Point Math
    // Library, specifically `UX_LOG` and `F_LOG1P` from `dpml_ux_log.c`, the polynomial evaluator
    // `EVALUATE_RATIONAL` (in its `SQUARE_TERM | POST_MULTIPLY` form) from `dpml_ux_ops_64.c`, and the
    // log2 constant table from `dpml_log_x.h`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // The evaluation runs entirely in the software binary128 engine, so Decimal64/Decimal128 obtain the
    // full ~34-digit accuracy Intel's reference does. A single log2 polynomial serves all bases; ln,
    // log10 apply a trailing multiply by ln2 / log10(2) while log2 uses the polynomial directly.

    private const int UxAddSub = 2; // ADD_SUB dual-output flag: writes sum to result[0], difference to result[1].

    // log2 fixed-point coefficients (dpml_log_x.h), degree 17, trailing exponent 2.
    private const int Log2Degree = 17;
    private const int Log2TrailingExponent = 2;

    private static readonly DiyFp128FixedCoefficient[] Log2Coefficients =
    [
        new(0x271eee7d56dac09b, 0x06cc4d0d2a1966ce),
        new(0x1ba3468b6f81e43d, 0x056711399caac22d),
        new(0xf7ca0b25a20f818f, 0x05f8b50232b2540a),
        new(0x7adfa93e3f28f8fe, 0x065df4e9cb8d055c),
        new(0xce5c4ea3f7891d9d, 0x06d6e7804c87d854),
        new(0xe820f58a9feb8d1e, 0x0762f8145c44b19a),
        new(0xe8c1f4c0f720bb2c, 0x080766bf41dad530),
        new(0x80535f751df3812c, 0x08cb27637d59049f),
        new(0x96e6a1d72c2ac1eb, 0x09b81e0fa68ac838),
        new(0x8c3b0c947df70971, 0x0adcd64dba1f8070),
        new(0xa70095aa11d8754e, 0x0c4f9d8b4a67ff05),
        new(0x64f2a61e05f3cefe, 0x0e347ab4698bb00e),
        new(0x572dc64d3936b199, 0x10c9a84994022d28),
        new(0x6a80ddd58c4ac6fe, 0x1484b13d7c02a8f8),
        new(0x645c921fa5c4559c, 0x1a61762a7aded93f),
        new(0x594e6629ae4a965a, 0x24eed8a1df37fcf2),
        new(0x3f82aa45785f1acb, 0x3d8e13b87407fae9),
        new(0xbe87fed0691d3e89, 0xb8aa3b295c17f0bb),
    ];

    // 1/sqrt(2) fraction MSD and the I_RECIP_SQRT_2 / I_SQRT_2 range constants (dpml_log_x.h).
    private const ulong LogOneOverSqrt2 = 0xb504f333f9de6484;
    private const ulong LogIRecipSqrt2 = 0x5a827999fcef3242;
    private const ulong LogISqrt2 = 0xb504f333f9de6484;

    // Unpacked ln2, log10(2), and 2.0 (dpml_log_x.h).
    private static DiyFp128 LogLn2 => new DiyFp128(0, 0, 0xb17217f7d1cf79ab, 0xc9e3b39803f2f6af);
    private static DiyFp128 LogLog10Of2 => new DiyFp128(0, -1, 0x9a209a84fbcff798, 0x8f8959ac0b7c9178);
    private static DiyFp128 LogTwo => new DiyFp128(0, 2, 0x8000000000000000, 0);

    /// <summary>Converts a signed integer to an unpacked binary128 value (Intel's <c>WORD_TO_UX</c>).</summary>
    private static DiyFp128 DiyFp128FromWord(long n)
    {
        if (n == 0)
        {
            return new DiyFp128(0, UxZeroExponent, 0, 0);
        }

        uint sign = 0;
        ulong magnitude;

        if (n < 0)
        {
            sign = UxSignBit;
            magnitude = (ulong)(-n);
        }
        else
        {
            magnitude = (ulong)n;
        }

        int shift = (int)ulong.LeadingZeroCount(magnitude);
        return new DiyFp128(sign, 64 - shift, magnitude << shift, 0);
    }

    /// <summary>
    /// Evaluates the log polynomial (Intel's <c>EVALUATE_RATIONAL</c> in its <c>SQUARE_TERM |
    /// POST_MULTIPLY</c> form): <c>p(arg^2) * arg</c>, then applies the trailing exponent.
    /// </summary>
    private static void DiyFp128EvaluateLogPolynomial(scoped in DiyFp128 arg, ReadOnlySpan<DiyFp128FixedCoefficient> coefficients, int degree, int trailingExponent, out DiyFp128 result)
    {
        DiyFp128 a = arg;
        DiyFp128Multiply(ref a, ref a, out DiyFp128 argumentSquared);
        DiyFp128Normalize(ref argumentSquared);

        long shift = -(long)degree * argumentSquared._exponent;
        DiyFp128EvaluatePositivePolynomial(argumentSquared, shift, coefficients, 0, degree, out result);

        DiyFp128 original = arg;
        DiyFp128Multiply(ref original, ref result, out result);
        result._exponent += trailingExponent;
    }

    /// <summary>
    /// Computes <c>log_b(arg)</c> for a positive finite <paramref name="arg"/> (Intel's <c>UX_LOG</c>).
    /// <paramref name="scaleValid"/> selects the base: log2 uses the polynomial directly (no scale
    /// multiply), while ln and log10 post-multiply by ln2 / log10(2).
    /// </summary>
    private static DiyFp128 DiyFp128Log(DiyFp128 arg, bool scaleValid, scoped in DiyFp128 scale)
    {
        long m = arg._exponent;

        if (arg._hi <= LogOneOverSqrt2)
        {
            m--;
        }

        arg._exponent -= (int)m; // g in [1/sqrt2, sqrt2)

        Span<DiyFp128> tmp = stackalloc DiyFp128[2];
        DiyFp128AddSub(arg, DiyFp128One, UxAddSub | UxMagnitudeOnly, tmp); // tmp[0] = g + 1, tmp[1] = g - 1

        DiyFp128Divide(tmp[1], tmp[0], DiyFp128FullPrecision, out DiyFp128 z);
        DiyFp128EvaluateLogPolynomial(z, Log2Coefficients, Log2Degree, Log2TrailingExponent, out DiyFp128 poly);

        DiyFp128 result = DiyFp128FromWord(m);
        Span<DiyFp128> sum = stackalloc DiyFp128[1];
        DiyFp128AddSub(result, poly, UxAdd | UxNoNormalization, sum);
        result = sum[0];

        if (scaleValid)
        {
            DiyFp128 s = scale;
            DiyFp128Multiply(ref result, ref s, out result);
        }

        return result;
    }

    private static DiyFp128 DiyFp128Ln(scoped in DiyFp128 arg) => DiyFp128Log(arg, scaleValid: true, LogLn2);
    private static DiyFp128 DiyFp128Log2(scoped in DiyFp128 arg) => DiyFp128Log(arg, scaleValid: false, default);
    private static DiyFp128 DiyFp128Log10(scoped in DiyFp128 arg) => DiyFp128Log(arg, scaleValid: true, LogLog10Of2);

    /// <summary>
    /// Evaluates the natural log of the value whose reduced ratio is <paramref name="w"/> (Intel's
    /// <c>UX_LOG_POLY</c>): the log2 polynomial <c>w*p(w^2)</c> post-multiplied by <c>ln2</c>. Callers pass
    /// a carefully formed <c>w</c> to avoid the loss of significance in <c>UX_LOG</c>'s <c>(g-1)/(g+1)</c>.
    /// </summary>
    private static DiyFp128 DiyFp128LogPoly(scoped in DiyFp128 w)
    {
        DiyFp128EvaluateLogPolynomial(w, Log2Coefficients, Log2Degree, Log2TrailingExponent, out DiyFp128 result);
        DiyFp128 ln2 = LogLn2;
        DiyFp128Multiply(ref result, ref ln2, out result);
        return result;
    }

    /// <summary>
    /// Computes <c>log_b(1 + arg)</c> for a finite <paramref name="arg"/> (Intel's <c>F_LOG1P</c>). The
    /// small path evaluates the polynomial at <c>arg / (2 + arg)</c> to avoid the loss of significance in
    /// forming <c>1 + arg</c>; the big path forms <c>1 + arg</c> and calls <see cref="DiyFp128Log"/>.
    /// <paramref name="scaleValid"/> selects the base exactly as in <see cref="DiyFp128Log"/>.
    /// </summary>
    private static DiyFp128 DiyFp128Log1p(DiyFp128 arg, bool scaleValid, scoped in DiyFp128 scale)
    {
        int exponent = arg._exponent;
        uint sign = arg._sign;

        bool small;

        if (exponent >= 0)
        {
            small = false;
        }
        else if (exponent <= -2)
        {
            small = true;
        }
        else
        {
            ulong g = arg._hi >> 2;
            g = (sign != 0) ? (0UL - g) : g;
            g += UxMsb;
            small = (g - LogIRecipSqrt2) < (LogISqrt2 - LogIRecipSqrt2);
        }

        if (small)
        {
            Span<DiyFp128> t = stackalloc DiyFp128[1];
            DiyFp128AddSub(LogTwo, arg, UxAdd, t);
            DiyFp128Divide(arg, t[0], DiyFp128FullPrecision, out DiyFp128 reduced);
            DiyFp128EvaluateLogPolynomial(reduced, Log2Coefficients, Log2Degree, Log2TrailingExponent, out DiyFp128 result);

            if (scaleValid)
            {
                DiyFp128 s = scale;
                DiyFp128Multiply(ref result, ref s, out result);
            }

            return result;
        }
        else
        {
            Span<DiyFp128> t = stackalloc DiyFp128[1];
            DiyFp128AddSub(DiyFp128One, arg, UxAdd, t);
            return DiyFp128Log(t[0], scaleValid, scale);
        }
    }

    private static DiyFp128 DiyFp128Ln1p(scoped in DiyFp128 arg) => DiyFp128Log1p(arg, scaleValid: true, LogLn2);
    private static DiyFp128 DiyFp128Log2P1(scoped in DiyFp128 arg) => DiyFp128Log1p(arg, scaleValid: false, default);
    private static DiyFp128 DiyFp128Log10P1(scoped in DiyFp128 arg) => DiyFp128Log1p(arg, scaleValid: true, LogLog10Of2);
}
