// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void Reciprocal<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, ReciprocalOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void ReciprocalEstimate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, ReciprocalEstimateOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of the square root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void ReciprocalSqrt<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, ReciprocalSqrtOperator<T>>(x, destination);

        /// <summary>Computes the element-wise reciprocal of the square root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="x"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1 / <paramref name="x" />[i]</c>.
        /// </para>
        /// </remarks>
        public static void ReciprocalSqrtEstimate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, ReciprocalSqrtEstimateOperator<T>>(x, destination);

        private readonly struct ReciprocalOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.One / x;
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128<T>.One / x;
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256<T>.One / x;
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512<T>.One / x;
        }

        private readonly struct ReciprocalSqrtOperator<T> : IUnaryOperator<T, T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.One / T.Sqrt(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128<T>.One / Vector128.Sqrt(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256<T>.One / Vector256.Sqrt(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512<T>.One / Vector512.Sqrt(x);
        }

        private readonly struct ReciprocalEstimateOperator<T> : IUnaryOperator<T, T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.ReciprocalEstimate(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (Sse.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Sse.Reciprocal(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.ReciprocalEstimate(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.ReciprocalEstimate(x.AsDouble()).As<double, T>();
                }

                return Vector128<T>.One / x;
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (Avx.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx.Reciprocal(x.AsSingle()).As<float, T>();
                }

                return Vector256<T>.One / x;
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.Reciprocal14(x.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.Reciprocal14(x.AsDouble()).As<double, T>();
                }

                return Vector512<T>.One / x;
            }
        }

        private readonly struct ReciprocalSqrtEstimateOperator<T> : IUnaryOperator<T, T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.ReciprocalSqrtEstimate(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (Sse.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Sse.ReciprocalSqrt(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.ReciprocalSquareRootEstimate(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.ReciprocalSquareRootEstimate(x.AsDouble()).As<double, T>();
                }

                return Vector128<T>.One / Vector128.Sqrt(x);
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (Avx.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx.ReciprocalSqrt(x.AsSingle()).As<float, T>();
                }

                return Vector256<T>.One / Vector256.Sqrt(x);
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.ReciprocalSqrt14(x.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.ReciprocalSqrt14(x.AsDouble()).As<double, T>();
                }

                return Vector512<T>.One / Vector512.Sqrt(x);
            }
        }
    }
}
