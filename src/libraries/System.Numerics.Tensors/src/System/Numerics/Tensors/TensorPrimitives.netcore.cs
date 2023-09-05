// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" />[i]</c>.</remarks>
        public static unsafe void Add(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination)
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength(nameof(x), nameof(y));
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
                while (i <= oneVectorFromEnd)
                {
                    Vector512<float> sum = Vector512.LoadUnsafe(ref xRef, (uint)i) + Vector512.LoadUnsafe(ref yRef, (uint)i);
                    Vector512.StoreUnsafe(sum, ref dRef, (uint)i);

                    i += Vector512<float>.Count;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                while (i <= oneVectorFromEnd)
                {
                    Vector256<float> sum = Vector256.LoadUnsafe(ref xRef, (uint)i) + Vector256.LoadUnsafe(ref yRef, (uint)i);
                    Vector256.StoreUnsafe(sum, ref dRef, (uint)i);

                    i += Vector256<float>.Count;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                while (i <= oneVectorFromEnd)
                {
                    Vector128<float> sum = Vector128.LoadUnsafe(ref xRef, (uint)i) + Vector128.LoadUnsafe(ref yRef, (uint)i);
                    Vector128.StoreUnsafe(sum, ref dRef, (uint)i);

                    i += Vector128<float>.Count;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = Unsafe.Add(ref xRef, i) + Unsafe.Add(ref yRef, i);

                i++;
            }
        }

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" /></c>.</remarks>
        public static void Add(ReadOnlySpan<float> x, float y, Span<float> destination)
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
                    do
                    {
                        Vector512<float> sum = Vector512.LoadUnsafe(ref xRef, (uint)i) + yVec;
                        Vector512.StoreUnsafe(sum, ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> yVec = Vector256.Create(y);
                    do
                    {
                        Vector256<float> sum = Vector256.LoadUnsafe(ref xRef, (uint)i) + yVec;
                        Vector256.StoreUnsafe(sum, ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> yVec = Vector128.Create(y);
                    do
                    {
                        Vector128<float> sum = Vector128.LoadUnsafe(ref xRef, (uint)i) + yVec;
                        Vector128.StoreUnsafe(sum, ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = Unsafe.Add(ref xRef, i) + y;

                i++;
            }
        }
    }
}
