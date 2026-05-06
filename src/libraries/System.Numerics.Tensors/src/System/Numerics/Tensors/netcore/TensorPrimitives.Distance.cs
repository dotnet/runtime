// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The Euclidean distance.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes the equivalent of:
        /// <c>
        ///     Span&lt;T&gt; difference = ...;
        ///     TensorPrimitives.Subtract(x, y, difference);
        ///     T result = <typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(difference));
        /// </c>
        /// but without requiring additional temporary storage for the intermediate differences.
        /// </para>
        /// <para>
        /// If any element in either input tensor is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, NaN is returned.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Distance<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IRootFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            return T.Sqrt(Aggregate<T, SubtractSquaredOperator<T>, AddOperator<T>>(x, y));
        }

        /// <summary>(x - y) * (x - y)</summary>
        internal readonly struct SubtractSquaredOperator<T> : IBinaryOperator<T> where T : ISubtractionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y)
            {
                T tmp = x - y;
                return tmp * tmp;
            }

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> tmp = x - y;
                return tmp * tmp;
            }
        }
    }
}
