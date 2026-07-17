// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the exponential evaluation from the Intel(R) Decimal Floating-Point Math
    // Library, specifically `UX_EXP_REDUCE`, `UX_EXP_COMMON` from `dpml_ux_exp.c`, the polynomial
    // evaluators `EVALUATE_RATIONAL`, `__eval_pos_poly`, `__eval_neg_poly` from `dpml_ux_ops_64.c`, and
    // the exp/exp10 constant tables from `dpml_exp_x.h`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // The evaluation runs entirely in the software binary128 engine, so Decimal64/Decimal128 obtain the
    // full ~34-digit accuracy Intel's reference does. Only the b = e and b = 10 tables are ported here;
    // the argument reduction and polynomial machinery is shared. Intel's `EVALUATE_RATIONAL` is
    // specialized to the numerator-only (`STANDARD`) form the exp family uses.

    /// <summary>A 128-bit fixed-point polynomial coefficient (Intel's <c>FIXED_128</c>): <c>digits[0]</c>
    /// is the low limb, <c>digits[1]</c> the high limb.</summary>
    private readonly struct Float128FixedCoefficient(ulong lo, ulong hi)
    {
        internal readonly ulong Low = lo;
        internal readonly ulong High = hi;
    }

    // High 64 bits of a 64x64 product (Intel's UMULH).
    private static ulong Float128MultiplyHigh(ulong a, ulong b) => Math.BigMul(a, b, out _);

    // ---- exp (base e) constant table (dpml_exp_x.h) ----

    private const ulong ExpReciprocalLn2High = 0x5c551d94ae0bf85e; // high digits of 1/ln2
    private const ulong ExpLn2High = 0xb17217f7d1cf79ac;           // high digits of ln2
    private const int ExpReduceConstantExponent = 0;               // binary exponent of ln2
    private const int ExpDegree = 22;
    private const int ExpTrailingExponent = 1;

    // ln2_lo = ln2 - ln2_hi, as an unpacked value.
    private static Float128 ExpLn2Low => new Float128(UxSignBit, -66, 0xd871319ff0342542, 0xfc32f366359d2749);

    private static readonly Float128FixedCoefficient[] ExpCoefficients =
    [
        new(0x0219c7290393a749, 0x0000000000000000),
        new(0x2e468fc7b47b630c, 0x0000000000000000),
        new(0xca85ad657f5c80bd, 0x0000000000000003),
        new(0xd268b2cb49b64eae, 0x000000000000004b),
        new(0x9e18d9e0eb90c661, 0x00000000000005a0),
        new(0x1dc178468fe824f4, 0x000000000000654b),
        new(0xf9ccf1842631a1a2, 0x000000000006b9fc),
        new(0x9ccece542f079eeb, 0x00000000006b9fcf),
        new(0x301f26eff3934011, 0x00000000064e5d2a),
        new(0xa1b4271d14562c06, 0x000000005849184e),
        new(0x3625ed5697a1173a, 0x000000047bb63bfe),
        new(0x89c71fc24062e495, 0x00000035cc8acfea),
        new(0xeb8e5ddff9b4c26e, 0x0000024fc9f6ef13),
        new(0x338faac2198cd02d, 0x0000171de3a556c7),
        new(0xd00d00d00e2c1d71, 0x0000d00d00d00d00),
        new(0x8068068066cfb7b5, 0x0006806806806806),
        new(0x82d82d82d829d3b1, 0x002d82d82d82d82d),
        new(0x111111111113746f, 0x0111111111111111),
        new(0x5555555555555aa3, 0x0555555555555555),
        new(0x5555555555555380, 0x1555555555555555),
        new(0xfffffffffffffffe, 0x3fffffffffffffff),
        new(0x0000000000000000, 0x8000000000000000),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // ---- exp10 (base 10) constant table (dpml_exp_x.h) ----
    // The exp10 polynomial approximates 10^t directly, so the reduction subtracts scale*log10(2) and the
    // result is 10^reduced * 2^scale.

    private const ulong Exp10ReciprocalHigh = 0xd49a784bcd1b8afe; // high digits of log2(10)/4
    private const ulong Exp10Ln2High = 0x9a209a84fbcff799;        // high digits of log10(2)*2
    private const int Exp10ReduceConstantExponent = -1;           // binary exponent of log10(2)
    private const int Exp10Degree = 22;
    private const int Exp10TrailingExponent = 2;

    // log10(2)_lo, as an unpacked value.
    private static Float128 Exp10Ln2Low => new Float128(UxSignBit, -66, 0xe0ed4ca7e906dd0f, 0xb2a59e75785c196c);

    private static readonly Float128FixedCoefficient[] Exp10Coefficients =
    [
        new(0xaa326d76e12a5f3d, 0x000000000005d18c),
        new(0xbb46d2d76a135c14, 0x000000000037bd19),
        new(0x2188762e74d6a84b, 0x0000000001fba820),
        new(0x10a5eebae5e25723, 0x0000000011396f18),
        new(0xb3fcd05a246ea126, 0x000000008e20e630),
        new(0x11f8f23a20dd37fd, 0x00000004570fb29c),
        new(0x167b5d1d64bf3431, 0x000000200af8fbff),
        new(0xb407c79f854435f8, 0x000000dea8177bc6),
        new(0xaef77a1b0616e83b, 0x000005aa7a612e29),
        new(0x119b2348d3c5fba9, 0x0000227315a5882e),
        new(0x20d8613a1e07d507, 0x0000c27f096fc05f),
        new(0x7f472bc73dd8f81c, 0x0003f59fabb213ac),
        new(0x674c9f4591a76481, 0x0012ea52b2d182af),
        new(0xc9822f93893bb4f4, 0x005225f11764f507),
        new(0xf088ae28f92f4908, 0x014116b05fdaa5cd),
        new(0xc160bba8aa4224b1, 0x045b937f0ccea1ac),
        new(0xd9f3dcd36ebee310, 0x0d3f6b8423e45aeb),
        new(0x5c6542259124b3bc, 0x22853ffa3a9aec44),
        new(0xea51f65ed9f90d3b, 0x4af5d827f6631131),
        new(0x6a4f9d820d46ba57, 0x82382c8ef1652304),
        new(0x80a99ce52d65a6ec, 0xa9a92639e753443a),
        new(0xea56d62b82d30a2c, 0x935d8dddaaa8ac16),
        new(0x0000000000000000, 0x4000000000000000),
    ];

    // 1.0 as an unpacked value (Intel's UX_ONE).
    private static Float128 Float128One => new Float128(0, 1, 0x8000000000000000, 0);

    // ln2 as a full unpacked value, built from the exp table's high and low pieces.
    private static Float128 Float128Ln2
    {
        get
        {
            Span<Float128> single = stackalloc Float128[1];
            Float128AddSub(new Float128(0, 0, ExpLn2High, 0), ExpLn2Low, UxSub, single);
            return single[0];
        }
    }

    /// <summary>
    /// Reduces <paramref name="orig"/> as <c>lnb*x = scale*ln2 + reduced</c> with <c>|reduced| &lt;=
    /// ln2/2</c> (Intel's <c>UX_EXP_REDUCE</c>), returning <c>scale</c>. For <c>|x| &gt; 2^17</c> it
    /// returns a scale that forces the pack step to over/underflow.
    /// </summary>
    private static int Float128ExpReduce(scoped in Float128 orig, out Float128 reduced, ulong reciprocalLn2High, ulong ln2High, int reduceConstantExponent, scoped in Float128 ln2Low)
    {
        int exponent = orig._exponent;
        uint sign = orig._sign;

        if ((uint)(exponent + 1 - reduceConstantExponent) > 18)
        {
            // Either no reduction is necessary or the argument is out of range.
            reduced = orig;

            if (exponent > 0)
            {
                reduced._exponent = -128;
                return (sign != 0) ? -(1 << 15) : (1 << 15);
            }

            return 0;
        }

        // scale ~ nint(x*lnb/ln2), computed from the high bits of the significand.
        ulong msd = orig._hi >> 1;
        ulong scale = Float128MultiplyHigh(msd, reciprocalLn2High);
        int shift = (64 - 3) - exponent;
        scale += 1UL << (shift - 1);
        scale &= unchecked((ulong)(-(long)(1UL << shift)));

        // Normalize scale; it has at most two leading zeros.
        while ((long)scale > 0)
        {
            scale += scale;
            shift++;
        }

        int scaleExponent = 64 - shift;

        // scale*high_bits_of_ln2, renormalized so the following subtraction keeps x's last bit.
        ulong lsd = scale * ln2High;
        msd = Float128MultiplyHigh(scale, ln2High);
        exponent = scaleExponent;
        if ((long)msd > 0)
        {
            exponent--;
            msd = (msd + msd) + (lsd >> 63);
            lsd += lsd;
        }

        var tmp = new Float128(sign, exponent + reduceConstantExponent, msd, lsd);
        Span<Float128> single = stackalloc Float128[1];
        Float128AddSub(orig, tmp, UxSub, single);
        tmp = single[0];

        // Subtract scale*low_bits_of_ln2 to complete the reduced argument.
        var uxScale = new Float128(sign, scaleExponent, scale, 0);
        Float128 ln2LowLocal = ln2Low;
        Float128Multiply(ref uxScale, ref ln2LowLocal, out reduced);
        Float128AddSub(tmp, reduced, UxSub | UxNoNormalization, single);
        reduced = single[0];

        scale >>= shift;
        return (int)((sign != 0) ? -(long)scale : (long)scale);
    }

    /// <summary>
    /// Evaluates a Horner polynomial with positive argument (Intel's <c>__eval_pos_poly</c>):
    /// <c>s(k) = c(k) + x*s(k+1)</c>. Coefficients are stored in reverse order <c>c(n)..c(0)</c>.
    /// </summary>
    private static void Float128EvaluatePositivePolynomial(scoped in Float128 x, long shift, ReadOnlySpan<Float128FixedCoefficient> coefficients, int index, long count, out Float128 result)
    {
        ulong xHigh = x._hi;
        ulong xLow = x._lo;
        long shiftIncrement = x._exponent;
        ulong sLow = 0, sHigh = 0, cHigh, cLow, p1, p2, carry;
        long exponent;

        if (shift < 128)
        {
            goto CheckShift64To127;
        }

        ShiftGE128:
            shift += shiftIncrement;
            index++;
            count--;
            if (shift >= 128)
            {
                goto ShiftGE128;
            }

        CheckShift64To127:
            if (shift < 64)
            {
                goto CheckShift1To63;
            }
            if (sLow != 0)
            {
                goto Shift64To127;
            }

        Shift64To127ZeroLoop:
            sLow = coefficients[index].High >> (int)(shift - 64);
            shift += shiftIncrement;
            index++;
            count--;
            if (shift < 64)
            {
                goto CheckShift1To63;
            }
            if (sLow == 0)
            {
                goto Shift64To127ZeroLoop;
            }

        Shift64To127:
            p1 = Float128MultiplyHigh(sLow, xHigh);
            cLow = coefficients[index].High >> (int)(shift - 64);
            shift += shiftIncrement;
            index++;
            count--;
            sLow = cLow + p1;
            if (shift >= 64)
            {
                goto Shift64To127;
            }
        sHigh = (sLow < p1) ? 1UL : 0UL;

        CheckShift1To63:
            exponent = 0;
            if (shift == 0)
            {
                goto ShiftEQ0;
            }
            if (sHigh != 0)
            {
                goto Shift1To63;
            }

        Shift1To63ZeroLoop:
            p1 = Float128MultiplyHigh(sLow, xHigh);
            cHigh = coefficients[index].High;
            cLow = coefficients[index].Low;
            cLow = (cLow >> (int)shift) | (cHigh << (int)(64 - shift));
            sHigh = cHigh >> (int)shift;
            shift += shiftIncrement;
            index++;
            count--;
            sLow = cLow + p1;
            sHigh += (sLow < p1) ? 1UL : 0UL;
            if (shift == 0)
            {
                goto ShiftEQ0;
            }
            if (sHigh == 0)
            {
                goto Shift1To63ZeroLoop;
            }

        Shift1To63:
            while (count >= 0)
            {
                p1 = sHigh * xHigh;
                cHigh = coefficients[index].High;
                cLow = coefficients[index].Low;
                cLow = (cLow >> (int)shift) | (cHigh << (int)(64 - shift));
                cHigh >>= (int)shift;

                p2 = Float128MultiplyHigh(sHigh, xLow);
                cLow += p1;
                carry = (cLow < p1) ? 1UL : 0UL;
                count--;

                p1 = Float128MultiplyHigh(sLow, xHigh);
                cLow += p2;
                carry += (cLow < p2) ? 1UL : 0UL;
                shift += shiftIncrement;

                p2 = Float128MultiplyHigh(sHigh, xHigh);
                sLow = cLow + p1;
                carry += (sLow < p1) ? 1UL : 0UL;
                cHigh += carry;
                carry = (cHigh < carry) ? 1UL : 0UL;
                index++;

                sHigh = cHigh + p2;
                carry += (sHigh < p2) ? 1UL : 0UL;
                if (carry != 0)
                {
                    sLow = (sLow >> 1) | (sHigh << 63);
                    sHigh = (sHigh >> 1) | UxMsb;
                    shift++;
                    exponent++;
                }
                if (shift == 0)
                {
                    break;
                }
            }

        ShiftEQ0:
            while (count >= 0)
            {
                p1 = sHigh * xHigh;
                cHigh = coefficients[index].High;
                cLow = coefficients[index].Low;

                p2 = Float128MultiplyHigh(sHigh, xLow);
                cLow += p1;
                carry = (cLow < p1) ? 1UL : 0UL;
                count--;

                p1 = Float128MultiplyHigh(sLow, xHigh);
                cLow += p2;
                carry += (cLow < p2) ? 1UL : 0UL;

                p2 = Float128MultiplyHigh(sHigh, xHigh);
                sLow = cLow + p1;
                carry += (sLow < p1) ? 1UL : 0UL;
                cHigh += carry;
                carry = (cHigh < carry) ? 1UL : 0UL;
                index++;

                sHigh = cHigh + p2;
                carry += (sHigh < p2) ? 1UL : 0UL;
                if (carry != 0)
                {
                    sLow = (sLow >> 1) | (sHigh << 63);
                    sHigh = (sHigh >> 1) | UxMsb;
                    shift = 1;
                    exponent++;
                    if (count >= 0)
                    {
                        goto Shift1To63;
                    }
                }
            }

        result = new Float128(0, (int)exponent, sHigh, sLow);
    }

    /// <summary>
    /// Evaluates a Horner polynomial with negative argument (Intel's <c>__eval_neg_poly</c>):
    /// <c>s(k) = c(k) - x*s(k+1)</c>. Coefficients are stored in reverse order <c>c(n)..c(0)</c>.
    /// </summary>
    private static void Float128EvaluateNegativePolynomial(scoped in Float128 x, long shift, ReadOnlySpan<Float128FixedCoefficient> coefficients, int index, long count, out Float128 result)
    {
        ulong xHigh = x._hi;
        ulong xLow = x._lo;
        long shiftIncrement = x._exponent;
        ulong sLow = 0, sHigh = 0, cHigh, cLow, p1, p2, tmp;

        if (shift < 128)
        {
            goto CheckShift64To127;
        }

        ShiftGE128:
            shift += shiftIncrement;
            index++;
            count--;
            if (shift >= 128)
            {
                goto ShiftGE128;
            }

        CheckShift64To127:
            if (shift < 64)
            {
                goto CheckShift1To63;
            }
            if (sLow != 0)
            {
                goto Shift64To127;
            }

        Shift64To127ZeroLoop:
            sLow = coefficients[index].High >> (int)(shift - 64);
            shift += shiftIncrement;
            index++;
            count--;
            if (shift < 64)
            {
                goto CheckShift1To63;
            }
            if (sLow == 0)
            {
                goto Shift64To127ZeroLoop;
            }

        Shift64To127:
            p1 = Float128MultiplyHigh(sLow, xHigh);
            cLow = coefficients[index].High >> (int)(shift - 64);
            shift += shiftIncrement;
            index++;
            count--;
            sLow = cLow - p1;
            if (shift >= 64)
            {
                goto Shift64To127;
            }

        CheckShift1To63:
            if (shift == 0)
            {
                goto ShiftEQ0;
            }
            if (sHigh != 0)
            {
                goto Shift1To63;
            }

        Shift1To63ZeroLoop:
            p1 = Float128MultiplyHigh(sLow, xHigh);
            cHigh = coefficients[index].High;
            cLow = coefficients[index].Low;
            cLow = (cLow >> (int)shift) | (cHigh << (int)(64 - shift));
            sHigh = cHigh >> (int)shift;
            shift += shiftIncrement;
            index++;
            count--;
            sLow = cLow - p1;
            sHigh -= (sLow > cLow) ? 1UL : 0UL;
            if (shift == 0)
            {
                goto ShiftEQ0;
            }
            if (sHigh == 0)
            {
                goto Shift1To63ZeroLoop;
            }

        Shift1To63:
            p1 = sHigh * xHigh;
            cHigh = coefficients[index].High;
            cLow = coefficients[index].Low;
            cLow = (cLow >> (int)shift) | (cHigh << (int)(64 - shift));
            cHigh >>= (int)shift;

            p2 = Float128MultiplyHigh(sHigh, xLow);
            tmp = cLow - p1;
            cHigh -= (tmp > cLow) ? 1UL : 0UL;
            count--;

            p1 = Float128MultiplyHigh(sLow, xHigh);
            cLow = tmp - p2;
            cHigh -= (cLow > tmp) ? 1UL : 0UL;
            shift += shiftIncrement;

            p2 = Float128MultiplyHigh(sHigh, xHigh);
            sLow = cLow - p1;
            cHigh -= (sLow > cLow) ? 1UL : 0UL;
            index++;

            sHigh = cHigh - p2;
            if (shift != 0)
            {
                goto Shift1To63;
            }

        ShiftEQ0:
            while (count >= 0)
            {
                p1 = sHigh * xHigh;
                cHigh = coefficients[index].High;
                cLow = coefficients[index].Low;

                p2 = Float128MultiplyHigh(sHigh, xLow);
                tmp = cLow - p1;
                cHigh -= (tmp > cLow) ? 1UL : 0UL;
                count--;

                p1 = Float128MultiplyHigh(sLow, xHigh);
                cLow = tmp - p2;
                cHigh -= (cLow > tmp) ? 1UL : 0UL;

                p2 = Float128MultiplyHigh(sHigh, xHigh);
                sLow = cLow - p1;
                cHigh -= (sLow > cLow) ? 1UL : 0UL;
                index++;

                sHigh = cHigh - p2;
            }

        result = new Float128(0, 0, sHigh, sLow);
    }

    /// <summary>
    /// Evaluates the exp-family polynomial on the reduced argument (Intel's <c>EVALUATE_RATIONAL</c>
    /// specialized to the numerator-only <c>STANDARD</c> form).
    /// </summary>
    private static void Float128EvaluateExpPolynomial(Float128 argument, ReadOnlySpan<Float128FixedCoefficient> coefficients, int degree, int trailingExponent, out Float128 result)
    {
        Float128Normalize(ref argument);
        long shift = -(long)degree * argument._exponent;

        if (argument._sign != 0)
        {
            Float128EvaluateNegativePolynomial(argument, shift, coefficients, 0, degree, out result);
        }
        else
        {
            Float128EvaluatePositivePolynomial(argument, shift, coefficients, 0, degree, out result);
        }

        result._exponent += trailingExponent;
    }

    /// <summary>Computes <c>e^x</c> for an unpacked argument (Intel's <c>UX_EXP</c>).</summary>
    private static Float128 Float128Exp(scoped in Float128 argument)
    {
        int scale = Float128ExpReduce(argument, out Float128 reduced, ExpReciprocalLn2High, ExpLn2High, ExpReduceConstantExponent, ExpLn2Low);
        Float128EvaluateExpPolynomial(reduced, ExpCoefficients, ExpDegree, ExpTrailingExponent, out Float128 result);
        result._exponent += scale;
        return result;
    }

    /// <summary>Computes <c>10^x</c> for an unpacked argument (Intel's <c>UX_EXP10</c>).</summary>
    private static Float128 Float128Exp10(scoped in Float128 argument)
    {
        int scale = Float128ExpReduce(argument, out Float128 reduced, Exp10ReciprocalHigh, Exp10Ln2High, Exp10ReduceConstantExponent, Exp10Ln2Low);
        Float128EvaluateExpPolynomial(reduced, Exp10Coefficients, Exp10Degree, Exp10TrailingExponent, out Float128 result);
        result._exponent += scale;
        return result;
    }

    /// <summary>
    /// Computes <c>b^x - 1</c> for an unpacked argument (Intel's <c>UX_EXPM1</c>, generalized over the
    /// base-b table). For small reduced arguments a direct polynomial avoids the cancellation of
    /// <c>b^x - 1</c>; otherwise <c>b^x</c> is formed and one is subtracted.
    /// </summary>
    private static Float128 Float128ExpM1(scoped in Float128 argument, ulong reciprocalHigh, ulong ln2High, int reduceConstantExponent, scoped in Float128 ln2Low, ReadOnlySpan<Float128FixedCoefficient> coefficients, int degree, int trailingExponent)
    {
        int scale = Float128ExpReduce(argument, out Float128 reduced, reciprocalHigh, ln2High, reduceConstantExponent, ln2Low);
        Float128 result;

        if (scale == 0)
        {
            // |reduced| <= ln2/2: use the low degree-1 terms of the polynomial, post-multiplied by the
            // reduced argument. This leaves the exponent low by the table's trailing exponent.
            Float128Normalize(ref reduced);
            long shift = -(long)(degree - 1) * reduced._exponent;

            if (reduced._sign != 0)
            {
                Float128EvaluateNegativePolynomial(reduced, shift, coefficients, 0, degree - 1, out result);
            }
            else
            {
                Float128EvaluatePositivePolynomial(reduced, shift, coefficients, 0, degree - 1, out result);
            }

            Float128 reducedLocal = reduced;
            Float128Multiply(ref reducedLocal, ref result, out result);
            result._exponent += trailingExponent;
        }
        else
        {
            Float128EvaluateExpPolynomial(reduced, coefficients, degree, trailingExponent, out result);
            result._exponent += scale;

            Span<Float128> single = stackalloc Float128[1];
            Float128AddSub(result, Float128One, UxSub | UxNoNormalization | UxMagnitudeOnly, single);
            result = single[0];
        }

        return result;
    }

    /// <summary>Computes <c>e^x - 1</c> for an unpacked argument.</summary>
    private static Float128 Float128ExpM1(scoped in Float128 argument) =>
        Float128ExpM1(argument, ExpReciprocalLn2High, ExpLn2High, ExpReduceConstantExponent, ExpLn2Low, ExpCoefficients, ExpDegree, ExpTrailingExponent);

    /// <summary>Computes <c>10^x - 1</c> for an unpacked argument.</summary>
    private static Float128 Float128Exp10M1(scoped in Float128 argument) =>
        Float128ExpM1(argument, Exp10ReciprocalHigh, Exp10Ln2High, Exp10ReduceConstantExponent, Exp10Ln2Low, Exp10Coefficients, Exp10Degree, Exp10TrailingExponent);

    // Intel's software engine has no dedicated exp2 table (its decimal exp2 routes through a separate
    // templated binary128 engine), so 2^x is evaluated as e^(x*ln2) using the exp table's own ln2. A
    // dedicated exp2 table is a faithful-fidelity follow-up.

    /// <summary>Computes <c>2^x</c> for an unpacked argument as <c>e^(x*ln2)</c>.</summary>
    private static Float128 Float128Exp2(scoped in Float128 argument)
    {
        Float128 argumentLocal = argument;
        Float128 ln2 = Float128Ln2;
        Float128Multiply(ref argumentLocal, ref ln2, out Float128 scaled);
        return Float128Exp(scaled);
    }

    /// <summary>Computes <c>2^x - 1</c> for an unpacked argument as <c>expm1(x*ln2)</c>.</summary>
    private static Float128 Float128Exp2M1(scoped in Float128 argument)
    {
        Float128 argumentLocal = argument;
        Float128 ln2 = Float128Ln2;
        Float128Multiply(ref argumentLocal, ref ln2, out Float128 scaled);
        return Float128ExpM1(scaled);
    }
}
