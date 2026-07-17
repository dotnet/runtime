// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the inverse-trigonometric evaluation from the Intel(R) Decimal
    // Floating-Point Math Library, specifically `UX_ATAN2` and `UX_ASIN_ACOS` from
    // `dpml_ux_inv_trig.c`, the atan/asin coefficient tables and constant table from
    // `dpml_inv_trig_x.h`, and the rational-evaluation driver `EVALUATE_RATIONAL` from
    // `dpml_ux_ops_64.c` (shared with the forward trig port).
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // Decimal32 evaluates the inverse trig functions in binary64; Decimal64/Decimal128 route
    // through this engine so the wider formats keep their precision.

    private const int InvTrigAtanMapWidth = 4;
    private const int InvTrigAsinMapWidth = 6;
    private const int InvTrigAtanDegree = 0xb;
    private const int InvTrigAsinDegree = 0xb;

    // ASIN/ACOS interval maps (dpml_ux_inv_trig.c), precomputed from ASIN_MAP_FIELD.
    private const int InvTrigAsinMap = 0xf04e00;
    private const int InvTrigAcosMap = 0x1a30038;

    private static Float128 InvTrigOneThird => new Float128(0, -1, 0xaaaaaaaaaaaaaaaa, 0xaaaaaaaaaaaaaaaa);

    // INV_TRIG_CONS_BASE (dpml_inv_trig_x.h): 0, pi/4, pi/2, 3pi/4, pi. Intel spaces these entries
    // 24 bytes apart, so the packed byte offset divided by 24 selects the constant.
    private static readonly Float128[] InvTrigConstants =
    [
        new Float128(0, UxZeroExponent, 0, 0),                      // 0
        new Float128(0, 0, 0xc90fdaa22168c234, 0xc4c6628b80dc1cd1), // pi/4
        new Float128(0, 1, 0xc90fdaa22168c234, 0xc4c6628b80dc1cd1), // pi/2
        new Float128(0, 2, 0x96cbe3f9990e91a7, 0x9394c9e8a0a5159c), // 3pi/4
        new Float128(0, 2, 0xc90fdaa22168c234, 0xc4c6628b80dc1cd1), // pi
    ];

    private static readonly Float128FixedCoefficient[] InvTrigAtanNumeratorCoefficients =
    [
        new(0x0000000000000000, 0x0000000000000000),
        new(0x9b21db1817b033de, 0x00000000036a28b8),
        new(0x7af48d0cbbb9e258, 0x00000004a9d8aeac),
        new(0x710b595cb5f5477a, 0x000001d601b80364),
        new(0x82ff5ad5bdc83502, 0x00005360db2203cd),
        new(0xa46ea356b3ace8e0, 0x000803a15271c15d),
        new(0x511728bc47fd897a, 0x00752012d71df9b4),
        new(0xb0eebd1d38e6ccd7, 0x04261aad0c0e0aef),
        new(0x715215ee2223a644, 0x178d58e7069e5e06),
        new(0x1a5b5968daa31b09, 0x515e68b909775969),
        new(0xa67de44d68db7ef7, 0x9c53edb8b65e0e57),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly Float128FixedCoefficient[] InvTrigAtanDenominatorCoefficients =
    [
        new(0x753b0a86a07a791a, 0x0000000000060285),
        new(0xb62b5e42f41004bb, 0x000000001a6a8474),
        new(0x6af09bc24e1e2dad, 0x00000012cf340cf3),
        new(0x49426ee8106af1a7, 0x00000523bce40e29),
        new(0xd77ad56c6ccae258, 0x0000b5f6388d7935),
        new(0x95aa5864a5d93fd4, 0x000e856c505d9ab5),
        new(0xf9512f8649a8f559, 0x00b744f2c988c73a),
        new(0x247ce9cc4ddd2493, 0x05c2135495031b41),
        new(0xc6922892f40a72fc, 0x1d8eb88dde3bc4f4),
        new(0x0785210e97ff604a, 0x5daf5bd2629e79e5),
        new(0x51288ef813862999, 0xa6fe98636108b902),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly Float128FixedCoefficient[] InvTrigAsinNumeratorCoefficients =
    [
        new(0xbc844bd3285a9adb, 0x000000000018a298),
        new(0x24543a40ff2fc62e, 0x000000004b712f53),
        new(0x2553512c4db90d47, 0x0000002b42b22a11),
        new(0x4670c8ac9560de1d, 0x00000a0239855097),
        new(0x022dda0e53ef4cb8, 0x00013575bd533bc9),
        new(0xafc38a68688e8800, 0x00160d59ece50095),
        new(0x6123e0eea5f3e527, 0x00fcc7ee91e17495),
        new(0xfa699043ffd8cc09, 0x074facfd5647265e),
        new(0x7dd602b0df4a1e6d, 0x22edbcfce68005c2),
        new(0xa938fa69d688d50a, 0x67f826ed129b3e51),
        new(0xff93b5cb3865c5f2, 0xaf5c9b73f163dd08),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly Float128FixedCoefficient[] InvTrigAsinDenominatorCoefficients =
    [
        new(0xede27d48152467c1, 0x0000000000882734),
        new(0x1d75e618be470341, 0x00000000ca5275d0),
        new(0x001c0ab3c7d6f6e2, 0x000000559cc8243b),
        new(0x36449091ea1af30d, 0x000010830f45b29d),
        new(0x9692608b4850f9dd, 0x0001c28a726a35f0),
        new(0x755313b950b194c6, 0x001d43c1aa0112de),
        new(0x555ff65fd5bd1184, 0x013820000042983f),
        new(0xa448034f044ad977, 0x0884c1099a59728a),
        new(0x0743cfa35361e105, 0x26caad31c3ec7bec),
        new(0x5329169c42d6fdeb, 0x6ee5f75bdbf406d1),
        new(0x54e90b208dbb1b38, 0xb4b1f0c946b9325e),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // UX_ATAN2. When haveX is false this is the single-argument atan (Intel's null x pointer, aux_x = 1).
    private static Float128 Float128Atan2(Float128 y, Float128 x, bool haveX)
    {
        Float128 one = new Float128(0, 1, UxMsb, 0);
        int quotientExponent;
        Float128 auxX;
        uint sign;

        if (!haveX)
        {
            quotientExponent = y._exponent;
            auxX = one;
            x = one; // Intel treats the null x pointer as 1 in the divide/reduction.
            sign = 0;
        }
        else
        {
            quotientExponent = y._exponent - x._exponent;
            auxX = x;
            sign = x._sign;
            x._sign = 0;
            long diff = unchecked((long)y._hi - (long)x._hi);
            if (quotientExponent >= 0)
            {
                quotientExponent -= (diff == 0 && quotientExponent > 0) ? 1 : 0;
            }
            quotientExponent += (diff >= 0) ? 1 : 0;
        }

        int index = (sign != 0) ? 3 * InvTrigAtanMapWidth : 0;
        uint signY = y._sign;
        y._sign = 0;

        if (quotientExponent > 1)
        {
            // Reduced argument is x/y.
            index += 2 * InvTrigAtanMapWidth;
            (x, y) = (y, x);
            sign ^= UxSignBit;
        }
        else if (quotientExponent >= 0)
        {
            // Reduced argument is (y-x)/(y+x).
            index += InvTrigAtanMapWidth;
            Span<Float128> tmp = stackalloc Float128[2];
            Float128AddSub(y, auxX, UxAddSub | UxMagnitudeOnly | UxNoNormalization, tmp);
            y = tmp[1];
            x = tmp[0];
            Float128Normalize(ref y);
        }

        Float128Divide(y, x, Float128FullPrecision, out Float128 reduced);

        quotientExponent = reduced._exponent;
        if ((UxMsb & reduced._hi) == 0)
        {
            quotientExponent--;
        }
        if (quotientExponent >= 0)
        {
            // Force the reduced argument below 1/2; substitute 1/3 to keep the rational well-defined.
            index -= InvTrigAtanMapWidth;
            sign ^= UxSignBit;
            reduced = InvTrigOneThird;
        }

        reduced._exponent += 1; // P_SCALE(1)
        Span<Float128> result = stackalloc Float128[2];
        int flags = (TrigSquareTerm | TrigPostMultiply) | (TrigSquareTerm << TrigNumeratorFieldWidth);
        Float128EvaluateRational(reduced, InvTrigAtanNumeratorCoefficients, 0, InvTrigAtanDenominatorCoefficients, 1, InvTrigAtanDegree, flags, result);
        Float128 value = result[0];

        value._sign ^= sign;
        if (index != 0)
        {
            long map = ((long)0 << (0 * InvTrigAtanMapWidth))
                     + ((long)24 << (1 * InvTrigAtanMapWidth))
                     + ((long)48 << (2 * InvTrigAtanMapWidth))
                     + ((long)96 << (3 * InvTrigAtanMapWidth))
                     + ((long)72 << (4 * InvTrigAtanMapWidth))
                     + ((long)48 << (5 * InvTrigAtanMapWidth));
            int constantOffset = (int)((map >> index) & (0xFL << 3));
            Float128Normalize(ref value);
            Span<Float128> sum = stackalloc Float128[1];
            Float128AddSub(InvTrigConstants[constantOffset / 24], value, UxAdd | UxNoNormalization, sum);
            value = sum[0];
        }

        value._sign = signY;
        return value;
    }

    private static Float128 Float128Atan(scoped in Float128 arg) => Float128Atan2(arg, default, false);

    // UX_ASIN_ACOS with the asin/acos interval maps precomputed. Callers guarantee |arg| <= 1.
    private static Float128 Float128AsinAcos(Float128 arg, bool isAcos)
    {
        int indexMap = isAcos ? InvTrigAcosMap : InvTrigAsinMap;

        int index = (arg._sign != 0) ? 2 * InvTrigAsinMapWidth : 0;
        arg._sign = 0;
        int exponent = arg._exponent;
        int exponentIncrement = 0;

        if (exponent >= 0)
        {
            index += InvTrigAsinMapWidth;
            if (exponent < 1)
            {
                // 1/2 <= |x| < 1: compute sqrt((1-x)/2).
                exponentIncrement = 1;
                Span<Float128> t = stackalloc Float128[1];
                Float128AddSub(new Float128(0, 1, UxMsb, 0), arg, UxSub | UxMagnitudeOnly, t);
                arg = t[0];
                arg._exponent -= 1;
                arg = Float128Sqrt(arg);
            }
            else if (exponent == 1 && arg._hi == UxMsb && arg._lo == 0)
            {
                // |x| == 1: the reduced argument is zero.
                arg = new Float128(0, UxZeroExponent, 0, 0);
            }
        }

        arg._exponent += 1; // P_SCALE(1)
        Span<Float128> result = stackalloc Float128[2];
        int flags = (TrigSquareTerm | TrigPostMultiply | TrigAlternateSign)
                  | ((TrigSquareTerm | TrigAlternateSign) << TrigNumeratorFieldWidth);
        Float128EvaluateRational(arg, InvTrigAsinNumeratorCoefficients, 0, InvTrigAsinDenominatorCoefficients, 1, InvTrigAsinDegree, flags, result);
        Float128 value = result[0];

        int mapInfo = indexMap >> index;
        value._sign = ((mapInfo & 8) != 0) ? UxSignBit : 0;
        value._exponent += exponentIncrement;

        Span<Float128> sum = stackalloc Float128[1];
        Float128AddSub(InvTrigConstants[(mapInfo & 0xf0) / 24], value, UxAdd | UxNoNormalization, sum);
        value = sum[0];

        value._sign = ((mapInfo & 4) != 0) ? UxSignBit : 0;
        return value;
    }

    private static Float128 Float128Asin(scoped in Float128 arg) => Float128AsinAcos(arg, false);

    private static Float128 Float128Acos(scoped in Float128 arg) => Float128AsinAcos(arg, true);

    // True when a normalized, non-zero |arg| is strictly greater than 1 (outside the asin/acos domain).
    private static bool Float128MagnitudeExceedsOne(in Float128 arg)
        => arg._exponent > 1 || (arg._exponent == 1 && (arg._hi != UxMsb || arg._lo != 0));

    private static bool Float128MagnitudeIsOne(in Float128 arg)
        => arg._exponent == 1 && arg._hi == UxMsb && arg._lo == 0;
}
