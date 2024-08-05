// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> must not be empty.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes a sum of <c><typeparamref name="T"/>.Exp(x[i])</c> for all elements in <paramref name="x"/>.
        /// It then effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp(<paramref name="x" />[i]) / sum</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void SoftMax<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            T expSum = Aggregate<T, ExpOperator<T>, AddOperator<T>>(x);

            InvokeSpanScalarIntoSpan<T, ExpOperator<T>, DivideOperator<T>>(x, expSum, destination);
        }
    }
}
