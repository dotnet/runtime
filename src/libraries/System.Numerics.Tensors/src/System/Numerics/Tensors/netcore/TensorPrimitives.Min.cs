// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Min<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, MinPropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Min<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, MinPropagateNaNOperator<T>>(x, y, destination);

        /// <summary>T.Min(x, y) (but NaNs may not be propagated)</summary>
        internal readonly struct MinOperator<T> : IAggregationOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y)
            {
                if (typeof(T) == typeof(Half) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return x == y ?
                        (IsNegative(y) ? y : x) :
                        (y < x ? y : x);
                }

                return T.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Min(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Min(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Min(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Min(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Min(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Min(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Min(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.Min(x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(y), y, x),
                            Vector128.Min(x, y));
                }

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return Vector256.ConditionalSelect(Vector256.Equals(x, y),
                        Vector256.ConditionalSelect(IsNegative(y), y, x),
                        Vector256.Min(x, y));
                }

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return Vector512.ConditionalSelect(Vector512.Equals(x, y),
                        Vector512.ConditionalSelect(IsNegative(y), y, x),
                        Vector512.Min(x, y));
                }

                return Vector512.Min(x, y);
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
        }

        /// <summary>T.Min(x, y)</summary>
        internal readonly struct MinPropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Min(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Min(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Min(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Min(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Min(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Min(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Min(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.Min(x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                    Vector128.ConditionalSelect(IsNegative(x), x, y),
                                    Vector128.Min(x, y)),
                                y),
                            x);
                }

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                                    Vector256.ConditionalSelect(IsNegative(x), x, y),
                                    Vector256.Min(x, y)),
                                y),
                            x);
                }

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                                    Vector512.ConditionalSelect(IsNegative(x), x, y),
                                    Vector512.Min(x, y)),
                                y),
                            x);
                }

                return Vector512.Min(x, y);
            }
        }
    }
}
