// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise natural (base <c>e</c>) logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, LogOperator<T>>(x, destination);

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanSpanIntoSpan<T, LogBaseOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanScalarIntoSpan<T, LogBaseOperator<T>>(x, y, destination);

        /// <summary>T.Log(x)</summary>
        internal readonly struct LogOperator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(double))
                                            || (typeof(T) == typeof(float));

            public static T Invoke(T x) => T.Log(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Log(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return LogOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return LogOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Log(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return LogOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return LogOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Log(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return LogOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return LogOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }
        }

        /// <summary>T.Log(x, y)</summary>
        private readonly struct LogBaseOperator<T> : IBinaryOperator<T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => LogOperator<T>.Vectorizable;
            public static T Invoke(T x, T y) => T.Log(x, y);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
        }

#if !NET9_0_OR_GREATER
        /// <summary>double.Log(x)</summary>
        private readonly struct LogOperatorDouble : IUnaryOperator<double, double>
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

            private const ulong V_MIN = 0x00100000_00000000;    // SmallestNormal
            private const ulong V_MAX = 0x7FF00000_00000000;    // +Infinity
            private const ulong V_MSK = 0x000FFFFF_FFFFFFFF;    // (1 << 52) - 1
            private const ulong V_OFF = 0x3FE55555_55555555;    // 2.0 / 3.0

            private const double LN2_HEAD = 0.693359375;
            private const double LN2_TAIL = -0.00021219444005469057;

            private const double C02 = -0.499999999999999560;
            private const double C03 = +0.333333333333414750;
            private const double C04 = -0.250000000000297430;
            private const double C05 = +0.199999999975985220;
            private const double C06 = -0.166666666608919500;
            private const double C07 = +0.142857145600277100;
            private const double C08 = -0.125000005127831270;
            private const double C09 = +0.111110952357159440;
            private const double C10 = -0.099999750495501240;
            private const double C11 = +0.090914349823462390;
            private const double C12 = -0.083340600527551860;
            private const double C13 = +0.076817603328311300;
            private const double C14 = -0.071296718946287310;
            private const double C15 = +0.067963465211535730;
            private const double C16 = -0.063995035098960040;
            private const double C17 = +0.049370587082412105;
            private const double C18 = -0.045370170994891980;
            private const double C19 = +0.088970636003577750;
            private const double C20 = -0.086906174116908760;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Log(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector128<ulong> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt64() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<ulong>.Zero)
                {
                    Vector128<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector128<double> lessThanZeroMask = Vector128.LessThan(xBits, Vector128<long>.Zero).AsDouble();

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector128<double> zeroMask = Vector128.Equals(xBits << 1, Vector128<long>.Zero).AsDouble();

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector128<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector128.GreaterThanOrEqual(xBits, Vector128.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector128<double> subnormalMask = Vector128.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector128.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector128<ulong> vx = x.AsUInt64() - Vector128.Create(V_OFF);
                Vector128<double> n = Vector128.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector128.Create(V_MSK)) + Vector128.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector128<double> r = vx.AsDouble() - Vector128<double>.One;

                Vector128<double> r02 = r * r;
                Vector128<double> r04 = r02 * r02;
                Vector128<double> r08 = r04 * r04;
                Vector128<double> r16 = r08 * r08;

                // Compute log(x + 1) using Polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector128<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector128.Create(C18)) * r02)
                                          + ((r * C17) + Vector128.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector128.Create(C14)) * r02)
                                          + ((r * C13) + Vector128.Create(C12))) * r04)
                                        + ((((r * C11) + Vector128.Create(C10)) * r02)
                                          + ((r * C09) + Vector128.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector128.Create(C06)) * r02)
                                          + ((r * C05) + Vector128.Create(C04))) * r04)
                                        + ((((r * C03) + Vector128.Create(C02)) * r02) + r);

                return Vector128.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
                );
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector256<ulong> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt64() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<ulong>.Zero)
                {
                    Vector256<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector256<double> lessThanZeroMask = Vector256.LessThan(xBits, Vector256<long>.Zero).AsDouble();

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector256<double> zeroMask = Vector256.Equals(xBits << 1, Vector256<long>.Zero).AsDouble();

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector256<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector256.GreaterThanOrEqual(xBits, Vector256.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector256<double> subnormalMask = Vector256.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector256.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector256<ulong> vx = x.AsUInt64() - Vector256.Create(V_OFF);
                Vector256<double> n = Vector256.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector256.Create(V_MSK)) + Vector256.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector256<double> r = vx.AsDouble() - Vector256<double>.One;

                Vector256<double> r02 = r * r;
                Vector256<double> r04 = r02 * r02;
                Vector256<double> r08 = r04 * r04;
                Vector256<double> r16 = r08 * r08;

                // Compute log(x + 1) using Polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector256<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector256.Create(C18)) * r02)
                                          + ((r * C17) + Vector256.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector256.Create(C14)) * r02)
                                          + ((r * C13) + Vector256.Create(C12))) * r04)
                                        + ((((r * C11) + Vector256.Create(C10)) * r02)
                                          + ((r * C09) + Vector256.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector256.Create(C06)) * r02)
                                          + ((r * C05) + Vector256.Create(C04))) * r04)
                                        + ((((r * C03) + Vector256.Create(C02)) * r02) + r);

                return Vector256.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
                );
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector512<ulong> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt64() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<ulong>.Zero)
                {
                    Vector512<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector512<double> lessThanZeroMask = Vector512.LessThan(xBits, Vector512<long>.Zero).AsDouble();

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector512<double> zeroMask = Vector512.Equals(xBits << 1, Vector512<long>.Zero).AsDouble();

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector512<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector512.GreaterThanOrEqual(xBits, Vector512.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector512<double> subnormalMask = Vector512.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector512.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector512<ulong> vx = x.AsUInt64() - Vector512.Create(V_OFF);
                Vector512<double> n = Vector512.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector512.Create(V_MSK)) + Vector512.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector512<double> r = vx.AsDouble() - Vector512<double>.One;

                Vector512<double> r02 = r * r;
                Vector512<double> r04 = r02 * r02;
                Vector512<double> r08 = r04 * r04;
                Vector512<double> r16 = r08 * r08;

                // Compute log(x + 1) using Polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector512<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector512.Create(C18)) * r02)
                                          + ((r * C17) + Vector512.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector512.Create(C14)) * r02)
                                          + ((r * C13) + Vector512.Create(C12))) * r04)
                                        + ((((r * C11) + Vector512.Create(C10)) * r02)
                                          + ((r * C09) + Vector512.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector512.Create(C06)) * r02)
                                          + ((r * C05) + Vector512.Create(C04))) * r04)
                                        + ((((r * C03) + Vector512.Create(C02)) * r02) + r);

                return Vector512.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
                );
            }
        }

        /// <summary>float.Log(x)</summary>
        private readonly struct LogOperatorSingle : IUnaryOperator<float, float>
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

            private const uint V_MIN = 0x00800000;
            private const uint V_MAX = 0x7F800000;
            private const uint V_MASK = 0x007FFFFF;
            private const uint V_OFF = 0x3F2AAAAB;

            private const float V_LN2 = 0.6931472f;

            private const float C0 = 0.0f;
            private const float C1 = 1.0f;
            private const float C2 = -0.5000001f;
            private const float C3 = 0.33332965f;
            private const float C4 = -0.24999046f;
            private const float C5 = 0.20018855f;
            private const float C6 = -0.16700386f;
            private const float C7 = 0.13902695f;
            private const float C8 = -0.1197452f;
            private const float C9 = 0.14401625f;
            private const float C10 = -0.13657966f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Log(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector128<uint> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt32() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector128<float> zeroMask = Vector128.Equals(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector128<float> lessThanZeroMask = Vector128.LessThan(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector128<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector128.Equals(x, x)
                                          | Vector128.Equals(x, Vector128.Create(float.PositiveInfinity));

                    // subnormal
                    Vector128<float> subnormalMask = Vector128.AndNot(specialMask.AsSingle(), temp);

                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector128.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector128<uint> vx = x.AsUInt32() - Vector128.Create(V_OFF);
                Vector128<float> n = Vector128.ConvertToSingle(Vector128.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector128.Create(V_MASK)) + Vector128.Create(V_OFF);

                Vector128<float> r = vx.AsSingle() - Vector128<float>.One;

                Vector128<float> r2 = r * r;
                Vector128<float> r4 = r2 * r2;
                Vector128<float> r8 = r4 * r4;

                Vector128<float> q = (Vector128.Create(C10) * r2 + (Vector128.Create(C9) * r + Vector128.Create(C8)))
                                                          * r8 + (((Vector128.Create(C7) * r + Vector128.Create(C6))
                                                            * r2 + (Vector128.Create(C5) * r + Vector128.Create(C4)))
                                                           * r4 + ((Vector128.Create(C3) * r + Vector128.Create(C2))
                                                            * r2 + (Vector128.Create(C1) * r + Vector128.Create(C0))));

                return Vector128.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector128.Create(V_LN2) + q
                );
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector256<uint> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt32() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector256<float> zeroMask = Vector256.Equals(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector256<float> lessThanZeroMask = Vector256.LessThan(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector256<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector256.Equals(x, x)
                                          | Vector256.Equals(x, Vector256.Create(float.PositiveInfinity));

                    // subnormal
                    Vector256<float> subnormalMask = Vector256.AndNot(specialMask.AsSingle(), temp);

                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector256.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector256<uint> vx = x.AsUInt32() - Vector256.Create(V_OFF);
                Vector256<float> n = Vector256.ConvertToSingle(Vector256.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector256.Create(V_MASK)) + Vector256.Create(V_OFF);

                Vector256<float> r = vx.AsSingle() - Vector256<float>.One;

                Vector256<float> r2 = r * r;
                Vector256<float> r4 = r2 * r2;
                Vector256<float> r8 = r4 * r4;

                Vector256<float> q = (Vector256.Create(C10) * r2 + (Vector256.Create(C9) * r + Vector256.Create(C8)))
                                                          * r8 + (((Vector256.Create(C7) * r + Vector256.Create(C6))
                                                            * r2 + (Vector256.Create(C5) * r + Vector256.Create(C4)))
                                                           * r4 + ((Vector256.Create(C3) * r + Vector256.Create(C2))
                                                            * r2 + (Vector256.Create(C1) * r + Vector256.Create(C0))));

                return Vector256.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector256.Create(V_LN2) + q
                );
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector512<uint> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt32() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector512<float> zeroMask = Vector512.Equals(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector512<float> lessThanZeroMask = Vector512.LessThan(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector512<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector512.Equals(x, x)
                                          | Vector512.Equals(x, Vector512.Create(float.PositiveInfinity));

                    // subnormal
                    Vector512<float> subnormalMask = Vector512.AndNot(specialMask.AsSingle(), temp);

                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector512.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector512<uint> vx = x.AsUInt32() - Vector512.Create(V_OFF);
                Vector512<float> n = Vector512.ConvertToSingle(Vector512.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector512.Create(V_MASK)) + Vector512.Create(V_OFF);

                Vector512<float> r = vx.AsSingle() - Vector512<float>.One;

                Vector512<float> r2 = r * r;
                Vector512<float> r4 = r2 * r2;
                Vector512<float> r8 = r4 * r4;

                Vector512<float> q = (Vector512.Create(C10) * r2 + (Vector512.Create(C9) * r + Vector512.Create(C8)))
                                                          * r8 + (((Vector512.Create(C7) * r + Vector512.Create(C6))
                                                            * r2 + (Vector512.Create(C5) * r + Vector512.Create(C4)))
                                                           * r4 + ((Vector512.Create(C3) * r + Vector512.Create(C2))
                                                            * r2 + (Vector512.Create(C1) * r + Vector512.Create(C0))));

                return Vector512.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector512.Create(V_LN2) + q
                );
            }
        }
#endif
    }
}
