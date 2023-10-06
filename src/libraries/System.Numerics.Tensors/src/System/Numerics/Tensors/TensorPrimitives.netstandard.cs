﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <remarks>Assumes arguments have already been validated to be non-empty and equal length.</remarks>
        private static float CosineSimilarityCore(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            float dotProduct = 0f;
            float xSumOfSquares = 0f;
            float ySumOfSquares = 0f;

            if (Vector.IsHardwareAccelerated &&
                Vector<float>.Count <= 16 && // currently never greater than 8, but 16 would occur if/when AVX512 is supported, and logic in remainder handling assumes that maximum
                x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector<float> dotProductVector = Vector<float>.Zero;
                Vector<float> xSumOfSquaresVector = Vector<float>.Zero;
                Vector<float> ySumOfSquaresVector = Vector<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector<float>.Count;
                int i = 0;
                do
                {
                    Vector<float> xVec = AsVector(ref xRef, i);
                    Vector<float> yVec = AsVector(ref yRef, i);

                    dotProductVector += xVec * yVec;
                    xSumOfSquaresVector += xVec * xVec;
                    ySumOfSquaresVector += yVec * yVec;

                    i += Vector<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector<float> xVec = AsVector(ref xRef, x.Length - Vector<float>.Count);
                    Vector<float> yVec = AsVector(ref yRef, x.Length - Vector<float>.Count);

                    Vector<float> remainderMask = CreateRemainderMaskSingleVector(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector += xVec * yVec;
                    xSumOfSquaresVector += xVec * xVec;
                    ySumOfSquaresVector += yVec * yVec;
                }

                // Sum the vector lanes into the scalar result.
                for (int e = 0; e < Vector<float>.Count; e++)
                {
                    dotProduct += dotProductVector[e];
                    xSumOfSquares += xSumOfSquaresVector[e];
                    ySumOfSquares += ySumOfSquaresVector[e];
                }
            }
            else
            {
                // Vectorization isn't supported or there are too few elements to vectorize.
                // Use a scalar implementation.
                for (int i = 0; i < x.Length; i++)
                {
                    dotProduct += x[i] * y[i];
                    xSumOfSquares += x[i] * x[i];
                    ySumOfSquares += y[i] * y[i];
                }
            }

            // Sum(X * Y) / (|X| * |Y|)
            return dotProduct / (MathF.Sqrt(xSumOfSquares) * MathF.Sqrt(ySumOfSquares));
        }

        /// <summary>Performs an aggregation over all elements in <paramref name="x"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="TTransformOperator">Specifies the transform operation that should be applied to each element loaded from <paramref name="x"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied after the transform is applied to each element.
        /// </typeparam>
        private static float Aggregate<TTransformOperator, TAggregationOperator>(
            ReadOnlySpan<float> x, TTransformOperator transformOp = default, TAggregationOperator aggregationOp = default)
            where TTransformOperator : struct, IUnaryOperator
            where TAggregationOperator : struct, IAggregationOperator
        {
            if (x.Length == 0)
            {
                return 0;
            }

            float result;

            if (Vector.IsHardwareAccelerated && transformOp.CanVectorize && x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results
                Vector<float> resultVector = transformOp.Invoke(AsVector(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector<float>.Count;
                int i = Vector<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    resultVector = aggregationOp.Invoke(resultVector, transformOp.Invoke(AsVector(ref xRef, i)));
                    i += Vector<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    resultVector = aggregationOp.Invoke(resultVector,
                        Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            new Vector<float>(aggregationOp.IdentityValue),
                            transformOp.Invoke(AsVector(ref xRef, x.Length - Vector<float>.Count))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                result = resultVector[0];
                for (int f = 1; f < Vector<float>.Count; f++)
                {
                    result = aggregationOp.Invoke(result, resultVector[f]);
                }

                return result;
            }

            // Aggregate the remaining items in the input span.
            result = transformOp.Invoke(x[0]);
            for (int i = 1; i < x.Length; i++)
            {
                result = aggregationOp.Invoke(result, transformOp.Invoke(x[i]));
            }

            return result;
        }

        /// <summary>Performs an aggregation over all pair-wise elements in <paramref name="x"/> and <paramref name="y"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="TBinaryOperator">Specifies the binary operation that should be applied to the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied to the results of the binary operations on the pair-wise values.
        /// </typeparam>
        private static float Aggregate<TBinaryOperator, TAggregationOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, TBinaryOperator binaryOp = default, TAggregationOperator aggregationOp = default)
            where TBinaryOperator : struct, IBinaryOperator
            where TAggregationOperator : struct, IAggregationOperator
        {
            Debug.Assert(x.Length == y.Length);

            if (x.Length == 0)
            {
                return 0;
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);

            float result;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector<float> resultVector = binaryOp.Invoke(AsVector(ref xRef, 0), AsVector(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector<float>.Count;
                int i = Vector<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    resultVector = aggregationOp.Invoke(resultVector, binaryOp.Invoke(AsVector(ref xRef, i), AsVector(ref yRef, i)));
                    i += Vector<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    resultVector = aggregationOp.Invoke(resultVector,
                        Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            new Vector<float>(aggregationOp.IdentityValue),
                            binaryOp.Invoke(
                                AsVector(ref xRef, x.Length - Vector<float>.Count),
                                AsVector(ref yRef, x.Length - Vector<float>.Count))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                result = resultVector[0];
                for (int f = 1; f < Vector<float>.Count; f++)
                {
                    result = aggregationOp.Invoke(result, resultVector[f]);
                }

                return result;
            }

            // Aggregate the remaining items in the input span.
            result = binaryOp.Invoke(x[0], y[0]);
            for (int i = 1; i < x.Length; i++)
            {
                result = aggregationOp.Invoke(result, binaryOp.Invoke(x[i], y[i]));
            }

            return result;
        }

        /// <remarks>
        /// This is the same as <see cref="Aggregate{TTransformOperator, TAggregationOperator}(ReadOnlySpan{float}, TTransformOperator, TAggregationOperator)"/>
        /// with an identity transform, except it early exits on NaN.
        /// </remarks>
        private static float MinMaxCore<TMinMaxOperator>(ReadOnlySpan<float> x, TMinMaxOperator op = default)
            where TMinMaxOperator : struct, IBinaryOperator
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            float result = x[0];
            int i = 0;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector<float> resultVector = AsVector(ref xRef, 0), current;
                if (Vector.EqualsAll(resultVector, resultVector))
                {
                    int oneVectorFromEnd = x.Length - Vector<float>.Count;
                    i = Vector<float>.Count;

                    // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                    while (i <= oneVectorFromEnd)
                    {
                        // Load the next vector, and early exit on NaN.
                        current = AsVector(ref xRef, i);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        resultVector = op.Invoke(resultVector, current);
                        i += Vector<float>.Count;
                    }

                    // If any elements remain, handle them in one final vector.
                    if (i != x.Length)
                    {
                        current = AsVector(ref xRef, x.Length - Vector<float>.Count);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        resultVector = op.Invoke(resultVector, current);
                    }

                    // Aggregate the lanes in the vector to create the final scalar result.
                    for (int f = 0; f < Vector<float>.Count; f++)
                    {
                        result = op.Invoke(result, resultVector[f]);
                    }

                    return result;
                }
            }

            // Scalar path used when either vectorization is not supported, the input is too small to vectorize,
            // or a NaN is encountered.
            Scalar:
            for (; (uint)i < (uint)x.Length; i++)
            {
                float current = x[i];

                if (float.IsNaN(current))
                {
                    return current;
                }

                result = op.Invoke(result, current);
            }

            return result;
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan<TUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination, TUnaryOperator op = default)
            where TUnaryOperator : struct, IUnaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated && op.CanVectorize)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i));

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        ref Vector<float> dest = ref AsVector(ref dRef, lastVectorIndex);
                        dest = Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            dest,
                            op.Invoke(AsVector(ref xRef, lastVectorIndex)));
                    }

                    return;
                }
            }

            // Loop handling one element at a time.
            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i));

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanSpanIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination, TBinaryOperator op = default)
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i),
                                                          AsVector(ref yRef, i));

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        ref Vector<float> dest = ref AsVector(ref dRef, lastVectorIndex);
                        dest = Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            dest,
                            op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                      AsVector(ref yRef, lastVectorIndex)));
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i),
                                                    Unsafe.Add(ref yRef, i));

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination, TBinaryOperator op = default)
            where TBinaryOperator : struct, IBinaryOperator =>
            InvokeSpanScalarIntoSpan<IdentityOperator, TBinaryOperator>(x, y, destination, default, op);

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTransformOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/>.
        /// It is not used with <paramref name="y"/>.
        /// </typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the transformed value from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<TTransformOperator, TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination, TTransformOperator xTransformOp = default, TBinaryOperator binaryOp = default)
            where TTransformOperator : struct, IUnaryOperator
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated && xTransformOp.CanVectorize)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    // Loop handling one vector at a time.
                    Vector<float> yVec = new(y);
                    do
                    {
                        AsVector(ref dRef, i) = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, i)),
                                                          yVec);

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        ref Vector<float> dest = ref AsVector(ref dRef, lastVectorIndex);
                        dest = Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            dest,
                            binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, lastVectorIndex)), yVec));
                    }

                    return;
                }
            }

            // Loop handling one element at a time.
            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, i)),
                                                    y);

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/>, <paramref name="y"/>,
        /// and <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanSpanSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination, TTernaryOperator op = default)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != y.Length || x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);
            ValidateInputOutputSpanNonOverlapping(z, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i),
                                                          AsVector(ref yRef, i),
                                                          AsVector(ref zRef, i));

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        ref Vector<float> dest = ref AsVector(ref dRef, lastVectorIndex);
                        dest = Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            dest,
                            op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                      AsVector(ref yRef, lastVectorIndex),
                                      AsVector(ref zRef, lastVectorIndex)));
                    }

                    return;
                }
            }

            // Loop handling one element at a time.
            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i),
                                                    Unsafe.Add(ref yRef, i),
                                                    Unsafe.Add(ref zRef, i));

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>
        /// with <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanSpanScalarIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination, TTernaryOperator op = default)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    Vector<float> zVec = new(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i),
                                                          AsVector(ref yRef, i),
                                                          zVec);

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        ref Vector<float> dest = ref AsVector(ref dRef, lastVectorIndex);
                        dest = Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            dest,
                            op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                      AsVector(ref yRef, lastVectorIndex),
                                      zVec));
                    }

                    return;
                }
            }

            // Loop handling one element at a time.
            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i),
                                                    Unsafe.Add(ref yRef, i),
                                                    z);

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise element loaded from <paramref name="x"/>, with <paramref name="y"/>,
        /// and the element loaded from <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanScalarSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination, TTernaryOperator op = default)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(z, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    Vector<float> yVec = new(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i),
                                                          yVec,
                                                          AsVector(ref zRef, i));

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        ref Vector<float> dest = ref AsVector(ref dRef, lastVectorIndex);
                        dest = Vector.ConditionalSelect(
                            Vector.Equals(CreateRemainderMaskSingleVector(x.Length - i), Vector<float>.Zero),
                            dest,
                            op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                      yVec,
                                      AsVector(ref zRef, lastVectorIndex)));
                    }

                    return;
                }
            }

            // Loop handling one element at a time.
            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i),
                                                    y,
                                                    Unsafe.Add(ref zRef, i));

                i++;
            }
        }

        /// <summary>Loads a <see cref="Vector{Single}"/> that begins at the specified <paramref name="offset"/> from <paramref name="start"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector<float> AsVector(ref float start, int offset) =>
            ref Unsafe.As<float, Vector<float>>(
                ref Unsafe.Add(ref start, offset));

        /// <summary>Gets whether the specified <see cref="float"/> is negative.</summary>
        private static unsafe bool IsNegative(float f) => *(int*)&f < 0;

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        private static unsafe Vector<float> IsNegative(Vector<float> f) =>
            (Vector<float>)Vector.LessThan((Vector<int>)f, Vector<int>.Zero);

        /// <summary>Gets the base 2 logarithm of <paramref name="x"/>.</summary>
        private static float Log2(float x) => MathF.Log(x, 2);

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        private static unsafe Vector<float> CreateRemainderMaskSingleVector(int count)
        {
            Debug.Assert(Vector<float>.Count is 4 or 8 or 16);

            return AsVector(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (count * 16) + (16 - Vector<float>.Count));
        }

        /// <summary>x + y</summary>
        private readonly struct AddOperator : IAggregationOperator
        {
            public float Invoke(float x, float y) => x + y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x + y;
            public float IdentityValue => 0;
        }

        /// <summary>x - y</summary>
        private readonly struct SubtractOperator : IBinaryOperator
        {
            public float Invoke(float x, float y) => x - y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x - y;
        }

        /// <summary>(x - y) * (x - y)</summary>
        private readonly struct SubtractSquaredOperator : IBinaryOperator
        {
            public float Invoke(float x, float y)
            {
                float tmp = x - y;
                return tmp * tmp;
            }

            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> tmp = x - y;
                return tmp * tmp;
            }
        }

        /// <summary>x * y</summary>
        private readonly struct MultiplyOperator : IAggregationOperator
        {
            public float Invoke(float x, float y) => x * y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x * y;
            public float IdentityValue => 1;
        }

        /// <summary>x / y</summary>
        private readonly struct DivideOperator : IBinaryOperator
        {
            public float Invoke(float x, float y) => x / y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x / y;
        }

        /// <summary>MathF.Max(x, y) (but without guaranteed NaN propagation)</summary>
        private readonly struct MaxOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) =>
                x == y ?
                    (IsNegative(x) ? y : x) :
                    (y > x ? y : x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, y),
                    Vector.ConditionalSelect(IsNegative(x), y, x),
                    Vector.Max(x, y));
        }

        /// <summary>MathF.Max(x, y)</summary>
        private readonly struct MaxPropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) => MathF.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, x),
                    Vector.ConditionalSelect(Vector.Equals(y, y),
                        Vector.ConditionalSelect(Vector.Equals(x, y),
                            Vector.ConditionalSelect(IsNegative(x), y, x),
                            Vector.Max(x, y)),
                        y),
                    x);
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs (but NaNs may not be propagated)</summary>
        private readonly struct MaxMagnitudeOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return
                    yMag == xMag ?
                        (IsNegative(x) ? y : x) :
                        (xMag > yMag ? x : y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);
                return
                    Vector.ConditionalSelect(Vector.Equals(xMag, yMag),
                        Vector.ConditionalSelect(IsNegative(x), y, x),
                        Vector.ConditionalSelect(Vector.GreaterThan(xMag, yMag), x, y));
            }
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs</summary>
        private readonly struct MaxMagnitudePropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return xMag > yMag || float.IsNaN(xMag) || (xMag == yMag && !IsNegative(x)) ? x : y;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);
                return
                    Vector.ConditionalSelect(Vector.Equals(x, x),
                        Vector.ConditionalSelect(Vector.Equals(y, y),
                            Vector.ConditionalSelect(Vector.Equals(xMag, yMag),
                                Vector.ConditionalSelect(IsNegative(x), y, x),
                                Vector.ConditionalSelect(Vector.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
            }
        }

        /// <summary>MathF.Min(x, y) (but NaNs may not be propagated)</summary>
        private readonly struct MinOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) =>
                x == y ?
                    (IsNegative(y) ? y : x) :
                    (y < x ? y : x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, y),
                    Vector.ConditionalSelect(IsNegative(y), y, x),
                    Vector.Min(x, y));
        }

        /// <summary>MathF.Min(x, y)</summary>
        private readonly struct MinPropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) => MathF.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, x),
                    Vector.ConditionalSelect(Vector.Equals(y, y),
                        Vector.ConditionalSelect(Vector.Equals(x, y),
                            Vector.ConditionalSelect(IsNegative(x), x, y),
                            Vector.Min(x, y)),
                        y),
                    x);
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs (but NaNs may not be propagated)</summary>
        private readonly struct MinMagnitudeOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return
                    yMag == xMag ?
                        (IsNegative(y) ? y : x) :
                        (yMag < xMag ? y : x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);
                return
                    Vector.ConditionalSelect(Vector.Equals(yMag, xMag),
                        Vector.ConditionalSelect(IsNegative(y), y, x),
                        Vector.ConditionalSelect(Vector.LessThan(yMag, xMag), y, x));
            }
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs</summary>
        private readonly struct MinMagnitudePropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return xMag < yMag || float.IsNaN(xMag) || (xMag == yMag && IsNegative(x)) ? x : y;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);

                return
                    Vector.ConditionalSelect(Vector.Equals(x, x),
                        Vector.ConditionalSelect(Vector.Equals(y, y),
                            Vector.ConditionalSelect(Vector.Equals(yMag, xMag),
                                Vector.ConditionalSelect(IsNegative(x), x, y),
                                Vector.ConditionalSelect(Vector.LessThan(xMag, yMag), x, y)),
                            y),
                        x);
            }
        }

        /// <summary>-x</summary>
        private readonly struct NegateOperator : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => -x;
            public Vector<float> Invoke(Vector<float> x) => -x;
        }

        /// <summary>(x + y) * z</summary>
        private readonly struct AddMultiplyOperator : ITernaryOperator
        {
            public float Invoke(float x, float y, float z) => (x + y) * z;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z) => (x + y) * z;
        }

        /// <summary>(x * y) + z</summary>
        private readonly struct MultiplyAddOperator : ITernaryOperator
        {
            public float Invoke(float x, float y, float z) => (x * y) + z;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z) => (x * y) + z;
        }

        /// <summary>x</summary>
        private readonly struct IdentityOperator : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => x;
            public Vector<float> Invoke(Vector<float> x) => x;
        }

        /// <summary>x * x</summary>
        private readonly struct SquaredOperator : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => x * x;
            public Vector<float> Invoke(Vector<float> x) => x * x;
        }

        /// <summary>MathF.Abs(x)</summary>
        private readonly struct AbsoluteOperator : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => MathF.Abs(x);
            public Vector<float> Invoke(Vector<float> x) => Vector.Abs(x);
        }

        private readonly struct ExpOperator : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Exp(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftLeft (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Log(x)</summary>
        private readonly struct LogOperator : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Log(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftRightArithmetic (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Log2(x)</summary>
        private readonly struct Log2Operator : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => Log2(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftRightArithmetic (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>1f / (1f + MathF.Exp(-x))</summary>
        private readonly struct SigmoidOperator : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => 1.0f / (1.0f + MathF.Exp(-x));
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftRightArithmetic (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        private interface IUnaryOperator
        {
            bool CanVectorize { get; }
            float Invoke(float x);
            Vector<float> Invoke(Vector<float> x);
        }

        /// <summary>Operator that takes two input values and returns a single value.</summary>
        private interface IBinaryOperator
        {
            float Invoke(float x, float y);
            Vector<float> Invoke(Vector<float> x, Vector<float> y);
        }

        /// <summary><see cref="IBinaryOperator"/> that specializes horizontal aggregation of all elements in a vector.</summary>
        private interface IAggregationOperator : IBinaryOperator
        {
            float IdentityValue { get; }
        }

        /// <summary>Operator that takes three input values and returns a single value.</summary>
        private interface ITernaryOperator
        {
            float Invoke(float x, float y, float z);
            Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z);
        }
    }
}
