// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the standard deviation of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The standard deviation of all elements in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// If any of the input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result value is also NaN.
        /// </para>
        /// </remarks>
        public static T StdDev<T>(ReadOnlySpan<T> x)
            where T : IRootFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T mean = Average(x);
            T sumSquaredDiff = Aggregate<T, SquaredDifferenceOperator<T>, AddOperator<T>>(x, new(mean));
            T variance = sumSquaredDiff / T.CreateChecked(x.Length);
            return T.Sqrt(variance);
        }

        /// <summary>T.RootN(x, n)</summary>
        private readonly struct SquaredDifferenceOperator<T>(T subtrahend) : IStatefulUnaryOperator<T>
            where T : INumberBase<T>
        {
            private readonly T _subtrahend = subtrahend;

            public static bool Vectorizable => true;

            public T Invoke(T x)
            {
                T diff = x - _subtrahend;
                diff = T.Abs(diff); // only relevant to non-vectorizable types
                return diff * diff;
            }

            public Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> diff = x - Vector128.Create(_subtrahend);
                return diff * diff;
            }

            public Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> diff = x - Vector256.Create(_subtrahend);
                return diff * diff;
            }

            public Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> diff = x - Vector512.Create(_subtrahend);
                return diff * diff;
            }
        }
    }
}
