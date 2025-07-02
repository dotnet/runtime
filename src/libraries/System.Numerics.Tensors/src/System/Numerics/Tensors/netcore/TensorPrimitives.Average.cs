// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the mean of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The mean of all elements in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// If any of the input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result value is also NaN.
        /// </para>
        /// </remarks>
        public static T Average<T>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return Sum(x) / T.CreateChecked(x.Length);
        }
    }
}
