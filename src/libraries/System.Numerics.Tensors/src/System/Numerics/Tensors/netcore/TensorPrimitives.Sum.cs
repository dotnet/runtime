// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the sum of all elements in the specified tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding all elements in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// If any of the values in the input is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Sum<T>(ReadOnlySpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> =>
            Aggregate<T, IdentityOperator<T>, AddOperator<T>>(x);

        /// <summary>Computes the sum of the absolute values of every element in the specified tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the absolute value of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <exception cref="OverflowException"><typeparamref name="T"/> is a signed integer type and <paramref name="x"/> contained a value equal to <typeparamref name="T"/>'s minimum value.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; absoluteValues = ...;
        ///     TensorPrimitives.Abs(x, absoluteValues);
        ///     T result = TensorPrimitives.Sum(absoluteValues);
        /// </c>
        /// but without requiring intermediate storage for the absolute values. It corresponds to the <c>asum</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T SumOfMagnitudes<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T> =>
            Aggregate<T, AbsoluteOperator<T>, AddOperator<T>>(x);

        /// <summary>Computes the sum of the square of every element in the specified tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of adding the square of every element in <paramref name="x"/>, or zero if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; squaredValues = ...;
        ///     TensorPrimitives.Multiply(x, x, squaredValues);
        ///     T result = TensorPrimitives.Sum(squaredValues);
        /// </c>
        /// but without requiring intermediate storage for the squared values.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T SumOfSquares<T>(ReadOnlySpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T> =>
            Aggregate<T, SquaredOperator<T>, AddOperator<T>>(x);

        /// <summary>x * x</summary>
        internal readonly struct SquaredOperator<T> : IUnaryOperator<T, T> where T : IMultiplyOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => x * x;
            public static Vector128<T> Invoke(Vector128<T> x) => x * x;
            public static Vector256<T> Invoke(Vector256<T> x) => x * x;
            public static Vector512<T> Invoke(Vector512<T> x) => x * x;
        }
    }
}
