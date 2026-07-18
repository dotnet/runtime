// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the cube root evaluation from the Intel(R) Decimal Floating-Point Math
    // Library, specifically `UX_CBRT` from `dpml_ux_cbrt.c` and the reciprocal-cbrt polynomial and
    // Newton constant tables from `dpml_cbrt_x.h`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // A ~15-bit double reciprocal-cbrt polynomial seeds one double Newton step (which also folds in the
    // 2^(i/3) factor for the residual exponent), then a single binary128 Newton iteration lifts the
    // result to the full ~34-digit accuracy Decimal64/Decimal128 require.

    private const int CbrtDoubleExponentBias = 1023;
    private const int CbrtDoubleExponentPosition = 52;
    private const int CbrtDoubleExponentWidth = 11;

    // RECIP_CBRT_POLY coefficients (dpml_cbrt_x.h), Horner form over [1, 2).
    private static readonly double[] s_cbrtCoefficients =
    [
        BitConverter.UInt64BitsToDouble(0x4006ED4D2E803C66),
        BitConverter.UInt64BitsToDouble(0xC0102E13C6230110),
        BitConverter.UInt64BitsToDouble(0x400C33EEA71AF473),
        BitConverter.UInt64BitsToDouble(0xBFFC42EFA7679244),
        BitConverter.UInt64BitsToDouble(0x3FDE3D1A896AD7DA),
        BitConverter.UInt64BitsToDouble(0xBFAAD21E367E9BA1),
    ];

    // POW_CBRT_2_TABLE (dpml_cbrt_x.h): 2^(i/3) for i = 0, 1, 2.
    private static readonly double[] s_cbrtPow2Thirds =
    [
        BitConverter.UInt64BitsToDouble(0x3FF0000000000000),
        BitConverter.UInt64BitsToDouble(0x3FF428A2F98D728B),
        BitConverter.UInt64BitsToDouble(0x3FF965FEA53D6E3D),
    ];

    private static double CbrtFourteenNinths => BitConverter.UInt64BitsToDouble(0x3FF8E38E38E38E39);
    private static double CbrtSevenNinths => BitConverter.UInt64BitsToDouble(0x3FE8E38E38E38E39);
    private static double CbrtTwoNinths => BitConverter.UInt64BitsToDouble(0x3FCC71C71C71C71C);

    private static DiyFp128 DiyFp128Cbrt(DiyFp128 arg)
    {
        // f is the ux mantissa reinterpreted as a double in [1, 2).
        ulong msd = arg._hi;
        double f = BitConverter.UInt64BitsToDouble((((ulong)(CbrtDoubleExponentBias - 1)) << CbrtDoubleExponentPosition) + (msd >> CbrtDoubleExponentWidth));

        ReadOnlySpan<double> c = s_cbrtCoefficients;
        double z = c[0] + (f * (c[1] + (f * (c[2] + (f * (c[3] + (f * (c[4] + (f * c[5])))))))));

        // The true binary exponent of arg (value = f * 2^n with f in [1, 2)) is _exponent - 1. Split it
        // as n = 3*m + i with i in {0, 1, 2}; the 2^(i/3) residual is absorbed by the double Newton step.
        int n = arg._exponent - 1;
        int i = ((n % 3) + 3) % 3;
        int m = (n - i) / 3;

        double z2 = z * z;
        double z4 = z2 * z2;
        double f2 = f * f;
        double y = s_cbrtPow2Thirds[i] * ((((CbrtFourteenNinths * f) * z)
                       - (z4 * ((CbrtSevenNinths * f) * f2)))
                       + ((z4 * (z2 * z)) * ((CbrtTwoNinths * f) * (f2 * f2))));

        ulong yBits = BitConverter.DoubleToUInt64Bits(y);
        DiyFp128 result = default;
        result._sign = arg._sign;
        result._exponent = (int)(yBits >> CbrtDoubleExponentPosition) + m - (CbrtDoubleExponentBias - 1);
        result._hi = (yBits << CbrtDoubleExponentWidth) | UxMsb;
        result._lo = 0;

        // One binary128 Newton iteration: result <- (result / 2) * (result^3 + 2x) / (result^3 + x/2).
        DiyFp128 r = result;
        DiyFp128Multiply(ref r, ref r, out DiyFp128 cube);
        DiyFp128Multiply(ref r, ref cube, out cube);

        DiyFp128 term = arg;
        Span<DiyFp128> sums = stackalloc DiyFp128[2];
        term._exponent += 1; // 2*x
        DiyFp128AddSub(cube, term, UxAdd, sums.Slice(0, 1));
        term._exponent -= 2; // x/2
        DiyFp128AddSub(cube, term, UxAdd, sums.Slice(1, 1));

        DiyFp128Divide(sums[0], sums[1], DiyFp128FullPrecision, out DiyFp128 ratio);
        DiyFp128Multiply(ref r, ref ratio, out result);
        result._exponent -= 1;
        return result;
    }
}
