// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    internal static class VectorMath
    {
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
                                   | Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThanOrEqual(xBits, TVectorInt64.Create(double.PositiveInfinityBits)));

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
                                   | Unsafe.BitCast<TVectorInt64, TVectorDouble>(TVectorInt64.GreaterThanOrEqual(xBits, TVectorInt64.Create(double.PositiveInfinityBits)));

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
                                   | Unsafe.BitCast<TVectorInt32, TVectorSingle>(TVectorInt32.GreaterThanOrEqual(xBits, TVectorInt32.Create(float.PositiveInfinityBits)));

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
        private static TVectorDouble ConvertToDouble<TVectorInt64, TVectorDouble>(TVectorInt64 vector)
            where TVectorDouble : unmanaged, ISimdVector<TVectorDouble, double>
            where TVectorInt64 : unmanaged, ISimdVector<TVectorInt64, long>
        {
            Unsafe.SkipInit(out TVectorDouble result);

            if (typeof(TVectorInt64) == typeof(Vector64<long>))
            {
                return (TVectorDouble)(object)Vector64.ConvertToDouble((Vector64<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector128<long>))
            {
                return (TVectorDouble)(object)Vector128.ConvertToDouble((Vector128<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector256<long>))
            {
                return (TVectorDouble)(object)Vector256.ConvertToDouble((Vector256<long>)(object)vector);
            }
            else if (typeof(TVectorInt64) == typeof(Vector512<long>))
            {
                return (TVectorDouble)(object)Vector512.ConvertToDouble((Vector512<long>)(object)vector);
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

            if (typeof(TVectorInt32) == typeof(Vector64<int>))
            {
                return (TVectorSingle)(object)Vector64.ConvertToSingle((Vector64<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector128<int>))
            {
                return (TVectorSingle)(object)Vector128.ConvertToSingle((Vector128<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector256<int>))
            {
                return (TVectorSingle)(object)Vector256.ConvertToSingle((Vector256<int>)(object)vector);
            }
            else if (typeof(TVectorInt32) == typeof(Vector512<int>))
            {
                return (TVectorSingle)(object)Vector512.ConvertToSingle((Vector512<int>)(object)vector);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }
    }
}
