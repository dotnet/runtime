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
    private const int InvTrigAtanDegree = 0xB;
    private const int InvTrigAsinDegree = 0xB;

    // ASIN/ACOS interval maps (dpml_ux_inv_trig.c), precomputed from ASIN_MAP_FIELD.
    private const int InvTrigAsinMap = 0xF04E00;
    private const int InvTrigAcosMap = 0x1A30038;

    private static DiyFp128 InvTrigOneThird => new DiyFp128(0, -1, 0xAAAAAAAAAAAAAAAA, 0xAAAAAAAAAAAAAAAA);

    // INV_TRIG_CONS_BASE (dpml_inv_trig_x.h): 0, pi/4, pi/2, 3pi/4, pi. Intel spaces these entries
    // 24 bytes apart, so the packed byte offset divided by 24 selects the constant.
    private static readonly DiyFp128[] InvTrigConstants =
    [
        new DiyFp128(0, UxZeroExponent, 0, 0),                      // 0
        new DiyFp128(0, 0, 0xC90FDAA22168C234, 0xC4C6628B80DC1CD1), // pi/4
        new DiyFp128(0, 1, 0xC90FDAA22168C234, 0xC4C6628B80DC1CD1), // pi/2
        new DiyFp128(0, 2, 0x96CBE3F9990E91A7, 0x9394C9E8A0A5159C), // 3pi/4
        new DiyFp128(0, 2, 0xC90FDAA22168C234, 0xC4C6628B80DC1CD1), // pi
    ];

    private static readonly DiyFp128FixedCoefficient[] InvTrigAtanNumeratorCoefficients =
    [
        new(0x0000000000000000, 0x0000000000000000),
        new(0x9B21DB1817B033DE, 0x00000000036A28B8),
        new(0x7AF48D0CBBB9E258, 0x00000004A9D8AEAC),
        new(0x710B595CB5F5477A, 0x000001D601B80364),
        new(0x82FF5AD5BDC83502, 0x00005360DB2203CD),
        new(0xA46EA356B3ACE8E0, 0x000803A15271C15D),
        new(0x511728BC47FD897A, 0x00752012D71DF9B4),
        new(0xB0EEBD1D38E6CCD7, 0x04261AAD0C0E0AEF),
        new(0x715215EE2223A644, 0x178D58E7069E5E06),
        new(0x1A5B5968DAA31B09, 0x515E68B909775969),
        new(0xA67DE44D68DB7EF7, 0x9C53EDB8B65E0E57),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] InvTrigAtanDenominatorCoefficients =
    [
        new(0x753B0A86A07A791A, 0x0000000000060285),
        new(0xB62B5E42F41004BB, 0x000000001A6A8474),
        new(0x6AF09BC24E1E2DAD, 0x00000012CF340CF3),
        new(0x49426EE8106AF1A7, 0x00000523BCE40E29),
        new(0xD77AD56C6CCAE258, 0x0000B5F6388D7935),
        new(0x95AA5864A5D93FD4, 0x000E856C505D9AB5),
        new(0xF9512F8649A8F559, 0x00B744F2C988C73A),
        new(0x247CE9CC4DDD2493, 0x05C2135495031B41),
        new(0xC6922892F40A72FC, 0x1D8EB88DDE3BC4F4),
        new(0x0785210E97FF604A, 0x5DAF5BD2629E79E5),
        new(0x51288EF813862999, 0xA6FE98636108B902),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] InvTrigAsinNumeratorCoefficients =
    [
        new(0xBC844BD3285A9ADB, 0x000000000018A298),
        new(0x24543A40FF2FC62E, 0x000000004B712F53),
        new(0x2553512C4DB90D47, 0x0000002B42B22A11),
        new(0x4670C8AC9560DE1D, 0x00000A0239855097),
        new(0x022DDA0E53EF4CB8, 0x00013575BD533BC9),
        new(0xAFC38A68688E8800, 0x00160D59ECE50095),
        new(0x6123E0EEA5F3E527, 0x00FCC7EE91E17495),
        new(0xFA699043FFD8CC09, 0x074FACFD5647265E),
        new(0x7DD602B0DF4A1E6D, 0x22EDBCFCE68005C2),
        new(0xA938FA69D688D50A, 0x67F826ED129B3E51),
        new(0xFF93B5CB3865C5F2, 0xAF5C9B73F163DD08),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] InvTrigAsinDenominatorCoefficients =
    [
        new(0xEDE27D48152467C1, 0x0000000000882734),
        new(0x1D75E618BE470341, 0x00000000CA5275D0),
        new(0x001C0AB3C7D6F6E2, 0x000000559CC8243B),
        new(0x36449091EA1AF30D, 0x000010830F45B29D),
        new(0x9692608B4850F9DD, 0x0001C28A726A35F0),
        new(0x755313B950B194C6, 0x001D43C1AA0112DE),
        new(0x555FF65FD5BD1184, 0x013820000042983F),
        new(0xA448034F044AD977, 0x0884C1099A59728A),
        new(0x0743CFA35361E105, 0x26CAAD31C3EC7BEC),
        new(0x5329169C42D6FDEB, 0x6EE5F75BDBF406D1),
        new(0x54E90B208DBB1B38, 0xB4B1F0C946B9325E),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // UX_ATAN2. When haveX is false this is the single-argument atan (Intel's null x pointer, aux_x = 1).
    private static DiyFp128 DiyFp128Atan2(DiyFp128 y, DiyFp128 x, bool haveX)
    {
        DiyFp128 one = new DiyFp128(0, 1, UxMsb, 0);
        int quotientExponent;
        DiyFp128 auxX;
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
            Span<DiyFp128> tmp = [default, default];
            DiyFp128AddSub(y, auxX, UxAddSub | UxMagnitudeOnly | UxNoNormalization, tmp);
            y = tmp[1];
            x = tmp[0];
            DiyFp128Normalize(ref y);
        }

        DiyFp128Divide(y, x, DiyFp128FullPrecision, out DiyFp128 reduced);

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
        Span<DiyFp128> result = [default, default];
        int flags = (TrigSquareTerm | TrigPostMultiply) | (TrigSquareTerm << TrigNumeratorFieldWidth);
        DiyFp128EvaluateRational(reduced, InvTrigAtanNumeratorCoefficients, 0, InvTrigAtanDenominatorCoefficients, 1, InvTrigAtanDegree, flags, result);
        DiyFp128 value = result[0];

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
            DiyFp128Normalize(ref value);
            DiyFp128 sum = default;
            DiyFp128AddSub(InvTrigConstants[constantOffset / 24], value, UxAdd | UxNoNormalization, new Span<DiyFp128>(ref sum));
            value = sum;
        }

        value._sign = signY;
        return value;
    }

    private static DiyFp128 DiyFp128Atan(scoped in DiyFp128 arg) => DiyFp128Atan2(arg, default, false);

    // UX_ASIN_ACOS with the asin/acos interval maps precomputed. Callers guarantee |arg| <= 1.
    private static DiyFp128 DiyFp128AsinAcos(DiyFp128 arg, bool isAcos)
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
                DiyFp128 t = default;
                DiyFp128AddSub(new DiyFp128(0, 1, UxMsb, 0), arg, UxSub | UxMagnitudeOnly, new Span<DiyFp128>(ref t));
                arg = t;
                arg._exponent -= 1;
                arg = DiyFp128Sqrt(arg);
            }
            else if (exponent == 1 && arg._hi == UxMsb && arg._lo == 0)
            {
                // |x| == 1: the reduced argument is zero.
                arg = new DiyFp128(0, UxZeroExponent, 0, 0);
            }
        }

        arg._exponent += 1; // P_SCALE(1)
        Span<DiyFp128> result = [default, default];
        int flags = (TrigSquareTerm | TrigPostMultiply | TrigAlternateSign)
                  | ((TrigSquareTerm | TrigAlternateSign) << TrigNumeratorFieldWidth);
        DiyFp128EvaluateRational(arg, InvTrigAsinNumeratorCoefficients, 0, InvTrigAsinDenominatorCoefficients, 1, InvTrigAsinDegree, flags, result);
        DiyFp128 value = result[0];

        int mapInfo = indexMap >> index;
        value._sign = ((mapInfo & 8) != 0) ? UxSignBit : 0;
        value._exponent += exponentIncrement;

        DiyFp128 sum = default;
        DiyFp128AddSub(InvTrigConstants[(mapInfo & 0xF0) / 24], value, UxAdd | UxNoNormalization, new Span<DiyFp128>(ref sum));
        value = sum;

        value._sign = ((mapInfo & 4) != 0) ? UxSignBit : 0;
        return value;
    }

    private static DiyFp128 DiyFp128Asin(scoped in DiyFp128 arg) => DiyFp128AsinAcos(arg, false);

    private static DiyFp128 DiyFp128Acos(scoped in DiyFp128 arg) => DiyFp128AsinAcos(arg, true);

    // True when a normalized, non-zero |arg| is strictly greater than 1 (outside the asin/acos domain).
    private static bool DiyFp128MagnitudeExceedsOne(in DiyFp128 arg)
        => arg._exponent > 1 || (arg._exponent == 1 && (arg._hi != UxMsb || arg._lo != 0));

    private static bool DiyFp128MagnitudeIsOne(in DiyFp128 arg)
        => arg._exponent == 1 && arg._hi == UxMsb && arg._lo == 0;
}
