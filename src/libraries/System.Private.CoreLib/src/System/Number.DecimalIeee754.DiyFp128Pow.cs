// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the power evaluation from the Intel(R) Decimal Floating-Point Math Library,
    // specifically `UX_POW` from `dpml_ux_pow.c`, the polynomial evaluator `EVALUATE_RATIONAL` (in its
    // `POST_MULTIPLY` and `STANDARD` forms) from `dpml_ux_ops_64.c`, and the log2 / 2^h constant tables
    // from `dpml_pow_x.h`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // The evaluation runs entirely in the software binary128 engine, so Decimal64/Decimal128 obtain the
    // full ~34-digit accuracy Intel's reference does. x^y is formed as 2^(y*log2(x)), carrying log2(x)
    // in high/low pieces so the integer part I = rint(y*log2(x)) separates exactly from the fractional
    // 2^h that the polynomial evaluates.

    private const ulong PowMsdOfLn2 = 0xB17217F7D1CF79AB; // dpml_pow_x.h high word of ln2
    private const int PowExponentGuard = Float128ExponentWidth + 2; // F_EXP_WIDTH + 2 overflow screen
    private const int UxOverflowExponent = 1 << Float128ExponentWidth;
    private const int UxUnderflowExponent = -(1 << Float128ExponentWidth);

    // Unpacked 2/ln2 and log2_lo/ln2 (dpml_pow_x.h).
    private static DiyFp128 PowTwoOverLn2 => new DiyFp128(0, 2, 0xB8AA3B295C17F0BB, 0xBE87FED0691D3E88);
    private static DiyFp128 PowLn2LoOverLn2 => new DiyFp128(0, -63, 0x91A1E8F29E45C2C0, 0xB3DC7E64505AD73A);

    // log2 fixed-point coefficients for pow (dpml_pow_x.h), degree 17, trailing exponent -4.
    private const int PowLog2Degree = 17;
    private const int PowLog2TrailingExponent = -4;

    private static readonly DiyFp128FixedCoefficient[] PowLog2Coefficients =
    [
        new(0x846F0CDB9C3D3269, 0x0000000000000116),
        new(0x0ED54DB254EC30FA, 0x000000000000072B),
        new(0xC9FA6284DFE33A4B, 0x00000000000041BC),
        new(0x99F674DEFC256DAA, 0x0000000000024519),
        new(0xD7B95B07FBD9EAF3, 0x0000000000143436),
        new(0xA13BA5817DBA85BC, 0x0000000000B4AAAB),
        new(0xB7A943E619ECD788, 0x0000000006587797),
        new(0x50FCDA140E2310DC, 0x00000000396C809C),
        new(0x20DC94F8FC4954A4, 0x000000020B9CBE4A),
        new(0x726AE205A00351A9, 0x00000012D2328609),
        new(0x746DF3952E72008C, 0x000000AF210E17E1),
        new(0x13599009FB43DEC4, 0x00000674700E7651),
        new(0xD038E4EAF62944CF, 0x00003E01D7C437DB),
        new(0xAEE9DF3B28865F8F, 0x00026219E54D1542),
        new(0x5D557E397A082390, 0x0018402256FD52E7),
        new(0x2932877A7AA6F59B, 0x0103950187A04E84),
        new(0x47A3ED398C267804, 0x0BD19A0FD62F144C),
        new(0x5079024EDD11FEE3, 0xA3FE9FFD641DA382),
    ];

    // 2^h fixed-point coefficients (dpml_pow_x.h), degree 22, trailing exponent 1.
    private const int Pow2Degree = 22;
    private const int Pow2TrailingExponent = 1;

    private static readonly DiyFp128FixedCoefficient[] Pow2Coefficients =
    [
        new(0x00002B4C151832AB, 0x0000000000000000),
        new(0x000561D142DDB787, 0x0000000000000000),
        new(0x00A2D67FD1C367C8, 0x0000000000000000),
        new(0x125A7DA057182134, 0x0000000000000000),
        new(0xF7176BC7BA507C6D, 0x0000000000000001),
        new(0x088968A28FAC4875, 0x0000000000000033),
        new(0xA26B9E85115B54C3, 0x00000000000004E3),
        new(0xA10EC0E8D6AB2988, 0x00000000000070DB),
        new(0x26AC3C533FCB6035, 0x0000000000098A4B),
        new(0x8B3687CE8532C06F, 0x0000000000C0B0C9),
        new(0x7E14C2F18E3A0B6B, 0x000000000E1DEB28),
        new(0x8DD9260757EE4711, 0x00000000F465639A),
        new(0xC764FB7ECC717D30, 0x0000000F267A8AC5),
        new(0x3E1ED2538C4CB47E, 0x000000DA929E9CAF),
        new(0x11FEC7FF3074CB1A, 0x00000B160111D2E4),
        new(0x1A1AC54731EE7AD0, 0x00007FF2FF1622C3),
        new(0xDBD2C2A261AA9A77, 0x00050C244BE1B1E1),
        new(0x20E2FED34A2A80B1, 0x002BB0FFCF14CE62),
        new(0x9CCBBE0B53EEB456, 0x013B2AB6FBA4E772),
        new(0xCCE9D8AECCAF4903, 0x071AC235C1282FE2),
        new(0x6F16B06EC9735FBE, 0x1EBFBDFF82C58EA8),
        new(0xE4F1D9CC01F97B59, 0x58B90BFBE8E7BCD5),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    /// <summary>
    /// Evaluates the pow log2 polynomial (Intel's <c>EVALUATE_RATIONAL</c> in its <c>POST_MULTIPLY</c>
    /// form): <c>p(z^2) * z^2</c>, then applies the trailing exponent. The caller supplies <c>z^2</c> and
    /// multiplies the result by <c>z</c> afterwards to form <c>z^3 * p(z^2)</c>.
    /// </summary>
    private static void DiyFp128EvaluatePowLog2Polynomial(scoped in DiyFp128 argumentSquared, out DiyFp128 result)
    {
        DiyFp128 argument = argumentSquared;
        DiyFp128Normalize(ref argument);

        long shift = -(long)PowLog2Degree * argument._exponent;
        DiyFp128EvaluatePositivePolynomial(argument, shift, PowLog2Coefficients, 0, PowLog2Degree, out result);

        DiyFp128 postMultiply = argument;
        DiyFp128Multiply(ref postMultiply, ref result, out result);
        result._exponent += PowLog2TrailingExponent;
    }

    /// <summary>
    /// Evaluates <c>2^h</c> for <c>|h| &lt; 1/2</c> (Intel's <c>EVALUATE_RATIONAL</c> in its
    /// <c>STANDARD</c> form): plain Horner in <paramref name="hIn"/>, then the trailing exponent.
    /// </summary>
    private static void DiyFp128EvaluatePow2Polynomial(scoped in DiyFp128 hIn, out DiyFp128 result)
    {
        DiyFp128 argument = hIn;
        DiyFp128Normalize(ref argument);

        long shift = -(long)Pow2Degree * argument._exponent;

        if (argument._sign != 0)
        {
            DiyFp128EvaluateNegativePolynomial(argument, shift, Pow2Coefficients, 0, Pow2Degree, out result);
        }
        else
        {
            DiyFp128EvaluatePositivePolynomial(argument, shift, Pow2Coefficients, 0, Pow2Degree, out result);
        }

        result._exponent += Pow2TrailingExponent;
    }

    /// <summary>
    /// Computes <c>x^y</c> for a positive finite <paramref name="x"/> (Intel's <c>UX_POW</c>). The caller
    /// handles the IEEE special cases and the sign of a negative base raised to an integer power.
    /// </summary>
    private static DiyFp128 DiyFp128Pow(DiyFp128 x, DiyFp128 y)
    {
        Span<DiyFp128> tmp = [default, default, default];
        DiyFp128 single = default;
        Span<DiyFp128> pair = [default, default];

        // Put x = 2^n * g with 1/sqrt(2) <= g < sqrt(2); the local exponent holds n.
        long exponent = x._exponent;

        if (x._hi <= LogOneOverSqrt2)
        {
            exponent--;
        }

        x._exponent -= (int)exponent;

        // z = 2(g - 1) / ((g + 1) * ln2)
        DiyFp128 one = DiyFp128One;
        DiyFp128AddSub(x, one, UxAddSub, pair); // pair[0] = g + 1, pair[1] = g - 1
        tmp[0] = pair[0];
        tmp[1] = pair[1];

        DiyFp128Divide(PowTwoOverLn2, tmp[0], DiyFp128FullPrecision, out DiyFp128 r);
        DiyFp128Multiply(ref r, ref tmp[1], out DiyFp128 z);

        // Combine n with the high bits of z into the integer log2Hi.
        ulong highZ = z._hi;
        ulong log2Hi;
        uint sign;

        if (exponent == 0)
        {
            log2Hi = highZ;
            exponent = z._exponent;
            sign = z._sign;
        }
        else
        {
            tmp[2] = DiyFp128FromWord(exponent);
            exponent = tmp[2]._exponent;
            long count = exponent - z._exponent;
            log2Hi = tmp[2]._hi;
            sign = tmp[2]._sign;

            if (count >= 64)
            {
                highZ = 0;
            }
            else
            {
                int c = (int)count;
                ulong highBits = highZ >> c;
                highZ = highBits << c;
                highBits = (z._sign != tmp[2]._sign) ? (0UL - highBits) : highBits;
                log2Hi += highBits;
            }
        }

        // log2_lo = z^3 * p(z^2)
        DiyFp128 zSquaredArgument = z;
        DiyFp128Multiply(ref zSquaredArgument, ref zSquaredArgument, out tmp[2]);
        DiyFp128EvaluatePowLog2Polynomial(tmp[2], out DiyFp128 log2Lo);
        DiyFp128 zMultiply = z;
        DiyFp128Multiply(ref zMultiply, ref log2Lo, out log2Lo);

        if (highZ != 0)
        {
            // Extended-precision correction z_lo = (t1 - t0*u)*r - z_hi*(ln2_lo/ln2).
            z._lo = 0;
            z._hi = highZ;

            ulong productHigh = Math.BigMul(highZ, PowMsdOfLn2, out ulong productLow);
            DiyFp128 u = new DiyFp128(z._sign, z._exponent - 1, productHigh, productLow);

            DiyFp128ExtendedMultiply(ref tmp[0], ref u, out DiyFp128 extendedHigh, out DiyFp128 extendedLow);
            tmp[0] = extendedHigh;
            tmp[2] = extendedLow;

            DiyFp128AddSub(tmp[1], tmp[0], UxSub, new Span<DiyFp128>(ref single)); tmp[0] = single;
            DiyFp128AddSub(tmp[0], tmp[2], UxSub, new Span<DiyFp128>(ref single)); tmp[0] = single;
            DiyFp128Multiply(ref tmp[0], ref r, out tmp[0]);

            DiyFp128 ln2LoOverLn2 = PowLn2LoOverLn2;
            DiyFp128Multiply(ref z, ref ln2LoOverLn2, out tmp[1]);
            DiyFp128AddSub(tmp[0], tmp[1], UxSub, new Span<DiyFp128>(ref single)); z = single;
        }

        DiyFp128AddSub(z, log2Lo, UxAdd, new Span<DiyFp128>(ref single)); log2Lo = single;

        // When x is very close to 1, promote high bits of log2_lo into log2Hi.
        ulong increment = log2Lo._hi;
        long shiftCount = exponent - log2Lo._exponent;

        if (shiftCount < 64)
        {
            int c = (int)shiftCount;
            ulong mask = (c <= 0) ? 0UL : ((1UL << c) - 1);
            log2Lo._hi = increment & mask;
            increment = (c > 0) ? (increment >> c) : increment;
            increment = ((sign ^ log2Lo._sign) != 0) ? (0UL - increment) : increment;
            log2Hi += increment;
        }

        tmp[0] = new DiyFp128(sign, (int)exponent, log2Hi, 0);
        exponent += y._exponent;

        if (exponent > PowExponentGuard)
        {
            int overflowExponent = ((sign ^ y._sign) != 0) ? UxUnderflowExponent : UxOverflowExponent;
            return new DiyFp128(0, overflowExponent, UxMsb, 0);
        }

        // I = rint(y*log2(x)); h = y*log2(x) - I.
        ulong integerPart = 0;
        int roundShift = 0;
        sign ^= y._sign;

        DiyFp128 yMultiply = y;
        DiyFp128ExtendedMultiply(ref tmp[0], ref yMultiply, out DiyFp128 productHi, out DiyFp128 productLo);
        DiyFp128 h = productHi;
        tmp[0] = productLo;

        if (exponent >= 0)
        {
            integerPart = Math.BigMul(log2Hi, y._hi, out _);
            roundShift = 64 - (int)exponent;

            ulong roundBit = 1UL << (roundShift - 1);
            ulong rounded = integerPart + roundBit;
            roundBit += roundBit;

            if (rounded >= integerPart)
            {
                integerPart = rounded & (0UL - roundBit);
            }
            else
            {
                // A carry out occurred on the increment.
                roundShift--;
                integerPart = UxMsb;
                exponent++;
            }

            tmp[1] = new DiyFp128(sign, (int)exponent, integerPart, 0);
            DiyFp128AddSub(h, tmp[1], UxSub, new Span<DiyFp128>(ref single)); h = single;
            DiyFp128AddSub(h, tmp[0], UxAdd, new Span<DiyFp128>(ref single)); h = single;
        }

        DiyFp128 logLoMultiply = y;
        DiyFp128Multiply(ref logLoMultiply, ref log2Lo, out tmp[0]);
        DiyFp128AddSub(tmp[0], h, UxAdd, new Span<DiyFp128>(ref single)); h = single;

        DiyFp128EvaluatePow2Polynomial(h, out DiyFp128 result);

        integerPart = (roundShift >= 64) ? 0UL : (integerPart >> roundShift);
        ulong negated = 0UL - integerPart;
        integerPart = (sign != 0) ? negated : integerPart;
        result._exponent += (int)integerPart;
        return result;
    }
}
