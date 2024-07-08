// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NaN"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the result stored into the corresponding destination location is set to 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, ExpOperator<T>>(x, destination);

        /// <summary>T.Exp(x)</summary>
        internal readonly struct ExpOperator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(double))
                                            || (typeof(T) == typeof(float));

            public static T Invoke(T x) => T.Exp(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Exp(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return ExpOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return ExpOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Exp(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return ExpOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return ExpOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Exp(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return ExpOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return ExpOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }
        }

#if !NET9_0_OR_GREATER
        /// <summary>double.Exp(x)</summary>
        private readonly struct ExpOperatorDouble : IUnaryOperator<double, double>
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

            private const ulong V_ARG_MAX = 0x40862000_00000000;
            private const ulong V_DP64_BIAS = 1023;

            private const double V_EXPF_MIN = -709.782712893384;
            private const double V_EXPF_MAX = +709.782712893384;

            private const double V_EXPF_HUGE = 6755399441055744;
            private const double V_TBL_LN2 = 1.4426950408889634;

            private const double V_LN2_HEAD = +0.693359375;
            private const double V_LN2_TAIL = -0.00021219444005469057;

            private const double C3 = 0.5000000000000018;
            private const double C4 = 0.1666666666666617;
            private const double C5 = 0.04166666666649277;
            private const double C6 = 0.008333333333559272;
            private const double C7 = 0.001388888895122404;
            private const double C8 = 0.00019841269432677495;
            private const double C9 = 2.4801486521374483E-05;
            private const double C10 = 2.7557622532543023E-06;
            private const double C11 = 2.7632293298250954E-07;
            private const double C12 = 2.499430431958571E-08;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Exp(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                // x * (64.0 / ln(2))
                Vector128<double> z = x * Vector128.Create(V_TBL_LN2);

                Vector128<double> dn = z + Vector128.Create(V_EXPF_HUGE);

                // n = (int)z
                Vector128<ulong> n = dn.AsUInt64();

                // dn = (double)n
                dn -= Vector128.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                Vector128<double> r = x - (dn * Vector128.Create(V_LN2_HEAD)) - (dn * Vector128.Create(V_LN2_TAIL));

                Vector128<double> r2 = r * r;
                Vector128<double> r4 = r2 * r2;
                Vector128<double> r8 = r4 * r4;

                // Compute polynomial
                Vector128<double> poly = ((Vector128.Create(C12) * r + Vector128.Create(C11)) * r2 +
                                           Vector128.Create(C10) * r + Vector128.Create(C9)) * r8 +
                                         ((Vector128.Create(C8) * r + Vector128.Create(C7)) * r2 +
                                          (Vector128.Create(C6) * r + Vector128.Create(C5))) * r4 +
                                         ((Vector128.Create(C4) * r + Vector128.Create(C3)) * r2 + (r + Vector128<double>.One));

                // m = (n - j) / 64
                // result = polynomial * 2^m
                Vector128<double> ret = poly * ((n + Vector128.Create(V_DP64_BIAS)) << 52).AsDouble();

                // Check if -709 < vx < 709
                if (Vector128.GreaterThanAny(Vector128.Abs(x).AsUInt64(), Vector128.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                    Vector128<double> infinityMask = Vector128.GreaterThan(x, Vector128.Create(V_EXPF_MAX));

                    ret = Vector128.ConditionalSelect(
                        infinityMask,
                        Vector128.Create(double.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector128.AndNot(ret, Vector128.LessThan(x, Vector128.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                // x * (64.0 / ln(2))
                Vector256<double> z = x * Vector256.Create(V_TBL_LN2);

                Vector256<double> dn = z + Vector256.Create(V_EXPF_HUGE);

                // n = (int)z
                Vector256<ulong> n = dn.AsUInt64();

                // dn = (double)n
                dn -= Vector256.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                Vector256<double> r = x - (dn * Vector256.Create(V_LN2_HEAD)) - (dn * Vector256.Create(V_LN2_TAIL));

                Vector256<double> r2 = r * r;
                Vector256<double> r4 = r2 * r2;
                Vector256<double> r8 = r4 * r4;

                // Compute polynomial
                Vector256<double> poly = ((Vector256.Create(C12) * r + Vector256.Create(C11)) * r2 +
                                           Vector256.Create(C10) * r + Vector256.Create(C9)) * r8 +
                                         ((Vector256.Create(C8) * r + Vector256.Create(C7)) * r2 +
                                          (Vector256.Create(C6) * r + Vector256.Create(C5))) * r4 +
                                         ((Vector256.Create(C4) * r + Vector256.Create(C3)) * r2 + (r + Vector256<double>.One));

                // m = (n - j) / 64
                // result = polynomial * 2^m
                Vector256<double> ret = poly * ((n + Vector256.Create(V_DP64_BIAS)) << 52).AsDouble();

                // Check if -709 < vx < 709
                if (Vector256.GreaterThanAny(Vector256.Abs(x).AsUInt64(), Vector256.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                    Vector256<double> infinityMask = Vector256.GreaterThan(x, Vector256.Create(V_EXPF_MAX));

                    ret = Vector256.ConditionalSelect(
                        infinityMask,
                        Vector256.Create(double.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector256.AndNot(ret, Vector256.LessThan(x, Vector256.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                // x * (64.0 / ln(2))
                Vector512<double> z = x * Vector512.Create(V_TBL_LN2);

                Vector512<double> dn = z + Vector512.Create(V_EXPF_HUGE);

                // n = (int)z
                Vector512<ulong> n = dn.AsUInt64();

                // dn = (double)n
                dn -= Vector512.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                Vector512<double> r = x - (dn * Vector512.Create(V_LN2_HEAD)) - (dn * Vector512.Create(V_LN2_TAIL));

                Vector512<double> r2 = r * r;
                Vector512<double> r4 = r2 * r2;
                Vector512<double> r8 = r4 * r4;

                // Compute polynomial
                Vector512<double> poly = ((Vector512.Create(C12) * r + Vector512.Create(C11)) * r2 +
                                           Vector512.Create(C10) * r + Vector512.Create(C9)) * r8 +
                                         ((Vector512.Create(C8) * r + Vector512.Create(C7)) * r2 +
                                          (Vector512.Create(C6) * r + Vector512.Create(C5))) * r4 +
                                         ((Vector512.Create(C4) * r + Vector512.Create(C3)) * r2 + (r + Vector512<double>.One));

                // m = (n - j) / 64
                // result = polynomial * 2^m
                Vector512<double> ret = poly * ((n + Vector512.Create(V_DP64_BIAS)) << 52).AsDouble();

                // Check if -709 < vx < 709
                if (Vector512.GreaterThanAny(Vector512.Abs(x).AsUInt64(), Vector512.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                    Vector512<double> infinityMask = Vector512.GreaterThan(x, Vector512.Create(V_EXPF_MAX));

                    ret = Vector512.ConditionalSelect(
                        infinityMask,
                        Vector512.Create(double.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector512.AndNot(ret, Vector512.LessThan(x, Vector512.Create(V_EXPF_MIN)));
                }

                return ret;
            }
        }

        /// <summary>float.Exp(x)</summary>
        private readonly struct ExpOperatorSingle : IUnaryOperator<float, float>
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

            private const uint V_ARG_MAX = 0x42AE0000;

            private const float V_EXPF_MIN = -103.97208f;
            private const float V_EXPF_MAX = +88.72284f;

            private const double V_EXPF_HUGE = 6755399441055744;
            private const double V_TBL_LN2 = 1.4426950408889634;

            private const double C1 = 1.0000000754895704;
            private const double C2 = 0.6931472254087585;
            private const double C3 = 0.2402210737432219;
            private const double C4 = 0.05550297297702539;
            private const double C5 = 0.009676036358193323;
            private const double C6 = 0.001341000536524434;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Exp(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                // Convert x to double precision
                (Vector128<double> xl, Vector128<double> xu) = Vector128.Widen(x);

                // x * (64.0 / ln(2))
                Vector128<double> v_tbl_ln2 = Vector128.Create(V_TBL_LN2);

                Vector128<double> zl = xl * v_tbl_ln2;
                Vector128<double> zu = xu * v_tbl_ln2;

                Vector128<double> v_expf_huge = Vector128.Create(V_EXPF_HUGE);

                Vector128<double> dnl = zl + v_expf_huge;
                Vector128<double> dnu = zu + v_expf_huge;

                // n = (int)z
                Vector128<ulong> nl = dnl.AsUInt64();
                Vector128<ulong> nu = dnu.AsUInt64();

                // dn = (double)n
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector128<double> c1 = Vector128.Create(C1);
                Vector128<double> c2 = Vector128.Create(C2);
                Vector128<double> c3 = Vector128.Create(C3);
                Vector128<double> c4 = Vector128.Create(C4);
                Vector128<double> c5 = Vector128.Create(C5);
                Vector128<double> c6 = Vector128.Create(C6);

                Vector128<double> rl = zl - dnl;

                Vector128<double> rl2 = rl * rl;
                Vector128<double> rl4 = rl2 * rl2;

                Vector128<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector128<double> ru = zu - dnu;

                Vector128<double> ru2 = ru * ru;
                Vector128<double> ru4 = ru2 * ru2;

                Vector128<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)(poly + (n << 52))
                Vector128<float> ret = Vector128.Narrow(
                    (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                    (polyu.AsUInt64() + (nu << 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector128.GreaterThanAny(Vector128.Abs(x).AsUInt32(), Vector128.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector128<float> infinityMask = Vector128.GreaterThan(x, Vector128.Create(V_EXPF_MAX));

                    ret = Vector128.ConditionalSelect(
                        infinityMask,
                        Vector128.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector128.AndNot(ret, Vector128.LessThan(x, Vector128.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                // Convert x to double precision
                (Vector256<double> xl, Vector256<double> xu) = Vector256.Widen(x);

                // x * (64.0 / ln(2))
                Vector256<double> v_tbl_ln2 = Vector256.Create(V_TBL_LN2);

                Vector256<double> zl = xl * v_tbl_ln2;
                Vector256<double> zu = xu * v_tbl_ln2;

                Vector256<double> v_expf_huge = Vector256.Create(V_EXPF_HUGE);

                Vector256<double> dnl = zl + v_expf_huge;
                Vector256<double> dnu = zu + v_expf_huge;

                // n = (int)z
                Vector256<ulong> nl = dnl.AsUInt64();
                Vector256<ulong> nu = dnu.AsUInt64();

                // dn = (double)n
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector256<double> c1 = Vector256.Create(C1);
                Vector256<double> c2 = Vector256.Create(C2);
                Vector256<double> c3 = Vector256.Create(C3);
                Vector256<double> c4 = Vector256.Create(C4);
                Vector256<double> c5 = Vector256.Create(C5);
                Vector256<double> c6 = Vector256.Create(C6);

                Vector256<double> rl = zl - dnl;

                Vector256<double> rl2 = rl * rl;
                Vector256<double> rl4 = rl2 * rl2;

                Vector256<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector256<double> ru = zu - dnu;

                Vector256<double> ru2 = ru * ru;
                Vector256<double> ru4 = ru2 * ru2;

                Vector256<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)(poly + (n << 52))
                Vector256<float> ret = Vector256.Narrow(
                    (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                    (polyu.AsUInt64() + (nu << 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector256.GreaterThanAny(Vector256.Abs(x).AsUInt32(), Vector256.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector256<float> infinityMask = Vector256.GreaterThan(x, Vector256.Create(V_EXPF_MAX));

                    ret = Vector256.ConditionalSelect(
                        infinityMask,
                        Vector256.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector256.AndNot(ret, Vector256.LessThan(x, Vector256.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                // Convert x to double precision
                (Vector512<double> xl, Vector512<double> xu) = Vector512.Widen(x);

                // x * (64.0 / ln(2))
                Vector512<double> v_tbl_ln2 = Vector512.Create(V_TBL_LN2);

                Vector512<double> zl = xl * v_tbl_ln2;
                Vector512<double> zu = xu * v_tbl_ln2;

                Vector512<double> v_expf_huge = Vector512.Create(V_EXPF_HUGE);

                Vector512<double> dnl = zl + v_expf_huge;
                Vector512<double> dnu = zu + v_expf_huge;

                // n = (int)z
                Vector512<ulong> nl = dnl.AsUInt64();
                Vector512<ulong> nu = dnu.AsUInt64();

                // dn = (double)n
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector512<double> c1 = Vector512.Create(C1);
                Vector512<double> c2 = Vector512.Create(C2);
                Vector512<double> c3 = Vector512.Create(C3);
                Vector512<double> c4 = Vector512.Create(C4);
                Vector512<double> c5 = Vector512.Create(C5);
                Vector512<double> c6 = Vector512.Create(C6);

                Vector512<double> rl = zl - dnl;

                Vector512<double> rl2 = rl * rl;
                Vector512<double> rl4 = rl2 * rl2;

                Vector512<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector512<double> ru = zu - dnu;

                Vector512<double> ru2 = ru * ru;
                Vector512<double> ru4 = ru2 * ru2;

                Vector512<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)(poly + (n << 52))
                Vector512<float> ret = Vector512.Narrow(
                    (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                    (polyu.AsUInt64() + (nu << 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector512.GreaterThanAny(Vector512.Abs(x).AsUInt32(), Vector512.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector512<float> infinityMask = Vector512.GreaterThan(x, Vector512.Create(V_EXPF_MAX));

                    ret = Vector512.ConditionalSelect(
                        infinityMask,
                        Vector512.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector512.AndNot(ret, Vector512.LessThan(x, Vector512.Create(V_EXPF_MIN)));
                }

                return ret;
            }
        }
#endif
    }
}
