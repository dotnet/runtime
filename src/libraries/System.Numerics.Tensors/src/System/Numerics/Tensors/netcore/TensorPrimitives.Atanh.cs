// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise hyperbolic arc-tangent of the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Atanh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Atanh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, AtanhOperator<T>>(x, destination);

        /// <summary>T.Atanh(x)</summary>
        internal readonly struct AtanhOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            // This code is based on `atanhf` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // atanh(x) = 0.5 * log((1 + x) / (1 - x))

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Atanh(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Atanh(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Atanh(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AtanhDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AtanhSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Atanh(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Atanh(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AtanhDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AtanhSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Atanh(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Atanh(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AtanhDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AtanhSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> AtanhDouble(Vector128<double> x)
            {
                const double TINY_THRESHOLD = 2.98023223876953125e-08; // 2^-25

                Vector128<double> sign = x & Vector128.Create(-0.0);
                Vector128<double> ax = Vector128.Abs(x);

                Vector128<double> nanMask = Vector128.GreaterThanOrEqual(ax, Vector128<double>.One);
                Vector128<double> tinyMask = Vector128.LessThanOrEqual(ax, Vector128.Create(TINY_THRESHOLD));

                Vector128<double> onePlusX = Vector128<double>.One + ax;
                Vector128<double> oneMinusX = Vector128<double>.One - ax;
                Vector128<double> ratio = onePlusX / oneMinusX;
                Vector128<double> normal = Vector128.Create(0.5) * Vector128.Log(ratio);

                Vector128<double> result = Vector128.ConditionalSelect(tinyMask, ax, normal);
                result = Vector128.ConditionalSelect(nanMask, Vector128.Create(double.NaN), result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> AtanhSingle(Vector128<float> x)
            {
                const float TINY_THRESHOLD = 2.98023223876953125e-08f;

                Vector128<float> sign = x & Vector128.Create(-0.0f);
                Vector128<float> ax = Vector128.Abs(x);

                Vector128<float> nanMask = Vector128.GreaterThanOrEqual(ax, Vector128<float>.One);
                Vector128<float> tinyMask = Vector128.LessThanOrEqual(ax, Vector128.Create(TINY_THRESHOLD));

                Vector128<float> onePlusX = Vector128<float>.One + ax;
                Vector128<float> oneMinusX = Vector128<float>.One - ax;
                Vector128<float> ratio = onePlusX / oneMinusX;
                Vector128<float> normal = Vector128.Create(0.5f) * Vector128.Log(ratio);

                Vector128<float> result = Vector128.ConditionalSelect(tinyMask, ax, normal);
                result = Vector128.ConditionalSelect(nanMask, Vector128.Create(float.NaN), result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> AtanhDouble(Vector256<double> x)
            {
                const double TINY_THRESHOLD = 2.98023223876953125e-08;

                Vector256<double> sign = x & Vector256.Create(-0.0);
                Vector256<double> ax = Vector256.Abs(x);

                Vector256<double> nanMask = Vector256.GreaterThanOrEqual(ax, Vector256<double>.One);
                Vector256<double> tinyMask = Vector256.LessThanOrEqual(ax, Vector256.Create(TINY_THRESHOLD));

                Vector256<double> onePlusX = Vector256<double>.One + ax;
                Vector256<double> oneMinusX = Vector256<double>.One - ax;
                Vector256<double> ratio = onePlusX / oneMinusX;
                Vector256<double> normal = Vector256.Create(0.5) * Vector256.Log(ratio);

                Vector256<double> result = Vector256.ConditionalSelect(tinyMask, ax, normal);
                result = Vector256.ConditionalSelect(nanMask, Vector256.Create(double.NaN), result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> AtanhSingle(Vector256<float> x)
            {
                const float TINY_THRESHOLD = 2.98023223876953125e-08f;

                Vector256<float> sign = x & Vector256.Create(-0.0f);
                Vector256<float> ax = Vector256.Abs(x);

                Vector256<float> nanMask = Vector256.GreaterThanOrEqual(ax, Vector256<float>.One);
                Vector256<float> tinyMask = Vector256.LessThanOrEqual(ax, Vector256.Create(TINY_THRESHOLD));

                Vector256<float> onePlusX = Vector256<float>.One + ax;
                Vector256<float> oneMinusX = Vector256<float>.One - ax;
                Vector256<float> ratio = onePlusX / oneMinusX;
                Vector256<float> normal = Vector256.Create(0.5f) * Vector256.Log(ratio);

                Vector256<float> result = Vector256.ConditionalSelect(tinyMask, ax, normal);
                result = Vector256.ConditionalSelect(nanMask, Vector256.Create(float.NaN), result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> AtanhDouble(Vector512<double> x)
            {
                const double TINY_THRESHOLD = 2.98023223876953125e-08;

                Vector512<double> sign = x & Vector512.Create(-0.0);
                Vector512<double> ax = Vector512.Abs(x);

                Vector512<double> nanMask = Vector512.GreaterThanOrEqual(ax, Vector512<double>.One);
                Vector512<double> tinyMask = Vector512.LessThanOrEqual(ax, Vector512.Create(TINY_THRESHOLD));

                Vector512<double> onePlusX = Vector512<double>.One + ax;
                Vector512<double> oneMinusX = Vector512<double>.One - ax;
                Vector512<double> ratio = onePlusX / oneMinusX;
                Vector512<double> normal = Vector512.Create(0.5) * Vector512.Log(ratio);

                Vector512<double> result = Vector512.ConditionalSelect(tinyMask, ax, normal);
                result = Vector512.ConditionalSelect(nanMask, Vector512.Create(double.NaN), result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> AtanhSingle(Vector512<float> x)
            {
                const float TINY_THRESHOLD = 2.98023223876953125e-08f;

                Vector512<float> sign = x & Vector512.Create(-0.0f);
                Vector512<float> ax = Vector512.Abs(x);

                Vector512<float> nanMask = Vector512.GreaterThanOrEqual(ax, Vector512<float>.One);
                Vector512<float> tinyMask = Vector512.LessThanOrEqual(ax, Vector512.Create(TINY_THRESHOLD));

                Vector512<float> onePlusX = Vector512<float>.One + ax;
                Vector512<float> oneMinusX = Vector512<float>.One - ax;
                Vector512<float> ratio = onePlusX / oneMinusX;
                Vector512<float> normal = Vector512.Create(0.5f) * Vector512.Log(ratio);

                Vector512<float> result = Vector512.ConditionalSelect(tinyMask, ax, normal);
                result = Vector512.ConditionalSelect(nanMask, Vector512.Create(float.NaN), result);

                return result | sign;
            }
        }
    }
}
