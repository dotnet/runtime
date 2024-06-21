// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximumNumber` function. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T MinNumber<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            MinMaxCore<T, MinNumberOperator<T>>(x);

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MinNumber(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximumNumber` function. If either value is <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// the other is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MinNumber<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, MinNumberOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.MinNumber(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximumNumber` function. If either value is <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// the other is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void MinNumber<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, MinNumberOperator<T>>(x, y, destination);

        /// <summary>T.MinNumber(x, y)</summary>
        internal readonly struct MinNumberOperator<T> : IAggregationOperator<T> where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => T.MinNumber(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
#if NET9_0_OR_GREATER
                return Vector128.MinNumber(x, y);
#else
                if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
                {
                    return Vector128.ConditionalSelect(
                        (Vector128.Equals(x, y) & IsNegative(x)) | IsNaN(y) | Vector128.LessThan(x, y),
                        x,
                        y
                    );
                }

                return Vector128.Min(x, y);
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
#if NET9_0_OR_GREATER
                return Vector256.MinNumber(x, y);
#else
                if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
                {
                    return Vector256.ConditionalSelect(
                        (Vector256.Equals(x, y) & IsNegative(x)) | IsNaN(y) | Vector256.LessThan(x, y),
                        x,
                        y
                    );
                }

                return Vector256.Min(x, y);
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
#if NET9_0_OR_GREATER
                return Vector512.MinNumber(x, y);
#else
                if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
                {
                    return Vector512.ConditionalSelect(
                        (Vector512.Equals(x, y) & IsNegative(x)) | IsNaN(y) | Vector512.LessThan(x, y),
                        x,
                        y
                    );
                }

                return Vector512.Min(x, y);
#endif
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinNumberOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinNumberOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinNumberOperator<T>>(x);
        }
    }
}
