// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static partial class Number
{
    // This code is based on the inverse hyperbolic evaluation from the Intel(R) Decimal Floating-Point
    // Math Library, specifically `F_ASINH`, `F_ACOSH`, and `F_ATANH` from `dpml_ux_inv_hyper.c` and the
    // loss-of-significance thresholds from `dpml_inv_hyper_x.h`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // Each function reduces to a logarithm: asinh(x) = log(x + sqrt(x^2 + 1)),
    // acosh(x) = log(x + sqrt(x^2 - 1)), atanh(x) = (1/2) * log((1 + x) / (1 - x)). Near the point where
    // the reduced argument is 1 the naive ratio loses significance, so a small-argument path forms the
    // reduced ratio directly and evaluates it with `DiyFp128LogPoly`; otherwise the big path forms the
    // full argument and calls `DiyFp128Ln`. The evaluation runs entirely in the software binary128
    // engine, so Decimal64/Decimal128 obtain the full ~34-digit accuracy Intel's reference does.

    // Loss-of-significance thresholds (dpml_inv_hyper_x.h): the MSD boundaries selecting the small path.
    private const ulong InvHyperSqrt2Over4 = 0xb504f333f9de6484;         // sqrt(2) / 4
    private const ulong InvHyperThreeSqrt2Over4 = 0x87c3b666fb66cb63;    // 3 * sqrt(2) / 4
    private const ulong InvHyperSqrt2Minus1Squared = 0xafb0ccc06219b7ba; // (sqrt(2) - 1)^2

    /// <summary>Computes <c>asinh(x)</c> for a finite <paramref name="x"/> (Intel's <c>F_ASINH</c>).</summary>
    private static DiyFp128 DiyFp128Asinh(DiyFp128 x)
    {
        uint sign = x._sign;
        x._sign = 0; // |x|

        int exponent = x._exponent;
        ulong fHi = x._hi;

        DiyFp128 square = x;
        DiyFp128Multiply(ref square, ref square, out DiyFp128 tmp); // x^2

        Span<DiyFp128> one = stackalloc DiyFp128[1];
        DiyFp128AddSub(tmp, DiyFp128One, UxAdd, one); // x^2 + 1
        tmp = one[0];
        DiyFp128Normalize(ref tmp);
        tmp = DiyFp128Sqrt(tmp); // sqrt(x^2 + 1)

        DiyFp128 result;

        if ((exponent < -1) || ((exponent == -1) && (fHi <= InvHyperSqrt2Over4)))
        {
            DiyFp128AddSub(tmp, DiyFp128One, UxAdd, one); // sqrt(x^2 + 1) + 1
            DiyFp128Divide(x, one[0], DiyFp128FullPrecision, out tmp); // x / (sqrt(x^2 + 1) + 1)
            result = DiyFp128LogPoly(tmp);
        }
        else
        {
            DiyFp128AddSub(tmp, x, UxAdd, one); // sqrt(x^2 + 1) + x
            tmp = one[0];
            DiyFp128Normalize(ref tmp);
            result = DiyFp128Ln(tmp);
        }

        result._sign = sign; // asinh is odd
        return result;
    }

    /// <summary>Computes <c>acosh(x)</c> for a finite <paramref name="x"/> &gt;= 1 (Intel's <c>F_ACOSH</c>).</summary>
    private static DiyFp128 DiyFp128Acosh(DiyFp128 x)
    {
        int exponent = x._exponent;
        ulong fHi = x._hi;

        Span<DiyFp128> parts = stackalloc DiyFp128[2];
        DiyFp128AddSub(x, DiyFp128One, UxAddSub, parts); // parts[0] = x + 1, parts[1] = x - 1

        if ((exponent == 1) && (fHi <= InvHyperThreeSqrt2Over4))
        {
            DiyFp128Divide(parts[1], parts[0], DiyFp128FullPrecision, out DiyFp128 ratio); // (x - 1) / (x + 1)
            return DiyFp128LogPoly(DiyFp128Sqrt(ratio));
        }

        DiyFp128Multiply(ref parts[1], ref parts[0], out DiyFp128 product); // x^2 - 1
        DiyFp128Normalize(ref product);
        DiyFp128 root = DiyFp128Sqrt(product); // sqrt(x^2 - 1)

        Span<DiyFp128> sum = stackalloc DiyFp128[1];
        DiyFp128AddSub(root, x, UxAdd, sum); // sqrt(x^2 - 1) + x
        return DiyFp128Ln(sum[0]);
    }

    /// <summary>Computes <c>atanh(x)</c> for a finite <paramref name="x"/> with <c>|x| &lt; 1</c> (Intel's <c>F_ATANH</c>).</summary>
    private static DiyFp128 DiyFp128Atanh(DiyFp128 x)
    {
        uint sign = x._sign;
        x._sign = 0; // |x|

        int exponent = x._exponent;
        ulong fHi = x._hi;

        DiyFp128 result;

        if ((exponent < -2) || ((exponent == -2) && (fHi <= InvHyperSqrt2Minus1Squared)))
        {
            result = DiyFp128LogPoly(x); // log((1 + |x|) / (1 - |x|))
        }
        else
        {
            Span<DiyFp128> parts = stackalloc DiyFp128[2];
            DiyFp128AddSub(x, DiyFp128One, UxAddSub, parts); // parts[0] = |x| + 1, parts[1] = |x| - 1
            DiyFp128Divide(parts[1], parts[0], DiyFp128FullPrecision, out DiyFp128 ratio); // (|x| - 1) / (|x| + 1)
            DiyFp128Normalize(ref ratio);
            result = DiyFp128Ln(ratio); // magnitude only: log((1 - |x|) / (1 + |x|))
        }

        result._sign = sign;  // atanh is odd; overwrites the sign the log picked up
        result._exponent -= 1; // multiply by 1/2
        return result;
    }
}
