// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>Operator that takes one input value and returns two output values.</summary>
        private interface IUnaryInputBinaryOutput<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract (T, T) Invoke(T x);
            static abstract (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x);
            static abstract (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x);
            static abstract (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x);
        }

        /// <summary>Operator that takes two input values and returns two output values.</summary>
        private interface IBinaryInputBinaryOutput<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract (T, T) Invoke(T x, T y);
            static abstract (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x, Vector128<T> y);
            static abstract (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x, Vector256<T> y);
            static abstract (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x, Vector512<T> y);
            static abstract T RemainderMaskValue { get; }
        }

        private readonly struct SwappedBinaryInputBinaryOutput<TOperator, T> : IBinaryInputBinaryOutput<T>
            where TOperator : struct, IBinaryInputBinaryOutput<T>
        {
            public static bool Vectorizable => TOperator.Vectorizable;
            public static (T, T) Invoke(T x, T y) => TOperator.Invoke(y, x);
            public static (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x, Vector128<T> y) => TOperator.Invoke(y, x);
            public static (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x, Vector256<T> y) => TOperator.Invoke(y, x);
            public static (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x, Vector512<T> y) => TOperator.Invoke(y, x);
            public static T RemainderMaskValue => TOperator.RemainderMaskValue;
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination1"/> and <paramref name="destination2"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpanSpan<T, TUnaryOperator>(
            ReadOnlySpan<T> x, Span<T> destination1, Span<T> destination2)
            where TUnaryOperator : struct, IUnaryInputBinaryOutput<T>
        {
            if (x.Length > destination1.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination1));
            }

            if (x.Length > destination2.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination2));
            }

            ValidateInputOutputSpanNonOverlapping(x, destination1);
            ValidateInputOutputSpanNonOverlapping(x, destination2);
            ValidateOutputSpansNonOverlapping(x.Length, destination1, destination2);

            ref T sourceRef = ref MemoryMarshal.GetReference(x);
            ref T destination1Ref = ref MemoryMarshal.GetReference(destination1);
            ref T destination2Ref = ref MemoryMarshal.GetReference(destination2);
            int i = 0, oneVectorFromEnd;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TUnaryOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector512<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector512<T> first, Vector512<T> second) = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector512<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector512<T>.Count;

                        (Vector512<T> first, Vector512<T> second) = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TUnaryOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector256<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector256<T> first, Vector256<T> second) = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector256<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector256<T>.Count;

                        (Vector256<T> first, Vector256<T> second) = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TUnaryOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector128<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector128<T> first, Vector128<T> second) = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector128<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector128<T>.Count;

                        (Vector128<T> first, Vector128<T> second) = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                (T first, T second) = TUnaryOperator.Invoke(Unsafe.Add(ref sourceRef, i));
                Unsafe.Add(ref destination1Ref, i) = first;
                Unsafe.Add(ref destination2Ref, i) = second;
                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/> and writes the results
        /// to <paramref name="destination1"/> and <paramref name="destination2"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanSpanIntoSpanSpan<T, TOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination1, Span<T> destination2)
            where TOperator : struct, IBinaryInputBinaryOutput<T>
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination1.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination1));
            }

            if (x.Length > destination2.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination2));
            }

            ValidateInputOutputSpanNonOverlapping(x, destination1);
            ValidateInputOutputSpanNonOverlapping(x, destination2);
            ValidateInputOutputSpanNonOverlapping(y, destination1);
            ValidateInputOutputSpanNonOverlapping(y, destination2);
            ValidateOutputSpansNonOverlapping(x.Length, destination1, destination2);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T destination1Ref = ref MemoryMarshal.GetReference(destination1);
            ref T destination2Ref = ref MemoryMarshal.GetReference(destination2);
            int i = 0, oneVectorFromEnd;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector512<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector512<T> first, Vector512<T> second) = TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i), Vector512.LoadUnsafe(ref yRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector512<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        Vector512<T> mask = Vector512.Equals(CreateRemainderMaskVector512<T>(x.Length - i), Vector512<T>.Zero);

                        i = x.Length - Vector512<T>.Count;

                        Vector512<T> first = Vector512.ConditionalSelect(mask,
                            Vector512.Create(TOperator.RemainderMaskValue),
                            Vector512.LoadUnsafe(ref xRef, (uint)i));

                        Vector512<T> second = Vector512.ConditionalSelect(mask,
                            Vector512.Create(TOperator.RemainderMaskValue),
                            Vector512.LoadUnsafe(ref yRef, (uint)i));

                        (first, second) = TOperator.Invoke(first, second);

                        Vector512.ConditionalSelect(mask,
                            Vector512.LoadUnsafe(ref destination1Ref, (uint)i),
                            first)
                            .StoreUnsafe(ref destination1Ref, (uint)i);

                        Vector512.ConditionalSelect(mask,
                            Vector512.LoadUnsafe(ref destination2Ref, (uint)i),
                            second)
                            .StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector256<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector256<T> first, Vector256<T> second) = TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i), Vector256.LoadUnsafe(ref yRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector256<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        Vector256<T> mask = Vector256.Equals(CreateRemainderMaskVector256<T>(x.Length - i), Vector256<T>.Zero);

                        i = x.Length - Vector256<T>.Count;

                        Vector256<T> first = Vector256.ConditionalSelect(mask,
                            Vector256.Create(TOperator.RemainderMaskValue),
                            Vector256.LoadUnsafe(ref xRef, (uint)i));

                        Vector256<T> second = Vector256.ConditionalSelect(mask,
                            Vector256.Create(TOperator.RemainderMaskValue),
                            Vector256.LoadUnsafe(ref yRef, (uint)i));

                        (first, second) = TOperator.Invoke(first, second);

                        Vector256.ConditionalSelect(mask,
                            Vector256.LoadUnsafe(ref destination1Ref, (uint)i),
                            first)
                            .StoreUnsafe(ref destination1Ref, (uint)i);

                        Vector256.ConditionalSelect(mask,
                            Vector256.LoadUnsafe(ref destination2Ref, (uint)i),
                            second)
                            .StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector128<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector128<T> first, Vector128<T> second) = TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i), Vector128.LoadUnsafe(ref yRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector128<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        Vector128<T> mask = Vector128.Equals(CreateRemainderMaskVector128<T>(x.Length - i), Vector128<T>.Zero);

                        i = x.Length - Vector128<T>.Count;

                        Vector128<T> first = Vector128.ConditionalSelect(mask,
                            Vector128.Create(TOperator.RemainderMaskValue),
                            Vector128.LoadUnsafe(ref xRef, (uint)i));

                        Vector128<T> second = Vector128.ConditionalSelect(mask,
                            Vector128.Create(TOperator.RemainderMaskValue),
                            Vector128.LoadUnsafe(ref yRef, (uint)i));

                        (first, second) = TOperator.Invoke(first, second);

                        Vector128.ConditionalSelect(mask,
                            Vector128.LoadUnsafe(ref destination1Ref, (uint)i),
                            first)
                            .StoreUnsafe(ref destination1Ref, (uint)i);

                        Vector128.ConditionalSelect(mask,
                            Vector128.LoadUnsafe(ref destination2Ref, (uint)i),
                            second)
                            .StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                (T first, T second) = TOperator.Invoke(Unsafe.Add(ref xRef, i), Unsafe.Add(ref yRef, i));
                Unsafe.Add(ref destination1Ref, i) = first;
                Unsafe.Add(ref destination2Ref, i) = second;
                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/> and writes the results
        /// to <paramref name="destination1"/> and <paramref name="destination2"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanScalarIntoSpanSpan<T, TOperator>(
            ReadOnlySpan<T> x, T y, Span<T> destination1, Span<T> destination2)
            where TOperator : struct, IBinaryInputBinaryOutput<T>
        {
            if (x.Length > destination1.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination1));
            }

            if (x.Length > destination2.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination2));
            }

            ValidateInputOutputSpanNonOverlapping(x, destination1);
            ValidateInputOutputSpanNonOverlapping(x, destination2);
            ValidateOutputSpansNonOverlapping(x.Length, destination1, destination2);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T destination1Ref = ref MemoryMarshal.GetReference(destination1);
            ref T destination2Ref = ref MemoryMarshal.GetReference(destination2);
            int i = 0, oneVectorFromEnd;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector512<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<T> yVec = Vector512.Create(y);

                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector512<T> first, Vector512<T> second) = TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i), yVec);
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector512<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        Vector512<T> mask = Vector512.Equals(CreateRemainderMaskVector512<T>(x.Length - i), Vector512<T>.Zero);

                        i = x.Length - Vector512<T>.Count;

                        Vector512<T> first = Vector512.ConditionalSelect(mask,
                            Vector512.Create(TOperator.RemainderMaskValue),
                            Vector512.LoadUnsafe(ref xRef, (uint)i));

                        Vector512<T> second = Vector512.ConditionalSelect(mask,
                            Vector512.Create(TOperator.RemainderMaskValue),
                            yVec);

                        (first, second) = TOperator.Invoke(first, second);

                        Vector512.ConditionalSelect(mask,
                            Vector512.LoadUnsafe(ref destination1Ref, (uint)i),
                            first)
                            .StoreUnsafe(ref destination1Ref, (uint)i);

                        Vector512.ConditionalSelect(mask,
                            Vector512.LoadUnsafe(ref destination2Ref, (uint)i),
                            second)
                            .StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector256<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<T> yVec = Vector256.Create(y);

                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector256<T> first, Vector256<T> second) = TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i), yVec);
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector256<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        Vector256<T> mask = Vector256.Equals(CreateRemainderMaskVector256<T>(x.Length - i), Vector256<T>.Zero);

                        i = x.Length - Vector256<T>.Count;

                        Vector256<T> first = Vector256.ConditionalSelect(mask,
                            Vector256.Create(TOperator.RemainderMaskValue),
                            Vector256.LoadUnsafe(ref xRef, (uint)i));

                        Vector256<T> second = Vector256.ConditionalSelect(mask,
                            Vector256.Create(TOperator.RemainderMaskValue),
                            yVec);

                        (first, second) = TOperator.Invoke(first, second);

                        Vector256.ConditionalSelect(mask,
                            Vector256.LoadUnsafe(ref destination1Ref, (uint)i),
                            first)
                            .StoreUnsafe(ref destination1Ref, (uint)i);

                        Vector256.ConditionalSelect(mask,
                            Vector256.LoadUnsafe(ref destination2Ref, (uint)i),
                            second)
                            .StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector128<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<T> yVec = Vector128.Create(y);

                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector128<T> first, Vector128<T> second) = TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i), yVec);
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector128<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        Vector128<T> mask = Vector128.Equals(CreateRemainderMaskVector128<T>(x.Length - i), Vector128<T>.Zero);

                        i = x.Length - Vector128<T>.Count;

                        Vector128<T> first = Vector128.ConditionalSelect(mask,
                            Vector128.Create(TOperator.RemainderMaskValue),
                            Vector128.LoadUnsafe(ref xRef, (uint)i));

                        Vector128<T> second = Vector128.ConditionalSelect(mask,
                            Vector128.Create(TOperator.RemainderMaskValue),
                            yVec);

                        (first, second) = TOperator.Invoke(first, second);

                        Vector128.ConditionalSelect(mask,
                            Vector128.LoadUnsafe(ref destination1Ref, (uint)i),
                            first)
                            .StoreUnsafe(ref destination1Ref, (uint)i);

                        Vector128.ConditionalSelect(mask,
                            Vector128.LoadUnsafe(ref destination2Ref, (uint)i),
                            second)
                            .StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                (T first, T second) = TOperator.Invoke(Unsafe.Add(ref xRef, i), y);
                Unsafe.Add(ref destination1Ref, i) = first;
                Unsafe.Add(ref destination2Ref, i) = second;
                i++;
            }
        }

        private static void InvokeScalarSpanIntoSpanSpan<T, TOperator>(
            T x, ReadOnlySpan<T> y, Span<T> destination1, Span<T> destination2)
            where TOperator : struct, IBinaryInputBinaryOutput<T> =>
            InvokeSpanScalarIntoSpanSpan<T, SwappedBinaryInputBinaryOutput<TOperator, T>>(y, x, destination1, destination2);
    }
}
