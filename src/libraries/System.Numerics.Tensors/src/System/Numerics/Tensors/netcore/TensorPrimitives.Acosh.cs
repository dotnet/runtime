// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise hyperbolic arc-cosine of the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Acosh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Acosh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, AcoshOperator<T>>(x, destination);

        /// <summary>T.Acosh(x)</summary>
        internal readonly struct AcoshOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            // This code is based on `acoshf` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // --------------------
            // acosh(x) = log(x + sqrt(x^2 - 1))
            // Domain: x >= 1

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Acosh(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return AcoshDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AcoshSingle(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return AcoshDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AcoshSingle(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return AcoshDouble(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return AcoshSingle(x.AsSingle()).As<float, T>();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> AcoshDouble(Vector128<double> x)
            {
                const double LN2 = 0.693147180559945309417;
                const double NEAR_ONE_THRESHOLD = 1.0 + 2.98023223876953125e-08; // 1 + 2^-25
                const double LARGE_THRESHOLD = 268435456.0; // 2^28

                Vector128<double> nanMask = Vector128.LessThan(x, Vector128<double>.One);
                Vector128<double> nearOneMask = Vector128.LessThanOrEqual(x, Vector128.Create(NEAR_ONE_THRESHOLD));
                Vector128<double> largeMask = Vector128.GreaterThan(x, Vector128.Create(LARGE_THRESHOLD));

                Vector128<double> x2 = x * x;
                Vector128<double> sqrtArg = x2 - Vector128<double>.One;
                Vector128<double> normal = Vector128.Log(x + Vector128.Sqrt(sqrtArg));

                Vector128<double> large = Vector128.Create(LN2) + Vector128.Log(x);
                Vector128<double> nearOne = Vector128.Sqrt(Vector128.Create(2.0) * (x - Vector128<double>.One));

                Vector128<double> result = Vector128.ConditionalSelect(largeMask, large, normal);
                result = Vector128.ConditionalSelect(nearOneMask, nearOne, result);
                result = Vector128.ConditionalSelect(nanMask, Vector128.Create(double.NaN), result);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> AcoshSingle(Vector128<float> x)
            {
                const float LN2 = 0.693147180559945309417f;
                const float NEAR_ONE_THRESHOLD = 1.0f + 2.98023223876953125e-08f;
                const float LARGE_THRESHOLD = 268435456.0f;

                Vector128<float> nanMask = Vector128.LessThan(x, Vector128<float>.One);
                Vector128<float> nearOneMask = Vector128.LessThanOrEqual(x, Vector128.Create(NEAR_ONE_THRESHOLD));
                Vector128<float> largeMask = Vector128.GreaterThan(x, Vector128.Create(LARGE_THRESHOLD));

                Vector128<float> x2 = x * x;
                Vector128<float> sqrtArg = x2 - Vector128<float>.One;
                Vector128<float> normal = Vector128.Log(x + Vector128.Sqrt(sqrtArg));

                Vector128<float> large = Vector128.Create(LN2) + Vector128.Log(x);
                Vector128<float> nearOne = Vector128.Sqrt(Vector128.Create(2.0f) * (x - Vector128<float>.One));

                Vector128<float> result = Vector128.ConditionalSelect(largeMask, large, normal);
                result = Vector128.ConditionalSelect(nearOneMask, nearOne, result);
                result = Vector128.ConditionalSelect(nanMask, Vector128.Create(float.NaN), result);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> AcoshDouble(Vector256<double> x)
            {
                const double LN2 = 0.693147180559945309417;
                const double NEAR_ONE_THRESHOLD = 1.0 + 2.98023223876953125e-08;
                const double LARGE_THRESHOLD = 268435456.0;

                Vector256<double> nanMask = Vector256.LessThan(x, Vector256<double>.One);
                Vector256<double> nearOneMask = Vector256.LessThanOrEqual(x, Vector256.Create(NEAR_ONE_THRESHOLD));
                Vector256<double> largeMask = Vector256.GreaterThan(x, Vector256.Create(LARGE_THRESHOLD));

                Vector256<double> x2 = x * x;
                Vector256<double> sqrtArg = x2 - Vector256<double>.One;
                Vector256<double> normal = Vector256.Log(x + Vector256.Sqrt(sqrtArg));

                Vector256<double> large = Vector256.Create(LN2) + Vector256.Log(x);
                Vector256<double> nearOne = Vector256.Sqrt(Vector256.Create(2.0) * (x - Vector256<double>.One));

                Vector256<double> result = Vector256.ConditionalSelect(largeMask, large, normal);
                result = Vector256.ConditionalSelect(nearOneMask, nearOne, result);
                result = Vector256.ConditionalSelect(nanMask, Vector256.Create(double.NaN), result);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> AcoshSingle(Vector256<float> x)
            {
                const float LN2 = 0.693147180559945309417f;
                const float NEAR_ONE_THRESHOLD = 1.0f + 2.98023223876953125e-08f;
                const float LARGE_THRESHOLD = 268435456.0f;

                Vector256<float> nanMask = Vector256.LessThan(x, Vector256<float>.One);
                Vector256<float> nearOneMask = Vector256.LessThanOrEqual(x, Vector256.Create(NEAR_ONE_THRESHOLD));
                Vector256<float> largeMask = Vector256.GreaterThan(x, Vector256.Create(LARGE_THRESHOLD));

                Vector256<float> x2 = x * x;
                Vector256<float> sqrtArg = x2 - Vector256<float>.One;
                Vector256<float> normal = Vector256.Log(x + Vector256.Sqrt(sqrtArg));

                Vector256<float> large = Vector256.Create(LN2) + Vector256.Log(x);
                Vector256<float> nearOne = Vector256.Sqrt(Vector256.Create(2.0f) * (x - Vector256<float>.One));

                Vector256<float> result = Vector256.ConditionalSelect(largeMask, large, normal);
                result = Vector256.ConditionalSelect(nearOneMask, nearOne, result);
                result = Vector256.ConditionalSelect(nanMask, Vector256.Create(float.NaN), result);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> AcoshDouble(Vector512<double> x)
            {
                const double LN2 = 0.693147180559945309417;
                const double NEAR_ONE_THRESHOLD = 1.0 + 2.98023223876953125e-08;
                const double LARGE_THRESHOLD = 268435456.0;

                Vector512<double> nanMask = Vector512.LessThan(x, Vector512<double>.One);
                Vector512<double> nearOneMask = Vector512.LessThanOrEqual(x, Vector512.Create(NEAR_ONE_THRESHOLD));
                Vector512<double> largeMask = Vector512.GreaterThan(x, Vector512.Create(LARGE_THRESHOLD));

                Vector512<double> x2 = x * x;
                Vector512<double> sqrtArg = x2 - Vector512<double>.One;
                Vector512<double> normal = Vector512.Log(x + Vector512.Sqrt(sqrtArg));

                Vector512<double> large = Vector512.Create(LN2) + Vector512.Log(x);
                Vector512<double> nearOne = Vector512.Sqrt(Vector512.Create(2.0) * (x - Vector512<double>.One));

                Vector512<double> result = Vector512.ConditionalSelect(largeMask, large, normal);
                result = Vector512.ConditionalSelect(nearOneMask, nearOne, result);
                result = Vector512.ConditionalSelect(nanMask, Vector512.Create(double.NaN), result);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<float> AcoshSingle(Vector512<float> x)
            {
                const float LN2 = 0.693147180559945309417f;
                const float NEAR_ONE_THRESHOLD = 1.0f + 2.98023223876953125e-08f;
                const float LARGE_THRESHOLD = 268435456.0f;

                Vector512<float> nanMask = Vector512.LessThan(x, Vector512<float>.One);
                Vector512<float> nearOneMask = Vector512.LessThanOrEqual(x, Vector512.Create(NEAR_ONE_THRESHOLD));
                Vector512<float> largeMask = Vector512.GreaterThan(x, Vector512.Create(LARGE_THRESHOLD));

                Vector512<float> x2 = x * x;
                Vector512<float> sqrtArg = x2 - Vector512<float>.One;
                Vector512<float> normal = Vector512.Log(x + Vector512.Sqrt(sqrtArg));

                Vector512<float> large = Vector512.Create(LN2) + Vector512.Log(x);
                Vector512<float> nearOne = Vector512.Sqrt(Vector512.Create(2.0f) * (x - Vector512<float>.One));

                Vector512<float> result = Vector512.ConditionalSelect(largeMask, large, normal);
                result = Vector512.ConditionalSelect(nearOneMask, nearOne, result);
                result = Vector512.ConditionalSelect(nanMask, Vector512.Create(float.NaN), result);

                return result;
            }
        }
    }
}
