// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The result of multiplying all elements in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// If any of the input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Product<T>(ReadOnlySpan<T> x)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<T, IdentityOperator<T>, MultiplyOperator<T>>(x);
        }

        /// <summary>Computes the product of the element-wise differences of the numbers in the specified non-empty tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise subtraction of the elements in the second tensor from the first tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; differences = ...;
        ///     TensorPrimitives.Subtract(x, y, differences);
        ///     T result = TensorPrimitives.Product(differences);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate differences.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T ProductOfDifferences<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : ISubtractionOperators<T, T, T>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<T, SubtractOperator<T>, MultiplyOperator<T>>(x, y);
        }

        /// <summary>Computes the product of the element-wise sums of the numbers in the specified non-empty tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The result of multiplying the element-wise additions of the elements in each tensor.</returns>
        /// <exception cref="ArgumentException">Length of both input spans must be greater than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> must have the same length.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes:
        /// <c>
        ///     Span&lt;T&gt; sums = ...;
        ///     TensorPrimitives.Add(x, y, sums);
        ///     T result = TensorPrimitives.Product(sums);
        /// </c>
        /// but without requiring additional temporary storage for the intermediate sums.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T ProductOfSums<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Aggregate<T, AddOperator<T>, MultiplyOperator<T>>(x, y);
        }
    }
}
