// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the unpacked "x_float" software binary128 engine (the "ux" routines)
    // from the Intel(R) Decimal Floating-Point Math Library, specifically `MULTIPLY`,
    // `EXTENDED_MULTIPLY`, `ADDSUB`, `DIVIDE`, and `FFS_AND_SHIFT` from `dpml_ux_ops_64.c` /
    // `dpml_ux_ops.c`, and the finite unpack/pack from `UNPACK_X_OR_Y` / `PACK`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // All three decimal formats route their transcendental operations through this software binary128
    // core. Intel keeps Decimal32 on binary64, but routing it through the engine is both faster and
    // more accurate at 7 digits. This is the 64-bit-word specialization of Intel's engine
    // (`NUM_UX_FRACTION_DIGITS == 2`), so the 128-bit significand is a pair of <see cref="ulong"/>
    // limbs. Intel's table-driven exception dispatcher (the `class_to_action_map` machinery inside
    // `UNPACK_X_OR_Y`/`PACK`) is intentionally
    // not ported; NaN/Infinity/zero canonicalization is handled explicitly by the per-function
    // wrappers, matching the existing exact operations. That does not affect the result bits of any
    // finite computation.

    /// <summary>
    /// An unpacked software binary128 value (Intel's <c>UX_FLOAT</c>); the 128-bit-significand analogue
    /// of <see cref="DiyFp"/>. This is a working type, not a storage encoding: the represented value is
    /// <c>(-1)^sign * fraction * 2^(exponent - 128)</c>, where the 128-bit <c>fraction</c> is held in
    /// two 64-bit limbs and, when normalized, lies in <c>[2^127, 2^128)</c> (its high bit is set). It
    /// carries the full 128-bit fraction (15 guard bits beyond the binary128 significand) and a wide
    /// <see cref="int"/> exponent with sentinels across chained operations, rounding to the packed
    /// binary128 format only at the boundaries (<see cref="Float128UnpackFinite"/> /
    /// <see cref="Float128PackFinite"/>).
    /// </summary>
    internal struct DiyFp128
    {
        // The sign is stored as Intel does (0 for positive, 0x8000_0000 for negative) so the XOR-based
        // sign arithmetic in Multiply/AddSub ports verbatim.
        internal uint _sign;
        internal int _exponent;
        internal ulong _hi; // fraction[0] == G_UX_MSD (most significant limb)
        internal ulong _lo; // fraction[1] == G_UX_LSD (least significant limb)

        internal DiyFp128(uint sign, int exponent, ulong hi, ulong lo)
        {
            _sign = sign;
            _exponent = exponent;
            _hi = hi;
            _lo = lo;
        }

        internal readonly bool IsNegative => _sign != 0;
    }

    // UX_SIGN_BIT: sign flag stored in DiyFp128._sign.
    private const uint UxSignBit = 0x8000_0000;

    // UX_MSB: the most significant bit of a 64-bit fraction limb.
    private const ulong UxMsb = 0x8000_0000_0000_0000;

    // Binary128 (IEEE 754 quad) format constants, matching Intel's Q_* definitions for the 64-bit-word
    // configuration: 15-bit exponent field at bit 48 of the high word, bias 16383, 113-bit precision.
    private const int Float128ExponentBias = 16383;
    private const int Float128ExponentWidth = 15;
    private const int Float128ExponentPos = 48;
    private const int Float128Precision = 113;
    private const int Float128MinBinaryExponent = -16382;

    // SHIFT / CSHIFT from the engine: the fraction field is F_EXP_WIDTH bits below the packed limb.
    private const int UxShift = Float128ExponentWidth;        // 15
    private const int UxCShift = 64 - Float128ExponentWidth;  // 49

    // UX_ZERO_EXPONENT: MINUS_ONE << (F_EXP_WIDTH + 2).
    private const int UxZeroExponent = -1 << (Float128ExponentWidth + 2);

    // ADDSUB operation flags (Intel's dpml_ux.h). ADD drives the implicit operation; the higher bits
    // select magnitude-only and normalization behavior. The SUB/ADD_SUB/SUB_ADD selectors arrive with
    // the first core that uses them.
    private const int UxAdd = 0;
    private const int UxSub = 1;
    private const int UxMagnitudeOnly = 4;
    private const int UxNoNormalization = 8;
    private const int UxDoNormalization = 2 * UxNoNormalization; // 16

    // DIVIDE precision selectors (Intel's dpml_ux.h): HALF stops after the double-precision estimate;
    // FULL performs the integer refinement to the complete 128-bit significand.
    private const int DiyFp128HalfPrecision = 1;
    private const int DiyFp128FullPrecision = 2;

    // DIVIDE scaling constants (Intel's dpml_ux_ops_64.c), all exact powers of two.
    private const double TwoPow62 = 4611686018427387904.0;              // 2^62
    private const double TwoPow124 = TwoPow62 * TwoPow62;               // 2^124
    private const double RecipTwoPow16 = 1.0 / 65536.0;                 // 2^-16
    private const double RecipTwoPow60 = 1.0 / 1152921504606846976.0;   // 2^-60
    private const double RecipTwoPow184 = 4.0 / (TwoPow124 * TwoPow62); // 2^-184

    /// <summary>
    /// Normalizes an unpacked value so its most significant fraction bit is set, adjusting the exponent
    /// (Intel's <c>FFS_AND_SHIFT</c> with <c>FFS_NORMALIZE</c>). A zero fraction is canonicalized to the
    /// zero encoding. The shift is exact, so the leading-zero-count result matches Intel's bit search.
    /// </summary>
    private static void DiyFp128Normalize(ref DiyFp128 x)
    {
        ulong hi = x._hi;

        if ((hi & UxMsb) != 0)
        {
            // Already normalized.
            return;
        }

        ulong lo = x._lo;

        if ((hi | lo) == 0)
        {
            x._exponent = UxZeroExponent;
            x._sign = 0;
            return;
        }

        int cnt = 0;

        if (hi == 0)
        {
            hi = lo;
            lo = 0;
            cnt = 64;
        }

        int shift = (int)ulong.LeadingZeroCount(hi);

        if (shift != 0)
        {
            hi = (hi << shift) | (lo >> (64 - shift));
            lo <<= shift;
        }

        cnt += shift;
        x._hi = hi;
        x._lo = lo;
        x._exponent -= cnt;
    }

    /// <summary>
    /// Computes the high 128 bits of the product of two unpacked values (Intel's <c>MULTIPLY</c>). The
    /// low partial products are intentionally dropped, giving Intel's documented ~6 lsb error bound; the
    /// result is left un-normalized for the caller to normalize, exactly as the reference does.
    /// </summary>
    private static void DiyFp128Multiply(ref DiyFp128 x, ref DiyFp128 y, out DiyFp128 z)
    {
        ulong xHi = x._hi;
        ulong yHi = y._hi;
        ulong xLo = x._lo;
        ulong yLo = y._lo;

        ulong zLo = yHi * xHi;

        ulong p2 = Math.BigMul(yHi, xLo, out _);
        uint sign = x._sign ^ y._sign;
        int exponent = x._exponent + y._exponent;

        ulong p1 = Math.BigMul(yLo, xHi, out _);
        zLo += p2;
        ulong zHi = (zLo < p2) ? 1UL : 0UL;

        p2 = Math.BigMul(yHi, xHi, out _);
        zLo += p1;
        zHi += (zLo < p1) ? 1UL : 0UL;

        zHi += p2;

        z = new DiyFp128(sign, exponent, zHi, zLo);
    }

    /// <summary>
    /// Computes the exact 256-bit product of two unpacked values, returned as high and low unpacked
    /// halves (Intel's <c>EXTENDED_MULTIPLY</c>). The low half carries an exponent 128 less than the high.
    /// </summary>
    private static void DiyFp128ExtendedMultiply(ref DiyFp128 x, ref DiyFp128 y, out DiyFp128 hi, out DiyFp128 lo)
    {
        ulong xLo = x._lo;
        ulong yLo = y._lo;

        ulong p1 = yLo * xLo;
        ulong xHi = x._hi;
        ulong yHi = y._hi;

        ulong tmp = Math.BigMul(yLo, xLo, out _);
        uint sign = x._sign ^ y._sign;
        int exponent = x._exponent + y._exponent;
        ulong loLo = p1;

        p1 = yLo * xHi;

        ulong p2 = yHi * xLo;
        tmp += p1;
        ulong carry = (tmp < p1) ? 1UL : 0UL;

        p1 = xHi * yHi;
        tmp += p2;
        carry += (tmp < p2) ? 1UL : 0UL;
        ulong loHi = tmp;

        p2 = Math.BigMul(yHi, xLo, out _);
        tmp = p1 + carry;
        carry = (tmp < p1) ? 1UL : 0UL;

        p1 = Math.BigMul(yLo, xHi, out _);
        tmp += p2;
        carry += (tmp < p2) ? 1UL : 0UL;

        p2 = Math.BigMul(yHi, xHi, out _);
        tmp += p1;
        carry += (tmp < p1) ? 1UL : 0UL;
        ulong hiLo = tmp;

        ulong hiHi = p2 + carry;

        hi = new DiyFp128(sign, exponent, hiHi, hiLo);
        lo = new DiyFp128(sign, exponent - 128, loHi, loLo);
    }

    /// <summary>
    /// Adds and/or subtracts two unpacked values (Intel's <c>ADDSUB</c>). The larger operand is chosen by
    /// exponent, so operands may need explicit normalization first. <paramref name="result"/> receives one
    /// value for a single operation or two for the combined <c>ADD_SUB</c>/<c>SUB_ADD</c> forms.
    /// </summary>
    private static void DiyFp128AddSub(scoped in DiyFp128 xIn, scoped in DiyFp128 yIn, int flags, Span<DiyFp128> result)
    {
        DiyFp128 x = xIn;
        DiyFp128 y = yIn;

        uint sign = x._sign;
        int op = flags << 31;
        int tmp1 = (op ^ (int)sign) ^ (int)y._sign;
        int tmp2 = flags & UxMagnitudeOnly;
        sign = (tmp2 != 0) ? 0u : sign;
        op = (tmp2 != 0) ? op : tmp1;
        op = (op >> 31) & 1;

        DiyFp128 uxSave = default;
        int exponent = x._exponent;
        int shift = exponent - y._exponent;

        if (shift < 0)
        {
            (x, y) = (y, x);
            shift = -shift;
            exponent += shift;
            uxSave._sign = UxSignBit;
            sign ^= (op == UxAdd) ? 0u : UxSignBit;
        }

        // Align the digits of the smaller value (y).
        ulong lsd = y._lo;
        ulong msd = y._hi;

        int cnt = 2; // NUM_UX_FRACTION_DIGITS
        int cshift;
        while (true)
        {
            cshift = 64 - shift;
            if (cshift > 0)
            {
                break;
            }

            // DIGIT_SHIFT_FRACTION_RIGHT: move the high limb into the low, clearing the high.
            lsd = msd;
            msd = 0;
            shift = -cshift;

            if (--cnt == 0)
            {
                // Very large alignment shift: the smaller value is negligible.
                result[0] = x;
                result[0]._sign = sign;

                if ((flags & 0x2) != 0)
                {
                    result[1] = x;
                    result[1]._sign = sign ^ uxSave._sign;
                }

                return;
            }
        }

        if (shift != 0)
        {
            // BIT_SHIFT_FRACTION_RIGHT.
            lsd = (lsd >> shift) | (msd << cshift);
            msd >>= shift;
        }

        uxSave._hi = msd;
        uxSave._lo = lsd;

        while (true)
        {
            ulong tmpDigit = x._lo;
            ulong carry;

            if (op == UxAdd)
            {
                flags &= UxDoNormalization - 1;

                lsd += tmpDigit;
                carry = (lsd < tmpDigit) ? 1UL : 0UL;

                tmpDigit = x._hi;
                msd += carry;
                carry = (msd < carry) ? 1UL : 0UL;
                msd += tmpDigit;
                carry += (msd < tmpDigit) ? 1UL : 0UL;

                if (carry != 0)
                {
                    // Renormalize the single-bit overflow.
                    lsd = (lsd >> 1) | (msd << 63);
                    msd = (msd >> 1) | UxMsb;
                    exponent++;
                }
            }
            else
            {
                flags -= UxNoNormalization;

                carry = (lsd > tmpDigit) ? 1UL : 0UL;
                lsd = tmpDigit - lsd;

                tmpDigit = x._hi;
                msd += carry;
                carry = (msd < carry) ? 1UL : 0UL;
                msd = tmpDigit - msd;
                carry += (tmpDigit < msd) ? 1UL : 0UL;

                if (carry != 0)
                {
                    // Guessed the wrong operand order; negate the result.
                    sign ^= UxSignBit;
                    uxSave._sign = UxSignBit;
                    lsd = 0UL - lsd;
                    carry = (lsd == 0) ? 0UL : ulong.MaxValue;
                    msd = carry - msd;
                }
            }

            result[0]._hi = msd;
            result[0]._lo = lsd;
            result[0]._exponent = exponent;
            result[0]._sign = sign;

            if ((flags & UxDoNormalization) != 0)
            {
                DiyFp128Normalize(ref result[0]);
            }

            if ((flags & 0x2) == 0)
            {
                break;
            }

            // Combined ADD_SUB / SUB_ADD: produce the second result.
            op = 1 - op;
            flags ^= 0x2;
            result = result.Slice(1);
            msd = uxSave._hi;
            lsd = uxSave._lo;
            sign ^= uxSave._sign;
            exponent = x._exponent;
        }
    }

    /// <summary>
    /// Divides two unpacked values (Intel's <c>DIVIDE</c>). It estimates <c>1/b</c> in double precision to
    /// more than 70 bits with a Newton-style refinement, forms <c>q = a * (1/b)</c> in high/low double
    /// pieces, then (unless <paramref name="flags"/> is <see cref="DiyFp128HalfPrecision"/>) corrects the
    /// quotient to the full 128-bit significand with integer arithmetic. <paramref name="b"/> must be non-zero; it is normalized on a
    /// local copy if necessary, so the algorithm's assumption that the divisor is normalized holds.
    /// </summary>
    private static void DiyFp128Divide(scoped in DiyFp128 a, scoped in DiyFp128 b, int flags, out DiyFp128 c)
    {
        DiyFp128 bLocal = b;
        ulong b1 = bLocal._hi;
        ulong b2 = bLocal._lo;

        // If b isn't normalized the whole algorithm falls apart, so make sure that it is.
        if ((long)b1 >= 0)
        {
            DiyFp128Normalize(ref bLocal);
            b1 = bLocal._hi;
            b2 = bLocal._lo;
        }

        // Estimate 1/b in double precision to more than 70 bits: get an initial estimate and improve it
        // with a variation of Newton's iteration. TO_DOUBLE/TO_DIGIT are the signed integer<->double
        // conversions Intel uses (the operands are always non-negative and below 2^63 at these points).
        double r = TwoPow124 / (double)(long)(b1 >> 1);

        ulong mask = (1UL << 38) - 1;
        double bHi = (double)(long)((b1 & ~mask) >> 1);
        double bLo = RecipTwoPow16 * (double)(long)(((b1 & mask) << 15) | (b2 >> 49));

        ulong a1 = a._hi;
        ulong a2 = a._lo;

        uint sign = a._sign ^ bLocal._sign;
        int exponent = a._exponent - bLocal._exponent;

        // Get the high part of r as both an integer and a double, biasing it down so that r_lo stays
        // positive (see Intel's design note).
        ulong bigR = (ulong)(long)r;
        bigR = (bigR - (5UL << 8)) & ~((1UL << 36) - 1);
        double rHi = (double)(long)bigR;

        // 2*r_lo' = [ (2^124 - b_hi*r_hi) - b_lo*r_hi ] * (r / 2^184).
        double rLo = ((TwoPow124 - (bHi * rHi)) - (bLo * rHi)) * (RecipTwoPow184 * r);

        // q = a*(1/b), performed as q_hi + q_lo with a' biased below a so that the quotient stays < 2.
        double aFull = (double)(long)((a1 >> 11) << 10);
        double aHi = (double)(long)((a1 & ~mask) >> 1);
        double aLo = RecipTwoPow16 * (double)(long)(((a1 & mask) << 15) | (a2 >> 49));

        rHi *= RecipTwoPow60;
        double qHi = aHi * rHi;
        double qLo = (aLo * rHi) + (aFull * rLo);

        // Convert the high 65 bits of q_hi + q_lo into the integers S:Q1. Converting .25*q_hi avoids the
        // overflow a direct conversion of q_hi would cause.
        ulong q1 = (ulong)(long)(0.25 * qHi);
        ulong e = (ulong)(long)qLo;

        ulong s = q1 >> 62;
        q1 = (4 * q1) + e;
        s += (q1 < e) ? 1UL : 0UL;
        ulong q2 = 0;

        if (flags != DiyFp128HalfPrecision)
        {
            // Refine R to an integer approximation of 1/b (R/2^63 ~ 1/b); 2^64 saturates to 2^64 - 1.
            bigR = (bigR << 2) + (ulong)(long)(TwoPow62 * rLo);
            bigR = (bigR == 0) ? ~0UL : bigR;

            // Using S and Q1 as the current guess for the high 65 bits, compute the remainder N0:N1:N2
            // (N3 is not needed) of A - S':Q1'*B.
            mask = 0UL - s;

            ulong p11 = Math.BigMul(q1, b2, out _);
            ulong p01 = q1 * b1;
            ulong p00 = Math.BigMul(q1, b1, out _);

            ulong n2 = b2 & mask; // N2/N1 = B2/B1 when S == 1, 0 otherwise
            ulong n1 = b1 & mask;

            n2 += p11;
            ulong c1 = (n2 < p11) ? 1UL : 0UL;
            n2 += p01;
            c1 += (n2 < p01) ? 1UL : 0UL;

            n1 += p00;
            ulong n0 = (n1 < p00) ? 1UL : 0UL;
            n1 += c1;
            n0 += (n1 < c1) ? 1UL : 0UL;

            n0 = 0UL - n0;
            c1 = (a2 < n2) ? 1UL : 0UL;
            n2 = a2 - n2;
            n0 -= (a1 < n1) ? 1UL : 0UL;
            n1 = a1 - n1;
            n0 -= (n1 < c1) ? 1UL : 0UL;
            n1 -= c1;

            // The estimate to S:Q1 is off by at most one; derive the adjustment E and fix up N2.
            e = n0 | ((n1 != 0) ? 1UL : 0UL);
            mask = (e == 0) ? b1 : n0;
            n2 -= mask ^ b1;

            // Using R/2^63 ~ 1/b and the adjusted N2, approximate Q2. A high bit in Q2 means E was one
            // too low.
            q2 = Math.BigMul(bigR, n2, out _);

            e += ((long)q2 < 0) ? 1UL : 0UL;
            q2 = (2 * q2) + (((a1 | a2) != 0) ? 1UL : 0UL); // ensure 0/b is zero

            q1 += e;
            s = s + (ulong)((long)e >> 63) + ((q1 < e) ? 1UL : 0UL);
        }

        int shift = (int)s;
        c = new DiyFp128(
            sign,
            exponent + shift,
            (s << 63) | (q1 >> shift),
            ((q1 & s) << 63) | (q2 >> shift));
    }

    /// <summary>
    /// Unpacks a finite (normal or subnormal) binary128 value into <see cref="DiyFp128"/> form. Special
    /// classes (NaN/Infinity) are handled by the callers before reaching this path.
    /// </summary>
    private static DiyFp128 Float128UnpackFinite(UInt128 packed)
    {
        ulong word0 = packed.Upper;
        ulong word1 = packed.Lower;

        uint sign = (uint)((word0 & UxMsb) >> 32);
        int biasedExponent = (int)((word0 >> Float128ExponentPos) & ((1UL << Float128ExponentWidth) - 1));

        ulong hi = UxMsb | (word0 << UxShift) | (word1 >> UxCShift);
        ulong lo = word1 << UxShift;

        var result = new DiyFp128(sign, biasedExponent - Float128ExponentBias + 1, hi, lo);

        if (biasedExponent == 0)
        {
            if ((hi == UxMsb) && (lo == 0))
            {
                // +/-0.
                result._exponent = UxZeroExponent;
                return result;
            }

            // Subnormal: remove the (incorrectly assumed) hidden bit, adjust, and normalize.
            result._hi = hi - UxMsb;
            result._exponent++;
            DiyFp128Normalize(ref result);
        }

        return result;
    }

    /// <summary>
    /// Packs a finite <see cref="DiyFp128"/> into a binary128 bit pattern (Intel's <c>PACK</c>),
    /// including subnormal handling and the round-to-nearest step the reference applies. Overflow to
    /// infinity and NaN encodings are produced by the callers.
    /// </summary>
    private static UInt128 Float128PackFinite(DiyFp128 value)
    {
        DiyFp128Normalize(ref value);
        int exponent = value._exponent;

        if (exponent == UxZeroExponent)
        {
            // Encoded +/-0.
            return new UInt128((ulong)value._sign << 32, 0);
        }

        int shift = (Float128MinBinaryExponent + 1) - exponent;
        if (shift > 0)
        {
            // Subnormal: add the rounding boundary as a same-signed value so the shared rounding logic
            // below produces the correctly rounded denormal, then recover the biased exponent.
            var half = new DiyFp128(value._sign, exponent + shift, UxMsb, 0);
            DiyFp128 rounded = default;
            DiyFp128AddSub(half, value, UxAdd, new Span<DiyFp128>(ref rounded));
            value = rounded;

            exponent = 1 - Float128ExponentBias;
            if ((shift > Float128Precision) && (shift != -(UxZeroExponent - Float128MinBinaryExponent - 1)))
            {
                exponent--;
            }
        }

        // Round the 128-bit fraction to the 113-bit binary128 significand.
        ulong incr = 1UL << (Float128ExponentWidth - 1);

        ulong tmpDigit = value._lo;
        ulong currentDigit = tmpDigit + incr;
        ulong carry = (currentDigit < incr) ? 1UL : 0UL;
        currentDigit >>= UxShift;

        tmpDigit = value._hi;
        ulong nextDigit = tmpDigit + carry;
        carry = (nextDigit < carry) ? 1UL : 0UL;
        currentDigit |= nextDigit << UxCShift;
        ulong lowWord = currentDigit;
        currentDigit = nextDigit >> UxShift;

        if (carry != 0)
        {
            exponent++;
            currentDigit = UxMsb >> UxShift;
        }

        ulong biasedExponent = (ulong)(exponent + ((Float128ExponentBias - 1) - 1));
        currentDigit += biasedExponent << (UxCShift - 1);
        currentDigit |= (ulong)value._sign << 32;

        return new UInt128(currentDigit, lowWord);
    }
}
