// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper type for avoiding allocations while building arrays.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal struct ArrayBuilder<T>
    {
        private InlineArray16<T> _stackAllocatedBuffer = default;
        private const int StackAllocatedCapacity = 16;
        private const int DefaultHeapCapacity = 4;

        private T[]? _array; // Starts out null, initialized on first Add.
        private int _count; // Number of items into _array we're using.

        /// <summary>
        /// Initializes the <see cref="ArrayBuilder{T}"/> with a specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the array to allocate.</param>
        public ArrayBuilder(int capacity) : this()
        {
            Debug.Assert(capacity >= 0);
            if (capacity > StackAllocatedCapacity)
            {
                _array = new T[capacity - StackAllocatedCapacity];
            }
        }

        /// <summary>
        /// Gets the number of items this instance can store without re-allocating,
        /// or 0 if the backing array is <c>null</c>.
        /// </summary>
        public int Capacity => _array?.Length + StackAllocatedCapacity ?? StackAllocatedCapacity;

        /// <summary>
        /// Gets the number of items in the array currently in use.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets or sets the item at a certain index in the array.
        /// </summary>
        /// <param name="index">The index into the array.</param>
        public T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < _count);
                return index < StackAllocatedCapacity ? _stackAllocatedBuffer[index] : _array![index - StackAllocatedCapacity];
            }
        }

        /// <summary>
        /// Adds an item to the backing array, resizing it if necessary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (_count == Capacity)
            {
                EnsureCapacity(_count + 1);
            }

            UncheckedAdd(item);
        }

        /// <summary>
        /// Gets the first item in this builder.
        /// </summary>
        public T First()
        {
            Debug.Assert(_count > 0);
            return _stackAllocatedBuffer[0];
        }

        /// <summary>
        /// Gets the last item in this builder.
        /// </summary>
        public T Last()
        {
            Debug.Assert(_count > 0);
            return _count <= StackAllocatedCapacity ? _stackAllocatedBuffer[_count - 1] : _array![_count - StackAllocatedCapacity - 1];
        }

        /// <summary>
        /// Creates an array from the contents of this builder.
        /// </summary>
        /// <remarks>
        /// Do not call this method twice on the same builder.
        /// </remarks>
        public T[] ToArray()
        {
            if (_count == 0)
            {
                return Array.Empty<T>();
            }

            T[] result = new T[_count];
            int index = 0;
            foreach (T stackAllocatedValue in _stackAllocatedBuffer)
            {
                result[index++] = stackAllocatedValue;
                if (index >= _count)
                {
                    return result;
                }
            }

            _array.AsSpan(0, _count - StackAllocatedCapacity).CopyTo(result.AsSpan(start: StackAllocatedCapacity));

#if DEBUG
            // Try to prevent callers from using the ArrayBuilder after ToArray, if _count != 0.
            _count = -1;
            _array = null;
#endif

            return result;
        }

        /// <summary>
        /// Adds an item to the backing array, without checking if there is room.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use this method if you know there is enough space in the <see cref="ArrayBuilder{T}"/>
        /// for another item, and you are writing performance-sensitive code.
        /// </remarks>
        public void UncheckedAdd(T item)
        {
            Debug.Assert(_count < Capacity);
            if (_count < StackAllocatedCapacity)
            {
                _stackAllocatedBuffer[_count++] = item;
            }
            else
            {
                _array![_count++ - StackAllocatedCapacity] = item;
            }
        }

        private void EnsureCapacity(int minimum)
        {
            Debug.Assert(minimum > Capacity);

            if (minimum < StackAllocatedCapacity)
            {
                return;
            }

            if (_array == null)
            {
                // Initial capacity has not been set correctly, we will use the default size
                _array = new T[DefaultHeapCapacity];
                return;
            }

            int nextHeapCapacity = 2 * _array.Length;

            if ((uint)nextHeapCapacity > (uint)Array.MaxLength)
            {
                nextHeapCapacity = Math.Max(_array.Length + 1, Array.MaxLength);
            }

            nextHeapCapacity = Math.Max(nextHeapCapacity, minimum);

            T[] next = new T[nextHeapCapacity];
            if (_count > 0)
            {
                Array.Copy(_array!, next, _count);
            }
            _array = next;
        }
    }
}
