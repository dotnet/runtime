// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides a collection of methods for interoperating with <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>,
    /// <see cref="Span{T}"/>, and <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public static partial class MemoryMarshal
    {
        /// <summary>
        /// Get an array segment from the underlying memory.
        /// If unable to get the array segment, return false with a default array segment.
        /// </summary>
        public static bool TryGetArray<T>(ReadOnlyMemory<T> memory, out ArraySegment<T> segment)
        {
            object obj = memory.GetObjectStartLength(out int index, out int length);
            if (index < 0)
            {
                Debug.Assert(length >= 0);
                if (((MemoryManager<T>)obj).TryGetArray(out ArraySegment<T> arraySegment))
                {
                    segment = new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + (index & ReadOnlyMemory<T>.RemoveFlagsBitMask), length);
                    return true;
                }
            }
            else if (obj is T[] arr)
            {
                segment = new ArraySegment<T>(arr, index, length & ReadOnlyMemory<T>.RemoveFlagsBitMask);
                return true;
            }

            if ((length & ReadOnlyMemory<T>.RemoveFlagsBitMask) == 0)
            {
#if FEATURE_PORTABLE_SPAN
                segment = new ArraySegment<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);
#else
                segment = ArraySegment<T>.Empty;
#endif // FEATURE_PORTABLE_SPAN
                return true;
            }

            segment = default;
            return false;
        }

        /// <summary>
        /// Gets an <see cref="MemoryManager{T}"/> from the underlying read-only memory.
        /// If unable to get the <typeparamref name="TManager"/> type, returns false.
        /// </summary>
        /// <typeparam name="T">The element type of the <paramref name="memory" />.</typeparam>
        /// <typeparam name="TManager">The type of <see cref="MemoryManager{T}"/> to try and retrive.</typeparam>
        /// <param name="memory">The memory to get the manager for.</param>
        /// <param name="manager">The returned manager of the <see cref="ReadOnlyMemory{T}"/>.</param>
        /// <returns>A <see cref="bool"/> indicating if it was successful.</returns>
        public static bool TryGetMemoryManager<T, TManager>(ReadOnlyMemory<T> memory, out TManager manager)
            where TManager : MemoryManager<T>
        {
            TManager localManager; // Use register for null comparison rather than byref
            manager = localManager = memory.GetObjectStartLength(out _, out _) as TManager;
            return !ReferenceEquals(manager, null);
        }

        /// <summary>
        /// Gets an <see cref="MemoryManager{T}"/> and <paramref name="start" />, <paramref name="length" /> from the underlying read-only memory.
        /// If unable to get the <typeparamref name="TManager"/> type, returns false.
        /// </summary>
        /// <typeparam name="T">The element type of the <paramref name="memory" />.</typeparam>
        /// <typeparam name="TManager">The type of <see cref="MemoryManager{T}"/> to try and retrive.</typeparam>
        /// <param name="memory">The memory to get the manager for.</param>
        /// <param name="manager">The returned manager of the <see cref="ReadOnlyMemory{T}"/>.</param>
        /// <param name="start">The offset from the start of the <paramref name="manager" /> that the <paramref name="memory" /> represents.</param>
        /// <param name="length">The length of the <paramref name="manager" /> that the <paramref name="memory" /> represents.</param>
        /// <returns>A <see cref="bool"/> indicating if it was successful.</returns>
        public static bool TryGetMemoryManager<T, TManager>(ReadOnlyMemory<T> memory, out TManager manager, out int start, out int length)
           where TManager : MemoryManager<T>
        {
            TManager localManager; // Use register for null comparison rather than byref
            manager = localManager = memory.GetObjectStartLength(out start, out length) as TManager;
            start &= ReadOnlyMemory<T>.RemoveFlagsBitMask;

            Debug.Assert(length >= 0);

            if (ReferenceEquals(manager, null))
            {
                start = default;
                length = default;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates an <see cref="IEnumerable{T}"/> view of the given <paramref name="memory" /> to allow
        /// the <paramref name="memory" /> to be used in existing APIs that take an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the <paramref name="memory" />.</typeparam>
        /// <param name="memory">The ReadOnlyMemory to view as an <see cref="IEnumerable{T}"/></param>
        /// <returns>An <see cref="IEnumerable{T}"/> view of the given <paramref name="memory" /></returns>
        public static IEnumerable<T> ToEnumerable<T>(ReadOnlyMemory<T> memory)
        {
            for (int i = 0; i < memory.Length; i++)
                yield return memory.Span[i];
        }

        /// <summary>Attempts to get the underlying <see cref="string"/> from a <see cref="ReadOnlyMemory{T}"/>.</summary>
        /// <param name="memory">The memory that may be wrapping a <see cref="string"/> object.</param>
        /// <param name="text">The string.</param>
        /// <param name="start">The starting location in <paramref name="text"/>.</param>
        /// <param name="length">The number of items in <paramref name="text"/>.</param>
        /// <returns></returns>
        public static bool TryGetString(ReadOnlyMemory<char> memory, out string text, out int start, out int length)
        {
            if (memory.GetObjectStartLength(out int offset, out int count) is string s)
            {
                Debug.Assert(offset >= 0);
                Debug.Assert(count >= 0);
                text = s;
                start = offset;
                length = count;
                return true;
            }
            else
            {
                text = null;
                start = 0;
                length = 0;
                return false;
            }
        }

        /// <summary>
        /// Reads a structure of type T out of a read-only span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(ReadOnlySpan<byte> source)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
#endif
            if (Unsafe.SizeOf<T>() > source.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }
            return Unsafe.ReadUnaligned<T>(ref GetReference(source));
        }

        /// <summary>
        /// Reads a structure of type T out of a span of bytes.
        /// <returns>If the span is too small to contain the type T, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRead<T>(ReadOnlySpan<byte> source, out T value)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
#endif
            if (Unsafe.SizeOf<T>() > (uint)source.Length)
            {
                value = default;
                return false;
            }
            value = Unsafe.ReadUnaligned<T>(ref GetReference(source));
            return true;
        }

        /// <summary>
        /// Writes a structure of type T into a span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(Span<byte> destination, ref T value)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
#endif
            if ((uint)Unsafe.SizeOf<T>() > (uint)destination.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }
            Unsafe.WriteUnaligned<T>(ref GetReference(destination), value);
        }

        /// <summary>
        /// Writes a structure of type T into a span of bytes.
        /// <returns>If the span is too small to contain the type T, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWrite<T>(Span<byte> destination, ref T value)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
#endif
            if (Unsafe.SizeOf<T>() > (uint)destination.Length)
            {
                return false;
            }
            Unsafe.WriteUnaligned<T>(ref GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Creates a new memory over the portion of the pre-pinned target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The pre-pinned target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <remarks>This method should only be called on an array that is already pinned and 
        /// that array should not be unpinned while the returned Memory<typeparamref name="T"/> is still in use.
        /// Calling this method on an unpinned array could result in memory corruption.</remarks>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> CreateFromPinnedArray<T>(T[] array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                return default;
            }
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            // Before using _length, check if _length < 0, then 'and' it with RemoveFlagsBitMask
            return new Memory<T>((object)array, start, length | (1 << 31));
        }
    }
}
