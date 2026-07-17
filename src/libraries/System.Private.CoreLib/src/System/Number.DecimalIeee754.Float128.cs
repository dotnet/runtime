// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the unpacked "x_float" software binary128 engine (the "ux" routines)
    // from the Intel(R) Decimal Floating-Point Math Library, specifically `MULTIPLY`,
    // `EXTENDED_MULTIPLY`, `ADDSUB`, and `FFS_AND_SHIFT` from `dpml_ux_ops_64.c` / `dpml_ux_ops.c`,
    // and the finite unpack/pack from `UNPACK_X_OR_Y` / `PACK`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // The decimal transcendental operations route through this software binary128 core exactly as
    // Intel does (Decimal64/Decimal128 evaluate in binary128; Decimal32 stays on binary64). This is
    // the 64-bit-word specialization of Intel's engine (`NUM_UX_FRACTION_DIGITS == 2`), so the
    // 128-bit significand is a pair of <see cref="ulong"/> limbs. Intel's table-driven exception
    // dispatcher (the `class_to_action_map` machinery inside `UNPACK_X_OR_Y`/`PACK`) is intentionally
    // not ported; NaN/Infinity/zero canonicalization is handled explicitly by the per-function
    // wrappers, matching the existing exact operations. That does not affect the result bits of any
    // finite computation.

    /// <summary>
    /// An unpacked software binary128 value (Intel's <c>UX_FLOAT</c>). The represented value is
    /// <c>(-1)^sign * fraction * 2^(exponent - 128)</c>, where the 128-bit <c>fraction</c> is held in
    /// two 64-bit limbs and, when normalized, lies in <c>[2^127, 2^128)</c> (its high bit is set).
    /// </summary>
    internal struct Float128
    {
        // The sign is stored as Intel does (0 for positive, 0x8000_0000 for negative) so the XOR-based
        // sign arithmetic in Multiply/AddSub ports verbatim.
        internal uint _sign;
        internal int _exponent;
        internal ulong _hi; // fraction[0] == G_UX_MSD (most significant limb)
        internal ulong _lo; // fraction[1] == G_UX_LSD (least significant limb)

        internal Float128(uint sign, int exponent, ulong hi, ulong lo)
        {
            _sign = sign;
            _exponent = exponent;
            _hi = hi;
            _lo = lo;
        }

        internal readonly bool IsNegative => _sign != 0;
    }

    // UX_SIGN_BIT: sign flag stored in Float128._sign.
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
    private const int UxMagnitudeOnly = 4;
    private const int UxNoNormalization = 8;
    private const int UxDoNormalization = 2 * UxNoNormalization; // 16

    /// <summary>
    /// Normalizes an unpacked value so its most significant fraction bit is set, adjusting the exponent
    /// (Intel's <c>FFS_AND_SHIFT</c> with <c>FFS_NORMALIZE</c>). A zero fraction is canonicalized to the
    /// zero encoding. The shift is exact, so the leading-zero-count result matches Intel's bit search.
    /// </summary>
    private static void Float128Normalize(ref Float128 x)
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
    private static void Float128Multiply(ref Float128 x, ref Float128 y, out Float128 z)
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

        z = new Float128(sign, exponent, zHi, zLo);
    }

    /// <summary>
    /// Computes the exact 256-bit product of two unpacked values, returned as high and low unpacked
    /// halves (Intel's <c>EXTENDED_MULTIPLY</c>). The low half carries an exponent 128 less than the high.
    /// </summary>
    private static void Float128ExtendedMultiply(ref Float128 x, ref Float128 y, out Float128 hi, out Float128 lo)
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

        hi = new Float128(sign, exponent, hiHi, hiLo);
        lo = new Float128(sign, exponent - 128, loHi, loLo);
    }

    /// <summary>
    /// Adds and/or subtracts two unpacked values (Intel's <c>ADDSUB</c>). The larger operand is chosen by
    /// exponent, so operands may need explicit normalization first. <paramref name="result"/> receives one
    /// value for a single operation or two for the combined <c>ADD_SUB</c>/<c>SUB_ADD</c> forms.
    /// </summary>
    private static void Float128AddSub(scoped in Float128 xIn, scoped in Float128 yIn, int flags, Span<Float128> result)
    {
        Float128 x = xIn;
        Float128 y = yIn;

        uint sign = x._sign;
        int op = flags << 31;
        int tmp1 = (op ^ (int)sign) ^ (int)y._sign;
        int tmp2 = flags & UxMagnitudeOnly;
        sign = (tmp2 != 0) ? 0u : sign;
        op = (tmp2 != 0) ? op : tmp1;
        op = (op >> 31) & 1;

        Float128 uxSave = default;
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
                Float128Normalize(ref result[0]);
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
    /// Unpacks a finite (normal or subnormal) binary128 value into <see cref="Float128"/> form. Special
    /// classes (NaN/Infinity) are handled by the callers before reaching this path.
    /// </summary>
    private static Float128 Float128UnpackFinite(UInt128 packed)
    {
        ulong word0 = (ulong)(packed >> 64);
        ulong word1 = (ulong)packed;

        uint sign = (uint)((word0 & UxMsb) >> 32);
        int biasedExponent = (int)((word0 >> Float128ExponentPos) & ((1UL << Float128ExponentWidth) - 1));

        ulong hi = UxMsb | (word0 << UxShift) | (word1 >> UxCShift);
        ulong lo = word1 << UxShift;

        var result = new Float128(sign, biasedExponent - Float128ExponentBias + 1, hi, lo);

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
            Float128Normalize(ref result);
        }

        return result;
    }

    /// <summary>
    /// Packs a finite <see cref="Float128"/> into a binary128 bit pattern (Intel's <c>PACK</c>),
    /// including subnormal handling and the round-to-nearest step the reference applies. Overflow to
    /// infinity and NaN encodings are produced by the callers.
    /// </summary>
    private static UInt128 Float128PackFinite(Float128 value)
    {
        Float128Normalize(ref value);
        int exponent = value._exponent;

        if (exponent == UxZeroExponent)
        {
            // Encoded +/-0.
            return (UInt128)((ulong)value._sign << 32) << 64;
        }

        int shift = (Float128MinBinaryExponent + 1) - exponent;
        if (shift > 0)
        {
            // Subnormal: add the rounding boundary as a same-signed value so the shared rounding logic
            // below produces the correctly rounded denormal, then recover the biased exponent.
            var half = new Float128(value._sign, exponent + shift, UxMsb, 0);
            Span<Float128> rounded = stackalloc Float128[1];
            Float128AddSub(half, value, UxAdd, rounded);
            value = rounded[0];

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

        return ((UInt128)currentDigit << 64) | lowWord;
    }
}
