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

    private const ulong PowMsdOfLn2 = 0xb17217f7d1cf79ab; // dpml_pow_x.h high word of ln2
    private const int PowExponentGuard = Float128ExponentWidth + 2; // F_EXP_WIDTH + 2 overflow screen
    private const int UxOverflowExponent = 1 << Float128ExponentWidth;
    private const int UxUnderflowExponent = -(1 << Float128ExponentWidth);

    // Unpacked 2/ln2 and log2_lo/ln2 (dpml_pow_x.h).
    private static DiyFp128 PowTwoOverLn2 => new DiyFp128(0, 2, 0xb8aa3b295c17f0bb, 0xbe87fed0691d3e88);
    private static DiyFp128 PowLn2LoOverLn2 => new DiyFp128(0, -63, 0x91a1e8f29e45c2c0, 0xb3dc7e64505ad73a);

    // log2 fixed-point coefficients for pow (dpml_pow_x.h), degree 17, trailing exponent -4.
    private const int PowLog2Degree = 17;
    private const int PowLog2TrailingExponent = -4;

    private static readonly DiyFp128FixedCoefficient[] PowLog2Coefficients =
    [
        new(0x846f0cdb9c3d3269, 0x0000000000000116),
        new(0x0ed54db254ec30fa, 0x000000000000072b),
        new(0xc9fa6284dfe33a4b, 0x00000000000041bc),
        new(0x99f674defc256daa, 0x0000000000024519),
        new(0xd7b95b07fbd9eaf3, 0x0000000000143436),
        new(0xa13ba5817dba85bc, 0x0000000000b4aaab),
        new(0xb7a943e619ecd788, 0x0000000006587797),
        new(0x50fcda140e2310dc, 0x00000000396c809c),
        new(0x20dc94f8fc4954a4, 0x000000020b9cbe4a),
        new(0x726ae205a00351a9, 0x00000012d2328609),
        new(0x746df3952e72008c, 0x000000af210e17e1),
        new(0x13599009fb43dec4, 0x00000674700e7651),
        new(0xd038e4eaf62944cf, 0x00003e01d7c437db),
        new(0xaee9df3b28865f8f, 0x00026219e54d1542),
        new(0x5d557e397a082390, 0x0018402256fd52e7),
        new(0x2932877a7aa6f59b, 0x0103950187a04e84),
        new(0x47a3ed398c267804, 0x0bd19a0fd62f144c),
        new(0x5079024edd11fee3, 0xa3fe9ffd641da382),
    ];

    // 2^h fixed-point coefficients (dpml_pow_x.h), degree 22, trailing exponent 1.
    private const int Pow2Degree = 22;
    private const int Pow2TrailingExponent = 1;

    private static readonly DiyFp128FixedCoefficient[] Pow2Coefficients =
    [
        new(0x00002b4c151832ab, 0x0000000000000000),
        new(0x000561d142ddb787, 0x0000000000000000),
        new(0x00a2d67fd1c367c8, 0x0000000000000000),
        new(0x125a7da057182134, 0x0000000000000000),
        new(0xf7176bc7ba507c6d, 0x0000000000000001),
        new(0x088968a28fac4875, 0x0000000000000033),
        new(0xa26b9e85115b54c3, 0x00000000000004e3),
        new(0xa10ec0e8d6ab2988, 0x00000000000070db),
        new(0x26ac3c533fcb6035, 0x0000000000098a4b),
        new(0x8b3687ce8532c06f, 0x0000000000c0b0c9),
        new(0x7e14c2f18e3a0b6b, 0x000000000e1deb28),
        new(0x8dd9260757ee4711, 0x00000000f465639a),
        new(0xc764fb7ecc717d30, 0x0000000f267a8ac5),
        new(0x3e1ed2538c4cb47e, 0x000000da929e9caf),
        new(0x11fec7ff3074cb1a, 0x00000b160111d2e4),
        new(0x1a1ac54731ee7ad0, 0x00007ff2ff1622c3),
        new(0xdbd2c2a261aa9a77, 0x00050c244be1b1e1),
        new(0x20e2fed34a2a80b1, 0x002bb0ffcf14ce62),
        new(0x9ccbbe0b53eeb456, 0x013b2ab6fba4e772),
        new(0xcce9d8aeccaf4903, 0x071ac235c1282fe2),
        new(0x6f16b06ec9735fbe, 0x1ebfbdff82c58ea8),
        new(0xe4f1d9cc01f97b59, 0x58b90bfbe8e7bcd5),
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
