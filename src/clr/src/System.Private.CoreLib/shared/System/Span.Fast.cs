// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;
using Internal.Runtime.CompilerServices;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
    /// <summary>
    /// Span represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type- and memory-safe.
    /// </summary>
    [NonVersionable]
    public readonly ref partial struct Span<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly ByReference<T> _pointer;
        /// <summary>The number of elements this Span contains.</summary>
#if PROJECTN
        [Bound]
#endif
        private readonly int _length;

        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span(T[]? array)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            if (default(T)! == null && array.GetType() != typeof(T[])) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34757
                ThrowHelper.ThrowArrayTypeMismatchException();

            _pointer = new ByReference<T>(ref Unsafe.As<byte, T>(ref array.GetRawSzArrayData()));
            _length = array.Length;
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
        public Span(T[]? array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (default(T)! == null && array.GetType() != typeof(T[])) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34757
                ThrowHelper.ThrowArrayTypeMismatchException();
#if BIT64
            // See comment in Span<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif

            _pointer = new ByReference<T>(ref Unsafe.Add(ref Unsafe.As<byte, T>(ref array.GetRawSzArrayData()), start));
            _length = length;
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _pointer = new ByReference<T>(ref Unsafe.As<byte, T>(ref *(byte*)pointer));
            _length = length;
        }

        // Constructor for internal use only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span(ref T ptr, int length)
        {
            Debug.Assert(length >= 0);

            _pointer = new ByReference<T>(ref ptr);
            _length = length;
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
#if PROJECTN
            [BoundsChecking]
            get
            {
                return ref Unsafe.Add(ref _pointer.Value, index);
            }
#else
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [NonVersionable]
            get
            {
                if ((uint)index >= (uint)_length)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                return ref Unsafe.Add(ref _pointer.Value, index);
            }
#endif
        }

        /// <summary>
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of span within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe ref T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.AsRef<T>(null);
            if (_length != 0) ret = ref _pointer.Value;
            return ref ret;
        }

        /// <summary>
        /// Clears the contents of this span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                SpanHelpers.ClearWithReferences(ref Unsafe.As<T, IntPtr>(ref _pointer.Value), (nuint)_length * (nuint)(Unsafe.SizeOf<T>() / sizeof(nuint)));
            }
            else
            {
                SpanHelpers.ClearWithoutReferences(ref Unsafe.As<T, byte>(ref _pointer.Value), (nuint)_length * (nuint)Unsafe.SizeOf<T>());
            }
        }

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        public void Fill(T value)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                uint length = (uint)_length;
                if (length == 0)
                    return;

                T tmp = value; // Avoid taking address of the "value" argument. It would regress performance of the loop below.
                Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref _pointer.Value), Unsafe.As<T, byte>(ref tmp), length);
            }
            else
            {
                // Do all math as nuint to avoid unnecessary 64->32->64 bit integer truncations
                nuint length = (uint)_length;
                if (length == 0)
                    return;

                ref T r = ref _pointer.Value;

                // TODO: Create block fill for value types of power of two sizes e.g. 2,4,8,16

                nuint elementSize = (uint)Unsafe.SizeOf<T>();
                nuint i = 0;
                for (; i < (length & ~(nuint)7); i += 8)
                {
                    Unsafe.AddByteOffset<T>(ref r, (i + 0) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 1) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 2) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 3) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 4) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 5) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 6) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 7) * elementSize) = value;
                }
                if (i < (length & ~(nuint)3))
                {
                    Unsafe.AddByteOffset<T>(ref r, (i + 0) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 1) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 2) * elementSize) = value;
                    Unsafe.AddByteOffset<T>(ref r, (i + 3) * elementSize) = value;
                    i += 4;
                }
                for (; i < length; i++)
                {
                    Unsafe.AddByteOffset<T>(ref r, i * elementSize) = value;
                }
            }
        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination Span is shorter than the source Span.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination)
        {
            // Using "if (!TryCopyTo(...))" results in two branches: one for the length
            // check, and one for the result of TryCopyTo. Since these checks are equivalent,
            // we can optimize by performing the check once ourselves then calling Memmove directly.

            if ((uint)_length <= (uint)destination.Length)
            {
                Buffer.Memmove(ref destination._pointer.Value, ref _pointer.Value, (nuint)_length);
            }
            else
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        public bool TryCopyTo(Span<T> destination)
        {
            bool retVal = false;
            if ((uint)_length <= (uint)destination.Length)
            {
                Buffer.Memmove(ref destination._pointer.Value, ref _pointer.Value, (nuint)_length);
                retVal = true;
            }
            return retVal;
        }

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(Span<T> left, Span<T> right)
        {
            return left._length == right._length && Unsafe.AreSame<T>(ref left._pointer.Value, ref right._pointer.Value);
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Span{T}"/> to a <see cref="ReadOnlySpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(Span<T> span) => new ReadOnlySpan<T>(ref span._pointer.Value, span._length);

        /// <summary>
        /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
        /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                return new string(new ReadOnlySpan<char>(ref Unsafe.As<T, char>(ref _pointer.Value), _length));
            }
#if FEATURE_UTF8STRING
            else if (typeof(T) == typeof(Char8))
            {
                // TODO_UTF8STRING: Call into optimized transcoding routine when it's available.
                return Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ref Unsafe.As<T, byte>(ref _pointer.Value), _length));
            }
#endif // FEATURE_UTF8STRING
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
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref _pointer.Value, start), _length - start);
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
#if BIT64
            // Since start and length are both 32-bit, their sum can be computed across a 64-bit domain
            // without loss of fidelity. The cast to uint before the cast to ulong ensures that the
            // extension from 32- to 64-bit is zero-extending rather than sign-extending. The end result
            // of this is that if either input is negative or if the input sum overflows past Int32.MaxValue,
            // that information is captured correctly in the comparison against the backing _length field.
            // We don't use this same mechanism in a 32-bit process due to the overhead of 64-bit arithmetic.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif

            return new Span<T>(ref Unsafe.Add(ref _pointer.Value, start), length);
        }

        /// <summary>
        /// Copies the contents of this span into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            if (_length == 0)
                return Array.Empty<T>();

            var destination = new T[_length];
            Buffer.Memmove(ref Unsafe.As<byte, T>(ref destination.GetRawSzArrayData()), ref _pointer.Value, (nuint)_length);
            return destination;
        }
    }
}
