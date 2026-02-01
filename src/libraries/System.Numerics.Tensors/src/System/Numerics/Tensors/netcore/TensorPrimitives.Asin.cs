// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise angle in radians whose sine is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Asin(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Asin<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AsinOperator<T>>(x, destination);

        /// <summary>T.Asin(x)</summary>
        private readonly struct AsinOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
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
            // The term asin(f) is approximated by using a polynomial

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Asin(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Asin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Asin(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AsinDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AsinSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Asin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Asin(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AsinDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AsinSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Asin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Asin(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AsinDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AsinSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> AsinDouble(Vector128<double> x)
            {
                // Polynomial coefficients from AMD aocl-libm-ose
                Vector128<double> C1 = Vector128.Create(0.166666666666664);
                Vector128<double> C2 = Vector128.Create(0.0750000000006397);
                Vector128<double> C3 = Vector128.Create(0.0446428571088065);
                Vector128<double> C4 = Vector128.Create(0.0303819469180048);
                Vector128<double> C5 = Vector128.Create(0.0223717830326408);
                Vector128<double> C6 = Vector128.Create(0.0173549783672646);
                Vector128<double> C7 = Vector128.Create(0.0138887093438824);
                Vector128<double> C8 = Vector128.Create(0.0121483872130308);
                Vector128<double> C9 = Vector128.Create(0.00640855516049134);

                Vector128<double> sign = x & Vector128.Create(-0.0);
                Vector128<double> ax = Vector128.Abs(x);

                // For |x| > 0.5: g = 0.5*(1.0-|x|), r = -2.0*sqrt(g), n = pi/2
                // For |x| <= 0.5: g = |x|*|x|, r = |x|, n = 0
                Vector128<double> half = Vector128.Create(0.5);
                Vector128<double> needsTransform = Vector128.GreaterThan(ax, half);

                Vector128<double> g_hi = half * (Vector128<double>.One - ax);
                Vector128<double> r_hi = Vector128.Create(-2.0) * Vector128.Sqrt(g_hi);
                Vector128<double> n_hi = Vector128.Create(1.5707963267948966); // pi/2

                Vector128<double> g_lo = ax * ax;
                Vector128<double> r_lo = ax;
                Vector128<double> n_lo = Vector128<double>.Zero;

                Vector128<double> g = Vector128.ConditionalSelect(needsTransform, g_hi, g_lo);
                Vector128<double> r = Vector128.ConditionalSelect(needsTransform, r_hi, r_lo);
                Vector128<double> n = Vector128.ConditionalSelect(needsTransform, n_hi, n_lo);

                // Polynomial evaluation: poly = g * (C1 + g*(C2 + g*(C3 + ... + g*C9)))
                Vector128<double> poly = C9;
                poly = poly * g + C8;
                poly = poly * g + C7;
                poly = poly * g + C6;
                poly = poly * g + C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector128<double> result = poly * g * r + r + n;

                // Restore sign
                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> AsinSingle(Vector128<float> x)
            {
                // Polynomial coefficients from AMD vrs4_asinf
                Vector128<float> C1 = Vector128.Create(0.166666672f);
                Vector128<float> C2 = Vector128.Create(0.0749928579f);
                Vector128<float> C3 = Vector128.Create(0.0454382896f);
                Vector128<float> C4 = Vector128.Create(0.0242866669f);
                Vector128<float> C5 = Vector128.Create(0.0428296849f);

                Vector128<float> sign = x & Vector128.Create(-0.0f);
                Vector128<float> ax = Vector128.Abs(x);

                Vector128<float> half = Vector128.Create(0.5f);
                Vector128<float> needsTransform = Vector128.GreaterThan(ax, half);

                Vector128<float> g_hi = half * (Vector128<float>.One - ax);
                Vector128<float> r_hi = Vector128.Create(-2.0f) * Vector128.Sqrt(g_hi);
                Vector128<float> n_hi = Vector128.Create(1.57079637f); // pi/2

                Vector128<float> g_lo = ax * ax;
                Vector128<float> r_lo = ax;
                Vector128<float> n_lo = Vector128<float>.Zero;

                Vector128<float> g = Vector128.ConditionalSelect(needsTransform, g_hi, g_lo);
                Vector128<float> r = Vector128.ConditionalSelect(needsTransform, r_hi, r_lo);
                Vector128<float> n = Vector128.ConditionalSelect(needsTransform, n_hi, n_lo);

                // Polynomial evaluation
                Vector128<float> poly = C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector128<float> result = poly * g * r + r + n;

                // Restore sign
                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> AsinDouble(Vector256<double> x)
            {
                // Polynomial coefficients from AMD aocl-libm-ose
                Vector256<double> C1 = Vector256.Create(0.166666666666664);
                Vector256<double> C2 = Vector256.Create(0.0750000000006397);
                Vector256<double> C3 = Vector256.Create(0.0446428571088065);
                Vector256<double> C4 = Vector256.Create(0.0303819469180048);
                Vector256<double> C5 = Vector256.Create(0.0223717830326408);
                Vector256<double> C6 = Vector256.Create(0.0173549783672646);
                Vector256<double> C7 = Vector256.Create(0.0138887093438824);
                Vector256<double> C8 = Vector256.Create(0.0121483872130308);
                Vector256<double> C9 = Vector256.Create(0.00640855516049134);

                Vector256<double> sign = x & Vector256.Create(-0.0);
                Vector256<double> ax = Vector256.Abs(x);

                Vector256<double> half = Vector256.Create(0.5);
                Vector256<double> needsTransform = Vector256.GreaterThan(ax, half);

                Vector256<double> g_hi = half * (Vector256<double>.One - ax);
                Vector256<double> r_hi = Vector256.Create(-2.0) * Vector256.Sqrt(g_hi);
                Vector256<double> n_hi = Vector256.Create(1.5707963267948966);

                Vector256<double> g_lo = ax * ax;
                Vector256<double> r_lo = ax;
                Vector256<double> n_lo = Vector256<double>.Zero;

                Vector256<double> g = Vector256.ConditionalSelect(needsTransform, g_hi, g_lo);
                Vector256<double> r = Vector256.ConditionalSelect(needsTransform, r_hi, r_lo);
                Vector256<double> n = Vector256.ConditionalSelect(needsTransform, n_hi, n_lo);

                Vector256<double> poly = C9;
                poly = poly * g + C8;
                poly = poly * g + C7;
                poly = poly * g + C6;
                poly = poly * g + C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector256<double> result = poly * g * r + r + n;
                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> AsinSingle(Vector256<float> x)
            {
                Vector256<float> C1 = Vector256.Create(0.166666672f);
                Vector256<float> C2 = Vector256.Create(0.0749928579f);
                Vector256<float> C3 = Vector256.Create(0.0454382896f);
                Vector256<float> C4 = Vector256.Create(0.0242866669f);
                Vector256<float> C5 = Vector256.Create(0.0428296849f);

                Vector256<float> sign = x & Vector256.Create(-0.0f);
                Vector256<float> ax = Vector256.Abs(x);

                Vector256<float> half = Vector256.Create(0.5f);
                Vector256<float> needsTransform = Vector256.GreaterThan(ax, half);

                Vector256<float> g_hi = half * (Vector256<float>.One - ax);
                Vector256<float> r_hi = Vector256.Create(-2.0f) * Vector256.Sqrt(g_hi);
                Vector256<float> n_hi = Vector256.Create(1.57079637f);

                Vector256<float> g_lo = ax * ax;
                Vector256<float> r_lo = ax;
                Vector256<float> n_lo = Vector256<float>.Zero;

                Vector256<float> g = Vector256.ConditionalSelect(needsTransform, g_hi, g_lo);
                Vector256<float> r = Vector256.ConditionalSelect(needsTransform, r_hi, r_lo);
                Vector256<float> n = Vector256.ConditionalSelect(needsTransform, n_hi, n_lo);

                Vector256<float> poly = C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector256<float> result = poly * g * r + r + n;
                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> AsinDouble(Vector512<double> x)
            {
                Vector512<double> C1 = Vector512.Create(0.166666666666664);
                Vector512<double> C2 = Vector512.Create(0.0750000000006397);
                Vector512<double> C3 = Vector512.Create(0.0446428571088065);
                Vector512<double> C4 = Vector512.Create(0.0303819469180048);
                Vector512<double> C5 = Vector512.Create(0.0223717830326408);
                Vector512<double> C6 = Vector512.Create(0.0173549783672646);
                Vector512<double> C7 = Vector512.Create(0.0138887093438824);
                Vector512<double> C8 = Vector512.Create(0.0121483872130308);
                Vector512<double> C9 = Vector512.Create(0.00640855516049134);

                Vector512<double> sign = x & Vector512.Create(-0.0);
                Vector512<double> ax = Vector512.Abs(x);

                Vector512<double> half = Vector512.Create(0.5);
                Vector512<double> needsTransform = Vector512.GreaterThan(ax, half);

                Vector512<double> g_hi = half * (Vector512<double>.One - ax);
                Vector512<double> r_hi = Vector512.Create(-2.0) * Vector512.Sqrt(g_hi);
                Vector512<double> n_hi = Vector512.Create(1.5707963267948966);

                Vector512<double> g_lo = ax * ax;
                Vector512<double> r_lo = ax;
                Vector512<double> n_lo = Vector512<double>.Zero;

                Vector512<double> g = Vector512.ConditionalSelect(needsTransform, g_hi, g_lo);
                Vector512<double> r = Vector512.ConditionalSelect(needsTransform, r_hi, r_lo);
                Vector512<double> n = Vector512.ConditionalSelect(needsTransform, n_hi, n_lo);

                Vector512<double> poly = C9;
                poly = poly * g + C8;
                poly = poly * g + C7;
                poly = poly * g + C6;
                poly = poly * g + C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector512<double> result = poly * g * r + r + n;
                return result | sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> AsinSingle(Vector512<float> x)
            {
                Vector512<float> C1 = Vector512.Create(0.166666672f);
                Vector512<float> C2 = Vector512.Create(0.0749928579f);
                Vector512<float> C3 = Vector512.Create(0.0454382896f);
                Vector512<float> C4 = Vector512.Create(0.0242866669f);
                Vector512<float> C5 = Vector512.Create(0.0428296849f);

                Vector512<float> sign = x & Vector512.Create(-0.0f);
                Vector512<float> ax = Vector512.Abs(x);

                Vector512<float> half = Vector512.Create(0.5f);
                Vector512<float> needsTransform = Vector512.GreaterThan(ax, half);

                Vector512<float> g_hi = half * (Vector512<float>.One - ax);
                Vector512<float> r_hi = Vector512.Create(-2.0f) * Vector512.Sqrt(g_hi);
                Vector512<float> n_hi = Vector512.Create(1.57079637f);

                Vector512<float> g_lo = ax * ax;
                Vector512<float> r_lo = ax;
                Vector512<float> n_lo = Vector512<float>.Zero;

                Vector512<float> g = Vector512.ConditionalSelect(needsTransform, g_hi, g_lo);
                Vector512<float> r = Vector512.ConditionalSelect(needsTransform, r_hi, r_lo);
                Vector512<float> n = Vector512.ConditionalSelect(needsTransform, n_hi, n_lo);

                Vector512<float> poly = C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector512<float> result = poly * g * r + r + n;
                return result | sign;
            }
        }
    }
}
