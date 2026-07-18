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
    private readonly struct DiyFp128FixedCoefficient(ulong lo, ulong hi)
    {
        internal readonly ulong Low = lo;
        internal readonly ulong High = hi;
    }

    // High 64 bits of a 64x64 product (Intel's UMULH).
    private static ulong DiyFp128MultiplyHigh(ulong a, ulong b) => Math.BigMul(a, b, out _);

    // ---- exp (base e) constant table (dpml_exp_x.h) ----

    private const ulong ExpReciprocalLn2High = 0x5C551D94AE0BF85E; // high digits of 1/ln2
    private const ulong ExpLn2High = 0xB17217F7D1CF79AC;           // high digits of ln2
    private const int ExpReduceConstantExponent = 0;               // binary exponent of ln2
    private const int ExpDegree = 22;
    private const int ExpTrailingExponent = 1;

    // ln2_lo = ln2 - ln2_hi, as an unpacked value.
    private static DiyFp128 ExpLn2Low => new DiyFp128(UxSignBit, -66, 0xD871319FF0342542, 0xFC32F366359D2749);

    private static readonly DiyFp128FixedCoefficient[] ExpCoefficients =
    [
        new(0x0219C7290393A749, 0x0000000000000000),
        new(0x2E468FC7B47B630C, 0x0000000000000000),
        new(0xCA85AD657F5C80BD, 0x0000000000000003),
        new(0xD268B2CB49B64EAE, 0x000000000000004B),
        new(0x9E18D9E0EB90C661, 0x00000000000005A0),
        new(0x1DC178468FE824F4, 0x000000000000654B),
        new(0xF9CCF1842631A1A2, 0x000000000006B9FC),
        new(0x9CCECE542F079EEB, 0x00000000006B9FCF),
        new(0x301F26EFF3934011, 0x00000000064E5D2A),
        new(0xA1B4271D14562C06, 0x000000005849184E),
        new(0x3625ED5697A1173A, 0x000000047BB63BFE),
        new(0x89C71FC24062E495, 0x00000035CC8ACFEA),
        new(0xEB8E5DDFF9B4C26E, 0x0000024FC9F6EF13),
        new(0x338FAAC2198CD02D, 0x0000171DE3A556C7),
        new(0xD00D00D00E2C1D71, 0x0000D00D00D00D00),
        new(0x8068068066CFB7B5, 0x0006806806806806),
        new(0x82D82D82D829D3B1, 0x002D82D82D82D82D),
        new(0x111111111113746F, 0x0111111111111111),
        new(0x5555555555555AA3, 0x0555555555555555),
        new(0x5555555555555380, 0x1555555555555555),
        new(0xFFFFFFFFFFFFFFFE, 0x3FFFFFFFFFFFFFFF),
        new(0x0000000000000000, 0x8000000000000000),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // ---- exp10 (base 10) constant table (dpml_exp_x.h) ----
    // The exp10 polynomial approximates 10^t directly, so the reduction subtracts scale*log10(2) and the
    // result is 10^reduced * 2^scale.

    private const ulong Exp10ReciprocalHigh = 0xD49A784BCD1B8AFE; // high digits of log2(10)/4
    private const ulong Exp10Ln2High = 0x9A209A84FBCFF799;        // high digits of log10(2)*2
    private const int Exp10ReduceConstantExponent = -1;           // binary exponent of log10(2)
    private const int Exp10Degree = 22;
    private const int Exp10TrailingExponent = 2;

    // log10(2)_lo, as an unpacked value.
    private static DiyFp128 Exp10Ln2Low => new DiyFp128(UxSignBit, -66, 0xE0ED4CA7E906DD0F, 0xB2A59E75785C196C);

    private static readonly DiyFp128FixedCoefficient[] Exp10Coefficients =
    [
        new(0xAA326D76E12A5F3D, 0x000000000005D18C),
        new(0xBB46D2D76A135C14, 0x000000000037BD19),
        new(0x2188762E74D6A84B, 0x0000000001FBA820),
        new(0x10A5EEBAE5E25723, 0x0000000011396F18),
        new(0xB3FCD05A246EA126, 0x000000008E20E630),
        new(0x11F8F23A20DD37FD, 0x00000004570FB29C),
        new(0x167B5D1D64BF3431, 0x000000200AF8FBFF),
        new(0xB407C79F854435F8, 0x000000DEA8177BC6),
        new(0xAEF77A1B0616E83B, 0x000005AA7A612E29),
        new(0x119B2348D3C5FBA9, 0x0000227315A5882E),
        new(0x20D8613A1E07D507, 0x0000C27F096FC05F),
        new(0x7F472BC73DD8F81C, 0x0003F59FABB213AC),
        new(0x674C9F4591A76481, 0x0012EA52B2D182AF),
        new(0xC9822F93893BB4F4, 0x005225F11764F507),
        new(0xF088AE28F92F4908, 0x014116B05FDAA5CD),
        new(0xC160BBA8AA4224B1, 0x045B937F0CCEA1AC),
        new(0xD9F3DCD36EBEE310, 0x0D3F6B8423E45AEB),
        new(0x5C6542259124B3BC, 0x22853FFA3A9AEC44),
        new(0xEA51F65ED9F90D3B, 0x4AF5D827F6631131),
        new(0x6A4F9D820D46BA57, 0x82382C8EF1652304),
        new(0x80A99CE52D65A6EC, 0xA9A92639E753443A),
        new(0xEA56D62B82D30A2C, 0x935D8DDDAAA8AC16),
        new(0x0000000000000000, 0x4000000000000000),
    ];

    // 1.0 as an unpacked value (Intel's UX_ONE).
    private static DiyFp128 DiyFp128One => new DiyFp128(0, 1, 0x8000000000000000, 0);

    // ln2 as a full unpacked value, built from the exp table's high and low pieces.
    private static DiyFp128 DiyFp128Ln2
    {
        get
        {
            DiyFp128 single = default;
            DiyFp128AddSub(new DiyFp128(0, 0, ExpLn2High, 0), ExpLn2Low, UxSub, new Span<DiyFp128>(ref single));
            return single;
        }
    }

    /// <summary>
    /// Reduces <paramref name="orig"/> as <c>lnb*x = scale*ln2 + reduced</c> with <c>|reduced| &lt;=
    /// ln2/2</c> (Intel's <c>UX_EXP_REDUCE</c>), returning <c>scale</c>. For <c>|x| &gt; 2^17</c> it
    /// returns a scale that forces the pack step to over/underflow.
    /// </summary>
    private static int DiyFp128ExpReduce(scoped in DiyFp128 orig, out DiyFp128 reduced, ulong reciprocalLn2High, ulong ln2High, int reduceConstantExponent, scoped in DiyFp128 ln2Low)
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
        ulong scale = DiyFp128MultiplyHigh(msd, reciprocalLn2High);
        int shift = (64 - 3) - exponent;
        scale += 1UL << (shift - 1);
        scale &= unchecked((ulong)(-(long)(1UL << shift)));

        // Normalize scale; it has at most two leading zeros.
        int leadingZeros = (int)ulong.LeadingZeroCount(scale);
        scale <<= leadingZeros;
        shift += leadingZeros;

        int scaleExponent = 64 - shift;

        // scale*high_bits_of_ln2, renormalized so the following subtraction keeps x's last bit.
        ulong lsd = scale * ln2High;
        msd = DiyFp128MultiplyHigh(scale, ln2High);
        exponent = scaleExponent;
        if ((long)msd > 0)
        {
            exponent--;
            msd = (msd + msd) + (lsd >> 63);
            lsd += lsd;
        }

        var tmp = new DiyFp128(sign, exponent + reduceConstantExponent, msd, lsd);
        DiyFp128 single = default;
        DiyFp128AddSub(orig, tmp, UxSub, new Span<DiyFp128>(ref single));
        tmp = single;

        // Subtract scale*low_bits_of_ln2 to complete the reduced argument.
        var uxScale = new DiyFp128(sign, scaleExponent, scale, 0);
        DiyFp128 ln2LowLocal = ln2Low;
        DiyFp128Multiply(ref uxScale, ref ln2LowLocal, out reduced);
        DiyFp128AddSub(tmp, reduced, UxSub | UxNoNormalization, new Span<DiyFp128>(ref single));
        reduced = single;

        scale >>= shift;
        return (int)((sign != 0) ? -(long)scale : (long)scale);
    }

    /// <summary>
    /// Evaluates a Horner polynomial with positive argument (Intel's <c>__eval_pos_poly</c>):
    /// <c>s(k) = c(k) + x*s(k+1)</c>. Coefficients are stored in reverse order <c>c(n)..c(0)</c>.
    /// </summary>
    private static void DiyFp128EvaluatePositivePolynomial(scoped in DiyFp128 x, long shift, ReadOnlySpan<DiyFp128FixedCoefficient> coefficients, int index, long count, out DiyFp128 result)
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
            p1 = DiyFp128MultiplyHigh(sLow, xHigh);
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
            p1 = DiyFp128MultiplyHigh(sLow, xHigh);
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

                p2 = DiyFp128MultiplyHigh(sHigh, xLow);
                cLow += p1;
                carry = (cLow < p1) ? 1UL : 0UL;
                count--;

                p1 = DiyFp128MultiplyHigh(sLow, xHigh);
                cLow += p2;
                carry += (cLow < p2) ? 1UL : 0UL;
                shift += shiftIncrement;

                p2 = DiyFp128MultiplyHigh(sHigh, xHigh);
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

                p2 = DiyFp128MultiplyHigh(sHigh, xLow);
                cLow += p1;
                carry = (cLow < p1) ? 1UL : 0UL;
                count--;

                p1 = DiyFp128MultiplyHigh(sLow, xHigh);
                cLow += p2;
                carry += (cLow < p2) ? 1UL : 0UL;

                p2 = DiyFp128MultiplyHigh(sHigh, xHigh);
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

        result = new DiyFp128(0, (int)exponent, sHigh, sLow);
    }

    /// <summary>
    /// Evaluates a Horner polynomial with negative argument (Intel's <c>__eval_neg_poly</c>):
    /// <c>s(k) = c(k) - x*s(k+1)</c>. Coefficients are stored in reverse order <c>c(n)..c(0)</c>.
    /// </summary>
    private static void DiyFp128EvaluateNegativePolynomial(scoped in DiyFp128 x, long shift, ReadOnlySpan<DiyFp128FixedCoefficient> coefficients, int index, long count, out DiyFp128 result)
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
            p1 = DiyFp128MultiplyHigh(sLow, xHigh);
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
            p1 = DiyFp128MultiplyHigh(sLow, xHigh);
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

            p2 = DiyFp128MultiplyHigh(sHigh, xLow);
            tmp = cLow - p1;
            cHigh -= (tmp > cLow) ? 1UL : 0UL;
            count--;

            p1 = DiyFp128MultiplyHigh(sLow, xHigh);
            cLow = tmp - p2;
            cHigh -= (cLow > tmp) ? 1UL : 0UL;
            shift += shiftIncrement;

            p2 = DiyFp128MultiplyHigh(sHigh, xHigh);
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

                p2 = DiyFp128MultiplyHigh(sHigh, xLow);
                tmp = cLow - p1;
                cHigh -= (tmp > cLow) ? 1UL : 0UL;
                count--;

                p1 = DiyFp128MultiplyHigh(sLow, xHigh);
                cLow = tmp - p2;
                cHigh -= (cLow > tmp) ? 1UL : 0UL;

                p2 = DiyFp128MultiplyHigh(sHigh, xHigh);
                sLow = cLow - p1;
                cHigh -= (sLow > cLow) ? 1UL : 0UL;
                index++;

                sHigh = cHigh - p2;
            }

        result = new DiyFp128(0, 0, sHigh, sLow);
    }

    /// <summary>
    /// Evaluates the exp-family polynomial on the reduced argument (Intel's <c>EVALUATE_RATIONAL</c>
    /// specialized to the numerator-only <c>STANDARD</c> form).
    /// </summary>
    private static void DiyFp128EvaluateExpPolynomial(DiyFp128 argument, ReadOnlySpan<DiyFp128FixedCoefficient> coefficients, int degree, int trailingExponent, out DiyFp128 result)
    {
        DiyFp128Normalize(ref argument);
        long shift = -(long)degree * argument._exponent;

        if (argument._sign != 0)
        {
            DiyFp128EvaluateNegativePolynomial(argument, shift, coefficients, 0, degree, out result);
        }
        else
        {
            DiyFp128EvaluatePositivePolynomial(argument, shift, coefficients, 0, degree, out result);
        }

        result._exponent += trailingExponent;
    }

    /// <summary>Computes <c>e^x</c> for an unpacked argument (Intel's <c>UX_EXP</c>).</summary>
    private static DiyFp128 DiyFp128Exp(scoped in DiyFp128 argument)
    {
        int scale = DiyFp128ExpReduce(argument, out DiyFp128 reduced, ExpReciprocalLn2High, ExpLn2High, ExpReduceConstantExponent, ExpLn2Low);
        DiyFp128EvaluateExpPolynomial(reduced, ExpCoefficients, ExpDegree, ExpTrailingExponent, out DiyFp128 result);
        result._exponent += scale;
        return result;
    }

    /// <summary>Computes <c>10^x</c> for an unpacked argument (Intel's <c>UX_EXP10</c>).</summary>
    private static DiyFp128 DiyFp128Exp10(scoped in DiyFp128 argument)
    {
        int scale = DiyFp128ExpReduce(argument, out DiyFp128 reduced, Exp10ReciprocalHigh, Exp10Ln2High, Exp10ReduceConstantExponent, Exp10Ln2Low);
        DiyFp128EvaluateExpPolynomial(reduced, Exp10Coefficients, Exp10Degree, Exp10TrailingExponent, out DiyFp128 result);
        result._exponent += scale;
        return result;
    }

    /// <summary>
    /// Computes <c>b^x - 1</c> for an unpacked argument (Intel's <c>UX_EXPM1</c>, generalized over the
    /// base-b table). For small reduced arguments a direct polynomial avoids the cancellation of
    /// <c>b^x - 1</c>; otherwise <c>b^x</c> is formed and one is subtracted.
    /// </summary>
    private static DiyFp128 DiyFp128ExpM1(scoped in DiyFp128 argument, ulong reciprocalHigh, ulong ln2High, int reduceConstantExponent, scoped in DiyFp128 ln2Low, ReadOnlySpan<DiyFp128FixedCoefficient> coefficients, int degree, int trailingExponent)
    {
        int scale = DiyFp128ExpReduce(argument, out DiyFp128 reduced, reciprocalHigh, ln2High, reduceConstantExponent, ln2Low);
        DiyFp128 result;

        if (scale == 0)
        {
            // |reduced| <= ln2/2: use the low degree-1 terms of the polynomial, post-multiplied by the
            // reduced argument. This leaves the exponent low by the table's trailing exponent.
            DiyFp128Normalize(ref reduced);
            long shift = -(long)(degree - 1) * reduced._exponent;

            if (reduced._sign != 0)
            {
                DiyFp128EvaluateNegativePolynomial(reduced, shift, coefficients, 0, degree - 1, out result);
            }
            else
            {
                DiyFp128EvaluatePositivePolynomial(reduced, shift, coefficients, 0, degree - 1, out result);
            }

            DiyFp128 reducedLocal = reduced;
            DiyFp128Multiply(ref reducedLocal, ref result, out result);
            result._exponent += trailingExponent;
        }
        else
        {
            DiyFp128EvaluateExpPolynomial(reduced, coefficients, degree, trailingExponent, out result);
            result._exponent += scale;

            DiyFp128 single = default;
            DiyFp128AddSub(result, DiyFp128One, UxSub | UxNoNormalization | UxMagnitudeOnly, new Span<DiyFp128>(ref single));
            result = single;
        }

        return result;
    }

    /// <summary>Computes <c>e^x - 1</c> for an unpacked argument.</summary>
    private static DiyFp128 DiyFp128ExpM1(scoped in DiyFp128 argument) =>
        DiyFp128ExpM1(argument, ExpReciprocalLn2High, ExpLn2High, ExpReduceConstantExponent, ExpLn2Low, ExpCoefficients, ExpDegree, ExpTrailingExponent);

    /// <summary>Computes <c>10^x - 1</c> for an unpacked argument.</summary>
    private static DiyFp128 DiyFp128Exp10M1(scoped in DiyFp128 argument) =>
        DiyFp128ExpM1(argument, Exp10ReciprocalHigh, Exp10Ln2High, Exp10ReduceConstantExponent, Exp10Ln2Low, Exp10Coefficients, Exp10Degree, Exp10TrailingExponent);

    // Intel's software engine has no dedicated exp2 table (its decimal exp2 routes through a separate
    // templated binary128 engine), so 2^x is evaluated as e^(x*ln2) using the exp table's own ln2. A
    // dedicated exp2 table is a faithful-fidelity follow-up.

    /// <summary>Computes <c>2^x</c> for an unpacked argument as <c>e^(x*ln2)</c>.</summary>
    private static DiyFp128 DiyFp128Exp2(scoped in DiyFp128 argument)
    {
        DiyFp128 argumentLocal = argument;
        DiyFp128 ln2 = DiyFp128Ln2;
        DiyFp128Multiply(ref argumentLocal, ref ln2, out DiyFp128 scaled);
        return DiyFp128Exp(scaled);
    }

    /// <summary>Computes <c>2^x - 1</c> for an unpacked argument as <c>expm1(x*ln2)</c>.</summary>
    private static DiyFp128 DiyFp128Exp2M1(scoped in DiyFp128 argument)
    {
        DiyFp128 argumentLocal = argument;
        DiyFp128 ln2 = DiyFp128Ln2;
        DiyFp128Multiply(ref argumentLocal, ref ln2, out DiyFp128 scaled);
        return DiyFp128ExpM1(scaled);
    }
}
