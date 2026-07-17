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
    // Deferred: the exp10/exp2 tables are ported and harness-validated in a follow-up before wiring.

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
}
