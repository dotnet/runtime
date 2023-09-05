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
            int remaining = x.Length;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && remaining >= Vector512<float>.Count)
            {
                do
                {
                    Vector512.StoreUnsafe(Vector512.LoadUnsafe(ref xRef) + Vector512.LoadUnsafe(ref yRef), ref dRef);

                    xRef = ref Unsafe.Add(ref xRef, Vector512<float>.Count);
                    yRef = ref Unsafe.Add(ref yRef, Vector512<float>.Count);
                    dRef = ref Unsafe.Add(ref dRef, Vector512<float>.Count);
                    remaining -= Vector512<float>.Count;
                }
                while (remaining >= Vector512<float>.Count);
            }
#endif

            if (Vector256.IsHardwareAccelerated && remaining >= Vector256<float>.Count)
            {
                do
                {
                    Vector256.StoreUnsafe(Vector256.LoadUnsafe(ref xRef) + Vector256.LoadUnsafe(ref yRef), ref dRef);

                    xRef = ref Unsafe.Add(ref xRef, Vector256<float>.Count);
                    yRef = ref Unsafe.Add(ref yRef, Vector256<float>.Count);
                    dRef = ref Unsafe.Add(ref dRef, Vector256<float>.Count);
                    remaining -= Vector256<float>.Count;
                }
                while (remaining >= Vector256<float>.Count);
            }

            if (Vector128.IsHardwareAccelerated && remaining >= Vector128<float>.Count)
            {
                do
                {
                    Vector128.StoreUnsafe(Vector128.LoadUnsafe(ref xRef) + Vector128.LoadUnsafe(ref yRef), ref dRef);

                    xRef = ref Unsafe.Add(ref xRef, Vector128<float>.Count);
                    yRef = ref Unsafe.Add(ref yRef, Vector128<float>.Count);
                    dRef = ref Unsafe.Add(ref dRef, Vector128<float>.Count);
                    remaining -= Vector128<float>.Count;
                }
                while (remaining >= Vector128<float>.Count);
            }

            while (remaining != 0)
            {
                dRef = xRef + yRef;

                xRef = ref Unsafe.Add(ref xRef, 1);
                yRef = ref Unsafe.Add(ref yRef, 1);
                dRef = ref Unsafe.Add(ref dRef, 1);
                remaining--;
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
            int remaining = x.Length;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && remaining >= Vector512<float>.Count)
            {
                Vector512<float> yVec = Vector512.Create(y);
                do
                {
                    Vector512.StoreUnsafe(Vector512.LoadUnsafe(ref xRef) + yVec, ref dRef);

                    xRef = ref Unsafe.Add(ref xRef, Vector512<float>.Count);
                    dRef = ref Unsafe.Add(ref dRef, Vector512<float>.Count);
                    remaining -= Vector512<float>.Count;
                }
                while (remaining >= Vector512<float>.Count);
            }
#endif

            if (Vector256.IsHardwareAccelerated && remaining >= Vector256<float>.Count)
            {
                Vector256<float> yVec = Vector256.Create(y);
                do
                {
                    Vector256.StoreUnsafe(Vector256.LoadUnsafe(ref xRef) + yVec, ref dRef);

                    xRef = ref Unsafe.Add(ref xRef, Vector256<float>.Count);
                    dRef = ref Unsafe.Add(ref dRef, Vector256<float>.Count);
                    remaining -= Vector256<float>.Count;
                }
                while (remaining >= Vector256<float>.Count);
            }

            if (Vector128.IsHardwareAccelerated && remaining >= Vector128<float>.Count)
            {
                Vector128<float> yVec = Vector128.Create(y);
                do
                {
                    Vector128.StoreUnsafe(Vector128.LoadUnsafe(ref xRef) + yVec, ref dRef);

                    xRef = ref Unsafe.Add(ref xRef, Vector128<float>.Count);
                    dRef = ref Unsafe.Add(ref dRef, Vector128<float>.Count);
                    remaining -= Vector128<float>.Count;
                }
                while (remaining >= Vector128<float>.Count);
            }

            while (remaining != 0)
            {
                dRef = xRef + y;

                xRef = ref Unsafe.Add(ref xRef, 1);
                dRef = ref Unsafe.Add(ref dRef, 1);
                remaining--;
            }
        }
    }
}
