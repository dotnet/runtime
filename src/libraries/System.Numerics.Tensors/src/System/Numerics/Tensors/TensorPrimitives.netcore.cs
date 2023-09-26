// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <see cref="float" />
        /// value to its nearest representable half-precision floating-point value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static void ConvertToHalf(ReadOnlySpan<float> source, Span<Half> destination)
        {
            if (source.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = (Half)source[i];
            }
        }

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each half-precision
        /// floating-point value to its nearest representable <see cref="float"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static void ConvertToSingle(ReadOnlySpan<Half> source, Span<float> destination)
        {
            if (source.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = (float)source[i];
            }
        }

        private static bool IsNegative(float f) => float.IsNegative(f);

        private static float MaxMagnitude(float x, float y) => MathF.MaxMagnitude(x, y);

        private static float MinMagnitude(float x, float y) => MathF.MinMagnitude(x, y);

        private static float Log2(float x) => MathF.Log2(x);

        private static float CosineSimilarityCore(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            float dotProduct = 0f;
            float xSumOfSquares = 0f;
            float ySumOfSquares = 0f;

            int i = 0;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector512<float> dotProductVector = Vector512<float>.Zero;
                Vector512<float> xSumOfSquaresVector = Vector512<float>.Zero;
                Vector512<float> ySumOfSquaresVector = Vector512<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                do
                {
                    Vector512<float> xVec = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<float> yVec = Vector512.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector512<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Sum the vector lanes into the scalar result.
                dotProduct += Vector512.Sum(dotProductVector);
                xSumOfSquares += Vector512.Sum(xSumOfSquaresVector);
                ySumOfSquares += Vector512.Sum(ySumOfSquaresVector);
            }
            else
#endif
            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector256<float> dotProductVector = Vector256<float>.Zero;
                Vector256<float> xSumOfSquaresVector = Vector256<float>.Zero;
                Vector256<float> ySumOfSquaresVector = Vector256<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                do
                {
                    Vector256<float> xVec = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<float> yVec = Vector256.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector256<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Sum the vector lanes into the scalar result.
                dotProduct += Vector256.Sum(dotProductVector);
                xSumOfSquares += Vector256.Sum(xSumOfSquaresVector);
                ySumOfSquares += Vector256.Sum(ySumOfSquaresVector);
            }
            else if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector128<float> dotProductVector = Vector128<float>.Zero;
                Vector128<float> xSumOfSquaresVector = Vector128<float>.Zero;
                Vector128<float> ySumOfSquaresVector = Vector128<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                do
                {
                    Vector128<float> xVec = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<float> yVec = Vector128.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector128<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Sum the vector lanes into the scalar result.
                dotProduct += Vector128.Sum(dotProductVector);
                xSumOfSquares += Vector128.Sum(xSumOfSquaresVector);
                ySumOfSquares += Vector128.Sum(ySumOfSquaresVector);
            }

            // Process any remaining elements past the last vector.
            for (; (uint)i < (uint)x.Length; i++)
            {
                dotProduct = MathF.FusedMultiplyAdd(x[i], y[i], dotProduct);
                xSumOfSquares = MathF.FusedMultiplyAdd(x[i], x[i], xSumOfSquares);
                ySumOfSquares = MathF.FusedMultiplyAdd(y[i], y[i], ySumOfSquares);
            }

            // Sum(X * Y) / (|X| * |Y|)
            return dotProduct / (MathF.Sqrt(xSumOfSquares) * MathF.Sqrt(ySumOfSquares));
        }

        private static float Aggregate<TLoad, TAggregate>(
            float identityValue, ReadOnlySpan<float> x)
            where TLoad : IUnaryOperator
            where TAggregate : IBinaryOperator
        {
            // Initialize the result to the identity value
            float result = identityValue;
            int i = 0;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results
                Vector512<float> resultVector = TLoad.Invoke(Vector512.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector512<float>.Count;
                do
                {
                    resultVector = TAggregate.Invoke(resultVector, TLoad.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector512<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                result = TAggregate.Invoke(result, TAggregate.Invoke(resultVector));
            }
            else
#endif
            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results
                Vector256<float> resultVector = TLoad.Invoke(Vector256.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector256<float>.Count;
                do
                {
                    resultVector = TAggregate.Invoke(resultVector, TLoad.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector256<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                result = TAggregate.Invoke(result, TAggregate.Invoke(resultVector));
            }
            else if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results
                Vector128<float> resultVector = TLoad.Invoke(Vector128.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector128<float>.Count;
                do
                {
                    resultVector = TAggregate.Invoke(resultVector, TLoad.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector128<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                result = TAggregate.Invoke(result, TAggregate.Invoke(resultVector));
            }

            // Aggregate the remaining items in the input span.
            for (; (uint)i < (uint)x.Length; i++)
            {
                result = TAggregate.Invoke(result, TLoad.Invoke(x[i]));
            }

            return result;
        }

        private static float Aggregate<TBinary, TAggregate>(
            float identityValue, ReadOnlySpan<float> x, ReadOnlySpan<float> y)
            where TBinary : IBinaryOperator
            where TAggregate : IBinaryOperator
        {
            // Initialize the result to the identity value
            float result = identityValue;
            int i = 0;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                // Load the first vector as the initial set of results
                Vector512<float> resultVector = TBinary.Invoke(Vector512.LoadUnsafe(ref xRef, 0), Vector512.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector512<float>.Count;
                do
                {
                    resultVector = TAggregate.Invoke(resultVector, TBinary.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i), Vector512.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector512<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                result = TAggregate.Invoke(result, TAggregate.Invoke(resultVector));
            }
            else
#endif
            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                // Load the first vector as the initial set of results
                Vector256<float> resultVector = TBinary.Invoke(Vector256.LoadUnsafe(ref xRef, 0), Vector256.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector256<float>.Count;
                do
                {
                    resultVector = TAggregate.Invoke(resultVector, TBinary.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i), Vector256.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector256<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                result = TAggregate.Invoke(result, TAggregate.Invoke(resultVector));
            }
            else if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                // Load the first vector as the initial set of results
                Vector128<float> resultVector = TBinary.Invoke(Vector128.LoadUnsafe(ref xRef, 0), Vector128.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector128<float>.Count;
                do
                {
                    resultVector = TAggregate.Invoke(resultVector, TBinary.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i), Vector128.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector128<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                result = TAggregate.Invoke(result, TAggregate.Invoke(resultVector));
            }

            // Aggregate the remaining items in the input span.
            for (; (uint)i < (uint)x.Length; i++)
            {
                result = TAggregate.Invoke(result, TBinary.Invoke(x[i], y[i]));
            }

            return result;
        }

        private static unsafe void InvokeSpanIntoSpan<TUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination)
            where TUnaryOperator : IUnaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, i));

                i++;
            }
        }

        private static unsafe void InvokeSpanSpanIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination)
            where TBinaryOperator : IBinaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                               Vector512.LoadUnsafe(ref yRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                               Vector512.LoadUnsafe(ref yRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                               Vector256.LoadUnsafe(ref yRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                               Vector256.LoadUnsafe(ref yRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                               Vector128.LoadUnsafe(ref yRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                               Vector128.LoadUnsafe(ref yRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                 Unsafe.Add(ref yRef, i));

                i++;
            }
        }

        private static unsafe void InvokeSpanScalarIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TBinaryOperator : IBinaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<float> yVec = Vector512.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                               yVec).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> yVec = Vector256.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                               yVec).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> yVec = Vector128.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                               yVec).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                 y);

                i++;
            }
        }

        private static unsafe void InvokeSpanSpanSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination)
            where TTernaryOperator : ITernaryOperator
        {
            if (x.Length != y.Length || x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                                Vector512.LoadUnsafe(ref yRef, (uint)i),
                                                Vector512.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                Vector512.LoadUnsafe(ref yRef, lastVectorIndex),
                                                Vector512.LoadUnsafe(ref zRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                                Vector256.LoadUnsafe(ref yRef, (uint)i),
                                                Vector256.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                Vector256.LoadUnsafe(ref yRef, lastVectorIndex),
                                                Vector256.LoadUnsafe(ref zRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                                Vector128.LoadUnsafe(ref yRef, (uint)i),
                                                Vector128.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                Vector128.LoadUnsafe(ref yRef, lastVectorIndex),
                                                Vector128.LoadUnsafe(ref zRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                  Unsafe.Add(ref yRef, i),
                                                                  Unsafe.Add(ref zRef, i));

                i++;
            }
        }

        private static unsafe void InvokeSpanSpanScalarIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination)
            where TTernaryOperator : ITernaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<float> zVec = Vector512.Create(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                                Vector512.LoadUnsafe(ref yRef, (uint)i),
                                                zVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                Vector512.LoadUnsafe(ref yRef, lastVectorIndex),
                                                zVec).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> zVec = Vector256.Create(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                                Vector256.LoadUnsafe(ref yRef, (uint)i),
                                                zVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                Vector256.LoadUnsafe(ref yRef, lastVectorIndex),
                                                zVec).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> zVec = Vector128.Create(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                                Vector128.LoadUnsafe(ref yRef, (uint)i),
                                                zVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                Vector128.LoadUnsafe(ref yRef, lastVectorIndex),
                                                zVec).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                  Unsafe.Add(ref yRef, i),
                                                                  z);

                i++;
            }
        }

        private static unsafe void InvokeSpanScalarSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination)
            where TTernaryOperator : ITernaryOperator
        {
            if (x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<float> yVec = Vector512.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                                yVec,
                                                Vector512.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                yVec,
                                                Vector512.LoadUnsafe(ref zRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> yVec = Vector256.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                                yVec,
                                                Vector256.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                yVec,
                                                Vector256.LoadUnsafe(ref zRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> yVec = Vector128.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                                yVec,
                                                Vector128.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                yVec,
                                                Vector128.LoadUnsafe(ref zRef, lastVectorIndex)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                  y,
                                                                  Unsafe.Add(ref zRef, i));

                i++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> FusedMultiplyAdd(Vector128<float> x, Vector128<float> y, Vector128<float> addend)
        {
            if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(x, y, addend);
            }

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.FusedMultiplyAdd(addend, x, y);
            }

            return (x * y) + addend;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> FusedMultiplyAdd(Vector256<float> x, Vector256<float> y, Vector256<float> addend)
        {
            if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(x, y, addend);
            }

            return (x * y) + addend;
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> FusedMultiplyAdd(Vector512<float> x, Vector512<float> y, Vector512<float> addend)
        {
            if (Avx512F.IsSupported)
            {
                return Avx512F.FusedMultiplyAdd(x, y, addend);
            }

            return (x * y) + addend;
        }
#endif

        private readonly struct AddOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x + y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x + y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x + y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x + y;
#endif

            public static float Invoke(Vector128<float> x) => Vector128.Sum(x);
            public static float Invoke(Vector256<float> x) => Vector256.Sum(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => Vector512.Sum(x);
#endif
        }

        private readonly struct SubtractOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x - y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x - y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x - y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x - y;
#endif
        }

        private readonly struct SubtractSquaredOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y)
            {
                float tmp = x - y;
                return tmp * tmp;
            }

            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                Vector128<float> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y)
            {
                Vector256<float> tmp = x - y;
                return tmp * tmp;
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y)
            {
                Vector512<float> tmp = x - y;
                return tmp * tmp;
            }
