// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>Operator that takes one input value and returns a single value.</summary>
        /// <remarks>The input type must be twice the size of the output type.</remarks>
        private interface IUnaryTwoToOneOperator<TInput, TOutput>
        {
            static abstract bool Vectorizable { get; }
            static abstract TOutput Invoke(TInput x);
            static abstract Vector128<TOutput> Invoke(Vector128<TInput> lower, Vector128<TInput> upper);
            static abstract Vector256<TOutput> Invoke(Vector256<TInput> lower, Vector256<TInput> upper);
            static abstract Vector512<TOutput> Invoke(Vector512<TInput> lower, Vector512<TInput> upper);
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TInput">The element input type.</typeparam>
        /// <typeparam name="TOutput">The element output type. Must be the same size as TInput if TInput and TOutput both support vectorization.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        /// <remarks>This should only be used when it's known that TInput/TOutput are vectorizable and the size of TInput is twice that of TOutput.</remarks>
        private static void InvokeSpanIntoSpan_2to1<TInput, TOutput, TUnaryOperator>(
            ReadOnlySpan<TInput> x, Span<TOutput> destination)
            where TUnaryOperator : struct, IUnaryTwoToOneOperator<TInput, TOutput>
        {
            Debug.Assert(sizeof(TInput) == sizeof(TOutput) * 2);

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref TInput xRef = ref MemoryMarshal.GetReference(x);
            ref TOutput destinationRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, twoVectorsFromEnd;

            if (Vector512.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector512<TInput>.IsSupported);
                Debug.Assert(Vector512<TOutput>.IsSupported);

                twoVectorsFromEnd = x.Length - (Vector512<TInput>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(
                            Vector512.LoadUnsafe(ref xRef, (uint)i),
                            Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector512<TInput>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != x.Length)
                    {
                        i = x.Length - (Vector512<TInput>.Count * 2);

                        TUnaryOperator.Invoke(
                            Vector512.LoadUnsafe(ref xRef, (uint)i),
                            Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector256<TInput>.IsSupported);
                Debug.Assert(Vector256<TOutput>.IsSupported);

                twoVectorsFromEnd = x.Length - (Vector256<TInput>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(
                            Vector256.LoadUnsafe(ref xRef, (uint)i),
                            Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector256<TInput>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != x.Length)
                    {
                        i = x.Length - (Vector256<TInput>.Count * 2);

                        TUnaryOperator.Invoke(
                            Vector256.LoadUnsafe(ref xRef, (uint)i),
                            Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector128<TInput>.IsSupported);
                Debug.Assert(Vector128<TOutput>.IsSupported);

                twoVectorsFromEnd = x.Length - (Vector128<TInput>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(
                            Vector128.LoadUnsafe(ref xRef, (uint)i),
                            Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector128<TInput>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != x.Length)
                    {
                        i = x.Length - (Vector128<TInput>.Count * 2);

                        TUnaryOperator.Invoke(
                            Vector128.LoadUnsafe(ref xRef, (uint)i),
                            Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref destinationRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, i));
                i++;
            }
        }
    }
}
