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
    // `Float128` (`ux`) domain, so unlike Intel's hardware-binary128 BID wrappers it does not spuriously
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

    private const int HyperSinhCoshDegree = 0xb;
    private const int HyperSinhCoshTrailingExponent = 1;

    // Fixed point coefficients for sinh/cosh evaluation (dpml_exp_x.h, SINHCOSH_COEF_ARRAY numerator).
    private static readonly Float128FixedCoefficient[] HyperSinhCoefficients =
    [
        new(0x0000000000000000, 0x0000000000000000),
        new(0x2e4690eb84e45693, 0x0000000000000000),
        new(0xd268b21c12ffd219, 0x000000000000004b),
        new(0x1dc1787345bbf199, 0x000000000000654b),
        new(0x9ccece4dde16535a, 0x00000000006b9fcf),
        new(0xa1b4271d9e5e08b2, 0x000000005849184e),
        new(0x89c71fc2391817aa, 0x00000035cc8acfea),
        new(0x338faac219c8d92f, 0x0000171de3a556c7),
        new(0x8068068066ce9bd9, 0x0006806806806806),
        new(0x1111111111137719, 0x0111111111111111),
        new(0x555555555555537e, 0x1555555555555555),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // Fixed point coefficients for sinh/cosh evaluation (dpml_exp_x.h, SINHCOSH_COEF_ARRAY denominator).
    private static readonly Float128FixedCoefficient[] HyperCoshCoefficients =
    [
        new(0x021a7acfab2871a0, 0x0000000000000000),
        new(0xca853bed72a41925, 0x0000000000000003),
        new(0x9e18f89af7b71018, 0x00000000000005a0),
        new(0xf9ccecdb5c564d82, 0x000000000006b9fc),
        new(0x301f275eee64c398, 0x00000000064e5d2a),
        new(0x3625ed50108a94fe, 0x000000047bb63bfe),
        new(0xeb8e5de0376e0580, 0x0000024fc9f6ef13),
        new(0xd00d00d00ccc1e48, 0x0000d00d00d00d00),
        new(0x82d82d82d82e2a61, 0x002d82d82d82d82d),
        new(0x5555555555555442, 0x0555555555555555),
        new(0x0000000000000001, 0x4000000000000000),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // Intel's UX_HYPERBOLIC: argument reduction x = I*ln2 + z (|z| < ln2/2) then either a direct
    // polynomial (|x| < ln2/2, to avoid loss of significance) or exp(z)/exp(-z) reconstruction.
    private static void Float128Hyperbolic(scoped in Float128 argument, int funcCode, int evalFlags, int addsubOp, Span<Float128> result)
    {
        Float128 reduceArg = argument;
        uint sign = reduceArg._sign;
        reduceArg._sign = 0;
        sign = (funcCode == HyperCoshFunc) ? 0 : sign;

        int scale = Float128ExpReduce(reduceArg, out Float128 reduced, ExpReciprocalLn2High, ExpLn2High, ExpReduceConstantExponent, ExpLn2Low);

        int rationalFlags = (scale == 0) ? evalFlags : HyperSinhCoshEval;
        Float128EvaluateRational(reduced, HyperSinhCoefficients, HyperSinhCoshTrailingExponent, HyperCoshCoefficients, HyperSinhCoshTrailingExponent, HyperSinhCoshDegree, rationalFlags, result);

        if (scale != 0)
        {
            Span<Float128> tmp = stackalloc Float128[2];

            // cosh(z) +/- sinh(z) = exp(z):exp(-z), then scale to exp(x)/2 and exp(-x)/2.
            Float128AddSub(result[1], result[0], UxAddSub | UxNoNormalization, tmp);
            tmp[0]._exponent += scale - 1;
            tmp[1]._exponent -= scale + 1;

            // sinh(x)/cosh(x) = exp(x)/2 -/+ exp(-x)/2; for tanh divide the two results.
            Float128AddSub(tmp[0], tmp[1], addsubOp | UxMagnitudeOnly | UxNoNormalization, result);

            if (funcCode == HyperTanhFunc)
            {
                Float128Divide(result[0], result[1], Float128FullPrecision, out result[0]);
            }
        }

        result[0]._sign = sign;
    }

    private static Float128 Float128Sinh(scoped in Float128 argument)
    {
        Span<Float128> result = stackalloc Float128[2];
        Float128Hyperbolic(argument, HyperSinhFunc, HyperSinhEval, UxSub, result);
        return result[0];
    }

    private static Float128 Float128Cosh(scoped in Float128 argument)
    {
        Span<Float128> result = stackalloc Float128[2];
        Float128Hyperbolic(argument, HyperCoshFunc, HyperCoshEval, UxAdd, result);
        return result[0];
    }

    private static Float128 Float128Tanh(scoped in Float128 argument)
    {
        Span<Float128> result = stackalloc Float128[2];
        Float128Hyperbolic(argument, HyperTanhFunc, HyperTanhEval, UxSubAdd, result);
        return result[0];
    }
}