#endif
        }

        private readonly struct MultiplyOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x * y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x * y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x * y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x * y;
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(Vector128<float> x)
            {
                float f = x[0];
                for (int i = 1; i < Vector128<float>.Count; i++)
                {
                    f *= x[i];
                }
                return f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(Vector256<float> x)
            {
                float f = x[0];
                for (int i = 1; i < Vector256<float>.Count; i++)
                {
                    f *= x[i];
                }
                return f;
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(Vector512<float> x)
            {
                float f = x[0];
                for (int i = 1; i < Vector512<float>.Count; i++)
                {
                    f *= x[i];
                }
                return f;
            }
#endif
        }

        private readonly struct DivideOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x / y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x / y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x / y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x / y;
#endif
        }

        private readonly struct NegateOperator : IUnaryOperator
        {
            public static float Invoke(float x) => -x;
            public static Vector128<float> Invoke(Vector128<float> x) => -x;
            public static Vector256<float> Invoke(Vector256<float> x) => -x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => -x;
#endif
        }

        private readonly struct AddMultiplyOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x + y) * z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x + y) * z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x + y) * z;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x + y) * z;
#endif
        }

        private readonly struct MultiplyAddOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => MathF.FusedMultiplyAdd(x, y, z);
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => FusedMultiplyAdd(x, y, z);
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => FusedMultiplyAdd(x, y, z);
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => FusedMultiplyAdd(x, y, z);
#endif
        }

        private readonly struct IdentityOperator : IUnaryOperator
        {
            public static float Invoke(float x) => x;
            public static Vector128<float> Invoke(Vector128<float> x) => x;
            public static Vector256<float> Invoke(Vector256<float> x) => x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => x;
#endif
        }

        private readonly struct SquaredOperator : IUnaryOperator
        {
            public static float Invoke(float x) => x * x;
            public static Vector128<float> Invoke(Vector128<float> x) => x * x;
            public static Vector256<float> Invoke(Vector256<float> x) => x * x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => x * x;
#endif
        }

        private readonly struct AbsoluteOperator : IUnaryOperator
        {
            public static float Invoke(float x) => MathF.Abs(x);
            public static Vector128<float> Invoke(Vector128<float> x) => Vector128.Abs(x);
            public static Vector256<float> Invoke(Vector256<float> x) => Vector256.Abs(x);
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => Vector512.Abs(x);
#endif
        }

        private interface IUnaryOperator
        {
            static abstract float Invoke(float x);
            static abstract Vector128<float> Invoke(Vector128<float> x);
            static abstract Vector256<float> Invoke(Vector256<float> x);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x);
#endif
        }

        private interface IBinaryOperator
        {
            static abstract float Invoke(float x, float y);
            static abstract Vector128<float> Invoke(Vector128<float> x, Vector128<float> y);
            static abstract Vector256<float> Invoke(Vector256<float> x, Vector256<float> y);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x, Vector512<float> y);
#endif

            // Operations for aggregating all lanes in a vector into a single value.
            // These are not supported on most implementations.
            static virtual float Invoke(Vector128<float> x) => throw new NotSupportedException();
            static virtual float Invoke(Vector256<float> x) => throw new NotSupportedException();
#if NET8_0_OR_GREATER
            static virtual float Invoke(Vector512<float> x) => throw new NotSupportedException();
#endif
        }

        private interface ITernaryOperator
        {
            static abstract float Invoke(float x, float y, float z);
            static abstract Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z);
            static abstract Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z);
#endif
        }
    }
}
