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

        /// <summary>Clears a block of memory.</summary>
        /// <param name="ptr">A pointer to the block of memory that should be cleared.</param>
        /// <param name="byteCount">The size, in bytes, of the block to clear.</param>
        /// <remarks>
        ///     <para>If this method is called with <paramref name="ptr" /> being <see langword="null"/> and <paramref name="byteCount" /> being <c>0</c>, it will be equivalent to a no-op.</para>
        ///     <para>The behavior when <paramref name="ptr" /> is <see langword="null"/> and <paramref name="byteCount" /> is greater than <c>0</c> is undefined.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static unsafe void Clear(void* ptr, nuint byteCount)
        {
            SpanHelpers.ClearWithoutReferences(ref *(byte*)ptr, byteCount);
        }

        /// <summary>
        /// Copies a block of memory from memory location <paramref name="source"/>
        /// to memory location <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">A pointer to the source of data to be copied.</param>
        /// <param name="destination">A pointer to the destination memory block where the data is to be copied.</param>
        /// <param name="byteCount">The size, in bytes, to be copied from the source location to the destination.</param>
        [CLSCompliant(false)]
        public static void Copy(void* source, void* destination, nuint byteCount)
        {
            SpanHelpers.Memmove(ref *(byte*)destination, ref *(byte*)source, byteCount);
        }

        /// <summary>
        /// Copies the byte <paramref name="value"/> to the first <paramref name="byteCount"/> bytes
        /// of the memory located at <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr">A pointer to the block of memory to fill.</param>
        /// <param name="byteCount">The number of bytes to be set to <paramref name="value"/>.</param>
        /// <param name="value">The value to be set.</param>
        [CLSCompliant(false)]
        public static void Fill(void* ptr, nuint byteCount, byte value)
        {
            SpanHelpers.Fill(ref *(byte*)ptr, byteCount, value);
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
