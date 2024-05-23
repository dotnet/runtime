// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c> for the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="multiplier">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" /> and the length of <paramref name="multiplier" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="multiplier"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />[i]) * <paramref name="multiplier" />[i]</c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> multiplier, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanSpanSpanIntoSpan<T, AddMultiplyOperator<T>>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c> for the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="multiplier">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />[i]) * <paramref name="multiplier" /></c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T multiplier, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanSpanScalarIntoSpan<T, AddMultiplyOperator<T>>(x, y, multiplier, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> + <paramref name="y" />) * <paramref name="multiplier" /></c> for the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="multiplier">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="multiplier" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="multiplier"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] + <paramref name="y" />) * <paramref name="multiplier" />[i]</c>.
        /// </para>
        /// <para>
        /// If any of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void AddMultiply<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> multiplier, Span<T> destination)
            where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T> =>
            InvokeSpanScalarSpanIntoSpan<T, AddMultiplyOperator<T>>(x, y, multiplier, destination);

        /// <summary>(x + y) * z</summary>
        internal readonly struct AddMultiplyOperator<T> : ITernaryOperator<T> where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static T Invoke(T x, T y, T z) => (x + y) * z;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z) => (x + y) * z;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z) => (x + y) * z;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z) => (x + y) * z;
        }
    }
}
