// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination1"/> and <paramref name="destination2"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan_TwoOutputs<T, TUnaryOperator>(
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
    }
}
