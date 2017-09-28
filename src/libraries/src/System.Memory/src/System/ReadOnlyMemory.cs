// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// ReadOnlyMemory represents a contiguous region of arbitrary similar to ReadOnlySpan.
    /// Unlike ReadOnlySpan, it is not a byref-like type.
    /// </summary>
    public readonly struct ReadOnlyMemory<T>
    {
        // The highest order bit of _index is used to discern whether _arrayOrOwnedMemory is an array or an owned memory
        // if (_index >> 31) == 1, object _arrayOrOwnedMemory is an OwnedMemory<T>
        // else, object _arrayOrOwnedMemory is a T[]
        private readonly object _arrayOrOwnedMemory;
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
        public ReadOnlyMemory(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            _arrayOrOwnedMemory = array;
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
        public ReadOnlyMemory(T[] array, int start, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            _arrayOrOwnedMemory = array;
            _index = start;
            _length = length;
        }
        
        // Constructor for internal use only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlyMemory(OwnedMemory<T> owner, int index, int length)
        {
            if (owner == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.ownedMemory);
            if (index < 0 || length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            _arrayOrOwnedMemory = owner;
            _index = index | (1 << 31); // Before using _index, check if _index < 0, then 'and' it with RemoveOwnedFlagBitMask
            _length = length;
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="Memory{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyMemory<T>(T[] array) => new ReadOnlyMemory<T>(array);
        
        /// <summary>
        /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="Memory{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyMemory<T>(ArraySegment<T> arraySegment) => new ReadOnlyMemory<T>(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

        /// <summary>
        /// Returns an empty <see cref="Memory{T}"/>
        /// </summary>
        public static ReadOnlyMemory<T> Empty { get; } =
#if !netstandard11
            Array.Empty<T>();
#else
            new T[0];
#endif

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
        public ReadOnlyMemory<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            if (_index < 0)
                return new ReadOnlyMemory<T>((OwnedMemory<T>)_arrayOrOwnedMemory, (_index & RemoveOwnedFlagBitMask) + start, _length - start);
            return new ReadOnlyMemory<T>((T[])_arrayOrOwnedMemory, _index + start, _length - start);
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
        public ReadOnlyMemory<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            if (_index < 0)
                return new ReadOnlyMemory<T>((OwnedMemory<T>)_arrayOrOwnedMemory, (_index & RemoveOwnedFlagBitMask) + start, length);
            return new ReadOnlyMemory<T>((T[])_arrayOrOwnedMemory, _index + start, length);
        }

        /// <summary>
        /// Returns a span from the memory.
        /// </summary>
        public ReadOnlySpan<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index < 0)
                    return ((OwnedMemory<T>)_arrayOrOwnedMemory).Span.Slice(_index & RemoveOwnedFlagBitMask, _length);
                return new ReadOnlySpan<T>((T[])_arrayOrOwnedMemory, _index, _length);
            }
        }

        /// <summary>
        /// Returns a handle for the array.
        /// <param name="pin">If pin is true, the GC will not move the array and hence its address can be taken</param>
        /// </summary>
        public unsafe MemoryHandle Retain(bool pin = false)
        {
            MemoryHandle memoryHandle;
            if (pin)
            {
                if (_index < 0)
                {
                    memoryHandle = ((OwnedMemory<T>)_arrayOrOwnedMemory).Pin();
                }
                else
                {
                    var array = (T[])_arrayOrOwnedMemory;
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    void* pointer = Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), _index);
                    memoryHandle = new MemoryHandle(null, pointer, handle);
                }
            }
            else
            {
                if (_index < 0)
                {
                    ((OwnedMemory<T>)_arrayOrOwnedMemory).Retain();
                    memoryHandle = new MemoryHandle((OwnedMemory<T>)_arrayOrOwnedMemory);
                }
                else
                {
                    memoryHandle = new MemoryHandle(null);
                }
            }
            return memoryHandle;
        }

        /// <summary>
        /// Get an array segment from the underlying memory. 
        /// If unable to get the array segment, return false with a default array segment.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool DangerousTryGetArray(out ArraySegment<T> arraySegment)
        {
            if (_index < 0)
            {
                if (((OwnedMemory<T>)_arrayOrOwnedMemory).TryGetArray(out var segment))
                {
                    arraySegment = new ArraySegment<T>(segment.Array, segment.Offset + (_index & RemoveOwnedFlagBitMask), _length);
                    return true;
                }
            }
            else
            {
                arraySegment = new ArraySegment<T>((T[])_arrayOrOwnedMemory, _index, _length);
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
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyMemory<T> readOnlyMemory)
            {
                return Equals(readOnlyMemory);
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
        public bool Equals(ReadOnlyMemory<T> other)
        {
            return
                _arrayOrOwnedMemory == other._arrayOrOwnedMemory &&
                _index == other._index &&
                _length == other._length;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        [EditorBrowsable( EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return CombineHashCodes(_arrayOrOwnedMemory.GetHashCode(), (_index & RemoveOwnedFlagBitMask).GetHashCode(), _length.GetHashCode());
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
