// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

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
            object? obj = memory.GetObjectStartLength(out int index, out int length);

            // As an optimization, we skip the "is string?" check below if typeof(T) is not char,
            // as Memory<T> / ROM<T> can't possibly contain a string instance in this case.

            if (obj != null && !(
                (typeof(T) == typeof(char) && obj.GetType() == typeof(string))
#if FEATURE_UTF8STRING
                || ((typeof(T) == typeof(byte) || typeof(T) == typeof(Char8)) && obj.GetType() == typeof(Utf8String))
#endif // FEATURE_UTF8STRING
                ))
            {
                if (RuntimeHelpers.ObjectHasComponentSize(obj))
                {
                    // The object has a component size, which means it's variable-length, but we already
                    // checked above that it's not a string. The only remaining option is that it's a T[]
                    // or a U[] which is blittable to a T[] (e.g., int[] and uint[]).

                    // The array may be prepinned, so remove the high bit from the start index in the line below.
                    // The ArraySegment<T> ctor will perform bounds checking on index & length.

                    segment = new ArraySegment<T>(Unsafe.As<T[]>(obj), index & ReadOnlyMemory<T>.RemoveFlagsBitMask, length);
                    return true;
                }
                else
                {
                    // The object isn't null, and it's not variable-length, so the only remaining option
                    // is MemoryManager<T>. The ArraySegment<T> ctor will perform bounds checking on index & length.

                    Debug.Assert(obj is MemoryManager<T>);
                    if (Unsafe.As<MemoryManager<T>>(obj).TryGetArray(out ArraySegment<T> tempArraySegment))
                    {
                        segment = new ArraySegment<T>(tempArraySegment.Array!, tempArraySegment.Offset + index, length);
                        return true;
                    }
                }
            }

            // If we got to this point, the object is null, or it's a string, or it's a MemoryManager<T>
            // which isn't backed by an array. We'll quickly homogenize all zero-length Memory<T> instances
            // to an empty array for the purposes of reporting back to our caller.

            if (length == 0)
            {
                segment = ArraySegment<T>.Empty;
                return true;
            }

            // Otherwise, there's *some* data, but it's not convertible to T[].

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
        public static bool TryGetMemoryManager<T, TManager>(ReadOnlyMemory<T> memory, [NotNullWhen(true)] out TManager? manager)
            where TManager : MemoryManager<T>
        {
            TManager? localManager; // Use register for null comparison rather than byref
            manager = localManager = memory.GetObjectStartLength(out _, out _) as TManager;
            return localManager != null;
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
        public static bool TryGetMemoryManager<T, TManager>(ReadOnlyMemory<T> memory, [NotNullWhen(true)] out TManager? manager, out int start, out int length)
           where TManager : MemoryManager<T>
        {
            TManager? localManager; // Use register for null comparison rather than byref
            manager = localManager = memory.GetObjectStartLength(out start, out length) as TManager;

            Debug.Assert(length >= 0);

            if (localManager == null)
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
        public static bool TryGetString(ReadOnlyMemory<char> memory, [NotNullWhen(true)] out string? text, out int start, out int length)
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
            if (Unsafe.SizeOf<T>() > (uint)destination.Length)
            {
                return false;
            }
            Unsafe.WriteUnaligned<T>(ref GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Re-interprets a span of bytes as a reference to structure of type T.
        /// The type may not contain pointers or references. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <remarks>
        /// Supported only for platforms that support misaligned memory access or when the memory block is aligned by other means.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(Span<byte> span)
            where T : struct
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
            if (Unsafe.SizeOf<T>() > (uint)span.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }
            return ref Unsafe.As<byte, T>(ref GetReference(span));
        }

        /// <summary>
        /// Re-interprets a span of bytes as a reference to structure of type T.
        /// The type may not contain pointers or references. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <remarks>
        /// Supported only for platforms that support misaligned memory access or when the memory block is aligned by other means.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T AsRef<T>(ReadOnlySpan<byte> span)
            where T : struct
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }
            if (Unsafe.SizeOf<T>() > (uint)span.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }
            return ref Unsafe.As<byte, T>(ref GetReference(span));
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
        public static Memory<T> CreateFromPinnedArray<T>(T[]? array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                return default;
            }
            if (default(T)! == null && array.GetType() != typeof(T[])) // TODO-NULLABLE: default(T) == null warning (https://github.com/dotnet/roslyn/issues/34757)
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            // Before using _index, check if _index < 0, then 'and' it with RemoveFlagsBitMask
            return new Memory<T>((object)array, start | (1 << 31), length);
        }
    }
}
