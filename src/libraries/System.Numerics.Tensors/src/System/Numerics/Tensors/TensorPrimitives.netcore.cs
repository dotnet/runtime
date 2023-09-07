// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
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

        private readonly struct AddOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x + y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x + y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x + y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x + y;
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

        private readonly struct MultiplyOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x * y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x * y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x * y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x * y;
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
            public static Vector512<float> Invoke(Vector512<float> x) => -x;
        }

        private readonly struct AddMultiplyOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x + y) * z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x + y) * z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x + y) * z;
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x + y) * z;
        }

        private readonly struct MultiplyAddOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x * y) + z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x * y) + z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x * y) + z;
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x * y) + z;
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
