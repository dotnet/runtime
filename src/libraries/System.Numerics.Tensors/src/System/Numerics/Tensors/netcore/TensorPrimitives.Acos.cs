// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise angle in radians whose cosine is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Acos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Acos<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AcosOperator<T>>(x, destination);

        /// <summary>T.Acos(x)</summary>
        private readonly struct AcosOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
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

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Acos(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Acos(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Acos(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AcosDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AcosSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Acos(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Acos(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AcosDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AcosSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Acos(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Acos(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return AcosDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AcosSingle(x.AsSingle()).As<float, T>();
                }
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> AcosDouble(Vector128<double> x)
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

                Vector128<double> half = Vector128.Create(0.5);
                Vector128<double> negHalf = Vector128.Create(-0.5);

                Vector128<double> needsPositiveTransform = Vector128.GreaterThan(x, half);
                Vector128<double> needsNegativeTransform = Vector128.LessThan(x, negHalf);

                Vector128<double> g_pos = (Vector128<double>.One - x) * half;
                Vector128<double> r_pos = Vector128.Sqrt(g_pos);

                Vector128<double> g_neg = (Vector128<double>.One + x) * half;
                Vector128<double> r_neg = Vector128.Sqrt(g_neg);

                Vector128<double> ax = Vector128.Abs(x);
                Vector128<double> sign = x & Vector128.Create(-0.0);
                Vector128<double> g_mid = ax * ax;
                Vector128<double> r_mid = ax;

                Vector128<double> g = Vector128.ConditionalSelect(needsPositiveTransform, g_pos,
                    Vector128.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
                Vector128<double> r = Vector128.ConditionalSelect(needsPositiveTransform, r_pos,
                    Vector128.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

                Vector128<double> poly = C9;
                poly = poly * g + C8;
                poly = poly * g + C7;
                poly = poly * g + C6;
                poly = poly * g + C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector128<double> asinResult = poly * g * r + r;

                Vector128<double> result = Vector128.ConditionalSelect(needsPositiveTransform,
                    Vector128.Create(2.0) * asinResult,
                    Vector128.ConditionalSelect(needsNegativeTransform,
                        Vector128.Create(3.1415926535897932) - Vector128.Create(2.0) * asinResult,
                        Vector128.Create(1.5707963267948966) - (asinResult | sign)));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> AcosSingle(Vector128<float> x)
            {
                Vector128<float> C1 = Vector128.Create(0.166666672f);
                Vector128<float> C2 = Vector128.Create(0.0749928579f);
                Vector128<float> C3 = Vector128.Create(0.0454382896f);
                Vector128<float> C4 = Vector128.Create(0.0242866669f);
                Vector128<float> C5 = Vector128.Create(0.0428296849f);

                Vector128<float> half = Vector128.Create(0.5f);
                Vector128<float> negHalf = Vector128.Create(-0.5f);

                Vector128<float> needsPositiveTransform = Vector128.GreaterThan(x, half);
                Vector128<float> needsNegativeTransform = Vector128.LessThan(x, negHalf);

                Vector128<float> g_pos = (Vector128<float>.One - x) * half;
                Vector128<float> r_pos = Vector128.Sqrt(g_pos);

                Vector128<float> g_neg = (Vector128<float>.One + x) * half;
                Vector128<float> r_neg = Vector128.Sqrt(g_neg);

                Vector128<float> ax = Vector128.Abs(x);
                Vector128<float> sign = x & Vector128.Create(-0.0f);
                Vector128<float> g_mid = ax * ax;
                Vector128<float> r_mid = ax;

                Vector128<float> g = Vector128.ConditionalSelect(needsPositiveTransform, g_pos,
                    Vector128.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
                Vector128<float> r = Vector128.ConditionalSelect(needsPositiveTransform, r_pos,
                    Vector128.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

                Vector128<float> poly = C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector128<float> asinResult = poly * g * r + r;

                Vector128<float> result = Vector128.ConditionalSelect(needsPositiveTransform,
                    Vector128.Create(2.0f) * asinResult,
                    Vector128.ConditionalSelect(needsNegativeTransform,
                        Vector128.Create(3.14159274f) - Vector128.Create(2.0f) * asinResult,
                        Vector128.Create(1.57079637f) - (asinResult | sign)));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> AcosDouble(Vector256<double> x)
            {
                Vector256<double> C1 = Vector256.Create(0.166666666666664);
                Vector256<double> C2 = Vector256.Create(0.0750000000006397);
                Vector256<double> C3 = Vector256.Create(0.0446428571088065);
                Vector256<double> C4 = Vector256.Create(0.0303819469180048);
                Vector256<double> C5 = Vector256.Create(0.0223717830326408);
                Vector256<double> C6 = Vector256.Create(0.0173549783672646);
                Vector256<double> C7 = Vector256.Create(0.0138887093438824);
                Vector256<double> C8 = Vector256.Create(0.0121483872130308);
                Vector256<double> C9 = Vector256.Create(0.00640855516049134);

                Vector256<double> half = Vector256.Create(0.5);
                Vector256<double> negHalf = Vector256.Create(-0.5);

                Vector256<double> needsPositiveTransform = Vector256.GreaterThan(x, half);
                Vector256<double> needsNegativeTransform = Vector256.LessThan(x, negHalf);

                Vector256<double> g_pos = (Vector256<double>.One - x) * half;
                Vector256<double> r_pos = Vector256.Sqrt(g_pos);

                Vector256<double> g_neg = (Vector256<double>.One + x) * half;
                Vector256<double> r_neg = Vector256.Sqrt(g_neg);

                Vector256<double> ax = Vector256.Abs(x);
                Vector256<double> sign = x & Vector256.Create(-0.0);
                Vector256<double> g_mid = ax * ax;
                Vector256<double> r_mid = ax;

                Vector256<double> g = Vector256.ConditionalSelect(needsPositiveTransform, g_pos,
                    Vector256.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
                Vector256<double> r = Vector256.ConditionalSelect(needsPositiveTransform, r_pos,
                    Vector256.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

                Vector256<double> poly = C9;
                poly = poly * g + C8;
                poly = poly * g + C7;
                poly = poly * g + C6;
                poly = poly * g + C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector256<double> asinResult = poly * g * r + r;

                Vector256<double> result = Vector256.ConditionalSelect(needsPositiveTransform,
                    Vector256.Create(2.0) * asinResult,
                    Vector256.ConditionalSelect(needsNegativeTransform,
                        Vector256.Create(3.1415926535897932) - Vector256.Create(2.0) * asinResult,
                        Vector256.Create(1.5707963267948966) - (asinResult | sign)));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> AcosSingle(Vector256<float> x)
            {
                Vector256<float> C1 = Vector256.Create(0.166666672f);
                Vector256<float> C2 = Vector256.Create(0.0749928579f);
                Vector256<float> C3 = Vector256.Create(0.0454382896f);
                Vector256<float> C4 = Vector256.Create(0.0242866669f);
                Vector256<float> C5 = Vector256.Create(0.0428296849f);

                Vector256<float> half = Vector256.Create(0.5f);
                Vector256<float> negHalf = Vector256.Create(-0.5f);

                Vector256<float> needsPositiveTransform = Vector256.GreaterThan(x, half);
                Vector256<float> needsNegativeTransform = Vector256.LessThan(x, negHalf);

                Vector256<float> g_pos = (Vector256<float>.One - x) * half;
                Vector256<float> r_pos = Vector256.Sqrt(g_pos);

                Vector256<float> g_neg = (Vector256<float>.One + x) * half;
                Vector256<float> r_neg = Vector256.Sqrt(g_neg);

                Vector256<float> ax = Vector256.Abs(x);
                Vector256<float> sign = x & Vector256.Create(-0.0f);
                Vector256<float> g_mid = ax * ax;
                Vector256<float> r_mid = ax;

                Vector256<float> g = Vector256.ConditionalSelect(needsPositiveTransform, g_pos,
                    Vector256.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
                Vector256<float> r = Vector256.ConditionalSelect(needsPositiveTransform, r_pos,
                    Vector256.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

                Vector256<float> poly = C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector256<float> asinResult = poly * g * r + r;

                Vector256<float> result = Vector256.ConditionalSelect(needsPositiveTransform,
                    Vector256.Create(2.0f) * asinResult,
                    Vector256.ConditionalSelect(needsNegativeTransform,
                        Vector256.Create(3.14159274f) - Vector256.Create(2.0f) * asinResult,
                        Vector256.Create(1.57079637f) - (asinResult | sign)));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> AcosDouble(Vector512<double> x)
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

                Vector512<double> half = Vector512.Create(0.5);
                Vector512<double> negHalf = Vector512.Create(-0.5);

                Vector512<double> needsPositiveTransform = Vector512.GreaterThan(x, half);
                Vector512<double> needsNegativeTransform = Vector512.LessThan(x, negHalf);

                Vector512<double> g_pos = (Vector512<double>.One - x) * half;
                Vector512<double> r_pos = Vector512.Sqrt(g_pos);

                Vector512<double> g_neg = (Vector512<double>.One + x) * half;
                Vector512<double> r_neg = Vector512.Sqrt(g_neg);

                Vector512<double> ax = Vector512.Abs(x);
                Vector512<double> sign = x & Vector512.Create(-0.0);
                Vector512<double> g_mid = ax * ax;
                Vector512<double> r_mid = ax;

                Vector512<double> g = Vector512.ConditionalSelect(needsPositiveTransform, g_pos,
                    Vector512.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
                Vector512<double> r = Vector512.ConditionalSelect(needsPositiveTransform, r_pos,
                    Vector512.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

                Vector512<double> poly = C9;
                poly = poly * g + C8;
                poly = poly * g + C7;
                poly = poly * g + C6;
                poly = poly * g + C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector512<double> asinResult = poly * g * r + r;

                Vector512<double> result = Vector512.ConditionalSelect(needsPositiveTransform,
                    Vector512.Create(2.0) * asinResult,
                    Vector512.ConditionalSelect(needsNegativeTransform,
                        Vector512.Create(3.1415926535897932) - Vector512.Create(2.0) * asinResult,
                        Vector512.Create(1.5707963267948966) - (asinResult | sign)));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> AcosSingle(Vector512<float> x)
            {
                Vector512<float> C1 = Vector512.Create(0.166666672f);
                Vector512<float> C2 = Vector512.Create(0.0749928579f);
                Vector512<float> C3 = Vector512.Create(0.0454382896f);
                Vector512<float> C4 = Vector512.Create(0.0242866669f);
                Vector512<float> C5 = Vector512.Create(0.0428296849f);

                Vector512<float> half = Vector512.Create(0.5f);
                Vector512<float> negHalf = Vector512.Create(-0.5f);

                Vector512<float> needsPositiveTransform = Vector512.GreaterThan(x, half);
                Vector512<float> needsNegativeTransform = Vector512.LessThan(x, negHalf);

                Vector512<float> g_pos = (Vector512<float>.One - x) * half;
                Vector512<float> r_pos = Vector512.Sqrt(g_pos);

                Vector512<float> g_neg = (Vector512<float>.One + x) * half;
                Vector512<float> r_neg = Vector512.Sqrt(g_neg);

                Vector512<float> ax = Vector512.Abs(x);
                Vector512<float> sign = x & Vector512.Create(-0.0f);
                Vector512<float> g_mid = ax * ax;
                Vector512<float> r_mid = ax;

                Vector512<float> g = Vector512.ConditionalSelect(needsPositiveTransform, g_pos,
                    Vector512.ConditionalSelect(needsNegativeTransform, g_neg, g_mid));
                Vector512<float> r = Vector512.ConditionalSelect(needsPositiveTransform, r_pos,
                    Vector512.ConditionalSelect(needsNegativeTransform, r_neg, r_mid));

                Vector512<float> poly = C5;
                poly = poly * g + C4;
                poly = poly * g + C3;
                poly = poly * g + C2;
                poly = poly * g + C1;

                Vector512<float> asinResult = poly * g * r + r;

                Vector512<float> result = Vector512.ConditionalSelect(needsPositiveTransform,
                    Vector512.Create(2.0f) * asinResult,
                    Vector512.ConditionalSelect(needsNegativeTransform,
                        Vector512.Create(3.14159274f) - Vector512.Create(2.0f) * asinResult,
                        Vector512.Create(1.57079637f) - (asinResult | sign)));

                return result;
            }
        }
    }
}
