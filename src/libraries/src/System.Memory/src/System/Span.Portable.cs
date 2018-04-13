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
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly ref partial struct Span<T>
    {
        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span(T[] array)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            _length = array.Length;
            _pinnable = Unsafe.As<Pinnable<T>>(array);
            _byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment;
        }

        // This is a constructor that takes an array and start but not length. The reason we expose it as a static method as a constructor
        // is to mirror the actual api shape. This overload of the constructor was removed from the api surface area due to possible
        // confusion with other overloads that take an int parameter that don't represent a start index.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> Create(T[] array, int start)
        {
            if (array == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            IntPtr byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add<T>(start);
            int length = array.Length - start;
            return new Span<T>(pinnable: Unsafe.As<Pinnable<T>>(array), byteOffset: byteOffset, length: length);
        }

        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the span.</param>
        /// <param name="length">The number of items in the span.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span(T[] array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                this = default;
                return; // returns default
            }
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

        // Constructor for internal use only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span(Pinnable<T> pinnable, IntPtr byteOffset, int length)
        {
            Debug.Assert(length >= 0);

            _length = length;
            _pinnable = pinnable;
            _byteOffset = byteOffset;
        }

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
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of span within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe ref T GetPinnableReference()
        {
            if (_length != 0)
            {
                if (_pinnable == null)
                {
                    return ref Unsafe.AsRef<T>(_byteOffset.ToPointer());
                }
                return ref Unsafe.AddByteOffset<T>(ref _pinnable.Data, _byteOffset);
            }
            return ref Unsafe.AsRef<T>(null);
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
        /// Defines an implicit conversion of a <see cref="Span{T}"/> to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(Span<T> span) => new ReadOnlySpan<T>(span._pinnable, span._byteOffset, span._length);

        /// <summary>
        /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
        /// Otherwise, returns a <see cref="String"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                unsafe
                {
                    fixed (char* src = &Unsafe.As<T, char>(ref DangerousGetPinnableReference()))
                        return new string(src, 0, _length);
                }
            }
            return string.Format("System.Span<{0}>[{1}]", typeof(T).Name, _length);
        }

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
