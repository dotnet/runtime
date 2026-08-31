// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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
            where T : IRootFunctions<T>
        {
            if (typeof(T) == typeof(Half))
            {
                // Half is not implicitly vectorizable, but we can do so manually.
                return (T)(object)CosineSimilarityHalfCore(Rename<T, Half>(x), Rename<T, Half>(y));
            }

            return CosineSimilarityCore(x, y);
        }

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
                    Update(
                        Vector512.LoadUnsafe(ref xRef, (uint)i),
                        Vector512.LoadUnsafe(ref yRef, (uint)i),
                        ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);

                    i += Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector512<T> remainderMask = CreateRemainderMaskVector512<T>(x.Length - i);

                    Update(
                        Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count)) & remainderMask,
                        Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<T>.Count)) & remainderMask,
                        ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                }

                return Finalize(dotProductVector, xSumOfSquaresVector, ySumOfSquaresVector);
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
                    Update(
                        Vector256.LoadUnsafe(ref xRef, (uint)i),
                        Vector256.LoadUnsafe(ref yRef, (uint)i),
                        ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);

                    i += Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector256<T> remainderMask = CreateRemainderMaskVector256<T>(x.Length - i);

                    Update(
                        Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count)) & remainderMask,
                        Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<T>.Count)) & remainderMask,
                        ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                }

                return Finalize(dotProductVector, xSumOfSquaresVector, ySumOfSquaresVector);
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
                    Update(
                        Vector128.LoadUnsafe(ref xRef, (uint)i),
                        Vector128.LoadUnsafe(ref yRef, (uint)i),
                        ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);

                    i += Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector128<T> remainderMask = CreateRemainderMaskVector128<T>(x.Length - i);

                    Update(
                        Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count)) & remainderMask,
                        Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<T>.Count)) & remainderMask,
                        ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                }

                return Finalize(dotProductVector, xSumOfSquaresVector, ySumOfSquaresVector);
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            T dotProduct = T.Zero, xSumOfSquares = T.Zero, ySumOfSquares = T.Zero;
            for (int i = 0; i < x.Length; i++)
            {
                Update(x[i], y[i], ref dotProduct, ref xSumOfSquares, ref ySumOfSquares);
            }

            return Finalize(dotProduct, xSumOfSquares, ySumOfSquares);
        }

        /// <summary>Provides the same implementation as <see cref="CosineSimilarityCore"/>, but specifically for <see cref="Half"/>.</summary>
        private static Half CosineSimilarityHalfCore(ReadOnlySpan<Half> x, ReadOnlySpan<Half> y)
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
            // but only looping over each span once. As Half can't be vectorized implicitly, we reinterpret as shorts, as then
            // widen to float vectors, which can be vectorized.

            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<short>.Count)
            {
                ref short xRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(x));
                ref short yRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(y));

                // Accumulate with float vectors, casting at the very end back to Half.
                Vector512<float> dotProductVector = Vector512<float>.Zero;
                Vector512<float> xSumOfSquaresVector = Vector512<float>.Zero;
                Vector512<float> ySumOfSquaresVector = Vector512<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector512<short>.Count;
                int i = 0;
                do
                {
                    (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i));
                    (Vector512<float> yVecLower, Vector512<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(Vector512.LoadUnsafe(ref yRef, (uint)i));

                    Update(xVecLower, yVecLower, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                    Update(xVecUpper, yVecUpper, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);

                    i += Vector512<short>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector512<short> remainderMask = CreateRemainderMaskVector512<short>(x.Length - i);

                    (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(
                        Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<short>.Count)) & remainderMask);
                    (Vector512<float> yVecLower, Vector512<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(
                        Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<short>.Count)) & remainderMask);

                    Update(xVecLower, yVecLower, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                    Update(xVecUpper, yVecUpper, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                }

                return (Half)Finalize(dotProductVector, xSumOfSquaresVector, ySumOfSquaresVector);
            }

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<short>.Count)
            {
                ref short xRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(x));
                ref short yRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(y));

                // Accumulate with float vectors, casting at the very end back to Half.
                Vector256<float> dotProductVector = Vector256<float>.Zero;
                Vector256<float> xSumOfSquaresVector = Vector256<float>.Zero;
                Vector256<float> ySumOfSquaresVector = Vector256<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector256<short>.Count;
                int i = 0;
                do
                {
                    (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i));
                    (Vector256<float> yVecLower, Vector256<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(Vector256.LoadUnsafe(ref yRef, (uint)i));

                    Update(xVecLower, yVecLower, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                    Update(xVecUpper, yVecUpper, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);

                    i += Vector256<short>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector256<short> remainderMask = CreateRemainderMaskVector256<short>(x.Length - i);

                    (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(
                        Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<short>.Count)) & remainderMask);
                    (Vector256<float> yVecLower, Vector256<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(
                        Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<short>.Count)) & remainderMask);

                    Update(xVecLower, yVecLower, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                    Update(xVecUpper, yVecUpper, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                }

                return (Half)Finalize(dotProductVector, xSumOfSquaresVector, ySumOfSquaresVector);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<short>.Count)
            {
                ref short xRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(x));
                ref short yRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(y));

                // Accumulate with float vectors, casting at the very end back to Half.
                Vector128<float> dotProductVector = Vector128<float>.Zero;
                Vector128<float> xSumOfSquaresVector = Vector128<float>.Zero;
                Vector128<float> ySumOfSquaresVector = Vector128<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector128<short>.Count;
                int i = 0;
                do
                {
                    (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i));
                    (Vector128<float> yVecLower, Vector128<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(Vector128.LoadUnsafe(ref yRef, (uint)i));

                    Update(xVecLower, yVecLower, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                    Update(xVecUpper, yVecUpper, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);

                    i += Vector128<short>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector128<short> remainderMask = CreateRemainderMaskVector128<short>(x.Length - i);

                    (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(
                        Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<short>.Count)) & remainderMask);
                    (Vector128<float> yVecLower, Vector128<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(
                        Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<short>.Count)) & remainderMask);

                    Update(xVecLower, yVecLower, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                    Update(xVecUpper, yVecUpper, ref dotProductVector, ref xSumOfSquaresVector, ref ySumOfSquaresVector);
                }

                return (Half)Finalize(dotProductVector, xSumOfSquaresVector, ySumOfSquaresVector);
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            float dotProduct = 0, xSumOfSquares = 0, ySumOfSquares = 0;
            for (int i = 0; i < x.Length; i++)
            {
                Update((float)x[i], (float)y[i], ref dotProduct, ref xSumOfSquares, ref ySumOfSquares);
            }

            return (Half)Finalize(dotProduct, xSumOfSquares, ySumOfSquares);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Update<T>(T x, T y, ref T dotProduct, ref T xSumOfSquares, ref T ySumOfSquares) where T : INumberBase<T>
        {
            dotProduct = MultiplyAddEstimateOperator<T>.Invoke(x, y, dotProduct);
            xSumOfSquares = MultiplyAddEstimateOperator<T>.Invoke(x, x, xSumOfSquares);
            ySumOfSquares = MultiplyAddEstimateOperator<T>.Invoke(y, y, ySumOfSquares);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Update<T>(Vector128<T> xVec, Vector128<T> yVec, ref Vector128<T> dotProductVector, ref Vector128<T> xSumOfSquaresVector, ref Vector128<T> ySumOfSquaresVector) where T : INumberBase<T>
        {
            dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
            xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
            ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Update<T>(Vector256<T> xVec, Vector256<T> yVec, ref Vector256<T> dotProductVector, ref Vector256<T> xSumOfSquaresVector, ref Vector256<T> ySumOfSquaresVector) where T : INumberBase<T>
        {
            dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
            xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
            ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Update<T>(Vector512<T> xVec, Vector512<T> yVec, ref Vector512<T> dotProductVector, ref Vector512<T> xSumOfSquaresVector, ref Vector512<T> ySumOfSquaresVector) where T : INumberBase<T>
        {
            dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
            xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
            ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Finalize<T>(T dotProduct, T xSumOfSquares, T ySumOfSquares) where T : IRootFunctions<T> =>
            dotProduct / (T.Sqrt(xSumOfSquares) * T.Sqrt(ySumOfSquares));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Finalize<T>(Vector128<T> dotProductVector, Vector128<T> xSumOfSquaresVector, Vector128<T> ySumOfSquaresVector) where T : IRootFunctions<T> =>
            Vector128.Sum(dotProductVector) / (T.Sqrt(Vector128.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector128.Sum(ySumOfSquaresVector)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Finalize<T>(Vector256<T> dotProductVector, Vector256<T> xSumOfSquaresVector, Vector256<T> ySumOfSquaresVector) where T : IRootFunctions<T> =>
            Vector256.Sum(dotProductVector) / (T.Sqrt(Vector256.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector256.Sum(ySumOfSquaresVector)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Finalize<T>(Vector512<T> dotProductVector, Vector512<T> xSumOfSquaresVector, Vector512<T> ySumOfSquaresVector) where T : IRootFunctions<T> =>
            Vector512.Sum(dotProductVector) / (T.Sqrt(Vector512.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector512.Sum(ySumOfSquaresVector)));
    }
}
