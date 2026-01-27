// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise angle in radians whose tangent is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AtanOperator<T>>(x, destination);

        /// <summary>T.Atan(x)</summary>
        internal readonly struct AtanOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_atanf` and `vrd2_atan` from amd/aocl-libm-ose
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
            // All elements are approximated by using polynomial

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Atan(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Atan(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Atan(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AtanDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AtanSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Atan(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Atan(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AtanDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AtanSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Atan(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Atan(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AtanDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AtanSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> AtanDouble(Vector128<double> x)
            {
                // Polynomial coefficients from AMD aocl-libm-ose
                Vector128<double> C0 = Vector128.Create(-0.33333333333333265);
                Vector128<double> C1 = Vector128.Create(0.19999999999969587);
                Vector128<double> C2 = Vector128.Create(-0.1428571428026078);
                Vector128<double> C3 = Vector128.Create(0.11111110613838616);
                Vector128<double> C4 = Vector128.Create(-0.09090882990193784);
                Vector128<double> C5 = Vector128.Create(0.07691470703415716);
                Vector128<double> C6 = Vector128.Create(-0.06649949387937557);
                Vector128<double> C7 = Vector128.Create(0.05677994137264123);
                Vector128<double> C8 = Vector128.Create(-0.038358962102069113);

                Vector128<double> SQRT3 = Vector128.Create(1.7320508075688772);
                Vector128<double> RANGE = Vector128.Create(0.2679491924311227);
                Vector128<double> PI_BY_6 = Vector128.Create(0.5235987755982989);
                Vector128<double> PI_BY_2 = Vector128.Create(1.5707963267948966);
                Vector128<double> PI_BY_3 = Vector128.Create(1.0471975511965979);

                Vector128<double> sign = x & Vector128.Create(-0.0);
                Vector128<double> ax = Vector128.Abs(x);

                Vector128<double> F = Vector128.Create(1.0 / 0.2679491924311227);

                Vector128<double> cmp1 = Vector128.GreaterThanOrEqual(ax, F);
                Vector128<double> cmp2 = Vector128.GreaterThan(ax, Vector128<double>.One);
                Vector128<double> cmp3 = Vector128.GreaterThan(ax, RANGE);

                Vector128<double> aux1 = Vector128<double>.One / ax;
                Vector128<double> pival1 = PI_BY_2;

                Vector128<double> recip = Vector128<double>.One / ax;
                Vector128<double> aux2 = (recip * SQRT3 - Vector128<double>.One) / (SQRT3 + recip);
                Vector128<double> pival2 = PI_BY_3;

                Vector128<double> aux3 = (ax * SQRT3 - Vector128<double>.One) / (SQRT3 + ax);
                Vector128<double> pival3 = PI_BY_6;

                Vector128<double> aux4 = ax;
                Vector128<double> pival4 = Vector128<double>.Zero;

                Vector128<double> aux = Vector128.ConditionalSelect(cmp1, aux1,
                                        Vector128.ConditionalSelect(cmp2, aux2,
                                        Vector128.ConditionalSelect(cmp3, aux3, aux4)));

                Vector128<double> pival = Vector128.ConditionalSelect(cmp1, pival1,
                                          Vector128.ConditionalSelect(cmp2, pival2,
                                          Vector128.ConditionalSelect(cmp3, pival3, pival4)));

                Vector128<double> polysignMask = Vector128.ConditionalSelect(cmp1 | cmp2, Vector128.Create(-0.0), Vector128<double>.Zero);

                Vector128<double> aux2_poly = aux * aux;
                Vector128<double> poly = C8;
                poly = poly * aux2_poly + C7;
                poly = poly * aux2_poly + C6;
                poly = poly * aux2_poly + C5;
                poly = poly * aux2_poly + C4;
                poly = poly * aux2_poly + C3;
                poly = poly * aux2_poly + C2;
                poly = poly * aux2_poly + C1;
                poly = poly * aux2_poly + C0;

                poly = poly * aux2_poly * aux + aux;

                poly ^= polysignMask;

                Vector128<double> result = pival + poly;

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> AtanSingle(Vector128<float> x)
            {
                // Polynomial coefficients from AMD vrs4_atanf
                Vector128<float> C0 = Vector128.Create(-0.33332422375679016f);
                Vector128<float> C1 = Vector128.Create(0.199357271194458f);
                Vector128<float> C2 = Vector128.Create(-0.1281333565711975f);

                Vector128<float> SQRT3 = Vector128.Create(1.7320507764816284f);
                Vector128<float> RANGE = Vector128.Create(0.2679491937160492f);
                Vector128<float> PI_BY_6 = Vector128.Create(0.5235987901687622f);
                Vector128<float> PI_BY_2 = Vector128.Create(1.5707963705062866f);
                Vector128<float> PI_BY_3 = Vector128.Create(1.0471975803375244f);

                Vector128<float> sign = x & Vector128.Create(-0.0f);
                Vector128<float> ax = Vector128.Abs(x);

                Vector128<float> F = Vector128.Create(1.0f / 0.2679491937160492f);

                Vector128<float> cmp1 = Vector128.GreaterThanOrEqual(ax, F);
                Vector128<float> cmp2 = Vector128.GreaterThan(ax, Vector128<float>.One);
                Vector128<float> cmp3 = Vector128.GreaterThan(ax, RANGE);

                Vector128<float> aux1 = Vector128<float>.One / ax;
                Vector128<float> pival1 = PI_BY_2;

                Vector128<float> recip = Vector128<float>.One / ax;
                Vector128<float> aux2 = (recip * SQRT3 - Vector128<float>.One) / (SQRT3 + recip);
                Vector128<float> pival2 = PI_BY_3;

                Vector128<float> aux3 = (ax * SQRT3 - Vector128<float>.One) / (SQRT3 + ax);
                Vector128<float> pival3 = PI_BY_6;

                Vector128<float> aux4 = ax;
                Vector128<float> pival4 = Vector128<float>.Zero;

                Vector128<float> aux = Vector128.ConditionalSelect(cmp1, aux1,
                                       Vector128.ConditionalSelect(cmp2, aux2,
                                       Vector128.ConditionalSelect(cmp3, aux3, aux4)));

                Vector128<float> pival = Vector128.ConditionalSelect(cmp1, pival1,
                                         Vector128.ConditionalSelect(cmp2, pival2,
                                         Vector128.ConditionalSelect(cmp3, pival3, pival4)));

                Vector128<float> polysignMask = Vector128.ConditionalSelect(cmp1 | cmp2, Vector128.Create(-0.0f), Vector128<float>.Zero);

                Vector128<float> aux2_poly = aux * aux;
                Vector128<float> poly = C2;
                poly = poly * aux2_poly + C1;
                poly = poly * aux2_poly + C0;

                poly = poly * aux2_poly * aux + aux;

                poly ^= polysignMask;

                Vector128<float> result = pival + poly;

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> AtanDouble(Vector256<double> x)
            {
                Vector256<double> C0 = Vector256.Create(-0.33333333333333265);
                Vector256<double> C1 = Vector256.Create(0.19999999999969587);
                Vector256<double> C2 = Vector256.Create(-0.1428571428026078);
                Vector256<double> C3 = Vector256.Create(0.11111110613838616);
                Vector256<double> C4 = Vector256.Create(-0.09090882990193784);
                Vector256<double> C5 = Vector256.Create(0.07691470703415716);
                Vector256<double> C6 = Vector256.Create(-0.06649949387937557);
                Vector256<double> C7 = Vector256.Create(0.05677994137264123);
                Vector256<double> C8 = Vector256.Create(-0.038358962102069113);

                Vector256<double> SQRT3 = Vector256.Create(1.7320508075688772);
                Vector256<double> RANGE = Vector256.Create(0.2679491924311227);
                Vector256<double> PI_BY_6 = Vector256.Create(0.5235987755982989);
                Vector256<double> PI_BY_2 = Vector256.Create(1.5707963267948966);
                Vector256<double> PI_BY_3 = Vector256.Create(1.0471975511965979);

                Vector256<double> sign = x & Vector256.Create(-0.0);
                Vector256<double> ax = Vector256.Abs(x);

                Vector256<double> F = Vector256.Create(1.0 / 0.2679491924311227);

                Vector256<double> cmp1 = Vector256.GreaterThanOrEqual(ax, F);
                Vector256<double> cmp2 = Vector256.GreaterThan(ax, Vector256<double>.One);
                Vector256<double> cmp3 = Vector256.GreaterThan(ax, RANGE);

                Vector256<double> aux1 = Vector256<double>.One / ax;
                Vector256<double> pival1 = PI_BY_2;

                Vector256<double> recip = Vector256<double>.One / ax;
                Vector256<double> aux2 = (recip * SQRT3 - Vector256<double>.One) / (SQRT3 + recip);
                Vector256<double> pival2 = PI_BY_3;

                Vector256<double> aux3 = (ax * SQRT3 - Vector256<double>.One) / (SQRT3 + ax);
                Vector256<double> pival3 = PI_BY_6;

                Vector256<double> aux4 = ax;
                Vector256<double> pival4 = Vector256<double>.Zero;

                Vector256<double> aux = Vector256.ConditionalSelect(cmp1, aux1,
                                        Vector256.ConditionalSelect(cmp2, aux2,
                                        Vector256.ConditionalSelect(cmp3, aux3, aux4)));

                Vector256<double> pival = Vector256.ConditionalSelect(cmp1, pival1,
                                          Vector256.ConditionalSelect(cmp2, pival2,
                                          Vector256.ConditionalSelect(cmp3, pival3, pival4)));

                Vector256<double> polysignMask = Vector256.ConditionalSelect(cmp1 | cmp2, Vector256.Create(-0.0), Vector256<double>.Zero);

                Vector256<double> aux2_poly = aux * aux;
                Vector256<double> poly = C8;
                poly = poly * aux2_poly + C7;
                poly = poly * aux2_poly + C6;
                poly = poly * aux2_poly + C5;
                poly = poly * aux2_poly + C4;
                poly = poly * aux2_poly + C3;
                poly = poly * aux2_poly + C2;
                poly = poly * aux2_poly + C1;
                poly = poly * aux2_poly + C0;

                poly = poly * aux2_poly * aux + aux;

                poly ^= polysignMask;

                Vector256<double> result = pival + poly;

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> AtanSingle(Vector256<float> x)
            {
                Vector256<float> C0 = Vector256.Create(-0.33332422375679016f);
                Vector256<float> C1 = Vector256.Create(0.199357271194458f);
                Vector256<float> C2 = Vector256.Create(-0.1281333565711975f);

                Vector256<float> SQRT3 = Vector256.Create(1.7320507764816284f);
                Vector256<float> RANGE = Vector256.Create(0.2679491937160492f);
                Vector256<float> PI_BY_6 = Vector256.Create(0.5235987901687622f);
                Vector256<float> PI_BY_2 = Vector256.Create(1.5707963705062866f);
                Vector256<float> PI_BY_3 = Vector256.Create(1.0471975803375244f);

                Vector256<float> sign = x & Vector256.Create(-0.0f);
                Vector256<float> ax = Vector256.Abs(x);

                Vector256<float> F = Vector256.Create(1.0f / 0.2679491937160492f);

                Vector256<float> cmp1 = Vector256.GreaterThanOrEqual(ax, F);
                Vector256<float> cmp2 = Vector256.GreaterThan(ax, Vector256<float>.One);
                Vector256<float> cmp3 = Vector256.GreaterThan(ax, RANGE);

                Vector256<float> aux1 = Vector256<float>.One / ax;
                Vector256<float> pival1 = PI_BY_2;

                Vector256<float> recip = Vector256<float>.One / ax;
                Vector256<float> aux2 = (recip * SQRT3 - Vector256<float>.One) / (SQRT3 + recip);
                Vector256<float> pival2 = PI_BY_3;

                Vector256<float> aux3 = (ax * SQRT3 - Vector256<float>.One) / (SQRT3 + ax);
                Vector256<float> pival3 = PI_BY_6;

                Vector256<float> aux4 = ax;
                Vector256<float> pival4 = Vector256<float>.Zero;

                Vector256<float> aux = Vector256.ConditionalSelect(cmp1, aux1,
                                       Vector256.ConditionalSelect(cmp2, aux2,
                                       Vector256.ConditionalSelect(cmp3, aux3, aux4)));

                Vector256<float> pival = Vector256.ConditionalSelect(cmp1, pival1,
                                         Vector256.ConditionalSelect(cmp2, pival2,
                                         Vector256.ConditionalSelect(cmp3, pival3, pival4)));

                Vector256<float> polysignMask = Vector256.ConditionalSelect(cmp1 | cmp2, Vector256.Create(-0.0f), Vector256<float>.Zero);

                Vector256<float> aux2_poly = aux * aux;
                Vector256<float> poly = C2;
                poly = poly * aux2_poly + C1;
                poly = poly * aux2_poly + C0;

                poly = poly * aux2_poly * aux + aux;

                poly ^= polysignMask;

                Vector256<float> result = pival + poly;

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> AtanDouble(Vector512<double> x)
            {
                Vector512<double> C0 = Vector512.Create(-0.33333333333333265);
                Vector512<double> C1 = Vector512.Create(0.19999999999969587);
                Vector512<double> C2 = Vector512.Create(-0.1428571428026078);
                Vector512<double> C3 = Vector512.Create(0.11111110613838616);
                Vector512<double> C4 = Vector512.Create(-0.09090882990193784);
                Vector512<double> C5 = Vector512.Create(0.07691470703415716);
                Vector512<double> C6 = Vector512.Create(-0.06649949387937557);
                Vector512<double> C7 = Vector512.Create(0.05677994137264123);
                Vector512<double> C8 = Vector512.Create(-0.038358962102069113);

                Vector512<double> SQRT3 = Vector512.Create(1.7320508075688772);
                Vector512<double> RANGE = Vector512.Create(0.2679491924311227);
                Vector512<double> PI_BY_6 = Vector512.Create(0.5235987755982989);
                Vector512<double> PI_BY_2 = Vector512.Create(1.5707963267948966);
                Vector512<double> PI_BY_3 = Vector512.Create(1.0471975511965979);

                Vector512<double> sign = x & Vector512.Create(-0.0);
                Vector512<double> ax = Vector512.Abs(x);

                Vector512<double> F = Vector512.Create(1.0 / 0.2679491924311227);

                Vector512<double> cmp1 = Vector512.GreaterThanOrEqual(ax, F);
                Vector512<double> cmp2 = Vector512.GreaterThan(ax, Vector512<double>.One);
                Vector512<double> cmp3 = Vector512.GreaterThan(ax, RANGE);

                Vector512<double> aux1 = Vector512<double>.One / ax;
                Vector512<double> pival1 = PI_BY_2;

                Vector512<double> recip = Vector512<double>.One / ax;
                Vector512<double> aux2 = (recip * SQRT3 - Vector512<double>.One) / (SQRT3 + recip);
                Vector512<double> pival2 = PI_BY_3;

                Vector512<double> aux3 = (ax * SQRT3 - Vector512<double>.One) / (SQRT3 + ax);
                Vector512<double> pival3 = PI_BY_6;

                Vector512<double> aux4 = ax;
                Vector512<double> pival4 = Vector512<double>.Zero;

                Vector512<double> aux = Vector512.ConditionalSelect(cmp1, aux1,
                                        Vector512.ConditionalSelect(cmp2, aux2,
                                        Vector512.ConditionalSelect(cmp3, aux3, aux4)));

                Vector512<double> pival = Vector512.ConditionalSelect(cmp1, pival1,
                                          Vector512.ConditionalSelect(cmp2, pival2,
                                          Vector512.ConditionalSelect(cmp3, pival3, pival4)));

                Vector512<double> polysignMask = Vector512.ConditionalSelect(cmp1 | cmp2, Vector512.Create(-0.0), Vector512<double>.Zero);

                Vector512<double> aux2_poly = aux * aux;
                Vector512<double> poly = C8;
                poly = poly * aux2_poly + C7;
                poly = poly * aux2_poly + C6;
                poly = poly * aux2_poly + C5;
                poly = poly * aux2_poly + C4;
                poly = poly * aux2_poly + C3;
                poly = poly * aux2_poly + C2;
                poly = poly * aux2_poly + C1;
                poly = poly * aux2_poly + C0;

                poly = poly * aux2_poly * aux + aux;

                poly ^= polysignMask;

                Vector512<double> result = pival + poly;

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> AtanSingle(Vector512<float> x)
            {
                Vector512<float> C0 = Vector512.Create(-0.33332422375679016f);
                Vector512<float> C1 = Vector512.Create(0.199357271194458f);
                Vector512<float> C2 = Vector512.Create(-0.1281333565711975f);

                Vector512<float> SQRT3 = Vector512.Create(1.7320507764816284f);
                Vector512<float> RANGE = Vector512.Create(0.2679491937160492f);
                Vector512<float> PI_BY_6 = Vector512.Create(0.5235987901687622f);
                Vector512<float> PI_BY_2 = Vector512.Create(1.5707963705062866f);
                Vector512<float> PI_BY_3 = Vector512.Create(1.0471975803375244f);

                Vector512<float> sign = x & Vector512.Create(-0.0f);
                Vector512<float> ax = Vector512.Abs(x);

                Vector512<float> F = Vector512.Create(1.0f / 0.2679491937160492f);

                Vector512<float> cmp1 = Vector512.GreaterThanOrEqual(ax, F);
                Vector512<float> cmp2 = Vector512.GreaterThan(ax, Vector512<float>.One);
                Vector512<float> cmp3 = Vector512.GreaterThan(ax, RANGE);

                Vector512<float> aux1 = Vector512<float>.One / ax;
                Vector512<float> pival1 = PI_BY_2;

                Vector512<float> recip = Vector512<float>.One / ax;
                Vector512<float> aux2 = (recip * SQRT3 - Vector512<float>.One) / (SQRT3 + recip);
                Vector512<float> pival2 = PI_BY_3;

                Vector512<float> aux3 = (ax * SQRT3 - Vector512<float>.One) / (SQRT3 + ax);
                Vector512<float> pival3 = PI_BY_6;

                Vector512<float> aux4 = ax;
                Vector512<float> pival4 = Vector512<float>.Zero;

                Vector512<float> aux = Vector512.ConditionalSelect(cmp1, aux1,
                                       Vector512.ConditionalSelect(cmp2, aux2,
                                       Vector512.ConditionalSelect(cmp3, aux3, aux4)));

                Vector512<float> pival = Vector512.ConditionalSelect(cmp1, pival1,
                                         Vector512.ConditionalSelect(cmp2, pival2,
                                         Vector512.ConditionalSelect(cmp3, pival3, pival4)));

                Vector512<float> polysignMask = Vector512.ConditionalSelect(cmp1 | cmp2, Vector512.Create(-0.0f), Vector512<float>.Zero);

                Vector512<float> aux2_poly = aux * aux;
                Vector512<float> poly = C2;
                poly = poly * aux2_poly + C1;
                poly = poly * aux2_poly + C0;

                poly = poly * aux2_poly * aux + aux;

                poly ^= polysignMask;

                Vector512<float> result = pival + poly;

                return result | sign;
            }
        }
    }
}
