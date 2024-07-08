// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise linear interpolation between two values based on the given weight in the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="amount">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" /> and length of <paramref name="amount" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="amount"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Lerp(<paramref name="x" />[i], <paramref name="y" />[i], <paramref name="amount" />[i])</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Lerp<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> amount, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanSpanIntoSpan<T, LerpOperator<T>>(x, y, amount, destination);

        /// <summary>Computes the element-wise linear interpolation between two values based on the given weight in the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="amount">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Lerp(<paramref name="x" />[i], <paramref name="y" />[i], <paramref name="amount" />)</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Lerp<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T amount, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanScalarIntoSpan<T, LerpOperator<T>>(x, y, amount, destination);

        /// <summary>Computes the element-wise linear interpolation between two values based on the given weight in the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="amount">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="amount" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="amount"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Lerp(<paramref name="x" />[i], <paramref name="y" />, <paramref name="amount" />[i])</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Lerp<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> amount, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarSpanIntoSpan<T, LerpOperator<T>>(x, y, amount, destination);

        /// <summary>(x * (1 - z)) + (y * z)</summary>
        private readonly struct LerpOperator<T> : ITernaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            public static T Invoke(T x, T y, T amount) => T.Lerp(x, y, amount);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> amount) => (x * (Vector128<T>.One - amount)) + (y * amount);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> amount) => (x * (Vector256<T>.One - amount)) + (y * amount);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> amount) => (x * (Vector512<T>.One - amount)) + (y * amount);
        }
    }
}
