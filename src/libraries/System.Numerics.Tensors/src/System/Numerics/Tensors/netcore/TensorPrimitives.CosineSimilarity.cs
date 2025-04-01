// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public partial class TensorPrimitives
    {
        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The cosine similarity of the two tensors.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>TensorPrimitives.Dot(x, y) / (<typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(x)) * <typeparamref name="T"/>.Sqrt(TensorPrimitives.SumOfSquares(y)).</c>
        /// </para>
        /// <para>
        /// If any element in either input tensor is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, or <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// NaN is returned.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T CosineSimilarity<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where T : IRootFunctions<T> =>
            CosineSimilarityCore(x, y);

        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <remarks>Assumes arguments have already been validated to be non-empty and equal length.</remarks>
        private static T CosineSimilarityCore<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y) where T : IRootFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector512<T> dotProductVector = Vector512<T>.Zero;
                Vector512<T> xSumOfSquaresVector = Vector512<T>.Zero;
                Vector512<T> ySumOfSquaresVector = Vector512<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = 0;
                do
                {
                    Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);

                    i += Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));
                    Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<T>.Count));

                    Vector512<T> remainderMask = CreateRemainderMaskVector512<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector512.Sum(dotProductVector) /
                    (T.Sqrt(Vector512.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector512.Sum(ySumOfSquaresVector)));
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector256<T> dotProductVector = Vector256<T>.Zero;
                Vector256<T> xSumOfSquaresVector = Vector256<T>.Zero;
                Vector256<T> ySumOfSquaresVector = Vector256<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = 0;
                do
                {
                    Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);

                    i += Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));
                    Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<T>.Count));

                    Vector256<T> remainderMask = CreateRemainderMaskVector256<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector256.Sum(dotProductVector) /
                    (T.Sqrt(Vector256.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector256.Sum(ySumOfSquaresVector)));
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector128<T> dotProductVector = Vector128<T>.Zero;
                Vector128<T> xSumOfSquaresVector = Vector128<T>.Zero;
                Vector128<T> ySumOfSquaresVector = Vector128<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = 0;
                do
                {
                    Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);

                    i += Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));
                    Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<T>.Count));

                    Vector128<T> remainderMask = CreateRemainderMaskVector128<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector128.Sum(dotProductVector) /
                    (T.Sqrt(Vector128.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector128.Sum(ySumOfSquaresVector)));
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            T dotProduct = T.Zero, xSumOfSquares = T.Zero, ySumOfSquares = T.Zero;
            for (int i = 0; i < x.Length; i++)
            {
                dotProduct = MultiplyAddEstimateOperator<T>.Invoke(x[i], y[i], dotProduct);
                xSumOfSquares = MultiplyAddEstimateOperator<T>.Invoke(x[i], x[i], xSumOfSquares);
                ySumOfSquares = MultiplyAddEstimateOperator<T>.Invoke(y[i], y[i], ySumOfSquares);
            }

            // Sum(X * Y) / (|X| * |Y|)
            return
                dotProduct /
                (T.Sqrt(xSumOfSquares) * T.Sqrt(ySumOfSquares));
        }
    }
}
