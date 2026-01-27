// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Wasm;

namespace System.Runtime.Intrinsics
{
    internal static class VectorMath
    {
        public static TVectorDouble CosDouble<TVectorDouble, TVectorInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `cos` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // ---------------------
            // checks for special cases
            // if ( ux = infinity) raise overflow exception and return x
            // if x is NaN then raise invalid FP operation exception and return x.
            //
            // 1. Argument reduction
            // if |x| > 5e5 then
            //      __amd_remainder_piby2(x, &r, &rr, &region)
            // else
            //      Argument reduction
            //      Let z = |x| * 2/pi
            //      z = dn + r, where dn = round(z)
            //      rhead =  dn * pi/2_head
            //      rtail = dn * pi/2_tail
            //      r = z – dn = |x| - rhead – rtail
            //      expdiff = exp(dn) – exp(r)
            //      if(expdiff) > 15)
            //      rtail = |x| - dn*pi/2_tail2
            //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1 -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
            // rr = (|x| – rhead) – r + rtail
            //
            // 2. Polynomial approximation
            // if(dn is even)
            //       rr = rr * r;
            //       x4 = x2 * x2;
            //       s = 0.5 * x2;
            //       t =  s - 1.0;
            //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4 + x2 * (C5 + x2 * x6)))))
            //       r = (((1.0 + t) - s) - rr) + poly – t
            // else
            //       x3 = x2 * r
            //       poly = S2 + (r2 * (S3 + (r2 * (S4 + (r2 * (S5 + S6 * r2))))))
            //       r = r - ((x2 * (0.5*rr - x3 * poly)) - rr) - S1 * x3
            // if((sign + 1) & 2)
            //       return r
            // else
            //       return -r;
            //
            // if |x| < pi/4 && |x| > 2.0^(-13)
            //   cos(x) = 1.0 + x*x * (-0.5 + (C1*x*x + (C2*x*x + (C3*x*x
            //                              + (C4*x*x + (C5*x*x + C6*x*x))))))
            //
            // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
            //   cos(x) = 1.0 - x*x*0.5;;
            //
            // else return 1.0

            const long ARG_HUGE = 0x415312D000000000;       // 5e6
            const long ARG_LARGE = 0x3FE921FB54442D18;      // PI / 4
            const long ARG_SMALL = 0x3F20000000000000;      // 2^-13
            const long ARG_SMALLER = 0x3E40000000000000;    // 2^-27

            TVectorDouble ax = TVectorDouble.Abs(x);
            TVectorInt64 ux = Unsafe.BitCast<TVectorDouble, TVectorInt64>(ax);

            TVectorDouble result;

