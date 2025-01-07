// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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
            InvokeSpanSpanIntoSpan<T, MinOperator<T>>(x, y, destination);

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
            InvokeSpanScalarIntoSpan<T, MinOperator<T>>(x, y, destination);

        /// <summary>T.Min(x, y)</summary>
        internal readonly struct MinOperator<T> : IAggregationOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
#if !NET9_0_OR_GREATER
                if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
                {
                    return Vector128.ConditionalSelect(
                        Vector128.LessThan(x, y) | IsNaN(x) | (Vector128.Equals(x, y) & IsNegative(x)),
                        x,
                        y
                    );
                }
#endif

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
#if !NET9_0_OR_GREATER
                if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
                {
                    return Vector256.ConditionalSelect(
                        Vector256.LessThan(x, y) | IsNaN(x) | (Vector256.Equals(x, y) & IsNegative(x)),
                        x,
                        y
                    );
                }
#endif

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
#if !NET9_0_OR_GREATER
                if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
                {
                    return Vector512.ConditionalSelect(
                        Vector512.LessThan(x, y) | IsNaN(x) | (Vector512.Equals(x, y) & IsNegative(x)),
                        x,
                        y
                    );
                }
#endif

                return Vector512.Min(x, y);
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
        }
    }
}
