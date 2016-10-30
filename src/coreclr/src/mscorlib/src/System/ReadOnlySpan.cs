// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// ReadOnlySpan represents contiguous read-only region of arbitrary memory, with performance
    /// characteristics on par with T[]. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type- and memory-safe.
    /// </summary>
    public unsafe struct ReadOnlySpan<T>
    {
        /// <summary>A byref or a native ptr. Do not access directly</summary>
        private readonly IntPtr _rawPointer;
        /// <summary>The number of elements this ReadOnlySpan contains.</summary>
        private readonly int _length;

        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        public ReadOnlySpan(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            // TODO-SPAN: This has GC hole. It needs to be JIT intrinsic instead
            _rawPointer = (IntPtr)Unsafe.AsPointer(ref JitHelpers.GetArrayData(array));
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
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;&eq;Length).
        /// </exception>
        public ReadOnlySpan(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            // TODO-SPAN: This has GC hole. It needs to be JIT intrinsic instead
            _rawPointer = (IntPtr)Unsafe.AsPointer(ref Unsafe.Add(ref JitHelpers.GetArrayData(array), start));
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
        public unsafe ReadOnlySpan(void* pointer, int length)
        {
            if (JitHelpers.ContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _rawPointer = (IntPtr)pointer;
            _length = length;
        }

        /// <summary>
        /// An internal helper for creating spans.
        /// </summary>
        internal ReadOnlySpan(ref T ptr, int length)
        {
            // TODO-SPAN: This has GC hole. It needs to be JIT intrinsic instead
            _rawPointer = (IntPtr)Unsafe.AsPointer(ref ptr);
            _length = length;
        }

        /// <summary>
        /// An internal helper for accessing spans.
        /// </summary>
        internal unsafe ref T GetRawPointer()
        {
            // TODO-SPAN: This has GC hole. It needs to be JIT intrinsic instead
            return ref Unsafe.As<IntPtr, T>(ref *(IntPtr *)_rawPointer);
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Span{T}"/> to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(Span<T> slice)
        {
            return new ReadOnlySpan<T>(ref slice.GetRawPointer(), slice.Length);
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(T[] array)
        {
            return new ReadOnlySpan<T>(array);
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(ArraySegment<T> arraySegment)
        {
            return new ReadOnlySpan<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Returns an empty <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static ReadOnlySpan<T> Empty
        {
            get { return default(ReadOnlySpan<T>); }
        }

        /// <summary>
        /// Returns whether the <see cref="ReadOnlySpan{T}"/> is empty.
        /// </summary>
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

                return Unsafe.Add(ref GetRawPointer(), index);
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
            SpanHelper.CopyTo<T>(ref JitHelpers.GetArrayData(destination), ref GetRawPointer(), _length);
            return destination;
        }

        /// <summary>
        /// Forms a slice out of the given span, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;length).
        /// </exception>
        [MethodImpl(MethodImplOptions.NoOptimization)] // TODO-SPAN: Workaround for https://github.com/dotnet/coreclr/issues/7894
        public ReadOnlySpan<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<T>(ref Unsafe.Add(ref GetRawPointer(), start), _length - start);
        }

        /// <summary>
        /// Forms a slice out of the given span, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="end">The index at which to end this slice (exclusive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;&eq;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.NoOptimization)] // TODO-SPAN: Workaround for https://github.com/dotnet/coreclr/issues/7894
        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<T>(ref Unsafe.Add(ref GetRawPointer(), start), length);
        }

        /// <summary>
        /// Checks to see if two spans point at the same memory.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public bool Equals(ReadOnlySpan<T> other)
        {
            return (_length == other.Length) &&
                (_length == 0 || Unsafe.AreSame(ref GetRawPointer(), ref other.GetRawPointer()));
        }

        /// <summary>
        /// Copies the contents of this span into destination span. The destination
        /// must be at least as big as the source, and may be bigger.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(Span<T> destination)
        {
            if ((uint)_length > (uint)destination.Length)
                return false;

            SpanHelper.CopyTo<T>(ref destination.GetRawPointer(), ref GetRawPointer(), _length);
            return true;
        }
    }

    public static class ReadOnlySpanExtensions
    {
        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        public static ReadOnlySpan<char> Slice(this string text)
        {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);

            return new ReadOnlySpan<char>(ref text.GetFirstCharRef(), text.Length);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string, beginning at 'start'.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        public static ReadOnlySpan<char> Slice(this string text, int start)
        {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetFirstCharRef(), start), text.Length - start);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string, beginning at <paramref name="start"/>, of given <paramref name="length"/>.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The number of items in the span.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;&eq;Length).
        /// </exception>
        public static ReadOnlySpan<char> Slice(this string text, int start, int length)
        {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
            if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetFirstCharRef(), start), length);
        }
    }
}
