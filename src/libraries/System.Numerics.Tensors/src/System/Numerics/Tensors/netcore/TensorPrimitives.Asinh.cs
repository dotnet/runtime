// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise hyperbolic arc-sine of the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Asinh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Asinh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, AsinhOperator<T>>(x, destination);

        /// <summary>T.Asinh(x)</summary>
        internal readonly struct AsinhOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            // This code is based on `asinhf` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // asinh(x) = sign(x) * log(|x| + sqrt(x^2 + 1))

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Asinh(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Asinh(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Asinh(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AsinhDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AsinhSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Asinh(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Asinh(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AsinhDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AsinhSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Asinh(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Asinh(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AsinhDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AsinhSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> AsinhDouble(Vector128<double> x)
            {
                const double LN2 = 0.693147180559945309417;
                const double TINY_THRESHOLD = 2.98023223876953125e-08; // 2^-25
                const double LARGE_THRESHOLD = 268435456.0; // 2^28

                Vector128<double> sign = x & Vector128.Create(-0.0);
                Vector128<double> ax = Vector128.Abs(x);

                Vector128<double> tinyMask = Vector128.LessThanOrEqual(ax, Vector128.Create(TINY_THRESHOLD));
                Vector128<double> largeMask = Vector128.GreaterThan(ax, Vector128.Create(LARGE_THRESHOLD));

                Vector128<double> x2 = ax * ax;
                Vector128<double> sqrtArg = x2 + Vector128<double>.One;
                Vector128<double> normal = Vector128.Log(ax + Vector128.Sqrt(sqrtArg));

                Vector128<double> large = Vector128.Create(LN2) + Vector128.Log(ax);

                Vector128<double> result = Vector128.ConditionalSelect(largeMask, large, normal);
                result = Vector128.ConditionalSelect(tinyMask, ax, result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> AsinhSingle(Vector128<float> x)
            {
                const float LN2 = 0.693147180559945309417f;
                const float TINY_THRESHOLD = 2.98023223876953125e-08f;
                const float LARGE_THRESHOLD = 268435456.0f;

                Vector128<float> sign = x & Vector128.Create(-0.0f);
                Vector128<float> ax = Vector128.Abs(x);

                Vector128<float> tinyMask = Vector128.LessThanOrEqual(ax, Vector128.Create(TINY_THRESHOLD));
                Vector128<float> largeMask = Vector128.GreaterThan(ax, Vector128.Create(LARGE_THRESHOLD));

                Vector128<float> x2 = ax * ax;
                Vector128<float> sqrtArg = x2 + Vector128<float>.One;
                Vector128<float> normal = Vector128.Log(ax + Vector128.Sqrt(sqrtArg));

                Vector128<float> large = Vector128.Create(LN2) + Vector128.Log(ax);

                Vector128<float> result = Vector128.ConditionalSelect(largeMask, large, normal);
                result = Vector128.ConditionalSelect(tinyMask, ax, result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> AsinhDouble(Vector256<double> x)
            {
                const double LN2 = 0.693147180559945309417;
                const double TINY_THRESHOLD = 2.98023223876953125e-08;
                const double LARGE_THRESHOLD = 268435456.0;

                Vector256<double> sign = x & Vector256.Create(-0.0);
                Vector256<double> ax = Vector256.Abs(x);

                Vector256<double> tinyMask = Vector256.LessThanOrEqual(ax, Vector256.Create(TINY_THRESHOLD));
                Vector256<double> largeMask = Vector256.GreaterThan(ax, Vector256.Create(LARGE_THRESHOLD));

                Vector256<double> x2 = ax * ax;
                Vector256<double> sqrtArg = x2 + Vector256<double>.One;
                Vector256<double> normal = Vector256.Log(ax + Vector256.Sqrt(sqrtArg));

                Vector256<double> large = Vector256.Create(LN2) + Vector256.Log(ax);

                Vector256<double> result = Vector256.ConditionalSelect(largeMask, large, normal);
                result = Vector256.ConditionalSelect(tinyMask, ax, result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> AsinhSingle(Vector256<float> x)
            {
                const float LN2 = 0.693147180559945309417f;
                const float TINY_THRESHOLD = 2.98023223876953125e-08f;
                const float LARGE_THRESHOLD = 268435456.0f;

                Vector256<float> sign = x & Vector256.Create(-0.0f);
                Vector256<float> ax = Vector256.Abs(x);

                Vector256<float> tinyMask = Vector256.LessThanOrEqual(ax, Vector256.Create(TINY_THRESHOLD));
                Vector256<float> largeMask = Vector256.GreaterThan(ax, Vector256.Create(LARGE_THRESHOLD));

                Vector256<float> x2 = ax * ax;
                Vector256<float> sqrtArg = x2 + Vector256<float>.One;
                Vector256<float> normal = Vector256.Log(ax + Vector256.Sqrt(sqrtArg));

                Vector256<float> large = Vector256.Create(LN2) + Vector256.Log(ax);

                Vector256<float> result = Vector256.ConditionalSelect(largeMask, large, normal);
                result = Vector256.ConditionalSelect(tinyMask, ax, result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> AsinhDouble(Vector512<double> x)
            {
                const double LN2 = 0.693147180559945309417;
                const double TINY_THRESHOLD = 2.98023223876953125e-08;
                const double LARGE_THRESHOLD = 268435456.0;

                Vector512<double> sign = x & Vector512.Create(-0.0);
                Vector512<double> ax = Vector512.Abs(x);

                Vector512<double> tinyMask = Vector512.LessThanOrEqual(ax, Vector512.Create(TINY_THRESHOLD));
                Vector512<double> largeMask = Vector512.GreaterThan(ax, Vector512.Create(LARGE_THRESHOLD));

                Vector512<double> x2 = ax * ax;
                Vector512<double> sqrtArg = x2 + Vector512<double>.One;
                Vector512<double> normal = Vector512.Log(ax + Vector512.Sqrt(sqrtArg));

                Vector512<double> large = Vector512.Create(LN2) + Vector512.Log(ax);

                Vector512<double> result = Vector512.ConditionalSelect(largeMask, large, normal);
                result = Vector512.ConditionalSelect(tinyMask, ax, result);

                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> AsinhSingle(Vector512<float> x)
            {
                const float LN2 = 0.693147180559945309417f;
                const float TINY_THRESHOLD = 2.98023223876953125e-08f;
                const float LARGE_THRESHOLD = 268435456.0f;

                Vector512<float> sign = x & Vector512.Create(-0.0f);
                Vector512<float> ax = Vector512.Abs(x);

                Vector512<float> tinyMask = Vector512.LessThanOrEqual(ax, Vector512.Create(TINY_THRESHOLD));
                Vector512<float> largeMask = Vector512.GreaterThan(ax, Vector512.Create(LARGE_THRESHOLD));

                Vector512<float> x2 = ax * ax;
                Vector512<float> sqrtArg = x2 + Vector512<float>.One;
                Vector512<float> normal = Vector512.Log(ax + Vector512.Sqrt(sqrtArg));

                Vector512<float> large = Vector512.Create(LN2) + Vector512.Log(ax);

                Vector512<float> result = Vector512.ConditionalSelect(largeMask, large, normal);
                result = Vector512.ConditionalSelect(tinyMask, ax, result);

                return result | sign;
            }
        }
    }
}
