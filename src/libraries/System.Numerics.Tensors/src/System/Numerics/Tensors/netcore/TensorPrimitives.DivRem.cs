// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise quotient and remainder of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="quotientDestination">The quotient destination tensor, represented as a span.</param>
        /// <param name="remainderDestination">The remainder destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="quotientDestination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="remainderDestination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> or <paramref name="y"/> and <paramref name="quotientDestination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> or <paramref name="y"/> and <paramref name="remainderDestination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="quotientDestination" />[i], <paramref name="remainderDestination"/>[i]) = T.DivRem(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise values are also NaN.
        /// </para>
        /// </remarks>
        public static void DivRem<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> quotientDestination, Span<T> remainderDestination)
            where T : IBinaryInteger<T> =>
            InvokeSpanSpanIntoSpanSpan<T, DivRemOperator<T>>(x, y, quotientDestination, remainderDestination);

        /// <summary>Computes the element-wise quotient and remainder of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="quotientDestination">The quotient destination tensor, represented as a span.</param>
        /// <param name="remainderDestination">The remainder destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException"><paramref name="quotientDestination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="remainderDestination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="quotientDestination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="remainderDestination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="quotientDestination" />[i], <paramref name="remainderDestination"/>[i]) = T.DivRem(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise values are also NaN.
        /// </para>
        /// </remarks>
        public static void DivRem<T>(ReadOnlySpan<T> x, T y, Span<T> quotientDestination, Span<T> remainderDestination)
            where T : IBinaryInteger<T> =>
            InvokeSpanScalarIntoSpanSpan<T, DivRemOperator<T>>(x, y, quotientDestination, remainderDestination);

        /// <summary>Computes the element-wise quotient and remainder of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="quotientDestination">The quotient destination tensor, represented as a span.</param>
        /// <param name="remainderDestination">The remainder destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException"><paramref name="quotientDestination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="remainderDestination"/> is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="quotientDestination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="remainderDestination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="quotientDestination" />[i], <paramref name="remainderDestination"/>[i]) = T.DivRem(<paramref name="x" />, <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise values are also NaN.
        /// </para>
        /// </remarks>
        public static void DivRem<T>(T x, ReadOnlySpan<T> y, Span<T> quotientDestination, Span<T> remainderDestination)
            where T : IBinaryInteger<T> =>
            InvokeSpanScalarIntoSpanSpan<T, SwappedBinaryInputBinaryOutput<DivRemOperator<T>, T>>(y, x, quotientDestination, remainderDestination);

        /// <summary>Math.DivRem(x, y)</summary>
        private readonly struct DivRemOperator<T> : IBinaryInputBinaryOutput<T> where T : IBinaryInteger<T>
        {
            public static bool Vectorizable => true;

            public static (T, T) Invoke(T x, T y) => T.DivRem(x, y);

            public static (Vector128<T>, Vector128<T>) Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> quotient = x / y;
                return (quotient, x - (quotient * y));
            }

            public static (Vector256<T>, Vector256<T>) Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> quotient = x / y;
                return (quotient, x - (quotient * y));
            }

            public static (Vector512<T>, Vector512<T>) Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> quotient = x / y;
                return (quotient, x - (quotient * y));
            }

            public static T RemainderMaskValue => T.One;
        }
    }
}
