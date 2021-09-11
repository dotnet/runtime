// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static unsafe partial class NativeMemory
    {
        /// <summary>Allocates a block of memory of the specified size, in elements.</summary>
        /// <param name="elementCount">The count, in elements, of the block to allocate.</param>
        /// <param name="elementSize">The size, in bytes, of each element in the allocation.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        /// <exception cref="OutOfMemoryException">Allocating <paramref name="elementCount" /> * <paramref name="elementSize" /> bytes of memory failed.</exception>
        /// <remarks>
        ///     <para>This method allows <paramref name="elementCount" /> and/or <paramref name="elementSize" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a thin wrapper over the C <c>malloc</c> API.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* Alloc(nuint elementCount, nuint elementSize)
        {
            nuint byteCount = GetByteCount(elementCount, elementSize);
            return Alloc(byteCount);
        }

        /// <summary>Allocates and zeroes a block of memory of the specified size, in bytes.</summary>
        /// <param name="byteCount">The size, in bytes, of the block to allocate.</param>
        /// <returns>A pointer to the allocated and zeroed block of memory.</returns>
        /// <exception cref="OutOfMemoryException">Allocating <paramref name="byteCount" /> of memory failed.</exception>
        /// <remarks>
        ///     <para>This method allows <paramref name="byteCount" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a thin wrapper over the C <c>calloc</c> API.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* AllocZeroed(nuint byteCount)
        {
            return AllocZeroed(byteCount, elementSize: 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint GetByteCount(nuint elementCount, nuint elementSize)
        {
            // This is based on the `mi_count_size_overflow` and `mi_mul_overflow` methods from microsoft/mimalloc.
            // Original source is Copyright (c) 2019 Microsoft Corporation, Daan Leijen. Licensed under the MIT license

            // sqrt(nuint.MaxValue)
            nuint multiplyNoOverflow = (nuint)1 << (4 * sizeof(nuint));

            return ((elementSize >= multiplyNoOverflow) || (elementCount >= multiplyNoOverflow)) && (elementSize > 0) && ((nuint.MaxValue / elementSize) < elementCount) ? nuint.MaxValue : (elementCount * elementSize);
        }
    }
}
