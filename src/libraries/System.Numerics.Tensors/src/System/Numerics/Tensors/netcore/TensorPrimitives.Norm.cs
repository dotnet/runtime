// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the Euclidean norm of the specified tensor of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <returns>The norm.</returns>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(x))</c>.
        /// This is often referred to as the Euclidean norm or L2 norm.
        /// It corresponds to the <c>nrm2</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If any of the input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result value is also NaN.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Norm<T>(ReadOnlySpan<T> x)
            where T : IRootFunctions<T> =>
            T.Sqrt(SumOfSquares(x));
    }
}
