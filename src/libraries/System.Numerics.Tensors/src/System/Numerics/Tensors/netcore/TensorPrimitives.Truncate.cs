// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Truncate(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Truncate<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, TruncateOperator<T>>(x, destination);

        private readonly struct TruncateOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Truncate(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (Sse41.IsSupported) return Sse41.RoundToZero(x.AsSingle()).As<float, T>();
                    if (AdvSimd.IsSupported) return AdvSimd.RoundToZero(x.AsSingle()).As<float, T>();

                    return Vector128.ConditionalSelect(Vector128.GreaterThanOrEqual(x, Vector128<T>.Zero),
                        Vector128.Floor(x.AsSingle()).As<float, T>(),
                        Vector128.Ceiling(x.AsSingle()).As<float, T>());
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));

                    if (Sse41.IsSupported) return Sse41.RoundToZero(x.AsDouble()).As<double, T>();
                    if (AdvSimd.Arm64.IsSupported) return AdvSimd.Arm64.RoundToZero(x.AsDouble()).As<double, T>();

                    return Vector128.ConditionalSelect(Vector128.GreaterThanOrEqual(x, Vector128<T>.Zero),
                        Vector128.Floor(x.AsDouble()).As<double, T>(),
                        Vector128.Ceiling(x.AsDouble()).As<double, T>());
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (Avx.IsSupported) return Avx.RoundToZero(x.AsSingle()).As<float, T>();

                    return Vector256.ConditionalSelect(Vector256.GreaterThanOrEqual(x, Vector256<T>.Zero),
                        Vector256.Floor(x.AsSingle()).As<float, T>(),
                        Vector256.Ceiling(x.AsSingle()).As<float, T>());
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));

                    if (Avx.IsSupported) return Avx.RoundToZero(x.AsDouble()).As<double, T>();

                    return Vector256.ConditionalSelect(Vector256.GreaterThanOrEqual(x, Vector256<T>.Zero),
                        Vector256.Floor(x.AsDouble()).As<double, T>(),
                        Vector256.Ceiling(x.AsDouble()).As<double, T>());
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (Avx512F.IsSupported) return Avx512F.RoundScale(x.AsSingle(), 0b11).As<float, T>();

                    return Vector512.ConditionalSelect(Vector512.GreaterThanOrEqual(x, Vector512<T>.Zero),
                        Vector512.Floor(x.AsSingle()).As<float, T>(),
                        Vector512.Ceiling(x.AsSingle()).As<float, T>());
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));

                    if (Avx512F.IsSupported) return Avx512F.RoundScale(x.AsDouble(), 0b11).As<double, T>();

                    return Vector512.ConditionalSelect(Vector512.GreaterThanOrEqual(x, Vector512<T>.Zero),
                        Vector512.Floor(x.AsDouble()).As<double, T>(),
                        Vector512.Ceiling(x.AsDouble()).As<double, T>());
                }
            }
        }
    }
}
