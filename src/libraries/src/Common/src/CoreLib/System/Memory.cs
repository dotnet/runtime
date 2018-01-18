// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;
#if !FEATURE_PORTABLE_SPAN
using Internal.Runtime.CompilerServices;
#endif // FEATURE_PORTABLE_SPAN

namespace System
{
    /// <summary>
    /// Memory represents a contiguous region of arbitrary memory similar to <see cref="Span{T}"/>.
    /// Unlike <see cref="Span{T}"/>, it is not a byref-like type.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [DebuggerTypeProxy(typeof(MemoryDebugView<>))]
    public readonly struct Memory<T>
    {
        // NOTE: With the current implementation, Memory<T> and ReadOnlyMemory<T> must have the same layout,
        // as code uses Unsafe.As to cast between them.

        // The highest order bit of _index is used to discern whether _object is an array/string or an owned memory
        // if (_index >> 31) == 1, object _object is an OwnedMemory<T>
        // else, object _object is a T[] or a string. It can only be a string if the Memory<T> was created by
        // using unsafe / marshaling code to reinterpret a ReadOnlyMemory<char> wrapped around a string as
        // a Memory<T>.
        private readonly object _object;
        private readonly int _index;
        private readonly int _length;

        private const int RemoveOwnedFlagBitMask = 0x7FFFFFFF;

        /// <summary>
        /// Creates a new memory over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            _object = array;
            _index = 0;
            _length = array.Length;
        }

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="array"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _object = array;
            _index = start;
            _length = length;
        }

        // Constructor for internal use only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Memory(OwnedMemory<T> owner, int index, int length)
        {
            // No validation performed; caller must provide any necessary validation.
            _object = owner;
            _index = index | (1 << 31); // Before using _index, check if _index < 0, then 'and' it with RemoveOwnedFlagBitMask
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Memory(object obj, int index, int length)
        {
            // No validation performed; caller must provide any necessary validation.
            _object = obj;
            _index = index;
            _length = length;
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="Memory{T}"/>
        /// </summary>
        public static implicit operator Memory<T>(T[] array) => new Memory<T>(array);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="Memory{T}"/>
        /// </summary>
        public static implicit operator Memory<T>(ArraySegment<T> arraySegment) => new Memory<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Memory{T}"/> to a <see cref="ReadOnlyMemory{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyMemory<T>(Memory<T> memory) =>
            Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref memory);

        //Debugger Display = {T[length]}
        private string DebuggerDisplay => string.Format("{{{0}[{1}]}}", typeof(T).Name, _length);

        /// <summary>
        /// Returns an empty <see cref="Memory{T}"/>
        /// </summary>
        public static Memory<T> Empty => default;

        /// <summary>
        /// The number of items in the memory.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty => _length == 0;

        /// <summary>
        /// Forms a slice out of the given memory, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            }

            return new Memory<T>(_object, _index + start, _length - start);
        }

        /// <summary>
        /// Forms a slice out of the given memory, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;=Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            return new Memory<T>(_object, _index + start, length);
        }

        /// <summary>
        /// Returns a span from the memory.
        /// </summary>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index < 0)
                {
                    return ((OwnedMemory<T>)_object).Span.Slice(_index & RemoveOwnedFlagBitMask, _length);
                }
                else if (typeof(T) == typeof(char) && _object is string s)
                {
                    // This is dangerous, returning a writable span for a string that should be immutable.
                    // However, we need to handle the case where a ReadOnlyMemory<char> was created from a string
                    // and then cast to a Memory<T>. Such a cast can only be done with unsafe or marshaling code,
                    // in which case that's the dangerous operation performed by the dev, and we're just following
                    // suit here to make it work as best as possible.
#if FEATURE_PORTABLE_SPAN
                    return new Span<T>(Unsafe.As<Pinnable<T>>(s), MemoryExtensions.StringAdjustment, s.Length).Slice(_index, _length);
#else
                    return new Span<T>(ref Unsafe.As<char, T>(ref s.GetRawStringData()), s.Length).Slice(_index, _length);
#endif // FEATURE_PORTABLE_SPAN
                }
                else if (_object != null)
                {
                    return new Span<T>((T[])_object, _index, _length);
                }
                else
                {
                    return default;
                }
            }
        }

        /// <summary>
        /// Copies the contents of the memory into the destination. If the source
        /// and destination overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        ///
        /// <param name="destination">The Memory to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination is shorter than the source.
        /// </exception>
        /// </summary>
        public void CopyTo(Memory<T> destination) => Span.CopyTo(destination.Span);

        /// <summary>
        /// Copies the contents of the memory into the destination. If the source
        /// and destination overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        ///
        /// <returns>If the destination is shorter than the source, this method
        /// return false and no data is written to the destination.</returns>
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(Memory<T> destination) => Span.TryCopyTo(destination.Span);

        /// <summary>
        /// Returns a handle for the array.
        /// <param name="pin">If pin is true, the GC will not move the array and hence its address can be taken</param>
        /// </summary>
        public unsafe MemoryHandle Retain(bool pin = false)
        {
            MemoryHandle memoryHandle = default;
            if (pin)
            {
                if (_index < 0)
                {
                    memoryHandle = ((OwnedMemory<T>)_object).Pin();
                    memoryHandle.AddOffset((_index & RemoveOwnedFlagBitMask) * Unsafe.SizeOf<T>());
                }
                else if (typeof(T) == typeof(char) && _object is string s)
                {
                    // This case can only happen if a ReadOnlyMemory<char> was created around a string
                    // and then that was cast to a Memory<char> using unsafe / marshaling code.  This needs
                    // to work, however, so that code that uses a single Memory<char> field to store either
                    // a readable ReadOnlyMemory<char> or a writable Memory<char> can still be pinned and
                    // used for interop purposes.
                    GCHandle handle = GCHandle.Alloc(s, GCHandleType.Pinned);
#if FEATURE_PORTABLE_SPAN
                    void* pointer = Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), _index);
#else
                    void* pointer = Unsafe.Add<T>(Unsafe.AsPointer(ref s.GetRawStringData()), _index);
#endif // FEATURE_PORTABLE_SPAN
                    memoryHandle = new MemoryHandle(null, pointer, handle);
                }
                else if (_object is T[] array)
                {
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
#if FEATURE_PORTABLE_SPAN
                    void* pointer = Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), _index);