            if (TVectorInt64.LessThanAll(ux, TVectorInt64.Create(ARG_LARGE + 1)))
            {
                // We must be a finite value: (pi / 4) >= |x|
                TVectorDouble x2 = x * x;

                if (TVectorInt64.GreaterThanAny(ux, TVectorInt64.Create(ARG_SMALL - 1)))
                {
                    // at least one element is: |x| >= 2^-13
                    result = TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            CosDoublePoly(x),
                            x2,
                            TVectorDouble.Create(-0.5)),
                        x2,
                        TVectorDouble.One
                    );
                }
                else
                {
                    result = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(-0.5), x2, TVectorDouble.One);
                }
            }
            else if (TVectorInt64.LessThanAll(ux, TVectorInt64.Create(ARG_HUGE)))
            {
                // at least one element is: |x| > (pi / 4) -or- infinite -or- nan
                (TVectorDouble r, TVectorDouble rr, TVectorInt64 region) = SinCosReduce<TVectorDouble, TVectorInt64>(ax);

                TVectorDouble sin = SinDoubleLarge(r, rr);
                TVectorDouble cos = CosDoubleLarge(r, rr);

                result = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    cos,    // region 0 or 2
                    sin     // region 1 or 3
                );

                result = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals((region + TVectorInt64.One) & TVectorInt64.Create(2), TVectorInt64.Zero)),
                    +result,    // region 0 or 3
                    -result     // region 1 or 2
                );
            }
            else
            {
                return ScalarFallback(x);
            }

            return TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThan(ux, TVectorInt64.Create(ARG_SMALLER - 1))),
                result,             // for elements: |x| >= 2^-27, infinity, or NaN
                TVectorDouble.One   // for elements: 2^-27 > |x|
            );

            static TVectorDouble ScalarFallback(TVectorDouble x)
            {
                TVectorDouble result = TVectorDouble.Zero;

                for (int i = 0; i < TVectorDouble.ElementCount; i++)
                {
                    double scalar = double.Cos(x[i]);
                    result = result.WithElement(i, scalar);
                }

                return result;
            }
        }

        public static TVectorSingle CosSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `cosf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // ---------------------
            // Checks for special cases
            // if ( ux = infinity) raise overflow exception and return x
            // if x is NaN then raise invalid FP operation exception and return x.
            //
            // 1. Argument reduction
            // if |x| > 5e5 then
            //      __amd_remainder_piby2d2f((uint64_t)x, &r, &region)
            // else
            //      Argument reduction
            //      Let z = |x| * 2/pi
            //      z = dn + r, where dn = round(z)
            //      rhead =  dn * pi/2_head
            //      rtail = dn * pi/2_tail
            //      r = z – dn = |x| - rhead – rtail
            //      expdiff = exp(dn) – exp(r)
            //      if(expdiff) > 15)
            //      rtail = |x| - dn*pi/2_tail2
            //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1
            //          -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
            //
            // 2. Polynomial approximation
            // if(dn is even)
            //       x4 = x2 * x2;
            //       s = 0.5 * x2;
            //       t =  1.0 - s;
            //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * C4 )))
            //       r = t + poly
            // else
            //       x3 = x2 * r
            //       poly = x3 * (S1 + x2 * (S2 + x2 * (S3 + x2 * S4)))
            //       r = r + poly
            // if((sign + 1) & 2)
            //       return r
            // else
            //       return -r;
            //
            // if |x| < pi/4 && |x| > 2.0^(-13)
            //   r = 0.5 * x2;
            //   t = 1 - r;
            //   cos(x) = t + ((1.0 - t) - r) + (x*x * (x*x * C1 + C2*x*x + C3*x*x
            //             + C4*x*x +x*x*C5 + x*x*C6)))
            //
            // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
            //   cos(x) = 1.0 - x*x*0.5;;
            //
            // else return 1.0

            const int ARG_HUGE = 0x4A989680;    // 5e6
            const int ARG_LARGE = 0x3F490FDB;   // PI / 4
            const int ARG_SMALL = 0x3C000000;   // 2^-13
            const int ARG_SMALLER = 0x39000000; // 2^-27

            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorInt32 ux = Unsafe.BitCast<TVectorSingle, TVectorInt32>(ax);

            TVectorSingle result;

            if (TVectorInt32.LessThanAll(ux, TVectorInt32.Create(ARG_LARGE + 1)))
            {
                // We must be a finite value: (pi / 4) >= |x|

                if (TVectorInt32.GreaterThanAny(ux, TVectorInt32.Create(ARG_SMALL - 1)))
                {
                    // at least one element is: |x| >= 2^-13

                    if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
                    {
                        result = Narrow<TVectorDouble, TVectorSingle>(
                            CosSingleSmall(Widen<TVectorSingle, TVectorDouble>(x))
                        );
                    }
                    else
                    {
                        result = Narrow<TVectorDouble, TVectorSingle>(
                            CosSingleSmall(WidenLower<TVectorSingle, TVectorDouble>(x)),
                            CosSingleSmall(WidenUpper<TVectorSingle, TVectorDouble>(x))
                        );
                    }
                }
                else
                {
                    // at least one element is: 2^-13 > |x|
                    TVectorSingle x2 = x * x;
                    result = TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(-0.5f), x2, TVectorSingle.One);
                }
            }
            else if (TVectorInt32.LessThanAll(ux, TVectorInt32.Create(ARG_HUGE)))
            {
                // at least one element is: |x| > (pi / 4) -or- infinite -or- nan

                if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
                {
                    result = Narrow<TVectorDouble, TVectorSingle>(
                        CoreImpl(Widen<TVectorSingle, TVectorDouble>(ax))
                    );
                }
                else
                {
                    result = Narrow<TVectorDouble, TVectorSingle>(
                        CoreImpl(WidenLower<TVectorSingle, TVectorDouble>(ax)),
                        CoreImpl(WidenUpper<TVectorSingle, TVectorDouble>(ax))
                    );
                }
            }
            else
            {
                return ScalarFallback(x);
            }

            return TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.GreaterThan(ux, TVectorInt32.Create(ARG_SMALLER - 1))),
                result,             // for elements: |x| >= 2^-27, infinity, or NaN
                TVectorSingle.One   // for elements: 2^-27 > |x|
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static TVectorDouble CoreImpl(TVectorDouble ax)
            {
                (TVectorDouble r, _, TVectorInt64 region) = SinCosReduce<TVectorDouble, TVectorInt64>(ax);

                TVectorDouble sin = SinSinglePoly(r);
                TVectorDouble cos = CosSingleLarge(r);

                TVectorDouble result = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    cos,    // region 0 or 2
                    sin     // region 1 or 3
                );

                return TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals((region + TVectorInt64.One) & TVectorInt64.Create(2), TVectorInt64.Zero)),
                    +result,    // region 0 or 3
                    -result     // region 1 or 2
                );
            }

            static TVectorSingle ScalarFallback(TVectorSingle x)
            {
                TVectorSingle result = TVectorSingle.Zero;

                for (int i = 0; i < TVectorSingle.ElementCount; i++)
                {
                    float scalar = float.Cos(x[i]);
                    result = result.WithElement(i, scalar);
                }

                return result;
            }
        }

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
                return TVector.ConditionalSelect(TVector.IsNegative(value ^ sign), -value, value);
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

        public static TVectorDouble ExpDouble<TVectorDouble, TVectorUInt64>(TVectorDouble x)
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

            const double V_EXPF_HUGE = 6755399441055744;
            const double V_TBL_LN2 = 1.4426950408889634;

            const double V_LN2_HEAD = +0.693359375;
            const double V_LN2_TAIL = -0.00021219444005469057;

            const double C03 = 0.5000000000000018;
            const double C04 = 0.1666666666666617;
            const double C05 = 0.04166666666649277;
            const double C06 = 0.008333333333559272;
            const double C07 = 0.001388888895122404;
            const double C08 = 0.00019841269432677495;
            const double C09 = 2.4801486521374483E-05;
            const double C10 = 2.7557622532543023E-06;
            const double C11 = 2.7632293298250954E-07;
            const double C12 = 2.499430431958571E-08;

            // Check if -709 < vx < 709
            if (TVectorUInt64.LessThanOrEqualAll(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(TVectorDouble.Abs(x)), TVectorUInt64.Create(V_ARG_MAX)))
            {
                // x * (64.0 / ln(2))
                TVectorDouble dn = TVectorDouble.MultiplyAddEstimate(x, TVectorDouble.Create(V_TBL_LN2), TVectorDouble.Create(V_EXPF_HUGE));

                // n = (int)z
                TVectorUInt64 n = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(dn);

                // dn = (double)n
                dn -= TVectorDouble.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                TVectorDouble r = TVectorDouble.MultiplyAddEstimate(dn, TVectorDouble.Create(-V_LN2_HEAD), x);
                r = TVectorDouble.MultiplyAddEstimate(dn, TVectorDouble.Create(-V_LN2_TAIL), r);

                TVectorDouble r2 = r * r;
                TVectorDouble r4 = r2 * r2;
                TVectorDouble r8 = r4 * r4;

                // Compute polynomial
                TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C12), r, TVectorDouble.Create(C11)),
                        r2,
                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C10), r, TVectorDouble.Create(C09))),
                    r8,
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C08), r, TVectorDouble.Create(C07)),
                            r2,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C06), r, TVectorDouble.Create(C05))),
                        r4,
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C04), r, TVectorDouble.Create(C03)),
                            r2,
                            r + TVectorDouble.One
                        )
                    )
                );

                // m = (n - j) / 64
                // result = polynomial * 2^m
                return poly * Unsafe.BitCast<TVectorUInt64, TVectorDouble>((n + TVectorUInt64.Create(V_DP64_BIAS)) << 52);
            }
            else
            {
                return ScalarFallback(x);

                static TVectorDouble ScalarFallback(TVectorDouble x)
                {
                    TVectorDouble expResult = TVectorDouble.Zero;

                    for (int i = 0; i < TVectorDouble.ElementCount; i++)
                    {
                        double expScalar = double.Exp(x[i]);
                        expResult = expResult.WithElement(i, expScalar);
                    }

                    return expResult;
                }
            }
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

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                result = Narrow<TVectorDouble, TVectorSingle>(
                    CoreImpl(Widen<TVectorSingle, TVectorDouble>(x))
                );
            }
            else
            {
                result = Narrow<TVectorDouble, TVectorSingle>(
                    CoreImpl(WidenLower<TVectorSingle, TVectorDouble>(x)),
                    CoreImpl(WidenUpper<TVectorSingle, TVectorDouble>(x))
                );
            }

            // Check if -103 < |x| < 88
            if (TVectorUInt32.GreaterThanAny(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(TVectorSingle.Abs(x)), TVectorUInt32.Create(V_ARG_MAX)))
            {
                // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                TVectorSingle infinityMask = TVectorSingle.GreaterThan(x, TVectorSingle.Create(V_EXPF_MAX));

                result = TVectorSingle.ConditionalSelect(
                    infinityMask,
                    TVectorSingle.Create(float.PositiveInfinity),
                    result
                );

                // (x < V_EXPF_MIN) ? 0 : x
                result = TVectorSingle.AndNot(result, TVectorSingle.LessThan(x, TVectorSingle.Create(V_EXPF_MIN)));
            }

            return result;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static TVectorDouble CoreImpl(TVectorDouble x)
            {
                // x * (64.0 / ln(2))
                TVectorDouble z = x * TVectorDouble.Create(V_TBL_LN2);

                TVectorDouble v_expf_huge = TVectorDouble.Create(V_EXPF_HUGE);
                TVectorDouble dn = z + v_expf_huge;

                // n = (int)z
                TVectorUInt64 n = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(dn);

                // r = z - n
                TVectorDouble r = z - (dn - v_expf_huge);

                TVectorDouble r2 = r * r;
                TVectorDouble r4 = r2 * r2;

                TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                    r4,
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C6), r, TVectorDouble.Create(C5)),
                    TVectorDouble.MultiplyAddEstimate(
                        r2,
                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C4), r, TVectorDouble.Create(C3)),
                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C2), r, TVectorDouble.Create(C1))
                    )
                );

                // result = poly + (n << 52)
                return Unsafe.BitCast<TVectorUInt64, TVectorDouble>(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(poly) + (n << 52));
            }
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

            TVectorDouble infinityMask = TVectorDouble.IsPositiveInfinity(ax) | TVectorDouble.IsPositiveInfinity(ay);
            TVectorDouble nanMask = TVectorDouble.IsNaN(ax) | TVectorDouble.IsNaN(ay);

            TVectorUInt64 xBits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(ax);
            TVectorUInt64 yBits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(ay);

            TVectorUInt64 shiftedExponentMask = TVectorUInt64.Create(double.ShiftedBiasedExponentMask);
            TVectorUInt64 xExp = (xBits >> double.BiasedExponentShift) & shiftedExponentMask;
            TVectorUInt64 yExp = (yBits >> double.BiasedExponentShift) & shiftedExponentMask;

            TVectorUInt64 expDiff = xExp - yExp;

            // Cover cases where x or y is insignifican compared to the other
            TVectorDouble insignificanMask = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(
                TVectorUInt64.GreaterThanOrEqual(expDiff, TVectorUInt64.Create(double.SignificandLength + 1)) &
                TVectorUInt64.LessThanOrEqual(expDiff, TVectorUInt64.Create(unchecked((ulong)(-double.SignificandLength - 1))))
            );
            TVectorDouble insignificantResult = ax + ay;

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

            TVectorUInt64 xSubnormalMask = TVectorUInt64.IsZero(xExp) & scaleUpMask;
            xBits += subnormalBitsFix & xSubnormalMask;
            ax = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(xBits);
            ax -= subnormalFix & Unsafe.BitCast<TVectorUInt64, TVectorDouble>(xSubnormalMask);

            TVectorUInt64 ySubnormalMask = TVectorUInt64.IsZero(yExp) & scaleUpMask;
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

            rTail += TVectorDouble.MultiplyAddEstimate(xHead, xHead, -xx);
            rTail = TVectorDouble.MultiplyAddEstimate(xHead * 2, xTail, rTail);
            rTail = TVectorDouble.MultiplyAddEstimate(xTail, xTail, rTail);

            // We only need to do extra accounting when ax and ay have equal exponents
            TVectorDouble equalExponentsMask = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(TVectorUInt64.IsZero(expDiff));

            TVectorDouble rTailTmp = rTail;

            rTailTmp += TVectorDouble.MultiplyAddEstimate(yHead, yHead, -yy);
            rTailTmp = TVectorDouble.MultiplyAddEstimate(yHead * 2, yTail, rTailTmp);
            rTailTmp = TVectorDouble.MultiplyAddEstimate(yTail, yTail, rTailTmp);

            rTail = TVectorDouble.ConditionalSelect(equalExponentsMask, rTailTmp, rTail);

            TVectorDouble result = TVectorDouble.Sqrt(rHead + rTail) * expFix;

            // IEEE 754 requires that we return +Infinity
            // if either input is Infinity, even if one of
            // the inputs is NaN. Otherwise if either input
            // is NaN, we return NaN

            result = TVectorDouble.ConditionalSelect(insignificanMask, insignificantResult, result);
            result = TVectorDouble.ConditionalSelect(nanMask, TVectorDouble.Create(double.NaN), result);
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

            TVectorSingle infinityMask = TVectorSingle.IsPositiveInfinity(ax) | TVectorSingle.IsPositiveInfinity(ay);
            TVectorSingle nanMask = TVectorSingle.IsNaN(ax) | TVectorSingle.IsNaN(ay);

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                result = Narrow<TVectorDouble, TVectorSingle>(
                    CoreImpl(Widen<TVectorSingle, TVectorDouble>(ax), Widen<TVectorSingle, TVectorDouble>(ay))
                );
            }
            else
            {
                result = Narrow<TVectorDouble, TVectorSingle>(
                    CoreImpl(WidenLower<TVectorSingle, TVectorDouble>(ax), WidenLower<TVectorSingle, TVectorDouble>(ay)),
                    CoreImpl(WidenUpper<TVectorSingle, TVectorDouble>(ax), WidenUpper<TVectorSingle, TVectorDouble>(ay))
                );
            }

            // IEEE 754 requires that we return +Infinity
            // if either input is Infinity, even if one of
            // the inputs is NaN. Otherwise if either input
            // is NaN, we return NaN

            result = TVectorSingle.ConditionalSelect(nanMask, TVectorSingle.Create(float.NaN), result);
            result = TVectorSingle.ConditionalSelect(infinityMask, TVectorSingle.Create(float.PositiveInfinity), result);

            return result;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static TVectorDouble CoreImpl(TVectorDouble x, TVectorDouble y)
            {
                return TVectorDouble.Sqrt(TVectorDouble.MultiplyAddEstimate(x, x, y * y));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVectorSingle IsEvenIntegerSingle<TVectorSingle, TVectorUInt32>(TVectorSingle vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
        {
            TVectorUInt32 bits = Unsafe.BitCast<TVectorSingle, TVectorUInt32>(TVectorSingle.Abs(vector));

            TVectorUInt32 exponent = ((bits >> float.BiasedExponentShift) & TVectorUInt32.Create(float.ShiftedBiasedExponentMask)) - TVectorUInt32.Create(float.ExponentBias);
            TVectorUInt32 fractionalBits = TVectorUInt32.Create(float.BiasedExponentShift) - exponent;
            TVectorUInt32 firstIntegralBit = ShiftLeftUInt32(TVectorUInt32.One, fractionalBits);
            TVectorUInt32 fractionalBitMask = firstIntegralBit - TVectorUInt32.One;

            // We must be an integer in the range [1, 2^24) with the least significant integral bit clear
            // or in the range [2^24, +Infinity) in which case we are known to be an even integer
            TVectorUInt32 result = TVectorUInt32.GreaterThan(bits, TVectorUInt32.Create(0x3FFF_FFFF))
                                 & TVectorUInt32.LessThan(bits, TVectorUInt32.Create(float.PositiveInfinityBits))
                                 & ((TVectorUInt32.IsZero(bits & fractionalBitMask) & TVectorUInt32.IsZero(bits & firstIntegralBit))
                                  | TVectorUInt32.GreaterThan(bits, TVectorUInt32.Create(0x4B7F_FFFF)));

            // We are also an even integer if we are zero
            result |= TVectorUInt32.IsZero(bits);

            return Unsafe.BitCast<TVectorUInt32, TVectorSingle>(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVectorDouble IsEvenIntegerDouble<TVectorDouble, TVectorUInt64>(TVectorDouble vector)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            TVectorUInt64 bits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(TVectorDouble.Abs(vector));

            TVectorUInt64 exponent = ((bits >> double.BiasedExponentShift) & TVectorUInt64.Create(double.ShiftedBiasedExponentMask)) - TVectorUInt64.Create(double.ExponentBias);
            TVectorUInt64 fractionalBits = TVectorUInt64.Create(double.BiasedExponentShift) - exponent;
            TVectorUInt64 firstIntegralBit = ShiftLeftUInt64(TVectorUInt64.One, fractionalBits);
            TVectorUInt64 fractionalBitMask = firstIntegralBit - TVectorUInt64.One;

            // We must be an integer in the range [1, 2^53) with the least significant integral bit clear
            // or in the range [2^53, +Infinity) in which case we are known to be an even integer
            TVectorUInt64 result = TVectorUInt64.GreaterThan(bits, TVectorUInt64.Create(0x3FFF_FFFF_FFFF_FFFF))
                                 & TVectorUInt64.LessThan(bits, TVectorUInt64.Create(double.PositiveInfinityBits))
                                 & ((TVectorUInt64.IsZero(bits & fractionalBitMask) & TVectorUInt64.IsZero(bits & firstIntegralBit))
                                  | TVectorUInt64.GreaterThan(bits, TVectorUInt64.Create(0x433F_FFFF_FFFF_FFFF)));

            // We are also an even integer if we are zero
            result |= TVectorUInt64.IsZero(bits);

            return Unsafe.BitCast<TVectorUInt64, TVectorDouble>(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVectorSingle IsOddIntegerSingle<TVectorSingle, TVectorUInt32>(TVectorSingle vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
        {
            TVectorUInt32 bits = Unsafe.BitCast<TVectorSingle, TVectorUInt32>(TVectorSingle.Abs(vector));

            TVectorUInt32 exponent = ((bits >> float.BiasedExponentShift) & TVectorUInt32.Create(float.ShiftedBiasedExponentMask)) - TVectorUInt32.Create(float.ExponentBias);
            TVectorUInt32 fractionalBits = TVectorUInt32.Create(float.BiasedExponentShift) - exponent;
            TVectorUInt32 firstIntegralBit = ShiftLeftUInt32(TVectorUInt32.One, fractionalBits);
            TVectorUInt32 fractionalBitMask = firstIntegralBit - TVectorUInt32.One;

            // We must be an integer in the range [1, 2^24) with the least significant integral bit set
            TVectorUInt32 result = TVectorUInt32.GreaterThan(bits, TVectorUInt32.Create(0x3F7F_FFFF))
                                 & TVectorUInt32.LessThan(bits, TVectorUInt32.Create(0x4B80_0000))
                                 & TVectorUInt32.IsZero(bits & fractionalBitMask)
                                 & ~TVectorUInt32.IsZero(bits & firstIntegralBit);

            return Unsafe.BitCast<TVectorUInt32, TVectorSingle>(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVectorDouble IsOddIntegerDouble<TVectorDouble, TVectorUInt64>(TVectorDouble vector)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            TVectorUInt64 bits = Unsafe.BitCast<TVectorDouble, TVectorUInt64>(TVectorDouble.Abs(vector));

            TVectorUInt64 exponent = ((bits >> double.BiasedExponentShift) & TVectorUInt64.Create(double.ShiftedBiasedExponentMask)) - TVectorUInt64.Create(double.ExponentBias);
            TVectorUInt64 fractionalBits = TVectorUInt64.Create(double.BiasedExponentShift) - exponent;
            TVectorUInt64 firstIntegralBit = ShiftLeftUInt64(TVectorUInt64.One, fractionalBits);
            TVectorUInt64 fractionalBitMask = firstIntegralBit - TVectorUInt64.One;

            // We must be an integer in the range [1, 2^53) with the least significant integral bit set
            TVectorUInt64 result = TVectorUInt64.GreaterThan(bits, TVectorUInt64.Create(0x3FEF_FFFF_FFFF_FFFF))
                                 & TVectorUInt64.LessThan(bits, TVectorUInt64.Create(0x4340_0000_0000_0000))
                                 & TVectorUInt64.IsZero(bits & fractionalBitMask)
                                 & ~TVectorUInt64.IsZero(bits & firstIntegralBit);

            return Unsafe.BitCast<TVectorUInt64, TVectorDouble>(result);
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
                // double.IsNegative(x) ? double.NaN : x
                TVectorDouble isNegativeMask = TVectorDouble.IsNegative(x);

                specialResult = TVectorDouble.ConditionalSelect(
                    isNegativeMask,
                    TVectorDouble.Create(double.NaN),
                    specialResult
                );

                // double.IsZero(x) ? double.NegativeInfinity : x
                TVectorDouble zeroMask = TVectorDouble.IsZero(x);

                specialResult = TVectorDouble.ConditionalSelect(
                    zeroMask,
                    TVectorDouble.Create(double.NegativeInfinity),
                    specialResult
                );

                // double.IsZero(x) | double.IsNegative(x) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                TVectorDouble temp = zeroMask
                                   | isNegativeMask
                                   | TVectorDouble.IsNaN(x)
                                   | TVectorDouble.IsPositiveInfinity(x);

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

            TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                r16,
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.Create(C20),
                    r04,
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C19), r, TVectorDouble.Create(C18)),
                         r02,
                         TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C17), r, TVectorDouble.Create(C16)))),
                TVectorDouble.MultiplyAddEstimate(
                    r08,
                    TVectorDouble.MultiplyAddEstimate(
                        r04,
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C15), r, TVectorDouble.Create(C14)),
                            r02,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C13), r, TVectorDouble.Create(C12))),
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C11), r, TVectorDouble.Create(C10)),
                            r02,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C09), r, TVectorDouble.Create(C08)))),
                    TVectorDouble.MultiplyAddEstimate(
                        r04,
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C07), r, TVectorDouble.Create(C06)),
                            r02,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C05), r, TVectorDouble.Create(C04))),
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C03), r, TVectorDouble.Create(C02)),
                            r02,
                            r
                        )
                    )
                )
            );

            return TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt64, TVectorDouble>(specialMask),
                specialResult,
                TVectorDouble.MultiplyAddEstimate(
                    n,
                    TVectorDouble.Create(LN2_HEAD),
                    TVectorDouble.MultiplyAddEstimate(n, TVectorDouble.Create(LN2_TAIL), poly)
                )
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

            const float C02 = -0.5000001f;
            const float C03 = +0.33332965f;
            const float C04 = -0.24999046f;
            const float C05 = +0.20018855f;
            const float C06 = -0.16700386f;
            const float C07 = +0.13902695f;
            const float C08 = -0.1197452f;
            const float C09 = +0.14401625f;
            const float C10 = -0.13657966f;

            TVectorSingle specialResult = x;

            // x is subnormal or infinity or NaN
            TVectorUInt32 specialMask = TVectorUInt32.GreaterThanOrEqual(Unsafe.BitCast<TVectorSingle, TVectorUInt32>(x) - TVectorUInt32.Create(V_MIN), TVectorUInt32.Create(V_MAX - V_MIN));

            if (specialMask != TVectorUInt32.Zero)
            {
                // float.IsNegative(x) ? float.NaN : x
                TVectorSingle isNegativeMask = TVectorSingle.IsNegative(x);

                specialResult = TVectorSingle.ConditionalSelect(
                    isNegativeMask,
                    TVectorSingle.Create(float.NaN),
                    specialResult
                );

                // float.IsZero(x) ? float.NegativeInfinity : x
                TVectorSingle zeroMask = TVectorSingle.IsZero(x);

                specialResult = TVectorSingle.ConditionalSelect(
                    zeroMask,
                    TVectorSingle.Create(float.NegativeInfinity),
                    specialResult
                );

                // float.IsZero(x) | float.IsNegative(x) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                TVectorSingle temp = zeroMask
                                   | isNegativeMask
                                   | TVectorSingle.IsNaN(x)
                                   | TVectorSingle.IsPositiveInfinity(x);

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

            TVectorSingle q = TVectorSingle.MultiplyAddEstimate(
                TVectorSingle.MultiplyAddEstimate(
                    r2,
                    TVectorSingle.Create(C10),
                    TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C09), r, TVectorSingle.Create(C08))),
                r8,
                TVectorSingle.MultiplyAddEstimate(
                    TVectorSingle.MultiplyAddEstimate(
                        TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C07), r, TVectorSingle.Create(C06)),
                        r2,
                        TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C05), r, TVectorSingle.Create(C04))),
                    r4,
                    TVectorSingle.MultiplyAddEstimate(
                        TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C03), r, TVectorSingle.Create(C02)),
                        r2,
                        r
                    )
                )
            );

            return TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt32, TVectorSingle>(specialMask),
                specialResult,
                TVectorSingle.MultiplyAddEstimate(n, TVectorSingle.Create(V_LN2), q)
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
                // double.IsNegative(x) ? double.NaN : x
                TVectorDouble isNegativeMask = TVectorDouble.IsNegative(x);

                specialResult = TVectorDouble.ConditionalSelect(
                    isNegativeMask,
                    TVectorDouble.Create(double.NaN),
                    specialResult
                );

                // double.IsZero(x) ? double.NegativeInfinity : x
                TVectorDouble zeroMask = TVectorDouble.IsZero(x);

                specialResult = TVectorDouble.ConditionalSelect(
                    zeroMask,
                    TVectorDouble.Create(double.NegativeInfinity),
                    specialResult
                );

                // double.IsZero(x) | double.IsNegative(x) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                TVectorDouble temp = zeroMask
                                   | isNegativeMask
                                   | TVectorDouble.IsNaN(x)
                                   | TVectorDouble.IsPositiveInfinity(x);

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

            TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                r16,
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.Create(C20),
                    r04,
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C19), r, TVectorDouble.Create(C18)),
                         r02,
                         TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C17), r, TVectorDouble.Create(C16)))),
                TVectorDouble.MultiplyAddEstimate(
                    r08,
                    TVectorDouble.MultiplyAddEstimate(
                        r04,
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C15), r, TVectorDouble.Create(C14)),
                            r02,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C13), r, TVectorDouble.Create(C12))),
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C11), r, TVectorDouble.Create(C10)),
                            r02,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C09), r, TVectorDouble.Create(C08)))),
                    TVectorDouble.MultiplyAddEstimate(
                        r04,
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C07), r, TVectorDouble.Create(C06)),
                            r02,
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C05), r, TVectorDouble.Create(C04))),
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C03), r, TVectorDouble.Create(C02)),
                            r02,
                            r
                        )
                    )
                )
            );

            return TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt64, TVectorDouble>(specialMask),
                specialResult,
                TVectorDouble.MultiplyAddEstimate(
                    poly,
                    TVectorDouble.Create(LN2_HEAD),
                    TVectorDouble.MultiplyAddEstimate(poly, TVectorDouble.Create(LN2_TAIL), n)
                )
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
                // float.IsNegative(x) ? float.NaN : x
                TVectorSingle isNegativeMask = TVectorSingle.IsNegative(x);

                specialResult = TVectorSingle.ConditionalSelect(
                    isNegativeMask,
                    TVectorSingle.Create(float.NaN),
                    specialResult
                );

                // float.IsZero(x) ? float.NegativeInfinity : x
                TVectorSingle zeroMask = TVectorSingle.IsZero(x);

                specialResult = TVectorSingle.ConditionalSelect(
                    zeroMask,
                    TVectorSingle.Create(float.NegativeInfinity),
                    specialResult
                );

                // float.IsZero(x) | float.IsNegative(x) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                TVectorSingle temp = zeroMask
                                   | isNegativeMask
                                   | TVectorSingle.IsNaN(x)
                                   | TVectorSingle.IsPositiveInfinity(x);

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

            TVectorSingle poly = TVectorSingle.MultiplyAddEstimate(
                TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C9), r, TVectorSingle.Create(C8)),
                r8,
                TVectorSingle.MultiplyAddEstimate(
                    TVectorSingle.MultiplyAddEstimate(
                        TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C7), r, TVectorSingle.Create(C6)),
                        r2,
                        TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C5), r, TVectorSingle.Create(C4))),
                    r4,
                    TVectorSingle.MultiplyAddEstimate(
                        TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(C3), r, TVectorSingle.Create(C2)),
                        r2,
                        TVectorSingle.Create(C1) * r
                    )
                )
            );

            return TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorUInt32, TVectorSingle>(specialMask),
                specialResult,
                n + poly
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector Max<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return TVector.ConditionalSelect(
                    TVector.LessThan(y, x) | TVector.IsNaN(x) | (TVector.Equals(x, y) & TVector.IsNegative(y)),
                    x,
                    y
                );
            }
            return TVector.ConditionalSelect(TVector.GreaterThan(x, y), x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector MaxMagnitude<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                TVector xMag = TVector.Abs(x);
                TVector yMag = TVector.Abs(y);
                return TVector.ConditionalSelect(
                    TVector.GreaterThan(xMag, yMag) | TVector.IsNaN(xMag) | (TVector.Equals(xMag, yMag) & TVector.IsPositive(x)),
                    x,
                    y
                );
            }
            return MaxMagnitudeNumber<TVector, T>(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector MaxMagnitudeNumber<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return TVector.Max(x, y);
            }

            TVector xMag = TVector.Abs(x);
            TVector yMag = TVector.Abs(y);

            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return TVector.ConditionalSelect(
                    TVector.GreaterThan(xMag, yMag) | TVector.IsNaN(yMag) | (TVector.Equals(xMag, yMag) & TVector.IsPositive(x)),
                    x,
                    y
                );
            }

            Debug.Assert((typeof(T) == typeof(sbyte))
                      || (typeof(T) == typeof(short))
                      || (typeof(T) == typeof(int))
                      || (typeof(T) == typeof(long))
                      || (typeof(T) == typeof(nint)));

            return TVector.ConditionalSelect(
                (TVector.GreaterThan(xMag, yMag) & TVector.IsPositive(yMag)) | (TVector.Equals(xMag, yMag) & TVector.IsNegative(y)) | TVector.IsNegative(xMag),
                x,
                y
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector MaxNumber<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return TVector.ConditionalSelect(
                    TVector.LessThan(y, x) | TVector.IsNaN(y) | (TVector.Equals(x, y) & TVector.IsNegative(y)),
                    x,
                    y
                );
            }

            return TVector.Max(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector Min<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return TVector.ConditionalSelect(
                    TVector.LessThan(x, y) | TVector.IsNaN(x) | (TVector.Equals(x, y) & TVector.IsNegative(x)),
                    x,
                    y
                );
            }
            return TVector.ConditionalSelect(TVector.LessThan(x, y), x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector MinMagnitude<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                TVector xMag = TVector.Abs(x);
                TVector yMag = TVector.Abs(y);

                return TVector.ConditionalSelect(
                    TVector.LessThan(xMag, yMag) | TVector.IsNaN(xMag) | (TVector.Equals(xMag, yMag) & TVector.IsNegative(x)),
                    x,
                    y
                );
            }
            return MinMagnitudeNumber<TVector, T>(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector MinMagnitudeNumber<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return TVector.Min(x, y);
            }

            TVector xMag = TVector.Abs(x);
            TVector yMag = TVector.Abs(y);

            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return TVector.ConditionalSelect(
                    TVector.LessThan(xMag, yMag) | TVector.IsNaN(yMag) | (TVector.Equals(xMag, yMag) & TVector.IsNegative(x)),
                    x,
                    y
                );
            }

            Debug.Assert((typeof(T) == typeof(sbyte))
                      || (typeof(T) == typeof(short))
                      || (typeof(T) == typeof(int))
                      || (typeof(T) == typeof(long))
                      || (typeof(T) == typeof(nint)));

            return TVector.ConditionalSelect(
                (TVector.LessThan(xMag, yMag) & TVector.IsPositive(xMag)) | (TVector.Equals(xMag, yMag) & TVector.IsNegative(x)) | TVector.IsNegative(yMag),
                x,
                y
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector MinNumber<TVector, T>(TVector x, TVector y)
            where TVector : unmanaged, ISimdVector<TVector, T>
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return TVector.ConditionalSelect(
                    TVector.LessThan(x, y) | TVector.IsNaN(y) | (TVector.Equals(x, y) & TVector.IsNegative(x)),
                    x,
                    y
                );
            }
            return TVector.Min(x, y);
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

        public static TVectorDouble RoundDouble<TVectorDouble>(TVectorDouble vector, MidpointRounding mode)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            switch (mode)
            {
                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                case MidpointRounding.AwayFromZero:
                {
                    // manually fold BitDecrement(0.5)
                    return TVectorDouble.Truncate(vector + CopySign<TVectorDouble, double>(TVectorDouble.Create(0.49999999999999994), vector));
                }

                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value with an even least significant digit
                case MidpointRounding.ToEven:
                {
                    return TVectorDouble.Round(vector);
                }

                // Directed rounding: Round to the nearest value, toward to zero
                case MidpointRounding.ToZero:
                {
                    return TVectorDouble.Truncate(vector);
                }

                // Directed Rounding: Round down to the next value, toward negative infinity
                case MidpointRounding.ToNegativeInfinity:
                {
                    return TVectorDouble.Floor(vector);
                }

                // Directed rounding: Round up to the next value, toward positive infinity
                case MidpointRounding.ToPositiveInfinity:
                {
                    return TVectorDouble.Ceiling(vector);
                }

                default:
                {
                    ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
                    return default;
                }
            }
        }

        public static TVectorSingle RoundSingle<TVectorSingle>(TVectorSingle vector, MidpointRounding mode)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
        {
            switch (mode)
            {
                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                case MidpointRounding.AwayFromZero:
                {
                    // manually fold BitDecrement(0.5)
                    return TVectorSingle.Truncate(vector + CopySign<TVectorSingle, float>(TVectorSingle.Create(0.49999997f), vector));
                }

                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value with an even least significant digit
                case MidpointRounding.ToEven:
                {
                    return TVectorSingle.Round(vector);
                }

                // Directed rounding: Round to the nearest value, toward to zero
                case MidpointRounding.ToZero:
                {
                    return TVectorSingle.Truncate(vector);
                }

                // Directed Rounding: Round down to the next value, toward negative infinity
                case MidpointRounding.ToNegativeInfinity:
                {
                    return TVectorSingle.Floor(vector);
                }

                // Directed rounding: Round up to the next value, toward positive infinity
                case MidpointRounding.ToPositiveInfinity:
                {
                    return TVectorSingle.Ceiling(vector);
                }

                default:
                {
                    ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
                    return default;
                }
            }
        }

        public static (TVectorDouble Sin, TVectorDouble Cos) SinCosDouble<TVectorDouble, TVectorInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `sin` and `cos` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // See SinDouble and CosDouble for implementation details

            const long ARG_HUGE = 0x415312D000000000;       // 5e6
            const long ARG_LARGE = 0x3FE921FB54442D18;      // PI / 4
            const long ARG_SMALL = 0x3F20000000000000;      // 2^-13
            const long ARG_SMALLER = 0x3E40000000000000;    // 2^-27

            TVectorDouble ax = TVectorDouble.Abs(x);
            TVectorInt64 ux = Unsafe.BitCast<TVectorDouble, TVectorInt64>(ax);

            TVectorDouble sinResult, cosResult;

            if (TVectorInt64.LessThanAll(ux, TVectorInt64.Create(ARG_LARGE + 1)))
            {
                // We must be a finite value: (pi / 4) >= |x|
                TVectorDouble x2 = x * x;

                if (TVectorInt64.GreaterThanAny(ux, TVectorInt64.Create(ARG_SMALL - 1)))
                {
                    // at least one element is: |x| >= 2^-13
                    sinResult = SinDoublePoly(x);
                    cosResult = TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            CosDoublePoly(x),
                            x2,
                            TVectorDouble.Create(-0.5)),
                        x2,
                        TVectorDouble.One
                    );
                }
                else
                {
                    // at least one element is: 2^-13 > |x|
                    TVectorDouble x3 = x2 * x;
                    sinResult = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(-0.16666666666666666), x3, x);
                    cosResult = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(-0.5), x2, TVectorDouble.One);
                }
            }
            else if (TVectorInt64.LessThanAll(ux, TVectorInt64.Create(ARG_HUGE)))
            {
                // at least one element is: |x| > (pi / 4) -or- infinite -or- nan
                (TVectorDouble r, TVectorDouble rr, TVectorInt64 region) = SinCosReduce<TVectorDouble, TVectorInt64>(ax);

                TVectorDouble sin = SinDoubleLarge(r, rr);
                TVectorDouble cos = CosDoubleLarge(r, rr);

                sinResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    sin,    // region 0 or 2
                    cos     // region 1 or 3
                );

                cosResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    cos,    // region 0 or 2
                    sin     // region 1 or 3
                );

                TVectorInt64 sign = Unsafe.BitCast<TVectorDouble, TVectorInt64>(x) >>> 63;

                sinResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(((sign & (region >>> 1)) | (~sign & ~(region >>> 1))) & TVectorInt64.One, TVectorInt64.Zero)),
                    -sinResult, // negative in region 1 or 3, positive in region 0 or 2
                    +sinResult  // negative in region 0 or 2, positive in region 1 or 3
                );

                cosResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals((region + TVectorInt64.One) & TVectorInt64.Create(2), TVectorInt64.Zero)),
                    +cosResult, // region 0 or 3
                    -cosResult  // region 1 or 2
                );
            }
            else
            {
                return ScalarFallback(x);
            }

            sinResult = TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThan(ux, TVectorInt64.Create(ARG_SMALLER - 1))),
                sinResult,          // for elements: |x| >= 2^-27, infinity, or NaN
                x                   // for elements: 2^-27 > |x|
            );

            cosResult = TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThan(ux, TVectorInt64.Create(ARG_SMALLER - 1))),
                cosResult,          // for elements: |x| >= 2^-27, infinity, or NaN
                TVectorDouble.One   // for elements: 2^-27 > |x|
            );

            return (sinResult, cosResult);

            static (TVectorDouble Sin, TVectorDouble Cos) ScalarFallback(TVectorDouble x)
            {
                TVectorDouble sinResult = TVectorDouble.Zero;
                TVectorDouble cosResult = TVectorDouble.Zero;

                for (int i = 0; i < TVectorDouble.ElementCount; i++)
                {
                    (double sinScalar, double cosScalar) = double.SinCos(x[i]);
                    sinResult = sinResult.WithElement(i, sinScalar);
                    cosResult = cosResult.WithElement(i, cosScalar);
                }

                return (sinResult, cosResult);
            }
        }

        public static (TVectorSingle Sin, TVectorSingle Cos) SinCosSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `sinf` and `cosf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // See SinSingle and CosSingle for implementation details

            const int ARG_HUGE = 0x4A989680;    // 5e6
            const int ARG_LARGE = 0x3F490FDB;   // PI / 4
            const int ARG_SMALL = 0x3C000000;   // 2^-13
            const int ARG_SMALLER = 0x39000000; // 2^-27

            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorInt32 ux = Unsafe.BitCast<TVectorSingle, TVectorInt32>(ax);

            TVectorSingle sinResult, cosResult;

            if (TVectorInt32.LessThanAll(ux, TVectorInt32.Create(ARG_LARGE + 1)))
            {
                // We must be a finite value: (pi / 4) >= |x|

                if (TVectorInt32.GreaterThanAny(ux, TVectorInt32.Create(ARG_SMALL - 1)))
                {
                    // at least one element is: |x| >= 2^-13

                    if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
                    {
                        TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);

                        sinResult = Narrow<TVectorDouble, TVectorSingle>(
                            SinSinglePoly(dx)
                        );
                        cosResult = Narrow<TVectorDouble, TVectorSingle>(
                            CosSingleSmall(dx)
                        );
                    }
                    else
                    {
                        TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                        TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);

                        sinResult = Narrow<TVectorDouble, TVectorSingle>(
                            SinSinglePoly(dxLo),
                            SinSinglePoly(dxHi)
                        );
                        cosResult = Narrow<TVectorDouble, TVectorSingle>(
                            CosSingleSmall(dxLo),
                            CosSingleSmall(dxHi)
                        );
                    }
                }
                else
                {
                    // at least one element is: 2^-13 > |x|

                    TVectorSingle x2 = x * x;
                    TVectorSingle x3 = x2 * x;

                    sinResult = TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(-0.16666667f), x3, x);
                    cosResult = TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(-0.5f), x2, TVectorSingle.One);
                }
            }
            else if (TVectorInt32.LessThanAll(ux, TVectorInt32.Create(ARG_HUGE)))
            {
                // at least one element is: |x| > (pi / 4) -or- infinite -or- nan

                if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
                {
                    (TVectorDouble sin, TVectorDouble cos) = CoreImpl(Widen<TVectorSingle, TVectorDouble>(x));

                    sinResult = Narrow<TVectorDouble, TVectorSingle>(sin);
                    cosResult = Narrow<TVectorDouble, TVectorSingle>(cos);
                }
                else
                {
                    (TVectorDouble sinLo, TVectorDouble cosLo) = CoreImpl(WidenLower<TVectorSingle, TVectorDouble>(x));
                    (TVectorDouble sinHi, TVectorDouble cosHi) = CoreImpl(WidenUpper<TVectorSingle, TVectorDouble>(x));

                    sinResult = Narrow<TVectorDouble, TVectorSingle>(sinLo, sinHi);
                    cosResult = Narrow<TVectorDouble, TVectorSingle>(cosLo, cosHi);
                }
            }
            else
            {
                return ScalarFallback(x);
            }

            sinResult = TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.GreaterThan(ux, TVectorInt32.Create(ARG_SMALLER - 1))),
                sinResult,          // for elements: |x| >= 2^-27, infinity, or NaN
                x                   // for elements: 2^-27 > |x|
            );

            cosResult = TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.GreaterThan(ux, TVectorInt32.Create(ARG_SMALLER - 1))),
                cosResult,          // for elements: |x| >= 2^-27, infinity, or NaN
                TVectorSingle.One   // for elements: 2^-27 > |x|
            );

            return (sinResult, cosResult);

            static (TVectorDouble Sin, TVectorDouble Cos) CoreImpl(TVectorDouble x)
            {
                TVectorDouble ax = TVectorDouble.Abs(x);
                (TVectorDouble r, _, TVectorInt64 region) = SinCosReduce<TVectorDouble, TVectorInt64>(ax);

                TVectorDouble sin = SinSinglePoly(r);
                TVectorDouble cos = CosSingleLarge(r);

                TVectorDouble sinResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    sin,    // region 0 or 2
                    cos     // region 1 or 3
                );

                TVectorDouble cosResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    cos,    // region 0 or 2
                    sin     // region 1 or 3
                );

                TVectorInt64 sign = Unsafe.BitCast<TVectorDouble, TVectorInt64>(x) >>> 63;

                sinResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(((sign & (region >>> 1)) | (~sign & ~(region >>> 1))) & TVectorInt64.One, TVectorInt64.Zero)),
                    -sinResult, // negative in region 1 or 3, positive in region 0 or 2
                    +sinResult  // negative in region 0 or 2, positive in region 1 or 3
                );

                cosResult = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals((region + TVectorInt64.One) & TVectorInt64.Create(2), TVectorInt64.Zero)),
                    +cosResult, // region 0 or 3
                    -cosResult  // region 1 or 2
                );

                return (sinResult, cosResult);
            }

            static (TVectorSingle Sin, TVectorSingle Cos) ScalarFallback(TVectorSingle x)
            {
                TVectorSingle sinResult = TVectorSingle.Zero;
                TVectorSingle cosResult = TVectorSingle.Zero;

                for (int i = 0; i < TVectorSingle.ElementCount; i++)
                {
                    (float sinScalar, float cosScalar) = float.SinCos(x[i]);
                    sinResult = sinResult.WithElement(i, sinScalar);
                    cosResult = cosResult.WithElement(i, cosScalar);
                }

                return (sinResult, cosResult);
            }
        }

        public static TVectorDouble SinDouble<TVectorDouble, TVectorInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `sin` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // ---------------------
            // checks for special cases
            // if ( ux = infinity) raise overflow exception and return x
            // if x is NaN then raise invalid FP operation exception and return x.
            //
            // 1. Argument reduction
            // if |x| > 5e5 then
            //      __amd_remainder_piby2(x, &r, &rr, &region)
            // else
            //      Argument reduction
            //      Let z = |x| * 2/pi
            //      z = dn + r, where dn = round(z)
            //      rhead =  dn * pi/2_head
            //      rtail = dn * pi/2_tail
            //      r = z – dn = |x| - rhead – rtail
            //      expdiff = exp(dn) – exp(r)
            //      if(expdiff) > 15)
            //      rtail = |x| - dn*pi/2_tail2
            //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1 -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
            // rr = (|x| – rhead) – r + rtail
            //
            // 2. Polynomial approximation
            // if(dn is odd)
            //       rr = rr * r;
            //       x4 = x2 * x2;
            //       s = 0.5 * x2;
            //       t =  s - 1.0;
            //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4 + x2 * (C5 + x2 * x6)))))
            //       r = (((1.0 + t) - s) - rr) + poly – t
            // else
            //       x3 = x2 * r
            //       poly = S2 + (r2 * (S3 + (r2 * (S4 + (r2 * (S5 + S6 * r2))))))
            //       r = r - ((x2 * (0.5*rr - x3 * poly)) - rr) - S1 * x3
            // if(((sign & region) | ((~sign) & (~region))) & 1)
            //       return r
            // else
            //       return -r;
            //
            // if |x| < pi/4 && |x| > 2.0^(-13)
            //   sin(x) = x + (x * (r2 * (S1 + r2 * (S2 + r2 * (S3 + r2 * (S4 + r2 * (S5 + r2 * S6)))))))
            // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
            //   sin(x) = x - (x * x * x * (1/6));

            const long ARG_HUGE = 0x415312D000000000;       // 5e6
            const long ARG_LARGE = 0x3FE921FB54442D18;      // PI / 4
            const long ARG_SMALL = 0x3F20000000000000;      // 2^-13
            const long ARG_SMALLER = 0x3E40000000000000;    // 2^-27

            TVectorDouble ax = TVectorDouble.Abs(x);
            TVectorInt64 ux = Unsafe.BitCast<TVectorDouble, TVectorInt64>(ax);

            TVectorDouble result;

            if (TVectorInt64.LessThanAll(ux, TVectorInt64.Create(ARG_LARGE + 1)))
            {
                // We must be a finite value: (pi / 4) >= |x|
                TVectorDouble x2 = x * x;

                if (TVectorInt64.GreaterThanAny(ux, TVectorInt64.Create(ARG_SMALL - 1)))
                {
                    // at least one element is: |x| >= 2^-13
                    result = SinDoublePoly(x);
                }
                else
                {
                    // at least one element is: 2^-13 > |x|
                    TVectorDouble x3 = x2 * x;
                    result = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(-0.16666666666666666), x3, x);
                }
            }
            else if (TVectorInt64.LessThanAll(ux, TVectorInt64.Create(ARG_HUGE)))
            {
                // at least one element is: |x| > (pi / 4) -or- infinite -or- nan
                (TVectorDouble r, TVectorDouble rr, TVectorInt64 region) = SinCosReduce<TVectorDouble, TVectorInt64>(ax);

                TVectorDouble sin = SinDoubleLarge(r, rr);
                TVectorDouble cos = CosDoubleLarge(r, rr);

                result = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    sin,    // region 0 or 2
                    cos     // region 1 or 3
                );

                TVectorInt64 sign = Unsafe.BitCast<TVectorDouble, TVectorInt64>(x) >>> 63;

                result = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(((sign & (region >>> 1)) | (~sign & ~(region >>> 1))) & TVectorInt64.One, TVectorInt64.Zero)),
                    -result,    // negative in region 1 or 3, positive in region 0 or 2
                    +result     // negative in region 0 or 2, positive in region 1 or 3
                );
            }
            else
            {
                return ScalarFallback(x);
            }

            return TVectorDouble.ConditionalSelect(
                Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThan(ux, TVectorInt64.Create(ARG_SMALLER - 1))),
                result,     // for elements: |x| >= 2^-27, infinity, or NaN
                x           // for elements: 2^-27 > |x|
            );

            static TVectorDouble ScalarFallback(TVectorDouble x)
            {
                TVectorDouble result = TVectorDouble.Zero;

                for (int i = 0; i < TVectorDouble.ElementCount; i++)
                {
                    double scalar = double.Sin(x[i]);
                    result = result.WithElement(i, scalar);
                }

                return result;
            }
        }

        public static TVectorSingle SinSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `sinf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // ---------------------
            // checks for special cases
            // if ( ux = infinity) raise overflow exception and return x
            // if x is NaN then raise invalid FP operation exception and return x.
            //
            // 1. Argument reduction
            // if |x| > 5e5 then
            //      __amd_remainder_piby2(x, &r, &rr, &region)
            // else
            //      Argument reduction
            //      Let z = |x| * 2/pi
            //      z = dn + r, where dn = round(z)
            //      rhead =  dn * pi/2_head
            //      rtail = dn * pi/2_tail
            //      r = z – dn = |x| - rhead – rtail
            //      expdiff = exp(dn) – exp(r)
            //      if(expdiff) > 15)
            //      rtail = |x| - dn*pi/2_tail2
            //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1 -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
            // rr = (|x| – rhead) – r + rtail
            //
            // 2. Polynomial approximation
            // if(dn is odd)
            //       rr = rr * r;
            //       x4 = x2 * x2;
            //       s = 0.5 * x2;
            //       t =  s - 1.0;
            //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4))))
            //       r = (((1.0 + t) - s) - rr) + poly – t
            // else
            //       x3 = x2 * r
            //       poly = S2 + (r2 * (S3 + (r2 * (S4))))
            //       r = r - ((x2 * (0.5*rr - x3 * poly)) - rr) - S1 * x3
            // if(((sign & region) | ((~sign) & (~region))) & 1)
            //       return r
            // else
            //       return -r;
            //
            // if |x| < pi/4 && |x| > 2.0^(-13)
            //   sin(x) = x + (x * (r2 * (S1 + r2 * (S2 + r2 * (S3 + r2 * (S4)))))
            // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
            //   sin(x) = x - (x * x * x * (1/6));

            const int ARG_HUGE = 0x4A989680;    // 5e6
            const int ARG_LARGE = 0x3F490FDB;   // PI / 4
            const int ARG_SMALL = 0x3C000000;   // 2^-13
            const int ARG_SMALLER = 0x39000000; // 2^-27

            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorInt32 ux = Unsafe.BitCast<TVectorSingle, TVectorInt32>(ax);

            TVectorSingle result;

            if (TVectorInt32.LessThanAll(ux, TVectorInt32.Create(ARG_LARGE + 1)))
            {
                // We must be a finite value: (pi / 4) >= |x|

                if (TVectorInt32.GreaterThanAny(ux, TVectorInt32.Create(ARG_SMALL - 1)))
                {
                    // at least one element is: |x| >= 2^-13

                    if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
                    {
                        result = Narrow<TVectorDouble, TVectorSingle>(
                            SinSinglePoly(Widen<TVectorSingle, TVectorDouble>(x))
                        );
                    }
                    else
                    {
                        result = Narrow<TVectorDouble, TVectorSingle>(
                            SinSinglePoly(WidenLower<TVectorSingle, TVectorDouble>(x)),
                            SinSinglePoly(WidenUpper<TVectorSingle, TVectorDouble>(x))
                        );
                    }
                }
                else
                {
                    // at least one element is: 2^-13 > |x|
                    TVectorSingle x3 = (x * x) * x;
                    result = TVectorSingle.MultiplyAddEstimate(TVectorSingle.Create(-0.16666667f), x3, x);
                }
            }
            else if (TVectorInt32.LessThanAll(ux, TVectorInt32.Create(ARG_HUGE)))
            {
                // at least one element is: |x| > (pi / 4) -or- infinite -or- nan

                if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
                {
                    result = Narrow<TVectorDouble, TVectorSingle>(
                        CoreImpl(Widen<TVectorSingle, TVectorDouble>(x))
                    );
                }
                else
                {
                    result = Narrow<TVectorDouble, TVectorSingle>(
                        CoreImpl(WidenLower<TVectorSingle, TVectorDouble>(x)),
                        CoreImpl(WidenUpper<TVectorSingle, TVectorDouble>(x))
                    );
                }
            }
            else
            {
                return ScalarFallback(x);
            }

            return TVectorSingle.ConditionalSelect(
                Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.GreaterThan(ux, TVectorInt32.Create(ARG_SMALLER - 1))),
                result,     // for elements: |x| >= 2^-27, infinity, or NaN
                x           // for elements: 2^-27 > |x|
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static TVectorDouble CoreImpl(TVectorDouble x)
            {
                TVectorDouble ax = TVectorDouble.Abs(x);
                (TVectorDouble r, _, TVectorInt64 region) = SinCosReduce<TVectorDouble, TVectorInt64>(ax);

                TVectorDouble sin = SinSinglePoly(r);
                TVectorDouble cos = CosSingleLarge(r);

                TVectorDouble result = TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(region & TVectorInt64.One, TVectorInt64.Zero)),
                    sin,    // region 0 or 2
                    cos     // region 1 or 3
                );

                TVectorInt64 sign = Unsafe.BitCast<TVectorDouble, TVectorInt64>(x) >>> 63;

                return TVectorDouble.ConditionalSelect(
                    Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.Equals(((sign & (region >>> 1)) | (~sign & ~(region >>> 1))) & TVectorInt64.One, TVectorInt64.Zero)),
                    -result,    // negative in region 1 or 3, positive in region 0 or 2
                    +result     // negative in region 0 or 2, positive in region 1 or 3
                );
            }

            static TVectorSingle ScalarFallback(TVectorSingle x)
            {
                TVectorSingle result = TVectorSingle.Zero;

                for (int i = 0; i < TVectorSingle.ElementCount; i++)
                {
                    float scalar = float.Sin(x[i]);
                    result = result.WithElement(i, scalar);
                }

                return result;
            }
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
        private static TVectorDouble CosDoubleLarge<TVectorDouble>(TVectorDouble r, TVectorDouble rr)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            TVectorDouble r2 = r * r;
            TVectorDouble r4 = r2 * r2;

            TVectorDouble s = r2 * 0.5;
            TVectorDouble t = s - TVectorDouble.One;

            return TVectorDouble.MultiplyAddEstimate(
                CosDoublePoly(r),
                r4,
                TVectorDouble.MultiplyAddEstimate(r, rr, ((TVectorDouble.One + t) - s))
            ) - t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble CosDoublePoly<TVectorDouble>(TVectorDouble r)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            const double C1 = +0.041666666666666664;
            const double C2 = -0.0013888888888887398;
            const double C3 = +2.4801587298767044E-05;
            const double C4 = -2.755731727234489E-07;
            const double C5 = +2.0876146382372144E-09;
            const double C6 = -1.138263981623609E-11;

            TVectorDouble r2 = r * r;
            TVectorDouble r4 = r2 * r2;
            TVectorDouble r8 = r4 * r4;

            return TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C6), r2, TVectorDouble.Create(C5)),
                r8,
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C4), r2, TVectorDouble.Create(C3)),
                    r4,
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C2), r2, TVectorDouble.Create(C1))
                )
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble CosSingleLarge<TVectorDouble>(TVectorDouble r)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            TVectorDouble r2 = r * r;
            TVectorDouble r4 = r2 * r2;

            return TVectorDouble.MultiplyAddEstimate(
                CosSinglePoly(r),
                r4,
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(-0.5), r2, TVectorDouble.One)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble CosSinglePoly<TVectorDouble>(TVectorDouble r)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            const double C1 = +0.041666666666666664;
            const double C2 = -0.0013888888888887398;
            const double C3 = +2.4801587298767044E-05;
            const double C4 = -2.755731727234489E-07;

            TVectorDouble r2 = r * r;
            TVectorDouble r4 = r2 * r2;

            return TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C4), r2, TVectorDouble.Create(C3)),
                r4,
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C2), r2, TVectorDouble.Create(C1))
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble CosSingleSmall<TVectorDouble>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            TVectorDouble x2 = x * x;
            TVectorDouble x4 = x2 * x2;

            TVectorDouble r = x2 * 0.5;
            TVectorDouble t = TVectorDouble.One - r;
            TVectorDouble s = t + (TVectorDouble.One - t - r);

            return TVectorDouble.MultiplyAddEstimate(CosSinglePoly(x), x4, s);
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
        private static TVectorSingle Narrow<TVectorDouble, TVectorSingle>(TVectorDouble vector)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
        {
            Unsafe.SkipInit(out TVectorSingle result);

            if (typeof(TVectorDouble) == typeof(Vector128<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector64<float>));

                if (AdvSimd.Arm64.IsSupported)
                {
                    result = (TVectorSingle)(object)AdvSimd.Arm64.ConvertToSingleLower((Vector128<double>)(object)vector);
                }
                else if (PackedSimd.IsSupported)
                {
                    result = (TVectorSingle)(object)PackedSimd.ConvertToSingle((Vector128<double>)(object)vector).GetLower();
                }
                else
                {
                    Vector128<double> value = (Vector128<double>)(object)vector;
                    result = (TVectorSingle)(object)Vector64.Narrow(value.GetLower(), value.GetUpper());
                }
            }
            else if (typeof(TVectorDouble) == typeof(Vector256<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector128<float>));

                if (Avx.IsSupported)
                {
                    result = (TVectorSingle)(object)Avx.ConvertToVector128Single((Vector256<double>)(object)vector);
                }
                else
                {
                    Vector256<double> value = (Vector256<double>)(object)vector;
                    result = (TVectorSingle)(object)Vector128.Narrow(value.GetLower(), value.GetUpper());
                }
            }
            else if (typeof(TVectorDouble) == typeof(Vector512<double>))
            {
                Debug.Assert(typeof(TVectorSingle) == typeof(Vector256<float>));

                if (Avx512F.IsSupported)
                {
                    result = (TVectorSingle)(object)Avx512F.ConvertToVector256Single((Vector512<double>)(object)vector);
                }
                else
                {
                    Vector512<double> value = (Vector512<double>)(object)vector;
                    result = (TVectorSingle)(object)Vector256.Narrow(value.GetLower(), value.GetUpper());
                }
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
        private static TVectorUInt32 ShiftLeftUInt32<TVectorUInt32>(TVectorUInt32 vector, TVectorUInt32 shiftAmount)
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
        {
            Unsafe.SkipInit(out TVectorUInt32 result);

            if (typeof(TVectorUInt32) == typeof(Vector<uint>))
            {
                result = (TVectorUInt32)(object)Vector.ShiftLeft(
                    (Vector<uint>)(object)vector,
                    (Vector<uint>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt32) == typeof(Vector64<uint>))
            {
                result = (TVectorUInt32)(object)Vector64.ShiftLeft(
                    (Vector64<uint>)(object)vector,
                    (Vector64<uint>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt32) == typeof(Vector128<uint>))
            {
                result = (TVectorUInt32)(object)Vector128.ShiftLeft(
                    (Vector128<uint>)(object)vector,
                    (Vector128<uint>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt32) == typeof(Vector256<uint>))
            {
                result = (TVectorUInt32)(object)Vector256.ShiftLeft(
                    (Vector256<uint>)(object)vector,
                    (Vector256<uint>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt32) == typeof(Vector512<uint>))
            {
                result = (TVectorUInt32)(object)Vector512.ShiftLeft(
                    (Vector512<uint>)(object)vector,
                    (Vector512<uint>)(object)shiftAmount
                );
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorUInt64 ShiftLeftUInt64<TVectorUInt64>(TVectorUInt64 vector, TVectorUInt64 shiftAmount)
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            Unsafe.SkipInit(out TVectorUInt64 result);

            if (typeof(TVectorUInt64) == typeof(Vector<ulong>))
            {
                result = (TVectorUInt64)(object)Vector.ShiftLeft(
                    (Vector<ulong>)(object)vector,
                    (Vector<ulong>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt64) == typeof(Vector64<ulong>))
            {
                result = (TVectorUInt64)(object)Vector64.ShiftLeft(
                    (Vector64<ulong>)(object)vector,
                    (Vector64<ulong>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt64) == typeof(Vector128<ulong>))
            {
                result = (TVectorUInt64)(object)Vector128.ShiftLeft(
                    (Vector128<ulong>)(object)vector,
                    (Vector128<ulong>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt64) == typeof(Vector256<ulong>))
            {
                result = (TVectorUInt64)(object)Vector256.ShiftLeft(
                    (Vector256<ulong>)(object)vector,
                    (Vector256<ulong>)(object)shiftAmount
                );
            }
            else if (typeof(TVectorUInt64) == typeof(Vector512<ulong>))
            {
                result = (TVectorUInt64)(object)Vector512.ShiftLeft(
                    (Vector512<ulong>)(object)vector,
                    (Vector512<ulong>)(object)shiftAmount
                );
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble SinDoubleLarge<TVectorDouble>(TVectorDouble r, TVectorDouble rr)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            const double S1 = -0.16666666666666666;
            const double S2 = +0.00833333333333095;
            const double S3 = -0.00019841269836761127;
            const double S4 = +2.7557316103728802E-06;
            const double S5 = -2.5051132068021698E-08;
            const double S6 = +1.5918144304485914E-10;

            TVectorDouble r2 = r * r;
            TVectorDouble r3 = r2 * r;
            TVectorDouble r4 = r2 * r2;
            TVectorDouble r8 = r4 * r4;

            TVectorDouble sinPoly = TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.Create(S6),
                r8,
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S5), r2, TVectorDouble.Create(S4)),
                    r4,
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S3), r2, TVectorDouble.Create(S2))
                )
            );

            return r - TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.Create(-S1),
                r3,
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(rr, TVectorDouble.Create(0.5), -(r3 * sinPoly)),
                    r2,
                    -rr
                )
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble SinDoublePoly<TVectorDouble>(TVectorDouble r)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            const double S1 = -0.16666666666666666;
            const double S2 = +0.00833333333333095;
            const double S3 = -0.00019841269836761127;
            const double S4 = +2.7557316103728802E-06;
            const double S5 = -2.5051132068021698E-08;
            const double S6 = +1.5918144304485914E-10;

            TVectorDouble r2 = r * r;
            TVectorDouble r3 = r2 * r;
            TVectorDouble r4 = r2 * r2;
            TVectorDouble r8 = r4 * r4;

            TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S6), r2, TVectorDouble.Create(S5)),
                r8,
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S4), r2, TVectorDouble.Create(S3)),
                    r4,
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S2), r2, TVectorDouble.Create(S1)))
            );

            return TVectorDouble.MultiplyAddEstimate(poly, r3, r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble SinSinglePoly<TVectorDouble>(TVectorDouble r)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            const double S1 = -0.16666666666666666;
            const double S2 = +0.00833333333333095;
            const double S3 = -0.00019841269836761127;
            const double S4 = +2.7557316103728802E-06;

            TVectorDouble r2 = r * r;
            TVectorDouble r3 = r2 * r;
            TVectorDouble r4 = r2 * r2;

            return TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S4), r2, TVectorDouble.Create(S3)),
                    r4,
                    TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(S2), r2, TVectorDouble.Create(S1))),
                r3,
                r
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (TVectorDouble r, TVectorDouble rr, TVectorInt64 region) SinCosReduce<TVectorDouble, TVectorInt64>(TVectorDouble ax)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // reduce  the argument to be in a range from (-pi / 4) to (+pi / 4) by subtracting multiples of (pi / 2)

            const double V_ALM_SHIFT = 6755399441055744.0;
            const double V_TWO_BY_PI = 0.6366197723675814;

            const double V_PI_BY_TWO_1 = 1.5707963267341256;
            const double V_PI_BY_TWO_2 = 6.077100506303966E-11;
            const double V_PI_BY_TWO_2_TAIL = 2.0222662487959506E-21;

            // dn = (int)(|x| * 2 / pi)
            TVectorDouble npi2 = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(V_TWO_BY_PI), ax, TVectorDouble.Create(V_ALM_SHIFT));
            TVectorInt64 region = Unsafe.BitCast<TVectorDouble, TVectorInt64>(npi2);
            npi2 -= TVectorDouble.Create(V_ALM_SHIFT);

            TVectorDouble rhead = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(-V_PI_BY_TWO_1), npi2, ax);
            TVectorDouble rtail = npi2 * V_PI_BY_TWO_2;
            TVectorDouble r = rhead - rtail;

            rtail = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(V_PI_BY_TWO_2_TAIL), npi2, -(rhead - r - rtail));
            rhead = r;
            r -= rtail;

            return (r, (rhead - r) - rtail, region);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble Widen<TVectorSingle, TVectorDouble>(TVectorSingle vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            Unsafe.SkipInit(out TVectorDouble result);

            if (typeof(TVectorSingle) == typeof(Vector64<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector128<double>));

                if (AdvSimd.Arm64.IsSupported)
                {
                    result = (TVectorDouble)(object)AdvSimd.Arm64.ConvertToDouble((Vector64<float>)(object)vector);
                }
                else if (PackedSimd.IsSupported)
                {
                    Vector128<float> value = Vector128.Create((Vector64<float>)(object)vector, (Vector64<float>)(object)vector);
                    result = (TVectorDouble)(object)PackedSimd.ConvertToDoubleLower(value);
                }
                else
                {
                    Vector64<float> value = (Vector64<float>)(object)vector;

                    Vector64<double> lower = Vector64.WidenLower(value);
                    Vector64<double> upper = Vector64.WidenUpper(value);

                    result = (TVectorDouble)(object)Vector128.Create(lower, upper);
                }
            }
            else if (typeof(TVectorSingle) == typeof(Vector128<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector256<double>));

                if (Avx.IsSupported)
                {
                    result = (TVectorDouble)(object)Avx.ConvertToVector256Double((Vector128<float>)(object)vector);
                }
                else
                {
                    Vector128<float> value = (Vector128<float>)(object)vector;

                    Vector128<double> lower = Vector128.WidenLower(value);
                    Vector128<double> upper = Vector128.WidenUpper(value);

                    result = (TVectorDouble)(object)Vector256.Create(lower, upper);
                }
            }
            else if (typeof(TVectorSingle) == typeof(Vector256<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector512<double>));

                if (Avx512F.IsSupported)
                {
                    result = (TVectorDouble)(object)Avx512F.ConvertToVector512Double((Vector256<float>)(object)vector);
                }
                else
                {
                    Vector256<float> value = (Vector256<float>)(object)vector;

                    Vector256<double> lower = Vector256.WidenLower(value);
                    Vector256<double> upper = Vector256.WidenUpper(value);

                    result = (TVectorDouble)(object)Vector512.Create(lower, upper);
                }
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble WidenLower<TVectorSingle, TVectorDouble>(TVectorSingle vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            Unsafe.SkipInit(out TVectorDouble result);

            if (typeof(TVectorSingle) == typeof(Vector<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector<double>));
                result = (TVectorDouble)(object)Vector.WidenLower((Vector<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector64<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector64<double>));
                result = (TVectorDouble)(object)Vector64.WidenLower((Vector64<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector128<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector128<double>));
                result = (TVectorDouble)(object)Vector128.WidenLower((Vector128<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector256<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector256<double>));
                result = (TVectorDouble)(object)Vector256.WidenLower((Vector256<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector512<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector512<double>));
                result = (TVectorDouble)(object)Vector512.WidenLower((Vector512<float>)(object)vector);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVectorDouble WidenUpper<TVectorSingle, TVectorDouble>(TVectorSingle vector)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            Unsafe.SkipInit(out TVectorDouble result);

            if (typeof(TVectorSingle) == typeof(Vector<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector<double>));
                result = (TVectorDouble)(object)Vector.WidenUpper((Vector<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector64<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector64<double>));
                result = (TVectorDouble)(object)Vector64.WidenUpper((Vector64<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector128<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector128<double>));
                result = (TVectorDouble)(object)Vector128.WidenUpper((Vector128<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector256<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector256<double>));
                result = (TVectorDouble)(object)Vector256.WidenUpper((Vector256<float>)(object)vector);
            }
            else if (typeof(TVectorSingle) == typeof(Vector512<float>))
            {
                Debug.Assert(typeof(TVectorDouble) == typeof(Vector512<double>));
                result = (TVectorDouble)(object)Vector512.WidenUpper((Vector512<float>)(object)vector);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        public static TVectorDouble AsinDouble<TVectorDouble>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `vrs4_asinf` and `asinf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // The input domain should be in the [-1, +1] else a domain error is displayed
            //
            // asin(-x) = -asin(x)
            // asin(x) = pi/2-2*asin(sqrt(1/2*(1-x)))  when x > 1/2
            //
            // y = abs(x)
            // asin(y) = asin(g)  when y <= 0.5,  where g = y*y
            //         = pi/2-asin(g)  when y > 0.5, where g = 1/2*(1-y), y = -2*sqrt(g)
            // The term asin(f) is approximated by using a polynomial where the inputs lie in the interval [0 1/2]

            const double HALF = 0.5;
            const double PI_BY_TWO = 1.5707963267948966;

            // Polynomial coefficients from AMD aocl-libm-ose
            const double C1 = 0.166666666666664;      // 0x1.55555555552aap-3
            const double C2 = 0.0750000000006397;     // 0x1.333333337cbaep-4
            const double C3 = 0.0446428571088065;     // 0x1.6db6db3c0984p-5
            const double C4 = 0.0303819469180048;     // 0x1.f1c72dd86cbafp-6
            const double C5 = 0.0223717830326408;     // 0x1.6e89d3ff33aa4p-6
            const double C6 = 0.0173549783672646;     // 0x1.1c6d83ae664b6p-6
            const double C7 = 0.0138887093438824;     // 0x1.c6e1568b90518p-7
            const double C8 = 0.0121483872130308;     // 0x1.8f6a58977fe49p-7
            const double C9 = 0.00640855516049134;    // 0x1.a6ab10b3321bp-8

            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble ax = TVectorDouble.Abs(x);

            TVectorDouble result;
            TVectorDouble g;
            TVectorDouble r;
            TVectorDouble poly;
            TVectorDouble n;

            TVectorDouble cmp = TVectorDouble.Create(HALF);
            TVectorDouble needsTransform = TVectorDouble.GreaterThan(ax, cmp);

            // For |x| > 0.5: g = 0.5*(1.0-|x|), r = -2.0*sqrt(g)
            TVectorDouble g_hi = TVectorDouble.Create(HALF) * (TVectorDouble.One - ax);
            TVectorDouble r_hi = TVectorDouble.Create(-2.0) * TVectorDouble.Sqrt(g_hi);
            TVectorDouble n_hi = TVectorDouble.Create(PI_BY_TWO);

            // For |x| <= 0.5: g = |x|*|x|, r = |x|
            TVectorDouble g_lo = ax * ax;
            TVectorDouble r_lo = ax;
            TVectorDouble n_lo = TVectorDouble.Zero;

            g = TVectorDouble.ConditionalSelect(needsTransform, g_hi, g_lo);
            r = TVectorDouble.ConditionalSelect(needsTransform, r_hi, r_lo);
            n = TVectorDouble.ConditionalSelect(needsTransform, n_hi, n_lo);

            // Polynomial evaluation: poly = g * (C1 + g*(C2 + g*(C3 + ... + g*C9)))
            poly = TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(
                                TVectorDouble.MultiplyAddEstimate(
                                    TVectorDouble.MultiplyAddEstimate(
                                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C9), g, TVectorDouble.Create(C8)),
                                        g, TVectorDouble.Create(C7)),
                                    g, TVectorDouble.Create(C6)),
                                g, TVectorDouble.Create(C5)),
                            g, TVectorDouble.Create(C4)),
                        g, TVectorDouble.Create(C3)),
                    g, TVectorDouble.Create(C2)),
                g, TVectorDouble.Create(C1)
            );

            result = TVectorDouble.MultiplyAddEstimate(poly * g, r, r);
            result += n;

            // Restore sign
            result |= sign;

            return result;
        }

        public static TVectorSingle AsinSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `vrs4_asinf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // The input domain should be in the [-1, +1] else a domain error is displayed
            //
            // asin(-x) = -asin(x)
            // asin(x) = pi/2-2*asin(sqrt(1/2*(1-x)))  when x > 1/2
            //
            // y = abs(x)
            // asin(y) = asin(g)  when y <= 0.5,  where g = y*y
            //         = pi/2-asin(g)  when y > 0.5, where g = 1/2*(1-y), y = -2*sqrt(g)
            // The term asin(f) is approximated by using a polynomial

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(AsinDouble<TVectorDouble>(dx));
            }
            else
            {
                TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(
                    AsinDouble<TVectorDouble>(dxLo),
                    AsinDouble<TVectorDouble>(dxHi)
                );
            }
        }

        public static TVectorDouble AcosDouble<TVectorDouble>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `vrs4_acosf` and `acosf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // The input domain should be in the [-1, +1] else a domain error is displayed
            //
            // acos(x) = pi/2 - asin(x)             when |x| <= 0.5
            // acos(x) = 2*asin(sqrt((1-x)/2))      when x > 0.5
            // acos(x) = pi - 2*asin(sqrt((1+x)/2)) when x < -0.5

            const double HALF = 0.5;
            const double PI_BY_TWO = 1.5707963267948966;
            const double PI = 3.1415926535897932;

            // Polynomial coefficients from AMD aocl-libm-ose (same as asin)
            const double C1 = 0.166666666666664;      // 0x1.55555555552aap-3
            const double C2 = 0.0750000000006397;     // 0x1.333333337cbaep-4
            const double C3 = 0.0446428571088065;     // 0x1.6db6db3c0984p-5
            const double C4 = 0.0303819469180048;     // 0x1.f1c72dd86cbafp-6
            const double C5 = 0.0223717830326408;     // 0x1.6e89d3ff33aa4p-6
            const double C6 = 0.0173549783672646;     // 0x1.1c6d83ae664b6p-6
            const double C7 = 0.0138887093438824;     // 0x1.c6e1568b90518p-7
            const double C8 = 0.0121483872130308;     // 0x1.8f6a58977fe49p-7
            const double C9 = 0.00640855516049134;    // 0x1.a6ab10b3321bp-8

            TVectorDouble result;
            TVectorDouble g;
            TVectorDouble r;
            TVectorDouble poly;

            TVectorDouble cmp = TVectorDouble.Create(HALF);
            TVectorDouble cmpNeg = TVectorDouble.Create(-HALF);

            TVectorDouble needsPositiveTransform = TVectorDouble.GreaterThan(x, cmp);
            TVectorDouble needsNegativeTransform = TVectorDouble.LessThan(x, cmpNeg);

            // For x > 0.5: g = (1-x)/2, r = sqrt(g), result = 2*asin(r)
            TVectorDouble g_pos = (TVectorDouble.One - x) * TVectorDouble.Create(HALF);
            TVectorDouble r_pos = TVectorDouble.Sqrt(g_pos);

            // For x < -0.5: g = (1+x)/2, r = sqrt(g), result = pi - 2*asin(r)
            TVectorDouble g_neg = (TVectorDouble.One + x) * TVectorDouble.Create(HALF);
            TVectorDouble r_neg = TVectorDouble.Sqrt(g_neg);

            // For |x| <= 0.5: compute asin(x) and then pi/2 - asin(x)
            TVectorDouble ax = TVectorDouble.Abs(x);
            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble g_mid = ax * ax;
            TVectorDouble r_mid = ax;

            // Select appropriate g and r based on x
            g = TVectorDouble.ConditionalSelect(needsPositiveTransform, g_pos,
                TVectorDouble.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
            r = TVectorDouble.ConditionalSelect(needsPositiveTransform, r_pos,
                TVectorDouble.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

            // Polynomial evaluation: poly = g * (C1 + g*(C2 + g*(C3 + ... + g*C9)))
            poly = TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(
                                TVectorDouble.MultiplyAddEstimate(
                                    TVectorDouble.MultiplyAddEstimate(
                                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C9), g, TVectorDouble.Create(C8)),
                                        g, TVectorDouble.Create(C7)),
                                    g, TVectorDouble.Create(C6)),
                                g, TVectorDouble.Create(C5)),
                            g, TVectorDouble.Create(C4)),
                        g, TVectorDouble.Create(C3)),
                    g, TVectorDouble.Create(C2)),
                g, TVectorDouble.Create(C1)
            );

            // Compute asin approximation
            TVectorDouble asinResult = TVectorDouble.MultiplyAddEstimate(poly * g, r, r);

            // Apply final transformations based on range
            result = TVectorDouble.ConditionalSelect(needsPositiveTransform,
                TVectorDouble.Create(2.0) * asinResult,
                TVectorDouble.ConditionalSelect(needsNegativeTransform,
                    TVectorDouble.Create(PI) - TVectorDouble.Create(2.0) * asinResult,
                    TVectorDouble.Create(PI_BY_TWO) - (asinResult | sign)));

            return result;
        }

        public static TVectorSingle AcosSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `vrs4_acosf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // The input domain should be in the [-1, +1] else a domain error is displayed
            //
            // acos(x) = pi/2 - asin(x)             when |x| <= 0.5
            // acos(x) = 2*asin(sqrt((1-x)/2))      when x > 0.5
            // acos(x) = pi - 2*asin(sqrt((1+x)/2)) when x < -0.5

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(AcosDouble<TVectorDouble>(dx));
            }
            else
            {
                TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(
                    AcosDouble<TVectorDouble>(dxLo),
                    AcosDouble<TVectorDouble>(dxHi)
                );
            }
        }

        public static TVectorDouble AtanDouble<TVectorDouble>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `vrd2_atan` from amd/aocl-libm-ose
            // Copyright (C) 2008-2023 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // sign = sign(x)
            // x = abs(x)
            //
            // Argument Reduction for every x into the interval [-(2-sqrt(3)),+(2-sqrt(3))]
            // Use the following identities
            // atan(x) = pi/2 - atan(1/x)                when x > 1
            //         = pi/6 + atan(f)                  when f > (2-sqrt(3))
            // where f = (sqrt(3)*x-1)/(x+sqrt(3))
            //
            // All elements are approximated by using polynomial of degree 19

            const double SQRT3 = 1.7320508075688772;      // 0x1.bb67ae8584caap0
            const double RANGE = 0.2679491924311227;      // 0x1.126145e9ecd56p-2 (2-sqrt(3))
            const double PI_BY_6 = 0.5235987755982989;    // 0x1.0c152382d7366p-1
            const double PI_BY_2 = 1.5707963267948966;    // 0x1.921fb54442d18p0
            const double PI_BY_3 = 1.0471975511965979;    // 0x1.0c152382d7366p0

            // Polynomial coefficients
            const double C0 = -0.33333333333333265;       // -0x1.5555555555549p-2
            const double C1 = 0.19999999999969587;        // 0x1.9999999996eccp-3
            const double C2 = -0.1428571428026078;        // -0x1.24924922b2972p-3
            const double C3 = 0.11111110613838616;        // 0x1.c71c707163579p-4
            const double C4 = -0.09090882990193784;       // -0x1.745cd1358b0f1p-4
            const double C5 = 0.07691470703415716;        // 0x1.3b0aea74b0a51p-4
            const double C6 = -0.06649949387937557;       // -0x1.1061c5f6997a6p-4
            const double C7 = 0.05677994137264123;        // 0x1.d1242ae875135p-5
            const double C8 = -0.038358962102069113;      // -0x1.3a3c92f7949aep-5

            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble ax = TVectorDouble.Abs(x);

            const double F = 1.0 / RANGE;

            // Argument reduction
            TVectorDouble cmp1 = TVectorDouble.GreaterThanOrEqual(ax, TVectorDouble.Create(F));
            TVectorDouble cmp2 = TVectorDouble.GreaterThan(ax, TVectorDouble.One);
            TVectorDouble cmp3 = TVectorDouble.GreaterThan(ax, TVectorDouble.Create(RANGE));

            // If ax >= F: aux = 1/ax, pival = pi/2, polysign = negative
            TVectorDouble aux1 = TVectorDouble.One / ax;
            TVectorDouble pival1 = TVectorDouble.Create(PI_BY_2);

            // If ax > 1 (but < F): aux = (1/ax * sqrt(3) - 1) / (sqrt(3) + 1/ax), pival = pi/3, polysign = negative
            TVectorDouble recip = TVectorDouble.One / ax;
            TVectorDouble aux2 = (recip * TVectorDouble.Create(SQRT3) - TVectorDouble.One) / (TVectorDouble.Create(SQRT3) + recip);
            TVectorDouble pival2 = TVectorDouble.Create(PI_BY_3);

            // If ax > range (but <= 1): aux = (ax * sqrt(3) - 1) / (sqrt(3) + ax), pival = pi/6, polysign = positive
            TVectorDouble aux3 = (ax * TVectorDouble.Create(SQRT3) - TVectorDouble.One) / (TVectorDouble.Create(SQRT3) + ax);
            TVectorDouble pival3 = TVectorDouble.Create(PI_BY_6);

            // Default: aux = ax, pival = 0, polysign = positive
            TVectorDouble aux4 = ax;
            TVectorDouble pival4 = TVectorDouble.Zero;

            // Select based on conditions
            TVectorDouble aux = TVectorDouble.ConditionalSelect(cmp1, aux1,
                                TVectorDouble.ConditionalSelect(cmp2, aux2,
                                TVectorDouble.ConditionalSelect(cmp3, aux3, aux4)));

            TVectorDouble pival = TVectorDouble.ConditionalSelect(cmp1, pival1,
                                  TVectorDouble.ConditionalSelect(cmp2, pival2,
                                  TVectorDouble.ConditionalSelect(cmp3, pival3, pival4)));

            // polysign is negative when cmp1 or cmp2 is true
            TVectorDouble polysignMask = TVectorDouble.ConditionalSelect(cmp1 | cmp2, TVectorDouble.Create(-0.0), TVectorDouble.Zero);

            // Polynomial evaluation: poly = aux + C0*aux^3 + C1*aux^5 + C2*aux^7 + ...
            TVectorDouble aux2_poly = aux * aux;
            TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(
                                TVectorDouble.MultiplyAddEstimate(
                                    TVectorDouble.MultiplyAddEstimate(
                                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C8), aux2_poly, TVectorDouble.Create(C7)),
                                        aux2_poly, TVectorDouble.Create(C6)),
                                    aux2_poly, TVectorDouble.Create(C5)),
                                aux2_poly, TVectorDouble.Create(C4)),
                            aux2_poly, TVectorDouble.Create(C3)),
                        aux2_poly, TVectorDouble.Create(C2)),
                    aux2_poly, TVectorDouble.Create(C1)),
                aux2_poly, TVectorDouble.Create(C0)
            );

            poly = TVectorDouble.MultiplyAddEstimate(poly * aux2_poly, aux, aux);

            // Apply polysign
            poly ^= polysignMask;

            // result = pival + poly
            TVectorDouble result = pival + poly;

            // Restore original sign
            result |= sign;

            return result;
        }

        public static TVectorSingle AtanSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `vrs4_atanf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // sign = sign(x)
            // x = abs(x)
            //
            // Argument reduction: Use the following identities
            //
            // 1. If xi > 1,
            //      atan(xi) = pi/2 - atan(1/xi)
            //
            // 2. If f > (2-sqrt(3)),
            //      atan(x) = pi/6 + atan(f)
            //      where f = (sqrt(3)*xi-1)/(xi+sqrt(3))
            //
            //      atan(xi) is calculated using the polynomial,
            //      xi + C0*xi^3 + C1*xi^5 + C2*xi^7

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(AtanDouble<TVectorDouble>(dx));
            }
            else
            {
                TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(
                    AtanDouble<TVectorDouble>(dxLo),
                    AtanDouble<TVectorDouble>(dxHi)
                );
            }
        }

        public static TVectorDouble Atan2Double<TVectorDouble>(TVectorDouble y, TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `vrd2_atan2` from amd/aocl-libm-ose
            // Copyright (C) 2008-2023 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // atan2(y, x) has different definitions depending on the quadrant:
            // - If x > 0: atan2(y, x) = atan(y/x)
            // - If x < 0 and y >= 0: atan2(y, x) = atan(y/x) + pi
            // - If x < 0 and y < 0: atan2(y, x) = atan(y/x) - pi
            // - If x = 0 and y > 0: atan2(y, x) = pi/2
            // - If x = 0 and y < 0: atan2(y, x) = -pi/2
            //
            // Special cases:
            // - atan2(±0, +x) = ±0
            // - atan2(±0, -x) = ±pi
            // - atan2(±y, +0) = ±pi/2
            // - atan2(±y, -0) = ±pi/2
            // - atan2(±∞, +∞) = ±pi/4
            // - atan2(±∞, -∞) = ±3pi/4
            // - atan2(±y, +∞) = ±0
            // - atan2(±y, -∞) = ±pi

            const double PI = 3.141592653589793;           // 0x1.921fb54442d18p1
            const double PI_BY_2 = 1.5707963267948966;     // 0x1.921fb54442d18p0

            const double SQRT3 = 1.7320508075688772;       // 0x1.bb67ae8584caap0
            const double RANGE = 0.2679491924311227;       // 0x1.126145e9ecd56p-2 (2-sqrt(3))
            const double PI_BY_6 = 0.5235987755982989;     // 0x1.0c152382d7366p-1
            const double PI_BY_3 = 1.0471975511965979;     // 0x1.0c152382d7366p0

            // Polynomial coefficients
            const double C0 = -0.33333333333333265;        // -0x1.5555555555549p-2
            const double C1 = 0.19999999999969587;         // 0x1.9999999996eccp-3
            const double C2 = -0.1428571428026078;         // -0x1.24924922b2972p-3
            const double C3 = 0.11111110613838616;         // 0x1.c71c707163579p-4
            const double C4 = -0.09090882990193784;        // -0x1.745cd1358b0f1p-4
            const double C5 = 0.07691470703415716;         // 0x1.3b0aea74b0a51p-4
            const double C6 = -0.06649949387937557;        // -0x1.1061c5f6997a6p-4
            const double C7 = 0.05677994137264123;         // 0x1.d1242ae875135p-5
            const double C8 = -0.038358962102069113;       // -0x1.3a3c92f7949aep-5

            // Extract signs of y
            TVectorDouble signY = y & TVectorDouble.Create(-0.0);

            TVectorDouble ay = TVectorDouble.Abs(y);
            TVectorDouble ax = TVectorDouble.Abs(x);

            // Compute u = ay/ax (division by zero handled naturally)
            TVectorDouble u = ay / ax;

            const double F = 1.0 / RANGE;

            // Argument reduction for atan(u), same as in Atan
            TVectorDouble cmp1 = TVectorDouble.GreaterThanOrEqual(u, TVectorDouble.Create(F));
            TVectorDouble cmp2 = TVectorDouble.GreaterThan(u, TVectorDouble.One);
            TVectorDouble cmp3 = TVectorDouble.GreaterThan(u, TVectorDouble.Create(RANGE));

            TVectorDouble aux1 = TVectorDouble.One / u;
            TVectorDouble pival1 = TVectorDouble.Create(PI_BY_2);

            TVectorDouble aux2 = (aux1 * TVectorDouble.Create(SQRT3) - TVectorDouble.One) / (TVectorDouble.Create(SQRT3) + aux1);
            TVectorDouble pival2 = TVectorDouble.Create(PI_BY_3);

            TVectorDouble aux3 = (u * TVectorDouble.Create(SQRT3) - TVectorDouble.One) / (TVectorDouble.Create(SQRT3) + u);
            TVectorDouble pival3 = TVectorDouble.Create(PI_BY_6);

            TVectorDouble aux4 = u;
            TVectorDouble pival4 = TVectorDouble.Zero;

            TVectorDouble aux = TVectorDouble.ConditionalSelect(cmp1, aux1,
                                TVectorDouble.ConditionalSelect(cmp2, aux2,
                                TVectorDouble.ConditionalSelect(cmp3, aux3, aux4)));

            TVectorDouble pival = TVectorDouble.ConditionalSelect(cmp1, pival1,
                                  TVectorDouble.ConditionalSelect(cmp2, pival2,
                                  TVectorDouble.ConditionalSelect(cmp3, pival3, pival4)));

            TVectorDouble polysignMask = TVectorDouble.ConditionalSelect(cmp1 | cmp2, TVectorDouble.Create(-0.0), TVectorDouble.Zero);

            // Polynomial evaluation
            TVectorDouble aux2_poly = aux * aux;
            TVectorDouble poly = TVectorDouble.MultiplyAddEstimate(
                TVectorDouble.MultiplyAddEstimate(
                    TVectorDouble.MultiplyAddEstimate(
                        TVectorDouble.MultiplyAddEstimate(
                            TVectorDouble.MultiplyAddEstimate(
                                TVectorDouble.MultiplyAddEstimate(
                                    TVectorDouble.MultiplyAddEstimate(
                                        TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(C8), aux2_poly, TVectorDouble.Create(C7)),
                                        aux2_poly, TVectorDouble.Create(C6)),
                                    aux2_poly, TVectorDouble.Create(C5)),
                                aux2_poly, TVectorDouble.Create(C4)),
                            aux2_poly, TVectorDouble.Create(C3)),
                        aux2_poly, TVectorDouble.Create(C2)),
                    aux2_poly, TVectorDouble.Create(C1)),
                aux2_poly, TVectorDouble.Create(C0)
            );

            poly = TVectorDouble.MultiplyAddEstimate(poly * aux2_poly, aux, aux);
            poly ^= polysignMask;

            TVectorDouble atanU = pival + poly;

            // Adjust for quadrant
            // If x < 0, add or subtract pi based on sign of y
            TVectorDouble piAdjust = TVectorDouble.ConditionalSelect(
                TVectorDouble.GreaterThanOrEqual(y, TVectorDouble.Zero),
                TVectorDouble.Create(PI),
                TVectorDouble.Create(-PI)
            );

            TVectorDouble result = TVectorDouble.ConditionalSelect(
                TVectorDouble.LessThan(x, TVectorDouble.Zero),
                atanU + piAdjust,
                atanU
            );

            // Apply sign of y to result
            result |= signY;

            return result;
        }

        public static TVectorSingle Atan2Single<TVectorSingle, TVectorDouble>(TVectorSingle y, TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `vrs4_atan2f` from amd/aocl-libm-ose
            // Copyright (C) 2008-2023 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // Same as Atan2Double but using single precision

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dy = Widen<TVectorSingle, TVectorDouble>(y);
                TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(Atan2Double<TVectorDouble>(dy, dx));
            }
            else
            {
                TVectorDouble dyLo = WidenLower<TVectorSingle, TVectorDouble>(y);
                TVectorDouble dyHi = WidenUpper<TVectorSingle, TVectorDouble>(y);
                TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(
                    Atan2Double<TVectorDouble>(dyLo, dxLo),
                    Atan2Double<TVectorDouble>(dyHi, dxHi)
                );
            }
        }
    }
}
