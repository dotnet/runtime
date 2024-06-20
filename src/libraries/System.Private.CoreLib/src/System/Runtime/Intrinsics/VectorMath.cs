// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    internal static class VectorMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector CopySign<TVector, T>(TVector value, TVector sign)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            Debug.Assert((typeof(T) != typeof(byte))
                      && (typeof(T) != typeof(ushort))
                      && (typeof(T) != typeof(uint))
                      && (typeof(T) != typeof(ulong))
                      && (typeof(T) != typeof(nuint)));

            if (typeof(T) == typeof(float))
            {
                return TVector.ConditionalSelect(Create<TVector, T>(-0.0f), sign, value);
            }
            else if (typeof(T) == typeof(double))
            {
                return TVector.ConditionalSelect(Create<TVector, T>(-0.0), sign, value);
            }
            else
            {
                // All values are two's complement and so `value ^ sign` will produce a positive
                // number if the signs match and a negative number if the signs differ. When the
                // signs differ we want to negate the value and otherwise take the value as is.
                return TVector.ConditionalSelect(TVector.LessThan(value ^ sign, TVector.Zero), -value, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector DegreesToRadians<TVector, T>(TVector degrees)
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : IFloatingPointIeee754<T>
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return (degrees * TVector.Create(T.Pi)) / TVector.Create(T.CreateTruncating(180));
        }

        public static TVectorDouble ExpDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `vrd2_exp` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // ----------------------
            // 1. Argument Reduction:
            //      e^x = 2^(x/ln2) = 2^(x*(64/ln(2))/64)     --- (1)
            //
            //      Choose 'n' and 'f', such that
            //      x * 64/ln2 = n + f                        --- (2) | n is integer
            //                            | |f| <= 0.5
            //     Choose 'm' and 'j' such that,
            //      n = (64 * m) + j                          --- (3)
            //
            //     From (1), (2) and (3),
            //      e^x = 2^((64*m + j + f)/64)
            //          = (2^m) * (2^(j/64)) * 2^(f/64)
            //          = (2^m) * (2^(j/64)) * e^(f*(ln(2)/64))
            //
            // 2. Table Lookup
            //      Values of (2^(j/64)) are precomputed, j = 0, 1, 2, 3 ... 63
            //
            // 3. Polynomial Evaluation
            //   From (2),
            //     f = x*(64/ln(2)) - n
            //   Let,
            //     r  = f*(ln(2)/64) = x - n*(ln(2)/64)
            //
            // 4. Reconstruction
            //      Thus,
            //        e^x = (2^m) * (2^(j/64)) * e^r

            const ulong V_ARG_MAX = 0x40862000_00000000;
            const ulong V_DP64_BIAS = 1023;

            const double V_EXPF_MIN = -709.782712893384;
            const double V_EXPF_MAX = +709.782712893384;

            const double V_EXPF_HUGE = 6755399441055744;
            const double V_TBL_LN2 = 1.4426950408889634;

            const double V_LN2_HEAD = +0.693359375;
            const double V_LN2_TAIL = -0.00021219444005469057;

            const double C3  = 0.5000000000000018;
            const double C4  = 0.1666666666666617;
            const double C5  = 0.04166666666649277;
            const double C6  = 0.008333333333559272;
            const double C7  = 0.001388888895122404;
            const double C8  = 0.00019841269432677495;
            const double C9  = 2.4801486521374483E-05;
            const double C10 = 2.7557622532543023E-06;
            const double C11 = 2.7632293298250954E-07;
            const double C12 = 2.499430431958571E-08;

            // x * (64.0 / ln(2))
            TVectorDouble z = x * TVectorDouble.Create(V_TBL_LN2);

            TVectorDouble dn = z + TVectorDouble.Create(V_EXPF_HUGE);

            // n = (int)z
            TVectorUInt64 n = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(dn);

            // dn = (double)n
            dn -= TVectorDouble.Create(V_EXPF_HUGE);

            // r = x - (dn * (ln(2) / 64))
            // where ln(2) / 64 is split into Head and Tail values
            TVectorDouble r = x - (dn * TVectorDouble.Create(V_LN2_HEAD)) - (dn * TVectorDouble.Create(V_LN2_TAIL));

            TVectorDouble r2 = r * r;
            TVectorDouble r4 = r2 * r2;
            TVectorDouble r8 = r4 * r4;

            // Compute polynomial
            TVectorDouble poly = ((TVectorDouble.Create(C12) * r + TVectorDouble.Create(C11)) * r2 +
                                   TVectorDouble.Create(C10) * r + TVectorDouble.Create(C9))  * r8 +
                                 ((TVectorDouble.Create(C8)  * r + TVectorDouble.Create(C7))  * r2 +
                                  (TVectorDouble.Create(C6)  * r + TVectorDouble.Create(C5))) * r4 +
                                 ((TVectorDouble.Create(C4)  * r + TVectorDouble.Create(C3))  * r2 + (r + TVectorDouble.One));

            // m = (n - j) / 64
            // result = polynomial * 2^m
            TVectorDouble ret = poly * Unsafe.BitCast<TVectorUInt64, TVectorDouble>((n + TVectorUInt64.Create(V_DP64_BIAS)) << 52);

            // Check if -709 < vx < 709
            if (TVectorUInt64.GreaterThanAny(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(TVectorDouble.Abs(x)), TVectorUInt64.Create(V_ARG_MAX)))
            {
                // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                TVectorDouble infinityMask = TVectorDouble.GreaterThan(x, TVectorDouble.Create(V_EXPF_MAX));

                ret = TVectorDouble.ConditionalSelect(
                    infinityMask,
                    TVectorDouble.Create(double.PositiveInfinity),
                    ret
                );

                // (x < V_EXPF_MIN) ? 0 : x
                ret = TVectorDouble.AndNot(ret, TVectorDouble.LessThan(x, TVectorDouble.Create(V_EXPF_MIN)));
            }

            return ret;
        }

        public static TVectorSingle ExpSingle<TVectorSingle, TVectorUInt32, TVectorDouble, TVectorUInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `vrs4_expf` from amd/aocl-libm-ose
            // Copyright (C) 2019-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes:
            // 1. Argument Reduction:
            //      e^x = 2^(x/ln2)                          --- (1)
            //
            //      Let x/ln(2) = z                          --- (2)
            //
            //      Let z = n + r , where n is an integer    --- (3)
            //                      |r| <= 1/2
            //
            //     From (1), (2) and (3),
            //      e^x = 2^z
            //          = 2^(N+r)
            //          = (2^N)*(2^r)                        --- (4)
            //
            // 2. Polynomial Evaluation
            //   From (4),
            //     r   = z - N
            //     2^r = C1 + C2*r + C3*r^2 + C4*r^3 + C5 *r^4 + C6*r^5
            //
            // 4. Reconstruction
            //      Thus,
            //        e^x = (2^N) * (2^r)

            const uint V_ARG_MAX = 0x42AE0000;

            const float V_EXPF_MIN = -103.97208f;
            const float V_EXPF_MAX = +88.72284f;

            const double V_EXPF_HUGE = 6755399441055744;
            const double V_TBL_LN2 = 1.4426950408889634;

            const double C1 = 1.0000000754895704;
            const double C2 = 0.6931472254087585;
            const double C3 = 0.2402210737432219;
            const double C4 = 0.05550297297702539;
            const double C5 = 0.009676036358193323;
            const double C6 = 0.001341000536524434;

            // Convert x to double precision
            (TVectorDouble xl, TVectorDouble xu) = Widen<TVectorSingle, TVectorDouble>(x);

            // x * (64.0 / ln(2))
            TVectorDouble v_tbl_ln2 = TVectorDouble.Create(V_TBL_LN2);

            TVectorDouble zl = xl * v_tbl_ln2;
            TVectorDouble zu = xu * v_tbl_ln2;

            TVectorDouble v_expf_huge = TVectorDouble.Create(V_EXPF_HUGE);

            TVectorDouble dnl = zl + v_expf_huge;
            TVectorDouble dnu = zu + v_expf_huge;

            // n = (int)z
            TVectorUInt64 nl = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(dnl);
            TVectorUInt64 nu = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(dnu);

            // dn = (double)n
            dnl -= v_expf_huge;
            dnu -= v_expf_huge;

            // r = z - dn
            TVectorDouble c1 = TVectorDouble.Create(C1);
            TVectorDouble c2 = TVectorDouble.Create(C2);
            TVectorDouble c3 = TVectorDouble.Create(C3);
            TVectorDouble c4 = TVectorDouble.Create(C4);
            TVectorDouble c5 = TVectorDouble.Create(C5);
            TVectorDouble c6 = TVectorDouble.Create(C6);

            TVectorDouble rl = zl - dnl;

            TVectorDouble rl2 = rl * rl;
            TVectorDouble rl4 = rl2 * rl2;

            TVectorDouble polyl = (c4 * rl + c3) * rl2
                               + ((c6 * rl + c5) * rl4
                                + (c2 * rl + c1));


            TVectorDouble ru = zu - dnu;

            TVectorDouble ru2 = ru * ru;
            TVectorDouble ru4 = ru2 * ru2;

            TVectorDouble polyu = (c4 * ru + c3) * ru2
                               + ((c6 * ru + c5) * ru4
                                + (c2 * ru + c1));

            // result = (float)(poly + (n << 52))
            TVectorSingle ret = Narrow<TVectorDouble, TVectorSingle>(
                Unsafe.BitCast<TVectorUInt64, TVectorDouble>(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(polyl) + (nl << 52)),
                Unsafe.BitCast<TVectorUInt64, TVectorDouble>(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(polyu) + (nu << 52))
            );

            // Check if -103 < |x| < 88
            if (TVectorUInt32.GreaterThanAny(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(TVectorSingle.Abs(x)), TVectorUInt32.Create(V_ARG_MAX)))
            {
                // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                TVectorSingle infinityMask = TVectorSingle.GreaterThan(x, TVectorSingle.Create(V_EXPF_MAX));

                ret = TVectorSingle.ConditionalSelect(
                    infinityMask,
                    TVectorSingle.Create(float.PositiveInfinity),
                    ret
                );

                // (x < V_EXPF_MIN) ? 0 : x
                ret = TVectorSingle.AndNot(ret, TVectorSingle.LessThan(x, TVectorSingle.Create(V_EXPF_MIN)));
            }

            return ret;
        }

        public static TVectorDouble HypotDouble<TVectorDouble, TVectorUInt64>(TVectorDouble x, TVectorDouble y)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `hypot` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorDouble ax = TVectorDouble.Abs(x);
            TVectorDouble ay = TVectorDouble.Abs(y);

            TVectorDouble positiveInfinity = TVectorDouble.Create(float.PositiveInfinity);
            TVectorDouble infinityMask = TVectorDouble.Equals(ax, positiveInfinity) | TVectorDouble.Equals(ay, positiveInfinity);
            TVectorDouble notNaNMask = TVectorDouble.Equals(ax, ax) & TVectorDouble.Equals(ay, ay);

            TVectorUInt64 xBits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(ax);
            TVectorUInt64 yBits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(ay);

            TVectorUInt64 shiftedExponentMask = TVectorUInt64.Create(double.ShiftedExponentMask);
            TVectorUInt64 xExp = (xBits >> double.BiasedExponentShift) & shiftedExponentMask;
            TVectorUInt64 yExp = (yBits >> double.BiasedExponentShift) & shiftedExponentMask;

            TVectorUInt64 expDiff = xExp - yExp;

            // To prevent overflow, scale down by 2^+600
            TVectorUInt64 expBiasP500 = TVectorUInt64.Create(double.ExponentBias + 500);
            TVectorUInt64 scaleDownMask = TVectorUInt64.GreaterThan(xExp, expBiasP500) | TVectorUInt64.GreaterThan(yExp, expBiasP500);
            TVectorDouble expFix = TVectorDouble.ConditionalSelect(Unsafe.BitCast<TVectorUInt64, TVectorDouble>(scaleDownMask), TVectorDouble.Create(4.149515568880993E+180), TVectorDouble.One);
            TVectorUInt64 bitsFix = scaleDownMask & TVectorUInt64.Create(0xDA80000000000000);

            // To prevent underflow, scale up by 2^-600, but only if we didn't scale down already
            TVectorUInt64 expBiasM500 = TVectorUInt64.Create(double.ExponentBias - 500);
            TVectorUInt64 scaleUpMask = TVectorUInt64.AndNot(TVectorUInt64.LessThan(xExp, expBiasM500) | TVectorUInt64.LessThan(yExp, expBiasM500), scaleDownMask);
            expFix = TVectorDouble.ConditionalSelect(Unsafe.BitCast<TVectorUInt64, TVectorDouble>(scaleUpMask), TVectorDouble.Create(2.409919865102884E-181), expFix);
            bitsFix = TVectorUInt64.ConditionalSelect(scaleUpMask, TVectorUInt64.Create(0x2580000000000000), bitsFix);

            xBits += bitsFix;
            yBits += bitsFix;

            // For subnormal values when scaling up, do an additional fixing
            // up changing the adjustment to scale up by 2^601 instead and then
            // subtract a correction of 2^601 to account for the implicit bit.

            TVectorDouble subnormalFix = TVectorDouble.Create(9.232978617785736E-128);
            TVectorUInt64 subnormalBitsFix = TVectorUInt64.Create(0x0010000000000000);

            TVectorUInt64 xSubnormalMask = TVectorUInt64.Equals(xExp, TVectorUInt64.Zero) & scaleUpMask;
            xBits += subnormalBitsFix & xSubnormalMask;
            ax = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(xBits);
            ax -= subnormalFix & Unsafe.BitCast<TVectorUInt64, TVectorDouble>(xSubnormalMask);

            TVectorUInt64 ySubnormalMask = TVectorUInt64.Equals(yExp, TVectorUInt64.Zero) & scaleUpMask;
            yBits += subnormalBitsFix & ySubnormalMask;
            ay = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(yBits);
            ay -= subnormalFix & Unsafe.BitCast<TVectorUInt64, TVectorDouble>(ySubnormalMask);

            xBits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(ax);
            yBits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(ay);

            // Sort so ax is greater than ay
            TVectorDouble lessThanMask = TVectorDouble.LessThan(ax, ay);

            TVectorDouble tmp = ax;
            ax = TVectorDouble.ConditionalSelect(lessThanMask, ay, ax);
            ay = TVectorDouble.ConditionalSelect(lessThanMask, tmp, ay);

            TVectorUInt64 tmpBits = xBits;
            xBits = TVectorUInt64.ConditionalSelect(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(lessThanMask), yBits, xBits);
            yBits = TVectorUInt64.ConditionalSelect(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(lessThanMask), tmpBits, yBits);

            Debug.Assert(TVectorDouble.GreaterThanOrEqualAll(ax, ay));

            // Split ax and ay into a head and tail portion

            TVectorUInt64 headMask = TVectorUInt64.Create(0xFFFF_FFFF_F800_0000);
            TVectorDouble xHead = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(xBits & headMask);
            TVectorDouble yHead = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(yBits & headMask);

            TVectorDouble xTail = ax - xHead;
            TVectorDouble yTail = ay - yHead;

            // Compute (x * x) + (y * y) with extra precision
            //
            // This includes taking into account expFix which may
            // cause an underflow or overflow, but if it does that
            // will still be the correct result.

            TVectorDouble xx = ax * ax;
            TVectorDouble yy = ay * ay;

            TVectorDouble rHead = xx + yy;
            TVectorDouble rTail = (xx - rHead) + yy;

            rTail += (xHead * xHead) - xx;
            rTail += xHead * 2 * xTail;
            rTail += xTail * xTail;

            // We only need to do extra accounting when ax and ay have equal exponents
            TVectorDouble equalExponentsMask = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(TVectorUInt64.Equals(expDiff, TVectorUInt64.Zero));

            TVectorDouble rTailTmp = rTail;

            rTailTmp += (yHead * yHead) - yy;
            rTailTmp += yHead * 2 * yTail;
            rTailTmp += yTail * yTail;

            rTail = TVectorDouble.ConditionalSelect(equalExponentsMask, rTailTmp, rTail);

            TVectorDouble result = TVectorDouble.Sqrt(rHead + rTail) * expFix;

            // IEEE 754 requires that we return +Infinity
            // if either input is Infinity, even if one of
            // the inputs is NaN. Otherwise if either input
            // is NaN, we return NaN

            result = TVectorDouble.ConditionalSelect(notNaNMask, result, TVectorDouble.Create(double.NaN));
            result = TVectorDouble.ConditionalSelect(infinityMask, TVectorDouble.Create(double.PositiveInfinity), result);

            return result;
        }

        public static TVectorSingle HypotSingle<TVectorSingle, TVectorDouble>(TVectorSingle x, TVectorSingle y)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `hypotf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorSingle ay = TVectorSingle.Abs(y);

            TVectorSingle positiveInfinity = TVectorSingle.Create(float.PositiveInfinity);
            TVectorSingle infinityMask = TVectorSingle.Equals(ax, positiveInfinity) | TVectorSingle.Equals(ay, positiveInfinity);
            TVectorSingle notNaNMask = TVectorSingle.Equals(ax, ax) & TVectorSingle.Equals(ay, ay);

            (TVectorDouble xxLower, TVectorDouble xxUpper) = Widen<TVectorSingle, TVectorDouble>(ax);
            xxLower *= xxLower;
            xxUpper *= xxUpper;

            (TVectorDouble yyLower, TVectorDouble yyUpper) = Widen<TVectorSingle, TVectorDouble>(ay);
            yyLower *= yyLower;
            yyUpper *= yyUpper;

            TVectorSingle result = Narrow<TVectorDouble, TVectorSingle>(
                TVectorDouble.Sqrt(xxLower + yyLower),
                TVectorDouble.Sqrt(xxUpper + yyUpper)
            );

            // IEEE 754 requires that we return +Infinity
            // if either input is Infinity, even if one of
            // the inputs is NaN. Otherwise if either input
            // is NaN, we return NaN

            result = TVectorSingle.ConditionalSelect(notNaNMask, result, TVectorSingle.Create(float.NaN));
            result = TVectorSingle.ConditionalSelect(infinityMask, TVectorSingle.Create(float.PositiveInfinity), result);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector Lerp<TVector, T>(TVector x, TVector y, TVector amount)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            return TVector.MultiplyAddEstimate(x, TVector.One - amount, y * amount);
        }

        public static TVectorDouble LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `vrd2_log` from amd/aocl-libm-ose
            // Copyright (C) 2018-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Reduce x into the form:
            //        x = (-1)^s*2^n*m
            // s will be always zero, as log is defined for positive numbers
            // n is an integer known as the exponent
            // m is mantissa
            //
            // x is reduced such that the mantissa, m lies in [2/3,4/3]
            //      x = 2^n*m where m is in [2/3,4/3]
            //      log(x) = log(2^n*m)                 We have log(a*b) = log(a)+log(b)
            //             = log(2^n) + log(m)          We have log(a^n) = n*log(a)
            //             = n*log(2) + log(m)
            //             = n*log(2) + log(1+(m-1))
            //             = n*log(2) + log(1+f)        Where f = m-1
            //             = n*log(2) + log1p(f)        f lies in [-1/3,+1/3]
            //
            // Thus we have :
            // log(x) = n*log(2) + log1p(f)
            // In the above, the first term n*log(2), n can be calculated by using right shift operator and the value of log(2)
            // is known and is stored as a constant
            // The second term log1p(F) is approximated by using a polynomial

            const ulong V_MIN = double.SmallestNormalBits;
            const ulong V_MAX = double.PositiveInfinityBits;
            const ulong V_MSK = 0x000FFFFF_FFFFFFFF;    // (1 << 52) - 1
            const ulong V_OFF = 0x3FE55555_55555555;    // 2.0 / 3.0

            const double LN2_HEAD = 0.693359375;
            const double LN2_TAIL = -0.00021219444005469057;

            const double C02 = -0.499999999999999560;
            const double C03 = +0.333333333333414750;
            const double C04 = -0.250000000000297430;
            const double C05 = +0.199999999975985220;
            const double C06 = -0.166666666608919500;
            const double C07 = +0.142857145600277100;
            const double C08 = -0.125000005127831270;
            const double C09 = +0.111110952357159440;
            const double C10 = -0.099999750495501240;
            const double C11 = +0.090914349823462390;
            const double C12 = -0.083340600527551860;
            const double C13 = +0.076817603328311300;
            const double C14 = -0.071296718946287310;
            const double C15 = +0.067963465211535730;
            const double C16 = -0.063995035098960040;
            const double C17 = +0.049370587082412105;
            const double C18 = -0.045370170994891980;
            const double C19 = +0.088970636003577750;
            const double C20 = -0.086906174116908760;

            TVectorDouble specialResult = x;

            // x is zero, subnormal, infinity, or NaN
            TVectorUInt64 specialMask = TVectorUInt64.GreaterThanOrEqual(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(x) - TVectorUInt64.Create(V_MIN), TVectorUInt64.Create(V_MAX - V_MIN));

            if (specialMask != TVectorUInt64.Zero)
            {
                TVectorInt64 xBits = Unsafe.BitCast<TVectorDouble, TVectorInt64>(x);

                // (x < 0) ? float.NaN : x
                TVectorDouble lessThanZeroMask = Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.LessThan(xBits, TVectorInt64.Zero));

                specialResult = TVectorDouble.ConditionalSelect(
                    lessThanZeroMask,
                    TVectorDouble.Create(double.NaN),
                    specialResult
                );

                // double.IsZero(x) ? double.NegativeInfinity : x
                TVectorDouble zeroMask = Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(xBits << 1, TVectorInt64.Zero));

                specialResult = TVectorDouble.ConditionalSelect(
                    zeroMask,
                    TVectorDouble.Create(double.NegativeInfinity),
                    specialResult
                );

                // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                TVectorDouble temp = zeroMask
                                   | lessThanZeroMask
                                   | Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThanOrEqual(xBits, TVectorInt64.Create((long)double.PositiveInfinityBits)));

                // subnormal
                TVectorDouble subnormalMask = TVectorDouble.AndNot(Unsafe.BitCast<TVectorUInt64, TVectorDouble>(specialMask), temp);

                // multiply by 2^52, then normalize
                x = TVectorDouble.ConditionalSelect(
                    subnormalMask,
                    Unsafe.BitCast<TVectorUInt64, TVectorDouble>(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(x * 4503599627370496.0) - TVectorUInt64.Create(52ul << 52)),
                    x
                );

                specialMask = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(temp);
            }

            // Reduce the mantissa to [+2/3, +4/3]
            TVectorUInt64 vx = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(x) - TVectorUInt64.Create(V_OFF);
            TVectorDouble n = ConvertToDouble<TVectorInt64, TVectorDouble>(Unsafe.BitCast<TVectorUInt64, TVectorInt64>(vx) >> 52);
            vx = (vx & TVectorUInt64.Create(V_MSK)) + TVectorUInt64.Create(V_OFF);

            // Adjust the mantissa to [-1/3, +1/3]
            TVectorDouble r = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(vx) - TVectorDouble.One;

            TVectorDouble r02 = r * r;
            TVectorDouble r04 = r02 * r02;
            TVectorDouble r08 = r04 * r04;
            TVectorDouble r16 = r08 * r08;

            // Compute log(x + 1) using Polynomial approximation
            //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

            TVectorDouble poly = (((r04 * C20)
                                + ((((r * C19) + TVectorDouble.Create(C18)) * r02)
                                  + ((r * C17) + TVectorDouble.Create(C16)))) * r16)
                             + (((((((r * C15) + TVectorDouble.Create(C14)) * r02)
                                  + ((r * C13) + TVectorDouble.Create(C12))) * r04)
                                + ((((r * C11) + TVectorDouble.Create(C10)) * r02)
                                  + ((r * C09) + TVectorDouble.Create(C08)))) * r08)
                               + (((((r * C07) + TVectorDouble.Create(C06)) * r02)
                                  + ((r * C05) + TVectorDouble.Create(C04))) * r04)
                                + ((((r * C03) + TVectorDouble.Create(C02)) * r02) + r);

            return TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt64, TVectorDouble>(specialMask),
                specialResult,
                (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
            );
        }

        public static TVectorSingle LogSingle<TVectorSingle, TVectorInt32, TVectorUInt32>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
        {
            // This code is based on `vrs4_logf` from amd/aocl-libm-ose
            // Copyright (C) 2018-2019 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   logf(x)
            //          = logf(x)           if x ∈ F and x > 0
            //          = x                 if x = qNaN
            //          = 0                 if x = 1
            //          = -inf              if x = (-0, 0}
            //          = NaN               otherwise
            //
            // Assumptions/Expectations
            //      - ULP is derived to be << 4 (always)
            // - Some FPU Exceptions may not be available
            //      - Performance is at least 3x
            //
            // Implementation Notes:
            //  1. Range Reduction:
            //      x = 2^n*(1+f)                                          .... (1)
            //         where n is exponent and is an integer
            //             (1+f) is mantissa ∈ [1,2). i.e., 1 ≤ 1+f < 2    .... (2)
            //
            //    From (1), taking log on both sides
            //      log(x) = log(2^n * (1+f))
            //             = log(2^n) + log(1+f)
            //             = n*log(2) + log(1+f)                           .... (3)
            //
            //      let z = 1 + f
            //             log(z) = log(k) + log(z) - log(k)
            //             log(z) = log(kz) - log(k)
            //
            //    From (2), range of z is [1, 2)
            //       by simply dividing range by 'k', z is in [1/k, 2/k)  .... (4)
            //       Best choice of k is the one which gives equal and opposite values
            //       at extrema        +-      -+
            //              1          | 2      |
            //             --- - 1 = - |--- - 1 |
            //              k          | k      |                         .... (5)
            //                         +-      -+
            //
            //       Solving for k, k = 3/2,
            //    From (4), using 'k' value, range is therefore [-0.3333, 0.3333]
            //
            //  2. Polynomial Approximation:
            //     More information refer to tools/sollya/vrs4_logf.sollya
            //
            //     7th Deg -   Error abs: 0x1.04c4ac98p-22   rel: 0x1.2216e6f8p-19
            //     6th Deg -   Error abs: 0x1.179e97d8p-19   rel: 0x1.db676c1p-17

            const uint V_MIN = 0x00800000;
            const uint V_MAX = 0x7F800000;
            const uint V_MASK = 0x007FFFFF;
            const uint V_OFF = 0x3F2AAAAB;

            const float V_LN2 = 0.6931472f;

            const float C0 = 0.0f;
            const float C1 = 1.0f;
            const float C2 = -0.5000001f;
            const float C3 = 0.33332965f;
            const float C4 = -0.24999046f;
            const float C5 = 0.20018855f;
            const float C6 = -0.16700386f;
            const float C7 = 0.13902695f;
            const float C8 = -0.1197452f;
            const float C9 = 0.14401625f;
            const float C10 = -0.13657966f;

            TVectorSingle specialResult = x;

            // x is subnormal or infinity or NaN
            TVectorUInt32 specialMask = TVectorUInt32.GreaterThanOrEqual(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x) - TVectorUInt32.Create(V_MIN), TVectorUInt32.Create(V_MAX - V_MIN));

            if (specialMask != TVectorUInt32.Zero)
            {
                // float.IsZero(x) ? float.NegativeInfinity : x
                TVectorSingle zeroMask = TVectorSingle.Equals(x, TVectorSingle.Zero);

                specialResult = TVectorSingle.ConditionalSelect(
                    zeroMask,
                    TVectorSingle.Create(float.NegativeInfinity),
                    specialResult
                );

                // (x < 0) ? float.NaN : x
                TVectorSingle lessThanZeroMask = TVectorSingle.LessThan(x, TVectorSingle.Zero);

                specialResult = TVectorSingle.ConditionalSelect(
                    lessThanZeroMask,
                    TVectorSingle.Create(float.NaN),
                    specialResult
                );

                // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                TVectorSingle temp = zeroMask
                                   | lessThanZeroMask
                                   | ~TVectorSingle.Equals(x, x)
                                   | TVectorSingle.Equals(x, TVectorSingle.Create(float.PositiveInfinity));

                // subnormal
                TVectorSingle subnormalMask = TVectorSingle.AndNot(Unsafe.BitCast<TVectorUInt32, TVectorSingle>(specialMask), temp);

                x = TVectorSingle.ConditionalSelect(
                    subnormalMask,
                    Unsafe.BitCast<TVectorUInt32, TVectorSingle>(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x * 8388608.0f) - TVectorUInt32.Create(23u << 23)),
                    x
                );

                specialMask = Unsafe.BitCast<TVectorSingle, TVectorUInt32>(temp);
            }

            TVectorUInt32 vx = Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x) - TVectorUInt32.Create(V_OFF);
            TVectorSingle n = ConvertToSingle<TVectorInt32, TVectorSingle>(Unsafe.BitCast<TVectorUInt32, TVectorInt32>(vx) >> 23);

            vx = (vx & TVectorUInt32.Create(V_MASK)) + TVectorUInt32.Create(V_OFF);

            TVectorSingle r = Unsafe.BitCast<TVectorUInt32, TVectorSingle>(vx) - TVectorSingle.Create(1.0f);

            TVectorSingle r2 = r * r;
            TVectorSingle r4 = r2 * r2;
            TVectorSingle r8 = r4 * r4;

            TVectorSingle q = (TVectorSingle.Create(C10) * r2 + (TVectorSingle.Create(C9) * r + TVectorSingle.Create(C8)))
                                                       * r8 + (((TVectorSingle.Create(C7) * r + TVectorSingle.Create(C6))
                                                         * r2 + (TVectorSingle.Create(C5) * r + TVectorSingle.Create(C4)))
                                                        * r4 + ((TVectorSingle.Create(C3) * r + TVectorSingle.Create(C2))
                                                         * r2 + (TVectorSingle.Create(C1) * r + TVectorSingle.Create(C0))));

            return TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt32, TVectorSingle>(specialMask),
                specialResult,
                n * TVectorSingle.Create(V_LN2) + q
            );
        }

        public static TVectorDouble Log2Double<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `vrd2_log2` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Reduce x into the form:
            //        x = (-1)^s*2^n*m
            // s will be always zero, as log is defined for positive numbers
            // n is an integer known as the exponent
            // m is mantissa
            //
            // x is reduced such that the mantissa, m lies in [2/3,4/3]
            //      x = 2^n*m where m is in [2/3,4/3]
            //      log2(x) = log2(2^n*m)              We have log(a*b) = log(a)+log(b)
            //             = log2(2^n) + log2(m)       We have log(a^n) = n*log(a)
            //             = n + log2(m)
            //             = n + log2(1+(m-1))
            //             = n + ln(1+f) * log2(e)          Where f = m-1
            //             = n + log1p(f) * log2(e)         f lies in [-1/3,+1/3]
            //
            // Thus we have :
            // log(x) = n + log1p(f) * log2(e)
            // The second term log1p(F) is approximated by using a polynomial

            const ulong V_MIN = double.SmallestNormalBits;
            const ulong V_MAX = double.PositiveInfinityBits;
            const ulong V_MSK = 0x000FFFFF_FFFFFFFF;    // (1 << 52) - 1
            const ulong V_OFF = 0x3FE55555_55555555;    // 2.0 / 3.0

            const double LN2_HEAD = 1.44269180297851562500E+00;
            const double LN2_TAIL = 3.23791044778235969970E-06;

            const double C02 = -0.499999999999999560;
            const double C03 = +0.333333333333414750;
            const double C04 = -0.250000000000297430;
            const double C05 = +0.199999999975985220;
            const double C06 = -0.166666666608919500;
            const double C07 = +0.142857145600277100;
            const double C08 = -0.125000005127831270;
            const double C09 = +0.111110952357159440;
            const double C10 = -0.099999750495501240;
            const double C11 = +0.090914349823462390;
            const double C12 = -0.083340600527551860;
            const double C13 = +0.076817603328311300;
            const double C14 = -0.071296718946287310;
            const double C15 = +0.067963465211535730;
            const double C16 = -0.063995035098960040;
            const double C17 = +0.049370587082412105;
            const double C18 = -0.045370170994891980;
            const double C19 = +0.088970636003577750;
            const double C20 = -0.086906174116908760;

            TVectorDouble specialResult = x;

            // x is zero, subnormal, infinity, or NaN
            TVectorUInt64 specialMask = TVectorUInt64.GreaterThanOrEqual(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(x) - TVectorUInt64.Create(V_MIN), TVectorUInt64.Create(V_MAX - V_MIN));

            if (specialMask != TVectorUInt64.Zero)
            {
                TVectorInt64 xBits = Unsafe.BitCast<TVectorDouble, TVectorInt64>(x);

                // (x < 0) ? float.NaN : x
                TVectorDouble lessThanZeroMask = Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.LessThan(xBits, TVectorInt64.Zero));

                specialResult = TVectorDouble.ConditionalSelect(
                    lessThanZeroMask,
                    TVectorDouble.Create(double.NaN),
                    specialResult
                );

                // double.IsZero(x) ? double.NegativeInfinity : x
                TVectorDouble zeroMask = Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(xBits << 1, TVectorInt64.Zero));

                specialResult = TVectorDouble.ConditionalSelect(
                    zeroMask,
                    TVectorDouble.Create(double.NegativeInfinity),
                    specialResult
                );

                // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                TVectorDouble temp = zeroMask
                                   | lessThanZeroMask
                                   | Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThanOrEqual(xBits, TVectorInt64.Create((long)double.PositiveInfinityBits)));

                // subnormal
                TVectorDouble subnormalMask = TVectorDouble.AndNot(Unsafe.BitCast<TVectorUInt64, TVectorDouble>(specialMask), temp);

                // multiply by 2^52, then normalize
                x = TVectorDouble.ConditionalSelect(
                    subnormalMask,
                    Unsafe.BitCast<TVectorUInt64, TVectorDouble>(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(x * 4503599627370496.0) - TVectorUInt64.Create(52ul << 52)),
                    x
                );

                specialMask = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(temp);
            }

            // Reduce the mantissa to [+2/3, +4/3]
            TVectorUInt64 vx = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(x) - TVectorUInt64.Create(V_OFF);
            TVectorDouble n = ConvertToDouble<TVectorInt64, TVectorDouble>(Unsafe.BitCast<TVectorUInt64, TVectorInt64>(vx) >> 52);
            vx = (vx & TVectorUInt64.Create(V_MSK)) + TVectorUInt64.Create(V_OFF);

            // Adjust the mantissa to [-1/3, +1/3]
            TVectorDouble r = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(vx) - TVectorDouble.One;

            TVectorDouble r02 = r * r;
            TVectorDouble r04 = r02 * r02;
            TVectorDouble r08 = r04 * r04;
            TVectorDouble r16 = r08 * r08;

            // Compute log(x + 1) using polynomial approximation
            //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

            TVectorDouble poly = (((r04 * C20)
                                + ((((r * C19) + TVectorDouble.Create(C18)) * r02)
                                  + ((r * C17) + TVectorDouble.Create(C16)))) * r16)
                             + (((((((r * C15) + TVectorDouble.Create(C14)) * r02)
                                  + ((r * C13) + TVectorDouble.Create(C12))) * r04)
                                + ((((r * C11) + TVectorDouble.Create(C10)) * r02)
                                  + ((r * C09) + TVectorDouble.Create(C08)))) * r08)
                               + (((((r * C07) + TVectorDouble.Create(C06)) * r02)
                                  + ((r * C05) + TVectorDouble.Create(C04))) * r04)
                                + ((((r * C03) + TVectorDouble.Create(C02)) * r02) + r);

            return TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt64, TVectorDouble>(specialMask),
                specialResult,
                (poly * LN2_HEAD) + ((poly * LN2_TAIL) + n)
            );
        }

        public static TVectorSingle Log2Single<TVectorSingle, TVectorInt32, TVectorUInt32>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
        {
            // This code is based on `vrs4_log2f` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   log2f(x)
            //          = log2f(x)          if x ∈ F and x > 0
            //          = x                 if x = qNaN
            //          = 0                 if x = 1
            //          = -inf              if x = (-0, 0}
            //          = NaN               otherwise
            //
            // Assumptions/Expectations
            //      - Maximum ULP is observed to be at 4
            //      - Some FPU Exceptions may not be available
            //      - Performance is at least 3x
            //
            // Implementation Notes:
            //  1. Range Reduction:
            //      x = 2^n*(1+f)                                          .... (1)
            //         where n is exponent and is an integer
            //             (1+f) is mantissa ∈ [1,2). i.e., 1 ≤ 1+f < 2    .... (2)
            //
            //    From (1), taking log on both sides
            //      log2(x) = log2(2^n * (1+f))
            //             = n + log2(1+f)                           .... (3)
            //
            //      let z = 1 + f
            //             log2(z) = log2(k) + log2(z) - log2(k)
            //             log2(z) = log2(kz) - log2(k)
            //
            //    From (2), range of z is [1, 2)
            //       by simply dividing range by 'k', z is in [1/k, 2/k)  .... (4)
            //       Best choice of k is the one which gives equal and opposite values
            //       at extrema        +-      -+
            //              1          | 2      |
            //             --- - 1 = - |--- - 1 |
            //              k          | k      |                         .... (5)
            //                         +-      -+
            //
            //       Solving for k, k = 3/2,
            //    From (4), using 'k' value, range is therefore [-0.3333, 0.3333]
            //
            //  2. Polynomial Approximation:
            //     More information refer to tools/sollya/vrs4_logf.sollya
            //
            //     7th Deg -   Error abs: 0x1.04c4ac98p-22   rel: 0x1.2216e6f8p-19

            const uint V_MIN = float.SmallestNormalBits;
            const uint V_MAX = float.PositiveInfinityBits;
            const uint V_MSK = 0x007F_FFFF; // (1 << 23) - 1
            const uint V_OFF = 0x3F2A_AAAB; // 2.0 / 3.0

            const float C1 = +1.44269510f;
            const float C2 = -0.72134554f;
            const float C3 = +0.48089063f;
            const float C4 = -0.36084408f;
            const float C5 = +0.28889710f;
            const float C6 = -0.23594281f;
            const float C7 = +0.19948183f;
            const float C8 = -0.22616665f;
            const float C9 = +0.21228963f;

            TVectorSingle specialResult = x;

            // x is zero, subnormal, infinity, or NaN
            TVectorUInt32 specialMask = TVectorUInt32.GreaterThanOrEqual(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x) - TVectorUInt32.Create(V_MIN), TVectorUInt32.Create(V_MAX - V_MIN));

            if (specialMask != TVectorUInt32.Zero)
            {
                TVectorInt32 xBits = Unsafe.BitCast<TVectorSingle, TVectorInt32>(x);

                // (x < 0) ? float.NaN : x
                TVectorSingle lessThanZeroMask = Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.LessThan(xBits, TVectorInt32.Zero));

                specialResult = TVectorSingle.ConditionalSelect(
                    lessThanZeroMask,
                    TVectorSingle.Create(float.NaN),
                    specialResult
                );

                // float.IsZero(x) ? float.NegativeInfinity : x
                TVectorSingle zeroMask = Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.Equals(xBits << 1, TVectorInt32.Zero));

                specialResult = TVectorSingle.ConditionalSelect(
                    zeroMask,
                    TVectorSingle.Create(float.NegativeInfinity),
                    specialResult
                );

                // (x < 0) | float.IsZero(x) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                TVectorSingle temp = zeroMask
                                   | lessThanZeroMask
                                   | Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.GreaterThanOrEqual(xBits, TVectorInt32.Create((int)float.PositiveInfinityBits)));

                // subnormal
                TVectorSingle subnormalMask = TVectorSingle.AndNot(Unsafe.BitCast<TVectorUInt32, TVectorSingle>(specialMask), temp);

                // multiply by 2^23, then normalize
                x = TVectorSingle.ConditionalSelect(
                    subnormalMask,
                    Unsafe.BitCast<TVectorUInt32, TVectorSingle>(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x * 8388608.0f) - TVectorUInt32.Create(23u << 23)),
                    x
                );

                specialMask = Unsafe.BitCast<TVectorSingle, TVectorUInt32>(temp);
            }

            // Reduce the mantissa to [+2/3, +4/3]
            TVectorUInt32 vx = Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x) - TVectorUInt32.Create(V_OFF);
            TVectorSingle n = ConvertToSingle<TVectorInt32, TVectorSingle>(Unsafe.BitCast<TVectorUInt32, TVectorInt32>(vx) >> 23);
            vx = (vx & TVectorUInt32.Create(V_MSK)) + TVectorUInt32.Create(V_OFF);

            // Adjust the mantissa to [-1/3, +1/3]
            TVectorSingle r = Unsafe.BitCast<TVectorUInt32, TVectorSingle>(vx) - TVectorSingle.One;

            // Compute log(x + 1) using polynomial approximation
            //      C0 + (r * C1) + (r^2 * C2) + ... + (r^9 * C9)

            TVectorSingle r2 = r * r;
            TVectorSingle r4 = r2 * r2;
            TVectorSingle r8 = r4 * r4;

            TVectorSingle poly = (((TVectorSingle.Create(C9) * r) + TVectorSingle.Create(C8)) * r8)
                            + ((((((TVectorSingle.Create(C7) * r) + TVectorSingle.Create(C6)) * r2)
                                + ((TVectorSingle.Create(C5) * r) + TVectorSingle.Create(C4))) * r4)
                              + ((((TVectorSingle.Create(C3) * r) + TVectorSingle.Create(C2)) * r2)
                                 + (TVectorSingle.Create(C1) * r)));

            return TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt32, TVectorSingle>(specialMask),
                specialResult,
                n + poly
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector RadiansToDegrees<TVector, T>(TVector radians)
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : IFloatingPointIeee754<T>
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return (radians * TVector.Create(T.CreateTruncating(180))) / TVector.Create(T.Pi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble ConvertToDouble<TVectorInt64, TVectorDouble>(TVectorInt64 vector)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            Unsafe.SkipInit(out TVectorDouble result);

            if (typeof(TVectorInt64) == typeof(Vector<long>))
            {
                result = (TVectorDouble)(object)Vector.ConvertToDouble((Vector<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector64<long>))
            {
                result = (TVectorDouble)(object)Vector64.ConvertToDouble((Vector64<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector128<long>))
            {
                result = (TVectorDouble)(object)Vector128.ConvertToDouble((Vector128<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector256<long>))
            {
                result = (TVectorDouble)(object)Vector256.ConvertToDouble((Vector256<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector512<long>))
            {
                result = (TVectorDouble)(object)Vector512.ConvertToDouble((Vector512<long>)(object)vector);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorSingle ConvertToSingle<TVectorInt32, TVectorSingle>(TVectorInt32 vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
        {
            Unsafe.SkipInit(out TVectorSingle result);

            if (typeof(TVectorInt32) == typeof(Vector<int>))
            {
                result = (TVectorSingle)(object)Vector.ConvertToSingle((Vector<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector64<int>))
            {
                result = (TVectorSingle)(object)Vector64.ConvertToSingle((Vector64<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector128<int>))
            {
                result = (TVectorSingle)(object)Vector128.ConvertToSingle((Vector128<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector256<int>))
            {
                result = (TVectorSingle)(object)Vector256.ConvertToSingle((Vector256<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector512<int>))
            {
                result = (TVectorSingle)(object)Vector512.ConvertToSingle((Vector512<int>)(object)vector);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVector Create<TVector, T>(double value)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            Unsafe.SkipInit(out TVector result);

            if (typeof(TVector) == typeof(Vector<double>))
            {
                result = (TVector)(object)Vector.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector64<double>))
            {
                result = (TVector)(object)Vector64.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector128<double>))
            {
                result = (TVector)(object)Vector128.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector256<double>))
            {
                result = (TVector)(object)Vector256.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector512<double>))
            {
                result = (TVector)(object)Vector512.Create(value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVector Create<TVector, T>(float value)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            Unsafe.SkipInit(out TVector result);

            if (typeof(TVector) == typeof(Vector<float>))
            {
                result = (TVector)(object)Vector.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector64<float>))
            {
                result = (TVector)(object)Vector64.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector128<float>))
            {
                result = (TVector)(object)Vector128.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector256<float>))
            {
                result = (TVector)(object)Vector256.Create(value);
            }
            else if (typeof(TVector) == typeof(Vector512<float>))
            {
                result = (TVector)(object)Vector512.Create(value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorSingle Narrow<TVectorDouble, TVectorSingle>(TVectorDouble lower, TVectorDouble upper)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
        {
            Unsafe.SkipInit(out TVectorSingle result);

            if (typeof(TVectorDouble) == typeof(Vector<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector<float>));
                result = (TVectorSingle)(object)Vector.Narrow((Vector<double>)(object)lower, (Vector<double>)(object)upper);
            }
            else if (typeof(TVectorDouble) == typeof(Vector64<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector64<float>));
                result = (TVectorSingle)(object)Vector64.Narrow((Vector64<double>)(object)lower, (Vector64<double>)(object)upper);
            }
            else if (typeof(TVectorDouble) == typeof(Vector128<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector128<float>));
                result = (TVectorSingle)(object)Vector128.Narrow((Vector128<double>)(object)lower, (Vector128<double>)(object)upper);
            }
            else if (typeof(TVectorDouble) == typeof(Vector256<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector256<float>));
                result = (TVectorSingle)(object)Vector256.Narrow((Vector256<double>)(object)lower, (Vector256<double>)(object)upper);
            }
            else if (typeof(TVectorDouble) == typeof(Vector512<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector512<float>));
                result = (TVectorSingle)(object)Vector512.Narrow((Vector512<double>)(object)lower, (Vector512<double>)(object)upper);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (TVectorDouble Lower, TVectorDouble Upper) Widen<TVectorSingle, TVectorDouble>(TVectorSingle vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            Unsafe.SkipInit(out (TVectorDouble, TVectorDouble) result);

            if (typeof(TVectorSingle) == typeof(Vector<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector<double>));
                result = ((TVectorDouble, TVectorDouble))(object)Vector.Widen((Vector<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector64<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector64<double>));
                result = ((TVectorDouble, TVectorDouble))(object)Vector64.Widen((Vector64<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector128<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector128<double>));
                result = ((TVectorDouble, TVectorDouble))(object)Vector128.Widen((Vector128<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector256<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector256<double>));
                result = ((TVectorDouble, TVectorDouble))(object)Vector256.Widen((Vector256<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector512<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector512<double>));
                result = ((TVectorDouble, TVectorDouble))(object)Vector512.Widen((Vector512<float>)(object)vector);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }
    }
}
