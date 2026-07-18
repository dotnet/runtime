// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static partial class Number
{
    // This code is based on the hyperbolic evaluation from the Intel(R) Decimal Floating-Point Math
    // Library, specifically `UX_HYPERBOLIC` and `C_UX_HYPERBOLIC` from `dpml_ux_exp.c`, the
    // rational-evaluation driver `EVALUATE_RATIONAL` from `dpml_ux_ops_64.c`, the exp argument reduction
    // `UX_EXP_REDUCE`, and the sinh/cosh coefficient table (`SINHCOSH_COEF_ARRAY`) from `dpml_exp_x.h`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // Decimal32 evaluates the hyperbolic functions in binary64; Decimal64/Decimal128 route through this
    // engine so the wider formats keep their precision. The engine operates entirely in the wide-exponent
    // `DiyFp128` (`ux`) domain, so unlike Intel's hardware-binary128 BID wrappers it does not spuriously
    // overflow for large arguments -- the final pack to the decimal format performs the only saturation.

    // ADD_SUB (2) is defined in the log file; SUB_ADD writes the difference to result[0] and the sum to
    // result[1].
    private const int UxSubAdd = 3;

    // Distinct function selectors; only used for the cosh sign-force and the tanh divide.
    private const int HyperSinhFunc = 1;
    private const int HyperCoshFunc = 2;
    private const int HyperTanhFunc = 3;

    // EVALUATE_RATIONAL presets (dpml_ux_exp.c): sinh is the odd numerator z*P(z^2), cosh the even
    // denominator C(z^2). SKIP evaluates only the requested half when the reduced |x| < ln2/2.
    private const int HyperSinhEval = TrigSquareTerm | TrigPostMultiply | TrigSkip;
    private const int HyperCoshEval = TrigSkip | (TrigSquareTerm << TrigNumeratorFieldWidth);
    private const int HyperTanhEval = (TrigSquareTerm | TrigPostMultiply) | (TrigSquareTerm << TrigNumeratorFieldWidth);
    private const int HyperSinhCoshEval = HyperTanhEval | TrigNoDivide;

    private const int HyperSinhCoshDegree = 0xB;
    private const int HyperSinhCoshTrailingExponent = 1;

    // Fixed point coefficients for sinh/cosh evaluation (dpml_exp_x.h, SINHCOSH_COEF_ARRAY numerator).
    private static readonly DiyFp128FixedCoefficient[] HyperSinhCoefficients =
    [
        new(0x0000000000000000, 0x0000000000000000),
        new(0x2E4690EB84E45693, 0x0000000000000000),
        new(0xD268B21C12FFD219, 0x000000000000004B),
        new(0x1DC1787345BBF199, 0x000000000000654B),
        new(0x9CCECE4DDE16535A, 0x00000000006B9FCF),
        new(0xA1B4271D9E5E08B2, 0x000000005849184E),
        new(0x89C71FC2391817AA, 0x00000035CC8ACFEA),
        new(0x338FAAC219C8D92F, 0x0000171DE3A556C7),
        new(0x8068068066CE9BD9, 0x0006806806806806),
        new(0x1111111111137719, 0x0111111111111111),
        new(0x555555555555537E, 0x1555555555555555),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // Fixed point coefficients for sinh/cosh evaluation (dpml_exp_x.h, SINHCOSH_COEF_ARRAY denominator).
    private static readonly DiyFp128FixedCoefficient[] HyperCoshCoefficients =
    [
        new(0x021A7ACFAB2871A0, 0x0000000000000000),
        new(0xCA853BED72A41925, 0x0000000000000003),
        new(0x9E18F89AF7B71018, 0x00000000000005A0),
        new(0xF9CCECDB5C564D82, 0x000000000006B9FC),
        new(0x301F275EEE64C398, 0x00000000064E5D2A),
        new(0x3625ED50108A94FE, 0x000000047BB63BFE),
        new(0xEB8E5DE0376E0580, 0x0000024FC9F6EF13),
        new(0xD00D00D00CCC1E48, 0x0000D00D00D00D00),
        new(0x82D82D82D82E2A61, 0x002D82D82D82D82D),
        new(0x5555555555555442, 0x0555555555555555),
        new(0x0000000000000001, 0x4000000000000000),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // Intel's UX_HYPERBOLIC: argument reduction x = I*ln2 + z (|z| < ln2/2) then either a direct
    // polynomial (|x| < ln2/2, to avoid loss of significance) or exp(z)/exp(-z) reconstruction.
    private static void DiyFp128Hyperbolic(scoped in DiyFp128 argument, int funcCode, int evalFlags, int addsubOp, Span<DiyFp128> result)
    {
        DiyFp128 reduceArg = argument;
        uint sign = reduceArg._sign;
        reduceArg._sign = 0;
        sign = (funcCode == HyperCoshFunc) ? 0 : sign;

        int scale = DiyFp128ExpReduce(reduceArg, out DiyFp128 reduced, ExpReciprocalLn2High, ExpLn2High, ExpReduceConstantExponent, ExpLn2Low);

        int rationalFlags = (scale == 0) ? evalFlags : HyperSinhCoshEval;
        DiyFp128EvaluateRational(reduced, HyperSinhCoefficients, HyperSinhCoshTrailingExponent, HyperCoshCoefficients, HyperSinhCoshTrailingExponent, HyperSinhCoshDegree, rationalFlags, result);

        if (scale != 0)
        {
            Span<DiyFp128> tmp = [default, default];

            // cosh(z) +/- sinh(z) = exp(z):exp(-z), then scale to exp(x)/2 and exp(-x)/2.
            DiyFp128AddSub(result[1], result[0], UxAddSub | UxNoNormalization, tmp);
            tmp[0]._exponent += scale - 1;
            tmp[1]._exponent -= scale + 1;

            // sinh(x)/cosh(x) = exp(x)/2 -/+ exp(-x)/2; for tanh divide the two results.
            DiyFp128AddSub(tmp[0], tmp[1], addsubOp | UxMagnitudeOnly | UxNoNormalization, result);

            if (funcCode == HyperTanhFunc)
            {
                DiyFp128Divide(result[0], result[1], DiyFp128FullPrecision, out result[0]);
            }
        }

        result[0]._sign = sign;
    }

    private static DiyFp128 DiyFp128Sinh(scoped in DiyFp128 argument)
    {
        Span<DiyFp128> result = [default, default];
        DiyFp128Hyperbolic(argument, HyperSinhFunc, HyperSinhEval, UxSub, result);
        return result[0];
    }

    private static DiyFp128 DiyFp128Cosh(scoped in DiyFp128 argument)
    {
        Span<DiyFp128> result = [default, default];
        DiyFp128Hyperbolic(argument, HyperCoshFunc, HyperCoshEval, UxAdd, result);
        return result[0];
    }

    private static DiyFp128 DiyFp128Tanh(scoped in DiyFp128 argument)
    {
        Span<DiyFp128> result = [default, default];
        DiyFp128Hyperbolic(argument, HyperTanhFunc, HyperTanhEval, UxSubAdd, result);
        return result[0];
    }
}
