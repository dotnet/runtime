// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Span is a uniform API for dealing with arrays and subarrays, strings
    /// and substrings, and unmanaged memory buffers.  It adds minimal overhead
    /// to regular accesses and is a struct so that creation and subslicing do
    /// not require additional allocations.  It is type- and memory-safe.
    /// </summary>
    public struct Span<T>
    {
        /// <summary>A byref or a native ptr. Do not access directly</summary>
        internal /* readonly */ IntPtr _rawPointer;
        /// <summary>The number of elements this Span contains.</summary>
        internal readonly int _length;

        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant.</exception>
        public Span(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (default(T) == null) { // Arrays of valuetypes are never covariant
                if (array.GetType() != typeof(T[]))
                    ThrowHelper.ThrowArrayTypeMismatchException();
            }

            JitHelpers.SetByRef(out _rawPointer, ref JitHelpers.GetArrayData(array));
            _length = array.Length;
        }

        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the span.</param>
        /// <param name="length">The number of items in the span.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;&eq;Length).
        /// </exception>
        public Span(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (default(T) == null) { // Arrays of valuetypes are never covariant
                if (array.GetType() != typeof(T[]))
                    ThrowHelper.ThrowArrayTypeMismatchException();
            }
            if ((uint)start >= (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            JitHelpers.SetByRef(out _rawPointer, ref Unsafe.Add(ref JitHelpers.GetArrayData(array), start));
            _length = length;
        }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous, because we are creating arbitrarily typed T's
        /// out of a void*-typed block of memory.  And the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="ptr">An unmanaged pointer to memory.</param>
        /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is negative.
        /// </exception>
        [CLSCompliant(false)]
        public unsafe Span(void* ptr, int length)
        {
            if (JitHelpers.ContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _rawPointer = (IntPtr)ptr;
            _length = length;
        }

        /// <summary>
        /// An internal helper for creating spans. Not for public use.
        /// </summary>
        internal Span(ref T ptr, int length)
        {
            JitHelpers.SetByRef(out _rawPointer, ref ptr);
            _length = length;
        }

        public static implicit operator Span<T>(T[] array)
        {
            return new Span<T>(array);
        }

        public static implicit operator Span<T>(ArraySegment<T> arraySegment)
        {
            return new Span<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        public int Length
        {
            get { return _length; }
        }

        public static Span<T> Empty
        {
            get { return default(Span<T>); }
        }

        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        /// <summary>
        /// Fetches the element at the specified index.
        /// </summary>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when the specified <paramref name="index"/> is not in range (&lt;0 or &gt;&eq;Length).
        /// </exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_length)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return Unsafe.Add(ref JitHelpers.GetByRef<T>(ref _rawPointer), index);
            }
            set
            {
                if ((uint)index >= (uint)_length)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                Unsafe.Add(ref JitHelpers.GetByRef<T>(ref _rawPointer), index) = value;
            }
        }

        /// <summary>
        /// Copies the contents of this span into a new array.  This heap
        /// allocates, so should generally be avoided, however is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray()
        {
            if (_length == 0)
                return Array.Empty<T>();

            var destination = new T[_length];
            SpanHelper.CopyTo<T>(ref JitHelpers.GetArrayData(destination), ref JitHelpers.GetByRef<T>(ref _rawPointer), _length);
            return destination;
        }

        /// <summary>
        /// Forms a slice out of the given span, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        public Span<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref JitHelpers.GetByRef<T>(ref _rawPointer), start), _length - start);
        }

        /// <summary>
        /// Forms a slice out of the given span, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="end">The index at which to end this slice (exclusive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;&eq;Length).
        /// </exception>
        public Span<T> Slice(int start, int length)
        {
            if ((uint)start >= (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref JitHelpers.GetByRef<T>(ref _rawPointer), start), length);
        }

        /// <summary>
        /// Checks to see if two spans point at the same memory.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public bool Equals(Span<T> other)
        {
            return (_length == other._length) &&
                (_length == 0 || Unsafe.AreSame(ref JitHelpers.GetByRef<T>(ref _rawPointer), ref JitHelpers.GetByRef<T>(ref other._rawPointer)));
        }

        /// <summary>
        /// Copies the contents of this span into destination span. The destination
        /// must be at least as big as the source, and may be bigger.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(Span<T> destination)
        {
            if (Length > destination.Length)
                return false;

            SpanHelper.CopyTo<T>(ref JitHelpers.GetByRef<T>(ref destination._rawPointer), ref JitHelpers.GetByRef<T>(ref _rawPointer), _length);
            return true;
        }

        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="values"/>'s Length is longer than source span's Length.
        /// </exception>
        public void Set(ReadOnlySpan<T> values)
        {
            if ((uint)values._length > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            SpanHelper.CopyTo<T>(ref JitHelpers.GetByRef<T>(ref _rawPointer), ref JitHelpers.GetByRef<T>(ref values._rawPointer), values._length);
        }
    }

    public static class SpanExtensions
    {
        /// <summary>
        /// Casts a Span of one primitive type <typeparamref name="T"/> to Span of bytes.
        /// That type may not contain pointers. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <param name="source">The source slice, of type <typeparamref name="T"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="T"/> contains pointers.
        /// </exception>
        public static Span<byte> AsBytes<T>(this Span<T> source)
            where T : struct
        {
            if (JitHelpers.ContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));

            return new Span<byte>(
                ref JitHelpers.GetByRef<byte>(ref source._rawPointer),
                checked((int)(source._length * Unsafe.SizeOf<T>())));
        }

        /// <summary>
        /// Casts a ReadOnlySpan of one primitive type <typeparamref name="T"/> to ReadOnlySpan of bytes.
        /// That type may not contain pointers. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <param name="source">The source slice, of type <typeparamref name="T"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="T"/> contains pointers.
        /// </exception>
        public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source)
            where T : struct
        {
            if (JitHelpers.ContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));

            return new ReadOnlySpan<byte>(
                ref JitHelpers.GetByRef<byte>(ref source._rawPointer),
                checked((int)(source._length * Unsafe.SizeOf<T>())));
        }

        /// <summary>
        /// Casts a Span of one primitive type <typeparamref name="TFrom"/> to another primitive type <typeparamref name="TTo"/>.
        /// These types may not contain pointers. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <remarks>
        /// Supported only for platforms that support misaligned memory access.
        /// </remarks>
        /// <param name="source">The source slice, of type <typeparamref name="TFrom"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains pointers.
        /// </exception>
        public static unsafe Span<TTo> NonPortableCast<TFrom, TTo>(this Span<TFrom> source)
            where TFrom : struct
            where TTo : struct
        {
            if (JitHelpers.ContainsReferences<TFrom>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TFrom));
            if (JitHelpers.ContainsReferences<TTo>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TTo));

            return new Span<TTo>(
                ref JitHelpers.GetByRef<TTo>(ref source._rawPointer),
                checked((int)(source._length * Unsafe.SizeOf<TFrom>() / Unsafe.SizeOf<TTo>())));
        }

        /// <summary>
        /// Casts a ReadOnlySpan of one primitive type <typeparamref name="TFrom"/> to another primitive type <typeparamref name="TTo"/>.
        /// These types may not contain pointers. This is checked at runtime in order to preserve type safety.
        /// </summary>
        /// <remarks>
        /// Supported only for platforms that support misaligned memory access.
        /// </remarks>
        /// <param name="source">The source slice, of type <typeparamref name="TFrom"/>.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains pointers.
        /// </exception>
        public static unsafe ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this ReadOnlySpan<TFrom> source)
            where TFrom : struct
            where TTo : struct
        {
            if (JitHelpers.ContainsReferences<TFrom>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TFrom));
            if (JitHelpers.ContainsReferences<TTo>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TTo));

            return new ReadOnlySpan<TTo>(
                ref JitHelpers.GetByRef<TTo>(ref source._rawPointer),
                checked((int)(source._length * Unsafe.SizeOf<TFrom>() / Unsafe.SizeOf<TTo>())));
        }
    }

    internal static class SpanHelper
    {
        internal static unsafe void CopyTo<T>(ref T destination, ref T source, int elementsCount)
        {
            if (elementsCount == 0)
                return;

            if (Unsafe.AreSame(ref destination, ref source))
                return;

            if (!JitHelpers.ContainsReferences<T>())
            {
                fixed (byte* pDestination = &Unsafe.As<T, byte>(ref destination))
                {
                    fixed (byte* pSource = &Unsafe.As<T, byte>(ref source))
                    {
#if BIT64
                        Buffer.Memmove(pDestination, pSource, (ulong)elementsCount * (ulong)Unsafe.SizeOf<T>());
#else
                        Buffer.Memmove(pDestination, pSource, (uint)elementsCount * (uint)Unsafe.SizeOf<T>());
#endif
                    }
                }
            }
            else
            {
                if (JitHelpers.ByRefLessThan(ref destination, ref source)) // copy forward
                {
                    for (int i = 0; i < elementsCount; i++)
                        Unsafe.Add(ref destination, i) = Unsafe.Add(ref source, i);
                }
                else // copy backward to avoid overlapping issues
                {
                    for (int i = elementsCount - 1; i >= 0; i--)
                        Unsafe.Add(ref destination, i) = Unsafe.Add(ref source, i);
                }
            }
        }
    }
}
