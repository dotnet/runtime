// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The element in <paramref name="x"/> with the largest magnitude (absolute value).</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the positive value is considered to have the larger magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T MaxMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            MinMaxCore<T, MaxMagnitudeOperator<T>>(x);

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MaxMagnitude(<paramref name="x" />[i], <paramref name="y" />[i])</c>.</remarks>
        /// <remarks>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MaxMagnitude<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanSpanIntoSpan<T, MaxMagnitudePropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MaxMagnitude(<paramref name="x" />[i], <paramref name="y" />)</c>.</remarks>
        /// <remarks>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MaxMagnitude<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanScalarIntoSpan<T, MaxMagnitudePropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The minimum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. Negative 0 is considered smaller than positive 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Min<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            MinMaxCore<T, MinOperator<T>>(x);

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs (but NaNs may not be propagated)</summary>
        internal readonly struct MaxMagnitudeOperator<T> : IAggregationOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);

                Vector128<T> result =
                    Vector128.ConditionalSelect(Vector128.Equals(xMag, yMag),
                        Vector128.ConditionalSelect(IsNegative(x), y, x),
                        Vector128.ConditionalSelect(Vector128.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> negativeMagnitudeX = Vector128.LessThan(xMag, Vector128<T>.Zero);
                    Vector128<T> negativeMagnitudeY = Vector128.LessThan(yMag, Vector128<T>.Zero);
                    result = Vector128.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector128.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);

                Vector256<T> result =
                    Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                        Vector256.ConditionalSelect(IsNegative(x), y, x),
                        Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> negativeMagnitudeX = Vector256.LessThan(xMag, Vector256<T>.Zero);
                    Vector256<T> negativeMagnitudeY = Vector256.LessThan(yMag, Vector256<T>.Zero);
                    result = Vector256.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector256.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);

                Vector512<T> result =
                    Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                        Vector512.ConditionalSelect(IsNegative(x), y, x),
                        Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> negativeMagnitudeX = Vector512.LessThan(xMag, Vector512<T>.Zero);
                    Vector512<T> negativeMagnitudeY = Vector512.LessThan(yMag, Vector512<T>.Zero);
                    result = Vector512.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector512.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs</summary>
        internal readonly struct MaxMagnitudePropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                    Vector128.ConditionalSelect(IsNegative(x), y, x),
                                    Vector128.ConditionalSelect(Vector128.GreaterThan(yMag, xMag), y, x)),
                                y),
                            x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                                    Vector256.ConditionalSelect(IsNegative(x), y, x),
                                    Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                        Vector512.ConditionalSelect(Vector512.Equals(y, y),
                            Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                                Vector512.ConditionalSelect(IsNegative(x), y, x),
                                Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }
        }
    }
}