#else
                    void* pointer = Unsafe.Add<T>(Unsafe.AsPointer(ref array.GetRawSzArrayData()), _index);
#endif // FEATURE_PORTABLE_SPAN
                    memoryHandle = new MemoryHandle(null, pointer, handle);
                }
            }
            else
            {
                if (_index < 0)
                {
                    ((OwnedMemory<T>)_object).Retain();
                    memoryHandle = new MemoryHandle((OwnedMemory<T>)_object);
                }
            }
            return memoryHandle;
        }

        /// <summary>
        /// Get an array segment from the underlying memory.
        /// If unable to get the array segment, return false with a default array segment.
        /// </summary>
        public bool TryGetArray(out ArraySegment<T> arraySegment)
        {
            if (_index < 0)
            {
                if (((OwnedMemory<T>)_object).TryGetArray(out var segment))
                {
                    arraySegment = new ArraySegment<T>(segment.Array, segment.Offset + (_index & RemoveOwnedFlagBitMask), _length);
                    return true;
                }
            }
            else if (_object is T[] arr)
            {
                arraySegment = new ArraySegment<T>(arr, _index, _length);
                return true;
            }

            arraySegment = default(ArraySegment<T>);
            return false;
        }

        /// <summary>
        /// Copies the contents from the memory into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray() => Span.ToArray();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// Returns true if the object is Memory or ReadOnlyMemory and if both objects point to the same array and have the same length.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyMemory<T>)
            {
                return ((ReadOnlyMemory<T>)obj).Equals(this);
            }
            else if (obj is Memory<T> memory)
            {
                return Equals(memory);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the memory points to the same array and has the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public bool Equals(Memory<T> other)
        {
            return
                _object == other._object &&
                _index == other._index &&
                _length == other._length;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return _object != null ? CombineHashCodes(_object.GetHashCode(), _index.GetHashCode(), _length.GetHashCode()) : 0;
        }

        private static int CombineHashCodes(int left, int right)
        {
            return ((left << 5) + left) ^ right;
        }

        private static int CombineHashCodes(int h1, int h2, int h3)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), h3);
        }

    }
}
