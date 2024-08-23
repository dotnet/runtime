// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the dot product of two tensors containing numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The dot product.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes the equivalent of:
        /// <c>
        ///     Span&lt;T&gt; products = ...;
        ///     TensorPrimitives.Multiply(x, y, products);
        ///     T result = TensorPrimitives.Sum(products);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate products. It corresponds to the <c>dot</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If any of the input elements is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Dot<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T> =>
            Aggregate<T, MultiplyOperator<T>, AddOperator<T>>(x, y);
    }
}
