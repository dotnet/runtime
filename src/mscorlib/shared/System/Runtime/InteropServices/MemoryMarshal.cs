// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;
#if !FEATURE_PORTABLE_SPAN
using Internal.Runtime.CompilerServices;
#endif // FEATURE_PORTABLE_SPAN

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides a collection of methods for interoperating with <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>,
    /// <see cref="Span{T}"/>, and <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public static class MemoryMarshal
    {
        /// <summary>Creates a <see cref="Memory{T}"/> from a <see cref="ReadOnlyMemory{T}"/>.</summary>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/>.</param>
        /// <returns>A <see cref="Memory{T}"/> representing the same memory as the <see cref="ReadOnlyMemory{T}"/>, but writable.</returns>
        /// <remarks>
        /// <see cref="AsMemory{T}(ReadOnlyMemory{T})"/> must be used with extreme caution.  <see cref="ReadOnlyMemory{T}"/> is used
        /// to represent immutable data and other memory that is not meant to be written to; <see cref="Memory{T}"/> instances created
        /// by <see cref="AsMemory{T}(ReadOnlyMemory{T})"/> should not be written to.  The method exists to enable variables typed
        /// as <see cref="Memory{T}"/> but only used for reading to store a <see cref="ReadOnlyMemory{T}"/>.
        /// </remarks>
        public static Memory<T> AsMemory<T>(ReadOnlyMemory<T> readOnlyMemory) =>
            Unsafe.As<ReadOnlyMemory<T>, Memory<T>>(ref readOnlyMemory);

#if FEATURE_PORTABLE_SPAN
        /// <summary>
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns a reference to the location where the 0th element
        /// would have been stored. Such a reference can be used for pinning but must never be dereferenced.
        /// </summary>
        public static ref T GetReference<T>(Span<T> span)
        {
            if (span.Pinnable == null)
                unsafe { return ref Unsafe.AsRef<T>(span.ByteOffset.ToPointer()); }
            else
                return ref Unsafe.AddByteOffset<T>(ref span.Pinnable.Data, span.ByteOffset);
        }

        /// <summary>
        /// Returns a reference to the 0th element of the ReadOnlySpan. If the Span is empty, returns a reference to the location where the 0th element
        /// would have been stored. Such a reference can be used for pinning but must never be dereferenced.
        /// </summary>
        public static ref T GetReference<T>(ReadOnlySpan<T> span)
        {
            if (span.Pinnable == null)
                unsafe { return ref Unsafe.AsRef<T>(span.ByteOffset.ToPointer()); }
            else
                return ref Unsafe.AddByteOffset<T>(ref span.Pinnable.Data, span.ByteOffset);
        }
#else
        /// <summary>
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns a reference to the location where the 0th element
        /// would have been stored. Such a reference can be used for pinning but must never be dereferenced.
        /// </summary>
        public static ref T GetReference<T>(Span<T> span) => ref span._pointer.Value;

        /// <summary>
        /// Returns a reference to the 0th element of the ReadOnlySpan. If the Span is empty, returns a reference to the location where the 0th element
        /// would have been stored. Such a reference can be used for pinning but must never be dereferenced.
        /// </summary>
        public static ref T GetReference<T>(ReadOnlySpan<T> span) => ref span._pointer.Value;
#endif // FEATURE_PORTABLE_SPAN

        /// <summary>
        /// Get an array segment from the underlying memory.
        /// If unable to get the array segment, return false with a default array segment.
        /// </summary>
        public static bool TryGetArray<T>(ReadOnlyMemory<T> readOnlyMemory, out ArraySegment<T> arraySegment)
        {
            object obj = readOnlyMemory.GetObjectStartLength(out int index, out int length);
            if (index < 0)
            {
                if (((OwnedMemory<T>)obj).TryGetArray(out var segment))
                {
                    arraySegment = new ArraySegment<T>(segment.Array, segment.Offset + (index & ReadOnlyMemory<T>.RemoveOwnedFlagBitMask), length);
                    return true;
                }
            }
            else if (obj is T[] arr)
            {
                arraySegment = new ArraySegment<T>(arr, index, length);
                return true;
            }

            arraySegment = default;
            return false;
        }
    }
}
