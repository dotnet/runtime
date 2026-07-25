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
        new(0x271EEE7D56DAC09B, 0x06CC4D0D2A1966CE),
        new(0x1BA3468B6F81E43D, 0x056711399CAAC22D),
        new(0xF7CA0B25A20F818F, 0x05F8B50232B2540A),
        new(0x7ADFA93E3F28F8FE, 0x065DF4E9CB8D055C),
        new(0xCE5C4EA3F7891D9D, 0x06D6E7804C87D854),
        new(0xE820F58A9FEB8D1E, 0x0762F8145C44B19A),
        new(0xE8C1F4C0F720BB2C, 0x080766BF41DAD530),
        new(0x80535F751DF3812C, 0x08CB27637D59049F),
        new(0x96E6A1D72C2AC1EB, 0x09B81E0FA68AC838),
        new(0x8C3B0C947DF70971, 0x0ADCD64DBA1F8070),
        new(0xA70095AA11D8754E, 0x0C4F9D8B4A67FF05),
        new(0x64F2A61E05F3CEFE, 0x0E347AB4698BB00E),
        new(0x572DC64D3936B199, 0x10C9A84994022D28),
        new(0x6A80DDD58C4AC6FE, 0x1484B13D7C02A8F8),
        new(0x645C921FA5C4559C, 0x1A61762A7ADED93F),
        new(0x594E6629AE4A965A, 0x24EED8A1DF37FCF2),
        new(0x3F82AA45785F1ACB, 0x3D8E13B87407FAE9),
        new(0xBE87FED0691D3E89, 0xB8AA3B295C17F0BB),
    ];

    // 1/sqrt(2) fraction MSD and the I_RECIP_SQRT_2 / I_SQRT_2 range constants (dpml_log_x.h).
    private const ulong LogOneOverSqrt2 = 0xB504F333F9DE6484;
    private const ulong LogIRecipSqrt2 = 0x5A827999FCEF3242;
    private const ulong LogISqrt2 = 0xB504F333F9DE6484;

    // Unpacked ln2, log10(2), and 2.0 (dpml_log_x.h).
    private static DiyFp128 LogLn2 => new DiyFp128(0, 0, 0xB17217F7D1CF79AB, 0xC9E3B39803F2F6AF);
    private static DiyFp128 LogLog10Of2 => new DiyFp128(0, -1, 0x9A209A84FBCFF798, 0x8F8959AC0B7C9178);
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

        Span<DiyFp128> tmp = [default, default];
        DiyFp128AddSub(arg, DiyFp128One, UxAddSub | UxMagnitudeOnly, tmp); // tmp[0] = g + 1, tmp[1] = g - 1

        DiyFp128Divide(tmp[1], tmp[0], DiyFp128FullPrecision, out DiyFp128 z);
        DiyFp128EvaluateLogPolynomial(z, Log2Coefficients, Log2Degree, Log2TrailingExponent, out DiyFp128 poly);

        DiyFp128 result = DiyFp128FromWord(m);
        DiyFp128 sum = default;
        DiyFp128AddSub(result, poly, UxAdd | UxNoNormalization, new Span<DiyFp128>(ref sum));
        result = sum;

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
            DiyFp128 t = default;
            DiyFp128AddSub(LogTwo, arg, UxAdd, new Span<DiyFp128>(ref t));
            DiyFp128Divide(arg, t, DiyFp128FullPrecision, out DiyFp128 reduced);
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
            DiyFp128 t = default;
            DiyFp128AddSub(DiyFp128One, arg, UxAdd, new Span<DiyFp128>(ref t));
            return DiyFp128Log(t, scaleValid, scale);
        }
    }

    private static DiyFp128 DiyFp128Ln1p(scoped in DiyFp128 arg) => DiyFp128Log1p(arg, scaleValid: true, LogLn2);
    private static DiyFp128 DiyFp128Log2P1(scoped in DiyFp128 arg) => DiyFp128Log1p(arg, scaleValid: false, default);
    private static DiyFp128 DiyFp128Log10P1(scoped in DiyFp128 arg) => DiyFp128Log1p(arg, scaleValid: true, LogLog10Of2);
}
