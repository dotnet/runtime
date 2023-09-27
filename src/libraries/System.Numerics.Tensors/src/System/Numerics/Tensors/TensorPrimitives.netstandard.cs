// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        private static float CosineSimilarityCore(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            float dotProduct = 0f;
            float xSumOfSquares = 0f;
            float ySumOfSquares = 0f;

            int i = 0;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector<float> dotProductVector = Vector<float>.Zero;
                Vector<float> xSumOfSquaresVector = Vector<float>.Zero;
                Vector<float> ySumOfSquaresVector = Vector<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector<float>.Count;
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

                // Sum the vector lanes into the scalar result.
                for (int e = 0; e < Vector<float>.Count; e++)
                {
                    dotProduct += dotProductVector[e];
                    xSumOfSquares += xSumOfSquaresVector[e];
                    ySumOfSquares += ySumOfSquaresVector[e];
                }
            }

            // Process any remaining elements past the last vector.
            for (; (uint)i < (uint)x.Length; i++)
            {
                dotProduct += x[i] * y[i];
                xSumOfSquares += x[i] * x[i];
                ySumOfSquares += y[i] * y[i];
            }

            // Sum(X * Y) / (|X| * |Y|)
            return dotProduct / (MathF.Sqrt(xSumOfSquares) * MathF.Sqrt(ySumOfSquares));
        }

        private static float Aggregate<TLoad, TAggregate>(
            float identityValue, ReadOnlySpan<float> x, TLoad load = default, TAggregate aggregate = default)
            where TLoad : struct, IUnaryOperator
            where TAggregate : struct, IBinaryOperator
        {
            // Initialize the result to the identity value
            float result = identityValue;
            int i = 0;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results
                Vector<float> resultVector = load.Invoke(AsVector(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector<float>.Count;
                do
                {
                    resultVector = aggregate.Invoke(resultVector, load.Invoke(AsVector(ref xRef, i)));
                    i += Vector<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                for (int f = 0; f < Vector<float>.Count; f++)
                {
                    result = aggregate.Invoke(result, resultVector[f]);
                }
            }

            // Aggregate the remaining items in the input span.
            for (; (uint)i < (uint)x.Length; i++)
            {
                result = aggregate.Invoke(result, load.Invoke(x[i]));
            }

            return result;
        }

        private static float Aggregate<TBinary, TAggregate>(
            float identityValue, ReadOnlySpan<float> x, ReadOnlySpan<float> y, TBinary binary = default, TAggregate aggregate = default)
            where TBinary : struct, IBinaryOperator
            where TAggregate : struct, IBinaryOperator
        {
            // Initialize the result to the identity value
            float result = identityValue;
            int i = 0;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                // Load the first vector as the initial set of results
                Vector<float> resultVector = binary.Invoke(AsVector(ref xRef, 0), AsVector(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                i = Vector<float>.Count;
                do
                {
                    resultVector = aggregate.Invoke(resultVector, binary.Invoke(AsVector(ref xRef, i), AsVector(ref yRef, i)));
                    i += Vector<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Aggregate the lanes in the vector back into the scalar result
                for (int f = 0; f < Vector<float>.Count; f++)
                {
                    result = aggregate.Invoke(result, resultVector[f]);
                }
            }

            // Aggregate the remaining items in the input span.
            for (; (uint)i < (uint)x.Length; i++)
            {
                result = aggregate.Invoke(result, binary.Invoke(x[i], y[i]));
            }

            return result;
        }

        private static float MinMaxCore<TMinMax>(ReadOnlySpan<float> x, TMinMax minMax = default) where TMinMax : struct, IBinaryOperator
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            // Initialize the result to the identity value
            float result = x[0];
            int i = 0;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count * 2)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector<float> resultVector = AsVector(ref xRef, 0), current;
                if (Vector.EqualsAll(resultVector, resultVector))
                {
                    int oneVectorFromEnd = x.Length - Vector<float>.Count;

                    // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                    i = Vector<float>.Count;
                    do
                    {
                        // Load the next vector, and early exit on NaN.
                        current = AsVector(ref xRef, i);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        resultVector = minMax.Invoke(resultVector, current);
                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // If any elements remain, handle them in one final vector.
                    if (i != x.Length)
                    {
                        current = AsVector(ref xRef, x.Length - Vector<float>.Count);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        resultVector = minMax.Invoke(resultVector, current);
                    }

                    // Aggregate the lanes in the vector to create the final scalar result.
                    for (int f = 0; f < Vector<float>.Count; f++)
                    {
                        result = minMax.Invoke(result, resultVector[f]);
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

                result = minMax.Invoke(result, current);
            }

            return result;
        }

        private static void InvokeSpanIntoSpan<TUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination, TUnaryOperator op = default)
            where TUnaryOperator : struct, IUnaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
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
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i));

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        AsVector(ref dRef, lastVectorIndex) = op.Invoke(AsVector(ref xRef, lastVectorIndex));
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
                        AsVector(ref dRef, lastVectorIndex) = op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                                                        AsVector(ref yRef, lastVectorIndex));
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

        private static void InvokeSpanScalarIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination, TBinaryOperator op = default)
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector<float>.Count;
                if (oneVectorFromEnd >= 0)
                {
                    // Loop handling one vector at a time.
                    Vector<float> yVec = new(y);
                    do
                    {
                        AsVector(ref dRef, i) = op.Invoke(AsVector(ref xRef, i),
                                                          yVec);

                        i += Vector<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        int lastVectorIndex = x.Length - Vector<float>.Count;
                        AsVector(ref dRef, lastVectorIndex) = op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                                                        yVec);
                    }

                    return;
                }
            }

            // Loop handling one element at a time.
            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i),
                                                    y);

                i++;
            }
        }

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
                        AsVector(ref dRef, lastVectorIndex) = op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                                                        AsVector(ref yRef, lastVectorIndex),
                                                                        AsVector(ref zRef, lastVectorIndex));
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
                        AsVector(ref dRef, lastVectorIndex) = op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                                                        AsVector(ref yRef, lastVectorIndex),
                                                                        zVec);
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
                        AsVector(ref dRef, lastVectorIndex) = op.Invoke(AsVector(ref xRef, lastVectorIndex),
                                                                        yVec,
                                                                        AsVector(ref zRef, lastVectorIndex));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector<float> AsVector(ref float start, int offset) =>
            ref Unsafe.As<float, Vector<float>>(
                ref Unsafe.Add(ref start, offset));

        private static unsafe bool IsNegative(float f) => *(int*)&f < 0;

        private static unsafe Vector<float> IsNegative(Vector<float> f) =>
            (Vector<float>)Vector.LessThan((Vector<int>)f, Vector<int>.Zero);

        private static float Log2(float x) => MathF.Log(x, 2);

        private readonly struct AddOperator : IBinaryOperator
        {
            public float Invoke(float x, float y) => x + y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x + y;
        }

        private readonly struct SubtractOperator : IBinaryOperator
        {
            public float Invoke(float x, float y) => x - y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x - y;
        }

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

        private readonly struct MultiplyOperator : IBinaryOperator
        {
            public float Invoke(float x, float y) => x * y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x * y;
        }

        private readonly struct DivideOperator : IBinaryOperator
        {
            public float Invoke(float x, float y) => x / y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x / y;
        }

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

        private readonly struct NegateOperator : IUnaryOperator
        {
            public float Invoke(float x) => -x;
            public Vector<float> Invoke(Vector<float> x) => -x;
        }

        private readonly struct AddMultiplyOperator : ITernaryOperator
        {
            public float Invoke(float x, float y, float z) => (x + y) * z;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z) => (x + y) * z;
        }

        private readonly struct MultiplyAddOperator : ITernaryOperator
        {
            public float Invoke(float x, float y, float z) => (x * y) + z;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z) => (x * y) + z;
        }

        private readonly struct IdentityOperator : IUnaryOperator
        {
            public float Invoke(float x) => x;
            public Vector<float> Invoke(Vector<float> x) => x;
        }

        private readonly struct SquaredOperator : IUnaryOperator
        {
            public float Invoke(float x) => x * x;
            public Vector<float> Invoke(Vector<float> x) => x * x;
        }

        private readonly struct AbsoluteOperator : IUnaryOperator
        {
            public float Invoke(float x) => MathF.Abs(x);
            public Vector<float> Invoke(Vector<float> x) => Vector.Abs(x);
        }

        private interface IUnaryOperator
        {
            float Invoke(float x);
            Vector<float> Invoke(Vector<float> x);
        }

        private interface IBinaryOperator
        {
            float Invoke(float x, float y);
            Vector<float> Invoke(Vector<float> x, Vector<float> y);
        }

        private interface ITernaryOperator
        {
            float Invoke(float x, float y, float z);
            Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z);
        }
    }
}
