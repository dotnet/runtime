// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        private static void InvokeSpanIntoSpan<TUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination, TUnaryOperator op = default)
            where TUnaryOperator : IUnaryOperator
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
            where TBinaryOperator : IBinaryOperator
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
