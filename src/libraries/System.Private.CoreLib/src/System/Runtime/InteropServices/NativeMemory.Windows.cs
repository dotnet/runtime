// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>This class contains methods that are mainly used to manage native memory.</summary>
    public static unsafe partial class NativeMemory
    {
        /// <summary>Allocates an aligned block of memory of the specified size and alignment, in bytes.</summary>
        /// <param name="byteCount">The size, in bytes, of the block to allocate.</param>
        /// <param name="alignment">The alignment, in bytes, of the block to allocate. This must be a power of <c>2</c>.</param>
        /// <returns>A pointer to the allocated aligned block of memory.</returns>
        /// <exception cref="ArgumentException"><paramref name="alignment" /> is not a power of two.</exception>
        /// <exception cref="OutOfMemoryException">Allocating <paramref name="byteCount" /> of memory with <paramref name="alignment" /> failed.</exception>
        /// <remarks>
        ///     <para>This method allows <paramref name="byteCount" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a thin wrapper over the C <c>aligned_alloc</c> API or a platform dependent aligned allocation API such as <c>_aligned_malloc</c> on Win32.</para>
        ///     <para>This method is not compatible with <see cref="Free" /> or <see cref="Realloc" />, instead <see cref="AlignedFree" /> or <see cref="AlignedRealloc" /> should be called.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* AlignedAlloc(nuint byteCount, nuint alignment)
        {
            if (!BitOperations.IsPow2(alignment))
            {
                // The C standard doesn't define what a valid alignment is, however Windows and POSIX The Windows implementation requires a power of 2
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AlignmentMustBePow2);
            }

            // Unlike the C standard and POSIX, Windows does not requires size to be a multiple of alignment. However, we do want an "empty" allocation for zero
            void* result = Interop.Ucrtbase._aligned_malloc((byteCount != 0) ? byteCount : 1, alignment);

            if (result == null)
            {
                ThrowHelper.ThrowOutOfMemoryException();
            }

            return result;
        }

        /// <summary>Frees an aligned block of memory.</summary>
        /// <param name="ptr">A pointer to the aligned block of memory that should be freed.</param>
        /// <remarks>
        ///    <para>This method does nothing if <paramref name="ptr" /> is <c>null</c>.</para>
        ///    <para>This method is a thin wrapper over the C <c>free</c> API or a platform dependent aligned free API such as <c>_aligned_free</c> on Win32.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void AlignedFree(void* ptr)
        {
            if (ptr != null)
            {
                Interop.Ucrtbase._aligned_free(ptr);
            }
        }

        /// <summary>Reallocates an aligned block of memory of the specified size and alignment, in bytes.</summary>
        /// <param name="ptr">The previously allocated block of memory.</param>
        /// <param name="byteCount">The size, in bytes, of the block to allocate.</param>
        /// <param name="alignment">The alignment, in bytes, of the block to allocate. This must be a power of <c>2</c>.</param>
        /// <returns>A pointer to the reallocated aligned block of memory.</returns>
        /// <exception cref="ArgumentException"><paramref name="alignment" /> is not a power of two.</exception>
        /// <exception cref="OutOfMemoryException">Reallocating <paramref name="byteCount" /> of memory with <paramref name="alignment" /> failed.</exception>
        /// <remarks>
        ///     <para>This method acts as <see cref="AlignedAlloc" /> if <paramref name="ptr" /> is <c>null</c>.</para>
        ///     <para>This method allows <paramref name="byteCount" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a platform dependent aligned reallocation API such as <c>_aligned_realloc</c> on Win32.</para>
        ///     <para>This method is not compatible with <see cref="Free" /> or <see cref="Realloc" />, instead <see cref="AlignedFree" /> or <see cref="AlignedRealloc" /> should be called.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* AlignedRealloc(void* ptr, nuint byteCount, nuint alignment)
        {
            if (!BitOperations.IsPow2(alignment))
            {
                // The C standard doesn't define what a valid alignment is, however Windows and POSIX The Windows implementation requires a power of 2
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AlignmentMustBePow2);
            }

            // Unlike the C standard and POSIX, Windows does not requires size to be a multiple of alignment. However, we do want an "empty" allocation for zero
            void* result = Interop.Ucrtbase._aligned_realloc(ptr, (byteCount != 0) ? byteCount : 1, alignment);

            if (result == null)
            {
                ThrowHelper.ThrowOutOfMemoryException();
            }

            return result;
        }

        /// <summary>Allocates a block of memory of the specified size, in bytes.</summary>
        /// <param name="byteCount">The size, in bytes, of the block to allocate.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        /// <exception cref="OutOfMemoryException">Allocating <paramref name="byteCount" /> of memory failed.</exception>
        /// <remarks>
        ///     <para>This method allows <paramref name="byteCount" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a thin wrapper over the C <c>malloc</c> API.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* Alloc(nuint byteCount)
        {
            // The Windows implementation handles size == 0 as we expect
            void* result = Interop.Ucrtbase.malloc(byteCount);

            if (result == null)
            {
                ThrowHelper.ThrowOutOfMemoryException();
            }

            return result;
        }

        /// <summary>Allocates and zeroes a block of memory of the specified size, in elements.</summary>
        /// <param name="elementCount">The count, in elements, of the block to allocate.</param>
        /// <param name="elementSize">The size, in bytes, of each element in the allocation.</param>
        /// <returns>A pointer to the allocated and zeroed block of memory.</returns>
        /// <exception cref="OutOfMemoryException">Allocating <paramref name="elementCount" /> * <paramref name="elementSize" /> bytes of memory failed.</exception>
        /// <remarks>
        ///     <para>This method allows <paramref name="elementCount" /> and/or <paramref name="elementSize" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a thin wrapper over the C <c>calloc</c> API.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* AllocZeroed(nuint elementCount, nuint elementSize)
        {
            // The Windows implementation handles num == 0 && size == 0 as we expect
            void* result = Interop.Ucrtbase.calloc(elementCount, elementSize);

            if (result == null)
            {
                ThrowHelper.ThrowOutOfMemoryException();
            }

            return result;
        }

        /// <summary>Frees a block of memory.</summary>
        /// <param name="ptr">A pointer to the block of memory that should be freed.</param>
        /// <remarks>
        ///    <para>This method does nothing if <paramref name="ptr" /> is <c>null</c>.</para>
        ///    <para>This method is a thin wrapper over the C <c>free</c> API.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void Free(void* ptr)
        {
            if (ptr != null)
            {
                Interop.Ucrtbase.free(ptr);
            }
        }

        /// <summary>Reallocates a block of memory to be the specified size, in bytes.</summary>
        /// <param name="ptr">The previously allocated block of memory.</param>
        /// <param name="byteCount">The size, in bytes, of the reallocated block.</param>
        /// <returns>A pointer to the reallocated block of memory.</returns>
        /// <exception cref="OutOfMemoryException">Reallocating <paramref name="byteCount" /> of memory failed.</exception>
        /// <remarks>
        ///     <para>This method acts as <see cref="Alloc" /> if <paramref name="ptr" /> is <c>null</c>.</para>
        ///     <para>This method allows <paramref name="byteCount" /> to be <c>0</c> and will return a valid pointer that should not be dereferenced and that should be passed to free to avoid memory leaks.</para>
        ///     <para>This method is a thin wrapper over the C <c>realloc</c> API.</para>
        /// </remarks>
        [CLSCompliant(false)]
        public static void* Realloc(void* ptr, nuint byteCount)
        {
            // The Windows implementation treats size == 0 as Free, we want an "empty" allocation
            void* result = Interop.Ucrtbase.realloc(ptr, (byteCount != 0) ? byteCount : 1);

            if (result == null)
            {
                ThrowHelper.ThrowOutOfMemoryException();
            }

            return result;
        }
    }
}
