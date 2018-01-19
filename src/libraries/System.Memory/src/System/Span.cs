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
    /// Span represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type- and memory-safe.
    /// </summary>
    [DebuggerTypeProxy(typeof(SpanDebugView<>))]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly ref struct Span<T>
    {
        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            _length = array.Length;
            _pinnable = Unsafe.As<Pinnable<T>>(array);
            _byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment;
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
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            _length = length;
            _pinnable = Unsafe.As<Pinnable<T>>(array);
            _byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add<T>(start);
        }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.  Clearly this
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
        public unsafe Span(void* pointer, int length)
        {
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            _length = length;
            _pinnable = null;
            _byteOffset = new IntPtr(pointer);
        }

        /// <summary>
        /// Create a new span over a portion of a regular managed object. This can be useful
        /// if part of a managed object represents a "fixed array." This is dangerous because neither the
        /// <paramref name="length"/> is checked, nor <paramref name="obj"/> being null, nor the fact that
        /// "rawPointer" actually lies within <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The managed object that contains the data to span over.</param>
        /// <param name="objectData">A reference to data within that object.</param>
        /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Span<T> DangerousCreate(object obj, ref T objectData, int length)
        {
            Pinnable<T> pinnable = Unsafe.As<Pinnable<T>>(obj);
            IntPtr byteOffset = Unsafe.ByteOffset<T>(ref pinnable.Data, ref objectData);
            return new Span<T>(pinnable, byteOffset, length);
        }

        // Constructor for internal use only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span(Pinnable<T> pinnable, IntPtr byteOffset, int length)
        {
            Debug.Assert(length >= 0);

            _length = length;
            _pinnable = pinnable;
            _byteOffset = byteOffset;
        }

        //Debugger Display = {T[length]}
        private string DebuggerDisplay => string.Format("{{{0}[{1}]}}", typeof(T).Name, _length);

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty => _length == 0;

        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= ((uint)_length))
                    ThrowHelper.ThrowIndexOutOfRangeException();

                if (_pinnable == null)
                    unsafe { return ref Unsafe.Add<T>(ref Unsafe.AsRef<T>(_byteOffset.ToPointer()), index); }
                else
                    return ref Unsafe.Add<T>(ref Unsafe.AddByteOffset<T>(ref _pinnable.Data, _byteOffset), index);
            }
        }

        /// <summary>
        /// Clears the contents of this span.
        /// </summary>
        public unsafe void Clear()
        {
            int length = _length;

            if (length == 0)
                return;

            var byteLength = (UIntPtr)((uint)length * Unsafe.SizeOf<T>());

            if ((Unsafe.SizeOf<T>() & (sizeof(IntPtr) - 1)) != 0)
            {
                if (_pinnable == null)
                {
                    var ptr = (byte*)_byteOffset.ToPointer();

                    SpanHelpers.ClearLessThanPointerSized(ptr, byteLength);
                }
                else
                {
                    ref byte b = ref Unsafe.As<T, byte>(ref Unsafe.AddByteOffset<T>(ref _pinnable.Data, _byteOffset));

                    SpanHelpers.ClearLessThanPointerSized(ref b, byteLength);
                }
            }
            else
            {
                if (SpanHelpers.IsReferenceOrContainsReferences<T>())
                {
                    UIntPtr pointerSizedLength = (UIntPtr)((length * Unsafe.SizeOf<T>()) / sizeof(IntPtr));

                    ref IntPtr ip = ref Unsafe.As<T, IntPtr>(ref DangerousGetPinnableReference());

                    SpanHelpers.ClearPointerSizedWithReferences(ref ip, pointerSizedLength);
                }
                else
                {
                    ref byte b = ref Unsafe.As<T, byte>(ref DangerousGetPinnableReference());

                    SpanHelpers.ClearPointerSizedWithoutReferences(ref b, byteLength);
                }
            }
        }

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        public unsafe void Fill(T value)
        {
            int length = _length;

            if (length == 0)
                return;

            if (Unsafe.SizeOf<T>() == 1)
            {
                byte fill = Unsafe.As<T, byte>(ref value);
                if (_pinnable == null)
                {
                    Unsafe.InitBlockUnaligned(_byteOffset.ToPointer(), fill, (uint)length);
                }
                else
                {
                    ref byte r = ref Unsafe.As<T, byte>(ref Unsafe.AddByteOffset<T>(ref _pinnable.Data, _byteOffset));
                    Unsafe.InitBlockUnaligned(ref r, fill, (uint)length);
                }
            }
            else
            {
                ref T r = ref DangerousGetPinnableReference();

                // TODO: Create block fill for value types of power of two sizes e.g. 2,4,8,16

                // Simple loop unrolling
                int i = 0;
                for (; i < (length & ~7); i += 8)
                {
                    Unsafe.Add<T>(ref r, i + 0) = value;
                    Unsafe.Add<T>(ref r, i + 1) = value;
                    Unsafe.Add<T>(ref r, i + 2) = value;
                    Unsafe.Add<T>(ref r, i + 3) = value;
                    Unsafe.Add<T>(ref r, i + 4) = value;
                    Unsafe.Add<T>(ref r, i + 5) = value;
                    Unsafe.Add<T>(ref r, i + 6) = value;
                    Unsafe.Add<T>(ref r, i + 7) = value;
                }
                if (i < (length & ~3))
                {
                    Unsafe.Add<T>(ref r, i + 0) = value;
                    Unsafe.Add<T>(ref r, i + 1) = value;
                    Unsafe.Add<T>(ref r, i + 2) = value;
                    Unsafe.Add<T>(ref r, i + 3) = value;
                    i += 4;
                }
                for (; i < length; i++)
                {
                    Unsafe.Add<T>(ref r, i) = value;
                }
            }
        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
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

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        ///
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(Span<T> destination)
        {
            int length = _length;
            int destLength = destination._length;

            if ((uint)length == 0)
                return true;

            if ((uint)length > (uint)destLength)
                return false;

            ref T src = ref DangerousGetPinnableReference();
            ref T dst = ref destination.DangerousGetPinnableReference();
            SpanHelpers.CopyTo<T>(ref dst, destLength, ref src, length);
            return true;
        }

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(Span<T> left, Span<T> right)
        {
            return left._length == right._length && Unsafe.AreSame<T>(ref left.DangerousGetPinnableReference(), ref right.DangerousGetPinnableReference());
        }

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(Span<T> left, Span<T> right) => !(left == right);

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
            throw new NotSupportedException(SR.CannotCallEqualsOnSpan);
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
            throw new NotSupportedException(SR.CannotCallGetHashCodeOnSpan);
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="Span{T}"/>
        /// </summary>
        public static implicit operator Span<T>(T[] array) => array != null ? new Span<T>(array) : default;

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="Span{T}"/>
        /// </summary>
        public static implicit operator Span<T>(ArraySegment<T> arraySegment)
            => arraySegment.Array != null ? new Span<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count) : default;

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Span{T}"/> to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(Span<T> span) => new ReadOnlySpan<T>(span._pinnable, span._byteOffset, span._length);

        /// <summary>
        /// Forms a slice out of the given span, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            IntPtr newOffset = _byteOffset.Add<T>(start);
            int length = _length - start;
            return new Span<T>(_pinnable, newOffset, length);
        }

        /// <summary>
        /// Forms a slice out of the given span, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            IntPtr newOffset = _byteOffset.Add<T>(start);
            return new Span<T>(_pinnable, newOffset, length);
        }

        /// <summary>
        /// Copies the contents of this span into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray()
        {
            if (_length == 0)
                return SpanHelpers.PerTypeValues<T>.EmptyArray;

            T[] result = new T[_length];
            CopyTo(result);
            return result;
        }

        /// <summary>
        /// Returns a 0-length span whose base is the null pointer.
        /// </summary>
        public static Span<T> Empty => default(Span<T>);

        /// <summary>
        /// This method is obsolete, use System.Runtime.InteropServices.MemoryMarshal.GetReference instead.
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns a reference to the location where the 0th element
        /// would have been stored. Such a reference can be used for pinning but must never be dereferenced.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal ref T DangerousGetPinnableReference()
        {
            if (_pinnable == null)
                unsafe { return ref Unsafe.AsRef<T>(_byteOffset.ToPointer()); }
            else
                return ref Unsafe.AddByteOffset<T>(ref _pinnable.Data, _byteOffset);
        }

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref="Span{T}"/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly Span<T> _span;
            /// <summary>The next index to yield.</summary>
            private int _index;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="span">The span to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(Span<T> span)
            {
                _span = span;
                _index = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_index];
            }
        }

        // These expose the internal representation for Span-related apis use only.
        internal Pinnable<T> Pinnable => _pinnable;
        internal IntPtr ByteOffset => _byteOffset;

        //
        // If the Span was constructed from an object,
        //
        //   _pinnable   = that object (unsafe-casted to a Pinnable<T>)
        //   _byteOffset = offset in bytes from "ref _pinnable.Data" to "ref span[0]"
        //
        // If the Span was constructed from a native pointer,
        //
        //   _pinnable   = null
        //   _byteOffset = the pointer
        //
        private readonly Pinnable<T> _pinnable;
        private readonly IntPtr _byteOffset;
        private readonly int _length;
    }
}
