// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

namespace System
{
    /// <summary>
    /// ReadOnlySpan represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type- and memory-safe.
    /// </summary>
    public struct ReadOnlySpan<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        private readonly ByReference<T> _pointer;
        /// <summary>The number of elements this ReadOnlySpan contains.</summary>
        private readonly int _length;

        /// <summary>
        /// Creates a new read-only span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            _pointer = new ByReference<T>(ref JitHelpers.GetArrayData(array));
            _length = array.Length;
        }

        /// <summary>
        /// Creates a new read-only span over the portion of the target array beginning
        /// at 'start' index and covering the remainder of the array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the read-only span.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> is not in the range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan(T[] array, int start)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if ((uint)start > (uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _pointer = new ByReference<T>(ref Unsafe.Add(ref JitHelpers.GetArrayData(array), start));
            _length = array.Length - start;
        }

        /// <summary>
        /// Creates a new read-only span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the read-only span.</param>
        /// <param name="length">The number of items in the read-only span.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _pointer = new ByReference<T>(ref Unsafe.Add(ref JitHelpers.GetArrayData(array), start));
            _length = length;
        }

        /// <summary>
        /// Creates a new read-only span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous, because we are creating arbitrarily typed T's
        /// out of a void*-typed block of memory.  And the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="pointer">An unmanaged pointer to memory.</param>
        /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is negative.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpan(void* pointer, int length)
        {
            if (JitHelpers.ContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _pointer = new ByReference<T>(ref Unsafe.As<byte, T>(ref *(byte*)pointer));
            _length = length;
        }

        /// <summary>
        /// Create a new read-only span over a portion of a regular managed object. This can be useful
        /// if part of a managed object represents a "fixed array." This is dangerous because
        /// "length" is not checked, nor is the fact that "rawPointer" actually lies within the object.
        /// </summary>
        /// <param name="obj">The managed object that contains the data to span over.</param>
        /// <param name="objectData">A reference to data within that object.</param>
        /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when the specified object is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is negative.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length)
        {
            if (obj == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            return new ReadOnlySpan<T>(ref objectData, length);
        }

        // Constructor for internal use only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan(ref T ptr, int length)
        {
            Debug.Assert(length >= 0);

            _pointer = new ByReference<T>(ref ptr);
            _length = length;
        }

        /// <summary>
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns a reference to the location where the 0th element
        /// would have been stored. Such a reference can be used for pinning but must never be dereferenced.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T DangerousGetPinnableReference()
        {
            return ref _pointer.Value;
        }

        /// <summary>
        /// The number of items in the read-only span.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty => _length == 0;

        /// <summary>
        /// Returns the specified element of the read-only span.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return Unsafe.Add(ref _pointer.Value, index);
            }
        }

        /// <summary>
        /// Copies the contents of this read-only span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        ///
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination Span is shorter than the source Span.
        /// </exception>
        /// </summary>
        public void CopyTo(Span<T> destination)
        {
            if (!TryCopyTo(destination))
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
        }

        /// Copies the contents of this read-only span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(Span<T> destination)
        {
            if ((uint)_length > (uint)destination.Length)
                return false;

            SpanHelper.CopyTo<T>(ref destination.DangerousGetPinnableReference(), ref _pointer.Value, _length);
            return true;
        }

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
        {
            return left._length == right._length && Unsafe.AreSame<T>(ref left._pointer.Value, ref right._pointer.Value);
        }

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => !(left == right);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
        /// <exception cref="System.NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        /// </summary>
        [Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            ThrowHelper.ThrowNotSupportedException_CannotCallEqualsOnSpan();
            // Prevent compiler error CS0161: 'Span<T>.Equals(object)': not all code paths return a value
            return default(bool);
        }

        /// <summary>
        /// This method is not supported as spans cannot be boxed.
        /// <exception cref="System.NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        /// </summary>
        [Obsolete("GetHashCode() on Span will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            ThrowHelper.ThrowNotSupportedException_CannotCallGetHashCodeOnSpan();
            // Prevent compiler error CS0161: 'Span<T>.GetHashCode()': not all code paths return a value
            return default(int);
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(T[] array) => new ReadOnlySpan<T>(array);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(ArraySegment<T> arraySegment) => new ReadOnlySpan<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

        /// <summary>
        /// Forms a slice out of the given read-only span, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.NoOptimization)] // TODO-SPAN: Workaround for https://github.com/dotnet/coreclr/issues/7894
        public ReadOnlySpan<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<T>(ref Unsafe.Add(ref _pointer.Value, start), _length - start);
        }

        /// <summary>
        /// Forms a slice out of the given read-only span, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.NoOptimization)] // TODO-SPAN: Workaround for https://github.com/dotnet/coreclr/issues/7894
        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<T>(ref Unsafe.Add(ref _pointer.Value, start), length);
        }

        /// <summary>
        /// Copies the contents of this read-only span into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray()
        {
            if (_length == 0)
                return Array.Empty<T>();

            var destination = new T[_length];
            SpanHelper.CopyTo<T>(ref JitHelpers.GetArrayData(destination), ref _pointer.Value, _length);
            return destination;
        }

        /// <summary>
        /// Returns a 0-length read-only span whose base is the null pointer.
        /// </summary>
        public static ReadOnlySpan<T> Empty => default(ReadOnlySpan<T>);
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
