// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors.</summary>
        /// <param name="y">The first tensor, represented as a span.</param>
        /// <param name="x">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="y" /> must be same as length of <paramref name="x" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />[i], <paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2<T>(ReadOnlySpan<T> y, ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanIntoSpan<T, Atan2Operator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors.</summary>
        /// <param name="y">The first tensor, represented as a span.</param>
        /// <param name="x">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />[i], <paramref name="x" />)</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2<T>(ReadOnlySpan<T> y, T x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarIntoSpan<T, Atan2Operator<T>>(y, x, destination);

        /// <summary>Computes the element-wise arc-tangent for the quotient of two values in the specified tensors.</summary>
        /// <param name="y">The first tensor, represented as a scalar.</param>
        /// <param name="x">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atan2(<paramref name="y" />, <paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atan2<T>(T y, ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeScalarSpanIntoSpan<T, Atan2Operator<T>>(y, x, destination);

        /// <summary>T.Atan2(y, x)</summary>
        private readonly struct Atan2Operator<T> : IBinaryOperator<T>
            where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T y, T x) => T.Atan2(y, x);

            public static Vector128<T> Invoke(Vector128<T> y, Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Atan2(y.AsDouble(), x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Atan2(y.AsSingle(), x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return Atan2Double(y.AsDouble(), x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Atan2Single(y.AsSingle(), x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> y, Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Atan2(y.AsDouble(), x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Atan2(y.AsSingle(), x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return Atan2Double(y.AsDouble(), x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Atan2Single(y.AsSingle(), x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> y, Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Atan2(y.AsDouble(), x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Atan2(y.AsSingle(), x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return Atan2Double(y.AsDouble(), x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Atan2Single(y.AsSingle(), x.AsSingle()).As<float, T>();
                }
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> Atan2Double(Vector128<double> y, Vector128<double> x)
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

                Vector128<double> PI = Vector128.Create(3.141592653589793);
                Vector128<double> SQRT3 = Vector128.Create(1.7320508075688772);
                Vector128<double> RANGE = Vector128.Create(0.2679491924311227);
                Vector128<double> PI_BY_6 = Vector128.Create(0.5235987755982989);
                Vector128<double> PI_BY_2 = Vector128.Create(1.5707963267948966);
                Vector128<double> PI_BY_3 = Vector128.Create(1.0471975511965979);

                Vector128<double> signY = y & Vector128.Create(-0.0);
                Vector128<double> ay = Vector128.Abs(y);
                Vector128<double> ax = Vector128.Abs(x);

                Vector128<double> u = ay / ax;
                Vector128<double> F = Vector128.Create(1.0 / 0.2679491924311227);

                Vector128<double> cmp1 = Vector128.GreaterThanOrEqual(u, F);
                Vector128<double> cmp2 = Vector128.GreaterThan(u, Vector128<double>.One);
                Vector128<double> cmp3 = Vector128.GreaterThan(u, RANGE);

                Vector128<double> aux1 = Vector128<double>.One / u;
                Vector128<double> pival1 = PI_BY_2;

                Vector128<double> aux2 = (aux1 * SQRT3 - Vector128<double>.One) / (SQRT3 + aux1);
                Vector128<double> pival2 = PI_BY_3;

                Vector128<double> aux3 = (u * SQRT3 - Vector128<double>.One) / (SQRT3 + u);
                Vector128<double> pival3 = PI_BY_6;

                Vector128<double> aux4 = u;
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

                Vector128<double> atanU = pival + poly;

                Vector128<double> piAdjust = Vector128.ConditionalSelect(
                    Vector128.GreaterThanOrEqual(y, Vector128<double>.Zero),
                    PI,
                    -PI
                );

                Vector128<double> result = Vector128.ConditionalSelect(
                    Vector128.LessThan(x, Vector128<double>.Zero),
                    atanU + piAdjust,
                    atanU
                );

                return result | signY;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> Atan2Single(Vector128<float> y, Vector128<float> x)
            {
                // Widen to double and use double implementation
                Vector128<double> yLower = Vector128.WidenLower(y);
                Vector128<double> yUpper = Vector128.WidenUpper(y);
                Vector128<double> xLower = Vector128.WidenLower(x);
                Vector128<double> xUpper = Vector128.WidenUpper(x);

                Vector128<double> resultLower = Atan2Double(yLower, xLower);
                Vector128<double> resultUpper = Atan2Double(yUpper, xUpper);

                return Vector128.Narrow(resultLower, resultUpper);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> Atan2Double(Vector256<double> y, Vector256<double> x)
            {
                return Vector256.Create(
                    Atan2Double(y.GetLower(), x.GetLower()),
                    Atan2Double(y.GetUpper(), x.GetUpper())
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> Atan2Single(Vector256<float> y, Vector256<float> x)
            {
                return Vector256.Create(
                    Atan2Single(y.GetLower(), x.GetLower()),
                    Atan2Single(y.GetUpper(), x.GetUpper())
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> Atan2Double(Vector512<double> y, Vector512<double> x)
            {
                return Vector512.Create(
                    Atan2Double(y.GetLower(), x.GetLower()),
                    Atan2Double(y.GetUpper(), x.GetUpper())
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> Atan2Single(Vector512<float> y, Vector512<float> x)
            {
                return Vector512.Create(
                    Atan2Single(y.GetLower(), x.GetLower()),
                    Atan2Single(y.GetUpper(), x.GetUpper())
                );
            }
        }
    }
}
