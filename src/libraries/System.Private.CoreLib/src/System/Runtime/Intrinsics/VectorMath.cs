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

        public static TVectorDouble AsinDouble<TVectorDouble, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `asin` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // For abs(x) < 0.5 use arcsin(x) = x + x^3*R(x^2)
            // where R(x^2) is a rational minimax approximation to (arcsin(x) - x)/x^3.
            // For abs(x) >= 0.5 exploit the identity:
            // arcsin(x) = pi/2 - 2*arcsin(sqrt(1-x)/2)
            // together with the above rational approximation.

            // Rational polynomial coefficients for numerator (from Sollya via AMD)
            const double C1 = 0.227485835556935010735943483075;
            const double C2 = -0.445017216867635649900123110649;
            const double C3 = 0.275558175256937652532686256258;
            const double C4 = -0.0549989809235685841612020091328;
            const double C5 = 0.00109242697235074662306043804220;
            const double C6 = 0.0000482901920344786991880522822991;

            // Rational polynomial coefficients for denominator (from Sollya via AMD)
            const double D1 = 1.36491501334161032038194214209;
            const double D2 = -3.28431505720958658909889444194;
            const double D3 = 2.76568859157270989520376345954;
            const double D4 = -0.943639137032492685763471240072;
            const double D5 = 0.105869422087204370341222318533;

            // Constants for high-precision reconstruction
            const double PIBY2_TAIL = 6.1232339957367660e-17;   // 0x3c91a62633145c07
            const double HPIBY2_HEAD = 7.8539816339744831e-01;  // 0x3fe921fb54442d18
            const double PIBY2 = 1.5707963267948965e+00;        // 0x3ff921fb54442d18

            // Get sign and absolute value
            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble ax = TVectorDouble.Abs(x);

            // Check for transform region (|x| >= 0.5)
            TVectorDouble transformMask = TVectorDouble.GreaterThanOrEqual(ax, TVectorDouble.Create(0.5));

            // For |x| >= 0.5: r = 0.5 * (1.0 - ax), s = sqrt(r)
            TVectorDouble r_transform = TVectorDouble.Create(0.5) * (TVectorDouble.One - ax);
            TVectorDouble s = TVectorDouble.Sqrt(r_transform);

            // For |x| < 0.5: r = ax * ax
            TVectorDouble r_normal = ax * ax;

            // Select r based on transform
            TVectorDouble r = TVectorDouble.ConditionalSelect(transformMask, r_transform, r_normal);

            // Evaluate numerator polynomial: C1 + r*(C2 + r*(C3 + r*(C4 + r*(C5 + r*C6))))
            TVectorDouble poly_num = TVectorDouble.Create(C6);
            poly_num = TVectorDouble.MultiplyAddEstimate(poly_num, r, TVectorDouble.Create(C5));
            poly_num = TVectorDouble.MultiplyAddEstimate(poly_num, r, TVectorDouble.Create(C4));
            poly_num = TVectorDouble.MultiplyAddEstimate(poly_num, r, TVectorDouble.Create(C3));
            poly_num = TVectorDouble.MultiplyAddEstimate(poly_num, r, TVectorDouble.Create(C2));
            poly_num = TVectorDouble.MultiplyAddEstimate(poly_num, r, TVectorDouble.Create(C1));

            // Evaluate denominator polynomial: D1 + r*(D2 + r*(D3 + r*(D4 + r*D5)))
            TVectorDouble poly_deno = TVectorDouble.Create(D5);
            poly_deno = TVectorDouble.MultiplyAddEstimate(poly_deno, r, TVectorDouble.Create(D4));
            poly_deno = TVectorDouble.MultiplyAddEstimate(poly_deno, r, TVectorDouble.Create(D3));
            poly_deno = TVectorDouble.MultiplyAddEstimate(poly_deno, r, TVectorDouble.Create(D2));
            poly_deno = TVectorDouble.MultiplyAddEstimate(poly_deno, r, TVectorDouble.Create(D1));

            // u = r * poly_num / poly_deno
            TVectorDouble u = r * poly_num / poly_deno;

            // For transform region: reconstruct using high-low precision arithmetic
            // s1 = high part of s (clear low 32 bits)
            // c = (r - s1*s1) / (s + s1)
            // p = 2*s*u - (PIBY2_TAIL - 2*c)
            // q = HPIBY2_HEAD - 2*s1
            // v_transform = HPIBY2_HEAD - (p - q)
            TVectorDouble s1 = Unsafe.BitCast<TVectorUInt64, TVectorDouble>(Unsafe.BitCast<TVectorDouble, TVectorUInt64>(s) & TVectorUInt64.Create(0xFFFFFFFF00000000));
            TVectorDouble c = (r - s1 * s1) / (s + s1);
            TVectorDouble p = TVectorDouble.Create(2.0) * s * u - (TVectorDouble.Create(PIBY2_TAIL) - TVectorDouble.Create(2.0) * c);
            TVectorDouble q = TVectorDouble.Create(HPIBY2_HEAD) - TVectorDouble.Create(2.0) * s1;
            TVectorDouble v_transform = TVectorDouble.Create(HPIBY2_HEAD) - (p - q);

            // For normal region: v = ax + ax*u
            TVectorDouble v_normal = ax + ax * u;

            // Select result based on transform
            TVectorDouble v = TVectorDouble.ConditionalSelect(transformMask, v_transform, v_normal);

            // Toggle sign (XOR preserves sign inversion from original AMD AOCL)
            v ^= sign;

            // Handle x = ±1 exactly: asin(±1) = ±π/2
            TVectorDouble absXEqualsOne = TVectorDouble.Equals(TVectorDouble.Abs(x), TVectorDouble.One);
            v = TVectorDouble.ConditionalSelect(absXEqualsOne, TVectorDouble.Create(PIBY2) ^ sign, v);

            // Handle |x| > 1: returns NaN
            TVectorDouble absXGreaterThanOne = TVectorDouble.GreaterThan(TVectorDouble.Abs(x), TVectorDouble.One);
            v = TVectorDouble.ConditionalSelect(absXGreaterThanOne, TVectorDouble.Create(double.NaN), v);

            return v;
        }

        public static TVectorSingle AsinSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `asinf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorSingle sign = x & TVectorSingle.Create(-0.0f);
            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorSingle outOfRange = TVectorSingle.GreaterThan(ax, TVectorSingle.One);

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dax = Widen<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(AsinSingleCoreDouble<TVectorDouble>(dax));
            }
            else
            {
                TVectorDouble daxLo = WidenLower<TVectorSingle, TVectorDouble>(ax);
                TVectorDouble daxHi = WidenUpper<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AsinSingleCoreDouble<TVectorDouble>(daxLo),
                    AsinSingleCoreDouble<TVectorDouble>(daxHi));
            }

            result ^= sign;
            result = TVectorSingle.ConditionalSelect(outOfRange, TVectorSingle.Create(float.NaN), result);

            return result;
        }

        private static TVectorDouble AsinSingleCoreDouble<TVectorDouble>(TVectorDouble ax)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // Polynomial coefficients from Sollya (AMD aocl-libm-ose asinf.c)
            const double C1 = 0.1666666666666477;       // 0x1.55555555552aap-3
            const double C2 = 0.0750000000041797;       // 0x1.333333337cbaep-4
            const double C3 = 0.04464285678140856;      // 0x1.6db6db3c0984p-5
            const double C4 = 0.03038196065035564;      // 0x1.f1c72dd86cbafp-6
            const double C5 = 0.022371727970318958;     // 0x1.6e89d3ff33aa4p-6
            const double C6 = 0.01736009463784135;      // 0x1.1c6d83ae664b6p-6
            const double C7 = 0.013881842859634605;     // 0x1.c6e1568b90518p-7
            const double C8 = 0.012189191110336799;     // 0x1.8f6a58977fe49p-7
            const double C9 = 0.006449405266899452;     // 0x1.a6ab10b3321bp-8

            const double PIBY2 = 1.5707963267948966;    // 0x1.921fb54442d18p0

            TVectorDouble gtHalf = TVectorDouble.GreaterThanOrEqual(ax, TVectorDouble.Create(0.5));

            TVectorDouble g_hi = TVectorDouble.Create(0.5) * (TVectorDouble.One - ax);
            TVectorDouble y_hi = TVectorDouble.Create(-2.0) * TVectorDouble.Sqrt(g_hi);

            TVectorDouble g_lo = ax * ax;

            TVectorDouble g = TVectorDouble.ConditionalSelect(gtHalf, g_hi, g_lo);
            TVectorDouble y = TVectorDouble.ConditionalSelect(gtHalf, y_hi, ax);

            TVectorDouble poly = TVectorDouble.Create(C9);
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C8));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C7));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C6));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C5));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C4));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C3));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C2));
            poly = TVectorDouble.MultiplyAddEstimate(poly, g, TVectorDouble.Create(C1));

            TVectorDouble yPoly = y + y * g * poly;

            return TVectorDouble.ConditionalSelect(gtHalf, TVectorDouble.Create(PIBY2) + yPoly, yPoly);
        }

        public static TVectorDouble AsinhDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // The AMD AOCL-LibM scalar asinh implementation (asinh.c) uses range-based
            // polynomial lookup tables which cannot be trivially vectorized due to the cost
            // of gather instructions. Instead, this uses the mathematical identity:
            //   asinh(x) = sign(x) * log(|x| + sqrt(x^2 + 1))
            // with special handling for tiny and large values for improved accuracy.

            const double LN2 = 0.693147180559945309417;
            const double TINY_THRESHOLD = 2.98023223876953125e-08; // 2^-25
            const double LARGE_THRESHOLD = 268435456.0; // 2^28

            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble ax = TVectorDouble.Abs(x);

            // For very small values, return x
            TVectorDouble tinyMask = TVectorDouble.LessThanOrEqual(ax, TVectorDouble.Create(TINY_THRESHOLD));

            // For large values (|x| > 2^28), use log(2) + log(|x|)
            TVectorDouble largeMask = TVectorDouble.GreaterThan(ax, TVectorDouble.Create(LARGE_THRESHOLD));

            // Normal case: log(|x| + sqrt(x^2 + 1))
            TVectorDouble x2 = ax * ax;
            TVectorDouble sqrtArg = x2 + TVectorDouble.One;
            TVectorDouble normal = LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(ax + TVectorDouble.Sqrt(sqrtArg));

            // Large value case: log(2) + log(|x|)
            TVectorDouble large = TVectorDouble.Create(LN2) + LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(ax);

            // Select appropriate result based on magnitude
            TVectorDouble result = TVectorDouble.ConditionalSelect(largeMask, large, normal);
            result = TVectorDouble.ConditionalSelect(tinyMask, ax, result);

            // Restore sign
            result ^= sign;

            return result;
        }

        public static TVectorSingle AsinhSingle<TVectorSingle, TVectorInt32, TVectorUInt32, TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `asinhf` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorSingle sign = x & TVectorSingle.Create(-0.0f);
            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorSingle tinyMask = TVectorSingle.LessThan(ax, TVectorSingle.Create(0.000244140625f)); // RTEPS = 2^-12

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dax = Widen<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AsinhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(dax));
            }
            else
            {
                TVectorDouble daxLo = WidenLower<TVectorSingle, TVectorDouble>(ax);
                TVectorDouble daxHi = WidenUpper<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AsinhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(daxLo),
                    AsinhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(daxHi));
            }

            result ^= sign;
            result = TVectorSingle.ConditionalSelect(tinyMask, x, result);

            return result;
        }

        private static TVectorDouble AsinhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(
            TVectorDouble absx)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // Polynomial A coefficients (for |x| <= 2.0) from Sollya (AMD asinhf.c)
            const double A0 = -0.01152965835871758;     // -0x1.79cdc8cad8ecfp-7
            const double A1 = -0.014802041864737584;    // -0x1.e50886dc8d955p-7
            const double A2 = -0.005063201055468483;    // -0x1.4bd26af2415c3p-8
            const double A3 = -0.00041627277105834253;  // -0x1.b47e5f01aed5dp-12
            const double A4 = -1.1771989159549427e-06;  // -0x1.3c007e573c526p-20
            const double A5 = 0.06917795026025977;      //  0x1.1b5a569f8dd8ap-4
            const double A6 = 0.11994231760039391;      //  0x1.eb48a2b8008fbp-4
            const double A7 = 0.06582362487198468;      //  0x1.0d9d12c211cd8p-4
            const double A8 = 0.012600249786802279;     //  0x1.9ce28e60bc4f5p-7
            const double A9 = 0.0006284381367285534;    //  0x1.497b89f55e0fap-11

            // Polynomial B coefficients (for 2.0 < |x| <= 4.0) from Sollya (AMD asinhf.c)
            const double B0 = -0.0018546229069557859;   // -0x1.e62da2ed59b74p-10
            const double B1 = -0.0011367253350273402;   // -0x1.29fc588dcceedp-10
            const double B2 = -0.0001422083873005704;   // -0x1.2a3b8becef9dbp-13
            const double B3 = -3.3954601499308e-06;     // -0x1.c7bb1f54fd677p-19
            const double B4 = -1.51054665394481e-09;    // -0x1.9f3745642df74p-30
            const double B5 = 0.011148615858002477;     //  0x1.6d515e40bda61p-7
            const double B6 = 0.011778243798043956;     //  0x1.81f311f55d6cfp-7
            const double B7 = 0.0032590377353267485;    //  0x1.ab2b28fab47dcp-9
            const double B8 = 0.00025590204992406544;   //  0x1.0c552ef7695ep-12
            const double B9 = 4.341507869488909e-06;    //  0x1.235a8989d067ap-18

            const double LN2 = 6.93147180559945286227e-01;
            const double RECRTEPS = 4096.0;         // 1/sqrt(epsilon) ≈ 2^12

            TVectorDouble t = absx * absx;

            TVectorDouble x2 = absx * absx;
            TVectorDouble x4 = x2 * x2;
            TVectorDouble x6 = x4 * x2;
            TVectorDouble x8 = x4 * x4;

            // Polynomial A numerator: A0 + A1*x^2 + A2*x^4 + A3*x^6 + A4*x^8
            TVectorDouble numA = TVectorDouble.Create(A0);
            numA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A1), x2, numA);
            numA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A2), x4, numA);
            numA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A3), x6, numA);
            numA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A4), x8, numA);

            // Polynomial A denominator: A5 + A6*x^2 + A7*x^4 + A8*x^6 + A9*x^8
            TVectorDouble denA = TVectorDouble.Create(A5);
            denA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A6), x2, denA);
            denA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A7), x4, denA);
            denA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A8), x6, denA);
            denA = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A9), x8, denA);

            TVectorDouble polyA = numA / denA;
            TVectorDouble resultA = absx + absx * t * polyA;

            // Polynomial B numerator
            TVectorDouble numB = TVectorDouble.Create(B0);
            numB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B1), x2, numB);
            numB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B2), x4, numB);
            numB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B3), x6, numB);
            numB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B4), x8, numB);

            // Polynomial B denominator
            TVectorDouble denB = TVectorDouble.Create(B5);
            denB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B6), x2, denB);
            denB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B7), x4, denB);
            denB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B8), x6, denB);
            denB = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(B9), x8, denB);

            TVectorDouble polyB = numB / denB;
            TVectorDouble resultB = absx + absx * t * polyB;

            // For |x| > 4.0: log(|x| + sqrt(x^2+1))
            TVectorDouble sqrtArg = absx * absx + TVectorDouble.One;
            TVectorDouble resultLog = LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(absx + TVectorDouble.Sqrt(sqrtArg));

            // For |x| > 1/sqrt(eps): log(2) + log(|x|)
            TVectorDouble resultLargeLog = TVectorDouble.Create(LN2) + LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(absx);

            // Select based on ranges
            TVectorDouble leTwo = TVectorDouble.LessThanOrEqual(absx, TVectorDouble.Create(2.0));
            TVectorDouble leFour = TVectorDouble.LessThanOrEqual(absx, TVectorDouble.Create(4.0));
            TVectorDouble leRecrteps = TVectorDouble.LessThanOrEqual(absx, TVectorDouble.Create(RECRTEPS));

            TVectorDouble result = resultLargeLog;
            result = TVectorDouble.ConditionalSelect(leRecrteps, resultLog, result);
            result = TVectorDouble.ConditionalSelect(leFour, resultB, result);
            result = TVectorDouble.ConditionalSelect(leTwo, resultA, result);

            return result;
        }

        public static TVectorDouble AcoshDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // The AMD AOCL-LibM scalar acosh implementation (acosh.c) uses range-based
            // polynomial lookup tables which cannot be trivially vectorized due to the cost
            // of gather instructions. Instead, this uses the mathematical identity:
            //   acosh(x) = log(x + sqrt(x^2 - 1))
            // with special handling for x near 1 and large x for improved accuracy.

            const double LN2 = 0.693147180559945309417;
            const double NEAR_ONE_THRESHOLD = 1.0 + 2.98023223876953125e-08; // 1 + 2^-25
            const double LARGE_THRESHOLD = 268435456.0; // 2^28

            // Return NaN for x < 1
            TVectorDouble nanMask = TVectorDouble.LessThan(x, TVectorDouble.One);

            // For x close to 1 (1 < x <= 1 + 2^-25), use sqrt(2 * (x - 1))
            TVectorDouble nearOneMask = TVectorDouble.LessThanOrEqual(x, TVectorDouble.Create(NEAR_ONE_THRESHOLD));

            // For large values (x > 2^28), use log(2) + log(x)
            TVectorDouble largeMask = TVectorDouble.GreaterThan(x, TVectorDouble.Create(LARGE_THRESHOLD));

            // Normal case: log(x + sqrt(x^2 - 1))
            TVectorDouble x2 = x * x;
            TVectorDouble sqrtArg = x2 - TVectorDouble.One;
            TVectorDouble normal = LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(x + TVectorDouble.Sqrt(sqrtArg));

            // Large value case: log(2) + log(x)
            TVectorDouble large = TVectorDouble.Create(LN2) + LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(x);

            // Near one case: sqrt(2 * (x - 1))
            TVectorDouble nearOne = TVectorDouble.Sqrt(TVectorDouble.Create(2.0) * (x - TVectorDouble.One));

            // Select appropriate result based on magnitude
            TVectorDouble result = TVectorDouble.ConditionalSelect(largeMask, large, normal);
            result = TVectorDouble.ConditionalSelect(nearOneMask, nearOne, result);
            result = TVectorDouble.ConditionalSelect(nanMask, TVectorDouble.Create(double.NaN), result);

            return result;
        }

        public static TVectorSingle AcoshSingle<TVectorSingle, TVectorInt32, TVectorUInt32, TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `acoshf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // AMD acoshf.c uses mathematical identities (no polynomial approximation):
            // For x > 1/sqrt(eps): acosh(x) = log(2) + log(x)
            // For 2 < x <= 1/sqrt(eps): acosh(x) = log(x + sqrt(x^2 - 1))
            // For sqrt(eps) <= x <= 2: t=x-1, acosh(x) = log1p(t + sqrt(2t + t^2))
            // Widens to double for improved accuracy, matching AMD acoshf.c behavior.

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(AcoshDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(dx));
            }
            else
            {
                TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);
                return Narrow<TVectorDouble, TVectorSingle>(
                    AcoshDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(dxLo),
                    AcoshDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(dxHi)
                );
            }
        }

        public static TVectorDouble AtanhDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `atanh` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes (from atanh.c)
            // ------------------------------------
            // For |x| < 3.72e-9: atanh(x) = x (tiny approximation)
            // For |x| < 0.5: atanh(x) = x + x^3 * P(x^2)/Q(x^2) using [5,5] minimax rational polynomial
            // For |x| >= 0.5: atanh(x) = sign(x) * 0.5 * log1p(2|x|/(1-|x|))
            // Special cases: atanh(±1) = ±∞, atanh(x) = NaN for |x| > 1

            // [5,5] minimax rational polynomial coefficients from Sollya (atanh.c)
            // Numerator: evaluated as A0 + A1*r + A2*r^2 + A3*r^3 + A4*r^4 + A5*r^5 where r = x^2
            const double A0 = 4.74825735897473566460e-01;  // 0x1.e638b7bbea45ep-2
            const double A1 = -1.10283567978463414860e+00; // -0x1.1a53706989746p0
            const double A2 = 8.84681425365016482765e-01;  // 0x1.c4f4f6baa48ffp-1
            const double A3 = -2.81802109617808160813e-01; // -0x1.2090bb7302592p-2
            const double A4 = 2.87286386005485144812e-02;  // 0x1.d6b0a4cfde8fcp-6
            const double A5 = -1.04681588927531371807e-04; // -0x1.b711000f5a53bp-14

            // Denominator
            const double B0 = 1.42447720769242058836e+00;  // 0x1.6caa89ccefb46p0
            const double B1 = -4.16319336396935479883e+00; // -0x1.0a71c2944b0bfp2
            const double B2 = 4.54147006260845120806e+00;  // 0x1.22a7720caaa5dp2
            const double B3 = -2.26088837489884886267e+00; // -0x1.2164ca4f0c6f3p1
            const double B4 = 4.95611965555031008801e-01;  // 0x1.fb81b3fe42b33p-2
            const double B5 = -3.58615543701695377310e-02; // -0x1.25c7216683ecap-5

            const double HALF = 0.5;
            const double TINY_THRESHOLD = 3.72529029846191406250e-09; // 0x3e30000000000000 as double

            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble ax = TVectorDouble.Abs(x);

            // Special cases
            TVectorDouble nanMask = TVectorDouble.GreaterThan(ax, TVectorDouble.One);
            TVectorDouble infMask = TVectorDouble.Equals(ax, TVectorDouble.One);
            TVectorDouble tinyMask = TVectorDouble.LessThan(ax, TVectorDouble.Create(TINY_THRESHOLD));
            TVectorDouble smallMask = TVectorDouble.LessThan(ax, TVectorDouble.Create(HALF));

            // For |x| < 0.5: use [5,5] minimax rational polynomial
            // atanh(x) = x + x^3 * P(x^2)/Q(x^2)
            TVectorDouble r = x * x; // r = x^2

            // Evaluate numerator: A0 + A1*r + A2*r^2 + A3*r^3 + A4*r^4 + A5*r^5
            TVectorDouble num = TVectorDouble.Create(A5);
            num = TVectorDouble.MultiplyAddEstimate(num, r, TVectorDouble.Create(A4));
            num = TVectorDouble.MultiplyAddEstimate(num, r, TVectorDouble.Create(A3));
            num = TVectorDouble.MultiplyAddEstimate(num, r, TVectorDouble.Create(A2));
            num = TVectorDouble.MultiplyAddEstimate(num, r, TVectorDouble.Create(A1));
            num = TVectorDouble.MultiplyAddEstimate(num, r, TVectorDouble.Create(A0));

            // Evaluate denominator: B0 + B1*r + B2*r^2 + B3*r^3 + B4*r^4 + B5*r^5
            TVectorDouble den = TVectorDouble.Create(B5);
            den = TVectorDouble.MultiplyAddEstimate(den, r, TVectorDouble.Create(B4));
            den = TVectorDouble.MultiplyAddEstimate(den, r, TVectorDouble.Create(B3));
            den = TVectorDouble.MultiplyAddEstimate(den, r, TVectorDouble.Create(B2));
            den = TVectorDouble.MultiplyAddEstimate(den, r, TVectorDouble.Create(B1));
            den = TVectorDouble.MultiplyAddEstimate(den, r, TVectorDouble.Create(B0));

            TVectorDouble poly = num / den;
            TVectorDouble smallResult = x + (x * r) * poly;

            // For |x| >= 0.5: atanh(x) = sign(x) * 0.5 * log((1 + |x|) / (1 - |x|))
            TVectorDouble onePlusAx = TVectorDouble.One + ax;
            TVectorDouble oneMinusAx = TVectorDouble.One - ax;
            TVectorDouble largeResult = TVectorDouble.Create(HALF) * LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(onePlusAx / oneMinusAx);
            largeResult |= sign; // restore sign

            // Select based on magnitude
            TVectorDouble result = TVectorDouble.ConditionalSelect(smallMask, smallResult, largeResult);
            result = TVectorDouble.ConditionalSelect(tinyMask, x, result);
            result = TVectorDouble.ConditionalSelect(infMask, TVectorDouble.Create(double.PositiveInfinity) ^ sign, result);
            result = TVectorDouble.ConditionalSelect(nanMask, TVectorDouble.Create(double.NaN), result);

            return result;
        }

        public static TVectorSingle AtanhSingle<TVectorSingle, TVectorInt32, TVectorUInt32, TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorUInt32 : unmanaged, ISimdVector<TVectorUInt32, uint>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // This code is based on `atanhf` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorSingle sign = x & TVectorSingle.Create(-0.0f);
            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorSingle nanMask = TVectorSingle.GreaterThan(ax, TVectorSingle.One);
            TVectorSingle infMask = TVectorSingle.Equals(ax, TVectorSingle.One);
            TVectorSingle tinyMask = TVectorSingle.LessThan(ax, TVectorSingle.Create(1.220703125e-4f)); // 0x39000000

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dax = Widen<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AtanhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(dax));
            }
            else
            {
                TVectorDouble daxLo = WidenLower<TVectorSingle, TVectorDouble>(ax);
                TVectorDouble daxHi = WidenUpper<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AtanhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(daxLo),
                    AtanhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(daxHi));
            }

            result ^= sign;
            result = TVectorSingle.ConditionalSelect(tinyMask, x, result);
            result = TVectorSingle.ConditionalSelect(infMask, TVectorSingle.Create(float.PositiveInfinity) ^ sign, result);
            result = TVectorSingle.ConditionalSelect(nanMask, TVectorSingle.Create(float.NaN), result);

            return result;
        }

        private static TVectorDouble AtanhSingleCoreDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(
            TVectorDouble ax)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
            where TVectorUInt64 : unmanaged, ISimdVector<TVectorUInt64, ulong>
        {
            // [2,2] minimax rational polynomial coefficients from Sollya (AMD atanhf.c)
            const double A0 = 0.3945362865924835;       // 0x1.940152p-2
            const double A1 = -0.2812034785747528;      // -0x1.1ff3cep-2
            const double A2 = 0.00928342156112194;      // 0x1.3032fcp-7
            const double A3 = 1.183608889579773;        // 0x1.2f00fep0
            const double A4 = -1.5537744760513306;      // -0x1.8dc42ap0
            const double A5 = 0.452818900346756;        // 0x1.cfafc2p-2

            TVectorDouble smallMask = TVectorDouble.LessThan(ax, TVectorDouble.Create(0.5));

            // For |x| < 0.5: POLY_EVAL_EVEN_4(x, A0, A1, A2) / POLY_EVAL_EVEN_4(x, A3, A4, A5)
            TVectorDouble x2 = ax * ax;
            TVectorDouble x4 = x2 * x2;

            TVectorDouble num = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A2), x4,
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A1), x2, TVectorDouble.Create(A0)));
            TVectorDouble den = TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A5), x4,
                TVectorDouble.MultiplyAddEstimate(TVectorDouble.Create(A4), x2, TVectorDouble.Create(A3)));

            TVectorDouble poly = num / den;
            TVectorDouble t = ax * x2;
            TVectorDouble smallResult = ax + t * poly;

            // For |x| >= 0.5: atanh(x) = 0.5 * log1p(2|x|/(1-|x|))
            TVectorDouble r_pos = (TVectorDouble.Create(2.0) * ax) / (TVectorDouble.One - ax);
            TVectorDouble largeResult = TVectorDouble.Create(0.5) * LogDouble<TVectorDouble, TVectorInt64, TVectorUInt64>(TVectorDouble.One + r_pos);

            return TVectorDouble.ConditionalSelect(smallMask, smallResult, largeResult);
        }

        public static TVectorDouble AcosDouble<TVectorDouble>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `acos` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // Based on the value of x, acos(x) is calculated as:
            // 1. If x > 0.5:  acos(x) = 2 * asin(sqrt((1 - x) / 2))
            // 2. If x < -0.5: acos(x) = pi - 2*asin(sqrt((1 + x) / 2))
            // 3. If |x| <= 0.5: acos(x) = pi/2 - asin(x)

            // Polynomial coefficients from Sollya (AMD aocl-libm-ose acos.c)
            const double C1 = 0.166666666666654;      // 0x1.55555555552aap-3
            const double C2 = 0.0750000000006397;     // 0x1.333333337cbaep-4
            const double C3 = 0.0446428571088065;     // 0x1.6db6db3c0984p-5
            const double C4 = 0.0303819469180048;     // 0x1.f1c72dd86cbafp-6
            const double C5 = 0.0223717830326408;     // 0x1.6e89d3ff33aa4p-6
            const double C6 = 0.0173549783672646;     // 0x1.1c6d83ae664b6p-6
            const double C7 = 0.0138887093438824;     // 0x1.c6e1568b90518p-7
            const double C8 = 0.0121483872130308;     // 0x1.8f6a58977fe49p-7
            const double C9 = 0.00640855516049134;    // 0x1.a6ab10b3321bp-8
            const double C10 = 0.0197639134991274;    // 0x1.43305ebb2428fp-6
            const double C11 = -0.0164975096950116;   // -0x1.0e874ec5e3157p-6
            const double C12 = 0.0319093993379205;    // 0x1.06eec35b3b142p-5

            const double PI = 3.1415926535897933e+00;
            const double HALF = 0.5;

            // Constants for reconstruction (a[0], a[1], b[0], b[1])
            const double A0 = 0.0;
            const double A1 = 0.7853981633974483;     // pi/4
            const double B0 = 1.5707963267948966;     // pi/2
            const double B1 = 0.7853981633974483;     // pi/4

            // Get sign and absolute value of x
            TVectorDouble xneg = TVectorDouble.LessThan(x, TVectorDouble.Zero);
            TVectorDouble ax = TVectorDouble.Abs(x);

            // Check which region we're in
            TVectorDouble gtHalf = TVectorDouble.GreaterThanOrEqual(ax, TVectorDouble.Create(HALF));

            // For |x| >= 0.5: z = 0.5*(1-ax), ax = -2*sqrt(z)
            TVectorDouble z_hi = TVectorDouble.Create(HALF) * (TVectorDouble.One - ax);
            TVectorDouble y_hi = TVectorDouble.Create(-2.0) * TVectorDouble.Sqrt(z_hi);

            // For |x| < 0.5: z = ax*ax (use n=1 reconstruction)
            TVectorDouble z_lo = ax * ax;

            // Select z and ax based on region
            TVectorDouble z = TVectorDouble.ConditionalSelect(gtHalf, z_hi, z_lo);
            ax = TVectorDouble.ConditionalSelect(gtHalf, y_hi, ax);

            // Polynomial: C1 + z*(C2 + z*(C3 + ... + z*C12))
            TVectorDouble poly = TVectorDouble.Create(C12);
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C11));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C10));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C9));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C8));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C7));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C6));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C5));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C4));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C3));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C2));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C1));

            // poly = ax + ax * z * poly
            poly = ax + ax * z * poly;

            // Reconstruct result based on sign and region
            // if (xneg): result = (B[n] + poly) + B[n]
            // else:      result = (A[n] - poly) + A[n]
            // where n=0 if ax > 0.5, n=1 if ax <= 0.5

            // For n=0 (|x| > 0.5): A[0]=0, B[0]=pi/2
            // For n=1 (|x| <= 0.5): A[1]=pi/4, B[1]=pi/4

            TVectorDouble a = TVectorDouble.ConditionalSelect(gtHalf, TVectorDouble.Create(A0), TVectorDouble.Create(A1));
            TVectorDouble b = TVectorDouble.ConditionalSelect(gtHalf, TVectorDouble.Create(B0), TVectorDouble.Create(B1));

            TVectorDouble result_neg = (b + poly) + b;
            TVectorDouble result_pos = (a - poly) + a;

            TVectorDouble result = TVectorDouble.ConditionalSelect(xneg, result_neg, result_pos);

            // Handle special cases: |x| > 1 returns NaN, x = ±1 returns 0 or π
            TVectorDouble absXGreaterThanOne = TVectorDouble.GreaterThan(TVectorDouble.Abs(x), TVectorDouble.One);
            result = TVectorDouble.ConditionalSelect(absXGreaterThanOne, TVectorDouble.Create(double.NaN), result);

            TVectorDouble xEqualsOne = TVectorDouble.Equals(x, TVectorDouble.One);
            result = TVectorDouble.ConditionalSelect(xEqualsOne, TVectorDouble.Zero, result);

            TVectorDouble xEqualsNegOne = TVectorDouble.Equals(x, TVectorDouble.Create(-1.0));
            result = TVectorDouble.ConditionalSelect(xEqualsNegOne, TVectorDouble.Create(PI), result);

            return result;
        }

        public static TVectorSingle AcosSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `acosf` from amd/aocl-libm-ose
            // Copyright (C) 2021-2023 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorSingle outOfRange = TVectorSingle.GreaterThan(TVectorSingle.Abs(x), TVectorSingle.One);
            TVectorSingle xEqualsOne = TVectorSingle.Equals(x, TVectorSingle.One);
            TVectorSingle xEqualsNegOne = TVectorSingle.Equals(x, TVectorSingle.Create(-1.0f));

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dx = Widen<TVectorSingle, TVectorDouble>(x);
                result = Narrow<TVectorDouble, TVectorSingle>(AcosSingleCoreDouble<TVectorDouble>(dx));
            }
            else
            {
                TVectorDouble dxLo = WidenLower<TVectorSingle, TVectorDouble>(x);
                TVectorDouble dxHi = WidenUpper<TVectorSingle, TVectorDouble>(x);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AcosSingleCoreDouble<TVectorDouble>(dxLo),
                    AcosSingleCoreDouble<TVectorDouble>(dxHi));
            }

            result = TVectorSingle.ConditionalSelect(outOfRange, TVectorSingle.Create(float.NaN), result);
            result = TVectorSingle.ConditionalSelect(xEqualsOne, TVectorSingle.Zero, result);
            result = TVectorSingle.ConditionalSelect(xEqualsNegOne, TVectorSingle.Create(3.1415927f), result);

            return result;
        }

        private static TVectorDouble AcosSingleCoreDouble<TVectorDouble>(TVectorDouble dx)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // Polynomial coefficients from acosf.c
            const double C1 = 0.1666679084300995;       // 0x1.5555fcp-3
            const double C2 = 0.07494434714317322;      // 0x1.32f8d8p-4
            const double C3 = 0.045550186187028885;     // 0x1.7525aap-5
            const double C4 = 0.023858169093728065;     // 0x1.86e46ap-6
            const double C5 = 0.04263564199209213;      // 0x1.5d456cp-5

            // Reconstruction constants
            const double A0 = 0.0;
            const double A1 = 0.7853981852531433;       // 0x1.921fb6p-1 (pi/4 in float precision)
            const double B0 = 1.5707963705062866;       // 0x1.921fb6p0  (pi/2 in float precision)
            const double B1 = 0.7853981852531433;       // 0x1.921fb6p-1 (pi/4 in float precision)

            TVectorDouble xneg = TVectorDouble.LessThan(dx, TVectorDouble.Zero);
            TVectorDouble ax = TVectorDouble.Abs(dx);

            TVectorDouble gtHalf = TVectorDouble.GreaterThanOrEqual(ax, TVectorDouble.Create(0.5));

            TVectorDouble z_hi = TVectorDouble.Create(0.5) * (TVectorDouble.One - ax);
            TVectorDouble y_hi = TVectorDouble.Create(-2.0) * TVectorDouble.Sqrt(z_hi);

            TVectorDouble z_lo = ax * ax;

            TVectorDouble z = TVectorDouble.ConditionalSelect(gtHalf, z_hi, z_lo);
            ax = TVectorDouble.ConditionalSelect(gtHalf, y_hi, ax);

            TVectorDouble poly = TVectorDouble.Create(C5);
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C4));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C3));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C2));
            poly = TVectorDouble.MultiplyAddEstimate(poly, z, TVectorDouble.Create(C1));

            poly = ax + ax * z * poly;

            TVectorDouble a = TVectorDouble.ConditionalSelect(gtHalf, TVectorDouble.Create(A0), TVectorDouble.Create(A1));
            TVectorDouble b = TVectorDouble.ConditionalSelect(gtHalf, TVectorDouble.Create(B0), TVectorDouble.Create(B1));

            TVectorDouble result_neg = (b + poly) + b;
            TVectorDouble result_pos = (a - poly) + a;

            return TVectorDouble.ConditionalSelect(xneg, result_neg, result_pos);
        }

        public static TVectorDouble AtanDouble<TVectorDouble>(TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // This code is based on `atan` from amd/aocl-libm-ose
            // Copyright (C) 2008-2023 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // Argument reduction to range [-7/16,7/16]
            // Use the following identities:
            // atan(x) = pi/2 - atan(1/x)                when x > 39/16
            //         = pi/3 + atan((x-1.5)/(1+1.5*x))  when 19/16 < x <= 39/16
            //         = pi/4 + atan((x-1)/(1+x))        when 11/16 < x <= 19/16
            //         = pi/6 + atan((2x-1)/(2+x))       when 7/16 < x <= 11/16
            //         = atan(x)                         when x <= 7/16
            //
            // Core approximation: Remez(4,4) on [-7/16,7/16]

            // Range boundaries
            const double R7_16 = 0.4375;    // 7/16
            const double R11_16 = 0.6875;   // 11/16
            const double R19_16 = 1.1875;   // 19/16
            const double R39_16 = 2.4375;   // 39/16

            // (chi, clo) pairs for high-precision addition
            const double CHI_0 = 0.0;
            const double CLO_0 = 0.0;
            const double CHI_HALF = 4.63647609000806093515e-01;  // arctan(0.5)
            const double CLO_HALF = 2.26987774529616809294e-17;
            const double CHI_1 = 7.85398163397448278999e-01;     // arctan(1.0) = pi/4
            const double CLO_1 = 3.06161699786838240164e-17;
            const double CHI_1_5 = 9.82793723247329054082e-01;   // arctan(1.5)
            const double CLO_1_5 = 1.39033110312309953701e-17;
            const double CHI_INF = 1.57079632679489655800e+00;   // arctan(inf) = pi/2
            const double CLO_INF = 6.12323399573676480327e-17;

            // Remez(4,4) polynomial coefficients for numerator
            const double P0 = 0.268297920532545909e0;
            const double P1 = 0.447677206805497472e0;
            const double P2 = 0.220638780716667420e0;
            const double P3 = 0.304455919504853031e-1;
            const double P4 = 0.142316903342317766e-3;

            // Remez(4,4) polynomial coefficients for denominator
            const double Q0 = 0.804893761597637733e0;
            const double Q1 = 0.182596787737507063e1;
            const double Q2 = 0.141254259931958921e1;
            const double Q3 = 0.424602594203847109e0;
            const double Q4 = 0.389525873944742195e-1;

            TVectorDouble sign = x & TVectorDouble.Create(-0.0);
            TVectorDouble v = TVectorDouble.Abs(x);

            // Determine which region each element falls into
            TVectorDouble gtR39_16 = TVectorDouble.GreaterThan(v, TVectorDouble.Create(R39_16));
            TVectorDouble gtR19_16 = TVectorDouble.GreaterThan(v, TVectorDouble.Create(R19_16));
            TVectorDouble gtR11_16 = TVectorDouble.GreaterThan(v, TVectorDouble.Create(R11_16));
            TVectorDouble gtR7_16 = TVectorDouble.GreaterThan(v, TVectorDouble.Create(R7_16));

            // Compute reduced argument for each region

            // Region 5: x > 39/16: reduced = -1/v
            TVectorDouble reduced5 = -TVectorDouble.One / v;

            // Region 4: 19/16 < x <= 39/16: reduced = (v-1.5)/(1+1.5*v)
            TVectorDouble reduced4 = (v - TVectorDouble.Create(1.5)) / (TVectorDouble.One + TVectorDouble.Create(1.5) * v);

            // Region 3: 11/16 < x <= 19/16: reduced = (v-1)/(1+v)
            TVectorDouble reduced3 = (v - TVectorDouble.One) / (TVectorDouble.One + v);

            // Region 2: 7/16 < x <= 11/16: reduced = (2*v-1)/(2+v)
            TVectorDouble reduced2 = (TVectorDouble.Create(2.0) * v - TVectorDouble.One) / (TVectorDouble.Create(2.0) + v);

            // Region 1: x <= 7/16: reduced = v
            TVectorDouble reduced1 = v;

            // Select reduced argument
            TVectorDouble reduced = TVectorDouble.ConditionalSelect(gtR39_16, reduced5,
                                    TVectorDouble.ConditionalSelect(gtR19_16, reduced4,
                                    TVectorDouble.ConditionalSelect(gtR11_16, reduced3,
                                    TVectorDouble.ConditionalSelect(gtR7_16, reduced2, reduced1))));

            // Select chi (high part of constant)
            TVectorDouble chi = TVectorDouble.ConditionalSelect(gtR39_16, TVectorDouble.Create(CHI_INF),
                               TVectorDouble.ConditionalSelect(gtR19_16, TVectorDouble.Create(CHI_1_5),
                               TVectorDouble.ConditionalSelect(gtR11_16, TVectorDouble.Create(CHI_1),
                               TVectorDouble.ConditionalSelect(gtR7_16, TVectorDouble.Create(CHI_HALF), TVectorDouble.Create(CHI_0)))));

            // Select clo (low part of constant)
            TVectorDouble clo = TVectorDouble.ConditionalSelect(gtR39_16, TVectorDouble.Create(CLO_INF),
                               TVectorDouble.ConditionalSelect(gtR19_16, TVectorDouble.Create(CLO_1_5),
                               TVectorDouble.ConditionalSelect(gtR11_16, TVectorDouble.Create(CLO_1),
                               TVectorDouble.ConditionalSelect(gtR7_16, TVectorDouble.Create(CLO_HALF), TVectorDouble.Create(CLO_0)))));

            // Compute s = reduced^2
            TVectorDouble s = reduced * reduced;

            // Evaluate numerator polynomial: P0 + s*(P1 + s*(P2 + s*(P3 + s*P4)))
            TVectorDouble num = TVectorDouble.Create(P4);
            num = TVectorDouble.MultiplyAddEstimate(num, s, TVectorDouble.Create(P3));
            num = TVectorDouble.MultiplyAddEstimate(num, s, TVectorDouble.Create(P2));
            num = TVectorDouble.MultiplyAddEstimate(num, s, TVectorDouble.Create(P1));
            num = TVectorDouble.MultiplyAddEstimate(num, s, TVectorDouble.Create(P0));

            // Evaluate denominator polynomial: Q0 + s*(Q1 + s*(Q2 + s*(Q3 + s*Q4)))
            TVectorDouble denom = TVectorDouble.Create(Q4);
            denom = TVectorDouble.MultiplyAddEstimate(denom, s, TVectorDouble.Create(Q3));
            denom = TVectorDouble.MultiplyAddEstimate(denom, s, TVectorDouble.Create(Q2));
            denom = TVectorDouble.MultiplyAddEstimate(denom, s, TVectorDouble.Create(Q1));
            denom = TVectorDouble.MultiplyAddEstimate(denom, s, TVectorDouble.Create(Q0));

            // q = reduced * s * num / denom
            TVectorDouble q = reduced * s * num / denom;

            // result = chi - ((q - clo) - reduced)
            TVectorDouble result = chi - ((q - clo) - reduced);

            // Restore sign
            result ^= sign;

            return result;
        }

        public static TVectorSingle AtanSingle<TVectorSingle, TVectorInt32, TVectorDouble, TVectorInt64>(TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorInt32 : unmanaged, ISimdVector<TVectorInt32, int>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            // This code is based on `atanf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TVectorSingle nanMask = ~TVectorSingle.Equals(x, x);
            TVectorSingle sign = x & TVectorSingle.Create(-0.0f);
            TVectorSingle ax = TVectorSingle.Abs(x);
            TVectorSingle tinyMask = TVectorSingle.LessThan(ax, TVectorSingle.Create(1.9073486328125e-06f));  // 2^-19
            TVectorSingle overflowMask = TVectorSingle.GreaterThanOrEqual(ax, TVectorSingle.Create(67108864.0f)); // 2^26

            TVectorSingle result;

            if (TVectorSingle.ElementCount == TVectorDouble.ElementCount)
            {
                TVectorDouble dax = Widen<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(AtanSingleCoreDouble<TVectorDouble>(dax));
            }
            else
            {
                TVectorDouble daxLo = WidenLower<TVectorSingle, TVectorDouble>(ax);
                TVectorDouble daxHi = WidenUpper<TVectorSingle, TVectorDouble>(ax);
                result = Narrow<TVectorDouble, TVectorSingle>(
                    AtanSingleCoreDouble<TVectorDouble>(daxLo),
                    AtanSingleCoreDouble<TVectorDouble>(daxHi));
            }

            result ^= sign;
            result = TVectorSingle.ConditionalSelect(tinyMask, x, result);
            result = TVectorSingle.ConditionalSelect(overflowMask & ~nanMask, TVectorSingle.Create(1.5707964f) ^ sign, result);
            result = TVectorSingle.ConditionalSelect(nanMask, TVectorSingle.Create(float.NaN), result);

            return result;
        }

        private static TVectorDouble AtanSingleCoreDouble<TVectorDouble>(TVectorDouble ax)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // Rational polynomial coefficients for Remez(2,2) (AMD atanf.c)
            const double C0 = 0.296528598819239217902158651186e0;
            const double C1 = 0.192324546402108583211697690500e0;
            const double C2 = 0.470677934286149214138357545549e-2;
            const double C3 = 0.889585796862432286486651434570e0;
            const double C4 = 0.111072499995399550138837673349e1;
            const double C5 = 0.299309699959659728404442796915e0;

            // Argument reduction boundary constants
            const double R7_16 = 0.4375;               // 7/16
            const double R11_16 = 0.6875;               // 11/16
            const double R19_16 = 1.1875;               // 19/16
            const double R39_16 = 2.4375;               // 39/16

            // Precomputed arctan values for reduction points
            const double VALUE0 = 0.0;                                     // atan(0)
            const double VALUE1 = 4.63647609000806093515e-01;              // atan(0.5)
            const double VALUE2 = 7.85398163397448278999e-01;              // atan(1)
            const double VALUE3 = 9.82793723247329054082e-01;              // atan(1.5)

            TVectorDouble r1 = TVectorDouble.LessThan(ax, TVectorDouble.Create(R7_16));
            TVectorDouble r2 = TVectorDouble.LessThan(ax, TVectorDouble.Create(R11_16));
            TVectorDouble r3 = TVectorDouble.LessThan(ax, TVectorDouble.Create(R19_16));
            TVectorDouble r4 = TVectorDouble.LessThan(ax, TVectorDouble.Create(R39_16));

            // Start with region 5 (largest range) and work backwards
            TVectorDouble reduced = -TVectorDouble.One / ax;
            TVectorDouble c = TVectorDouble.Create(1.57079632679489655800e+00); // pi/2

            TVectorDouble x4 = (ax - TVectorDouble.Create(1.5)) / (TVectorDouble.One + TVectorDouble.Create(1.5) * ax);
            reduced = TVectorDouble.ConditionalSelect(r4, x4, reduced);
            c = TVectorDouble.ConditionalSelect(r4, TVectorDouble.Create(VALUE3), c);

            TVectorDouble x3 = (ax - TVectorDouble.One) / (TVectorDouble.One + ax);
            reduced = TVectorDouble.ConditionalSelect(r3, x3, reduced);
            c = TVectorDouble.ConditionalSelect(r3, TVectorDouble.Create(VALUE2), c);

            TVectorDouble x2 = (TVectorDouble.Create(2.0) * ax - TVectorDouble.One) / (TVectorDouble.Create(2.0) + ax);
            reduced = TVectorDouble.ConditionalSelect(r2, x2, reduced);
            c = TVectorDouble.ConditionalSelect(r2, TVectorDouble.Create(VALUE1), c);

            reduced = TVectorDouble.ConditionalSelect(r1, ax, reduced);
            c = TVectorDouble.ConditionalSelect(r1, TVectorDouble.Create(VALUE0), c);

            TVectorDouble s = reduced * reduced;

            TVectorDouble num = TVectorDouble.Create(C2);
            num = TVectorDouble.MultiplyAddEstimate(num, s, TVectorDouble.Create(C1));
            num = TVectorDouble.MultiplyAddEstimate(num, s, TVectorDouble.Create(C0));

            TVectorDouble den = TVectorDouble.Create(C5);
            den = TVectorDouble.MultiplyAddEstimate(den, s, TVectorDouble.Create(C4));
            den = TVectorDouble.MultiplyAddEstimate(den, s, TVectorDouble.Create(C3));

            TVectorDouble p = reduced * s * num / den;

            return c - (p - reduced);
        }

        public static TVectorDouble Atan2Double<TVectorDouble>(TVectorDouble y, TVectorDouble x)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // The AMD AOCL-LibM scalar atan2 implementation (atan2.c) uses a lookup table
            // (ATAN_TABLE with 241 entries) which cannot be trivially vectorized due to the
            // cost of gather instructions. Instead, this computes atan2(y,x) using the
            // already-vectorized AtanDouble implementation with quadrant adjustments.
            // - atan2(±∞, +∞) = ±pi/4
            // - atan2(±∞, -∞) = ±3pi/4
            // - atan2(±y, +∞) = ±0
            // - atan2(±y, -∞) = ±pi

            const double PI = 3.141592653589793;           // 0x1.921fb54442d18p1

            // Check for x being negative using standard comparison
            TVectorDouble xLessThanZero = TVectorDouble.LessThan(x, TVectorDouble.Zero);

            // For signed zero handling: check if x is -0 specifically
            // We detect -0 by checking: x == 0 AND 1/x < 0 (since 1/-0 = -∞)
            TVectorDouble xIsZero = TVectorDouble.Equals(x, TVectorDouble.Zero);
            TVectorDouble recipX = TVectorDouble.One / x;
            TVectorDouble recipXNegative = TVectorDouble.LessThan(recipX, TVectorDouble.Zero);
            TVectorDouble xIsNegativeOrNegZero = xLessThanZero | (xIsZero & recipXNegative);

            // Check for y sign using same technique
            TVectorDouble yLessThanZero = TVectorDouble.LessThan(y, TVectorDouble.Zero);
            TVectorDouble yIsZero = TVectorDouble.Equals(y, TVectorDouble.Zero);
            TVectorDouble recipY = TVectorDouble.One / y;
            TVectorDouble recipYNegative = TVectorDouble.LessThan(recipY, TVectorDouble.Zero);
            TVectorDouble yIsNegativeOrNegZero = yLessThanZero | (yIsZero & recipYNegative);

            // Compute atan(y/x) for the general case
            TVectorDouble ratio = y / x;
            TVectorDouble atanResult = AtanDouble<TVectorDouble>(ratio);

            // For x < 0 (or x = -0), we need to adjust by ±π
            TVectorDouble piAdjust = TVectorDouble.ConditionalSelect(
                yIsNegativeOrNegZero,
                TVectorDouble.Create(-PI),
                TVectorDouble.Create(PI)
            );

            // Apply pi adjustment when x is negative (or -0)
            TVectorDouble result = TVectorDouble.ConditionalSelect(
                xIsNegativeOrNegZero,
                atanResult + piAdjust,
                atanResult
            );

            // Special case: when both y = ±0 and x = ±0
            // atan2(±0, +0) = ±0
            // atan2(±0, -0) = ±π
            TVectorDouble zeroResult = TVectorDouble.ConditionalSelect(yIsNegativeOrNegZero, TVectorDouble.Create(-0.0), TVectorDouble.Zero);
            TVectorDouble piResult = TVectorDouble.ConditionalSelect(yIsNegativeOrNegZero, TVectorDouble.Create(-PI), TVectorDouble.Create(PI));

            TVectorDouble bothZero = xIsZero & yIsZero;
            TVectorDouble zeroXResult = TVectorDouble.ConditionalSelect(xIsNegativeOrNegZero, piResult, zeroResult);
            result = TVectorDouble.ConditionalSelect(bothZero, zeroXResult, result);

            // Special case: when both x and y are infinite
            // atan2(±∞, +∞) = ±π/4
            // atan2(±∞, -∞) = ±3π/4
            const double PI_OVER_4 = 0.78539816339744830961;   // 0x1.921fb54442d18p-1
            const double THREE_PI_OVER_4 = 2.3561944901923449; // 0x1.2d97c7f3321d2p+1
            TVectorDouble xIsInf = TVectorDouble.Equals(TVectorDouble.Abs(x), TVectorDouble.Create(double.PositiveInfinity));
            TVectorDouble yIsInf = TVectorDouble.Equals(TVectorDouble.Abs(y), TVectorDouble.Create(double.PositiveInfinity));
            TVectorDouble bothInf = xIsInf & yIsInf;
            TVectorDouble infBaseAngle = TVectorDouble.ConditionalSelect(xIsNegativeOrNegZero, TVectorDouble.Create(THREE_PI_OVER_4), TVectorDouble.Create(PI_OVER_4));
            TVectorDouble infResult = TVectorDouble.ConditionalSelect(yIsNegativeOrNegZero, -infBaseAngle, infBaseAngle);
            result = TVectorDouble.ConditionalSelect(bothInf, infResult, result);

            return result;
        }

        public static TVectorSingle Atan2Single<TVectorSingle, TVectorDouble>(TVectorSingle y, TVectorSingle x)
            where TVectorSingle : unmanaged, ISimdVector<TVectorSingle, float>
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
        {
            // Widens to double and calls Atan2Double for improved accuracy.

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
