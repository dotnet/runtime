// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper type for building dynamically-sized arrays while minimizing allocations and copying.
    /// </summary>
    internal struct LargeArrayBuilder<T>
    {
        private const int StartingCapacity = 4;
        private const int ResizeLimit = 32;

        private T[] _first;                 // The first buffer we store items in. Resized until ResizeLimit.
        private ArrayBuilder<T[]> _buffers; // After ResizeLimit * 2, we store previous buffers we've filled out here.
        private T[] _current;               // Current buffer we're reading into. If _count <= ResizeLimit, this is _first.
        private int _index;                 // Index into the current buffer.
        private int _count;                 // Count of all of the items in this builder.

        /// <summary>
        /// Constructs a new builder.
        /// </summary>
        /// <param name="initialize">Pass <c>true</c>.</param>
        public LargeArrayBuilder(bool initialize)
        {
            // This is a workaround for C# not having parameterless struct constructors yet.
            // Once it gets them, replace this with a parameterless constructor.
            Debug.Assert(initialize);

            _first = _current = Array.Empty<T>();
            _buffers = default(ArrayBuilder<T[]>);
            _index = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets the number of items added to the builder.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use <see cref="Add"/> if adding to the builder is a bottleneck for your use case.
        /// Otherwise, use <see cref="SlowAdd"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (_index == _current.Length)
            {
                AllocateBuffer();
            }

            _current[_index++] = item;
            _count++;
        }

        /// <summary>
        /// Adds a range of items to this builder.
        /// </summary>
        /// <param name="items">The sequence to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            Debug.Assert(items != null);

            using (IEnumerator<T> enumerator = items.GetEnumerator())
            {
                T[] destination = _current;
                int index = _index;

                // Continuously read in items from the enumerator, updating _count
                // and _index when we run out of space.

                while (enumerator.MoveNext())
                {
                    if (index == destination.Length)
                    {
                        // No more space in this buffer. Resize.
                        _count += index - _index;
                        _index = index;
                        AllocateBuffer();
                        destination = _current;
                        index = _index; // May have been reset to 0
                    }

                    destination[index++] = enumerator.Current;
                }

                // Final update to _count and _index.
                _count += index - _index;
                _index = index;
            }
        }

        /// <summary>
        /// Copies the contents of this builder to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in <see cref="array"/> to start copying.</param>
        /// <param name="count">The number of items to copy.</param>
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            Debug.Assert(array != null);
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0 && count <= Count);
            Debug.Assert(array.Length - arrayIndex >= count);
            
            for (int i = -1; count > 0; i++)
            {
                // Find the buffer we're copying from.
                T[] buffer = i < 0 ? _first : i < _buffers.Count ? _buffers[i] : _current;
                
                // Copy until we satisfy count, or we reach the end of the buffer.
                int toCopy = Math.Min(count, buffer.Length);
                Array.Copy(buffer, 0, array, arrayIndex, toCopy);

                // Increment variables to that position.
                count -= toCopy;
                arrayIndex += toCopy;
            }
        }

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use <see cref="Add"/> if adding to the builder is a bottleneck for your use case.
        /// Otherwise, use <see cref="SlowAdd"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SlowAdd(T item) => Add(item);

        /// <summary>
        /// Returns an array representation of this builder.
        /// </summary>
        public T[] ToArray()
        {
            if (_count == _first.Length)
            {
                // No resizing to do.
                return _first;
            }

            var array = new T[_count];
            CopyTo(array, 0, _count);
            return array;
        }
        
        private void AllocateBuffer()
        {
            // - On the first few adds, simply resize _first.
            // - When we pass ResizeLimit, allocate ResizeLimit elements for _current
            //   and start reading into _current. Set _index to 0.
            // - When _current runs out of space, add it to _buffers and repeat the
            //   above step, except with _current.Length * 2.

            Debug.Assert(_index == _current.Length, $"{nameof(AllocateBuffer)} was called, but there's more space.");

            // If _count is int.MinValue, we want to go down the other path which will raise an exception.
            if ((uint)_count < (uint)ResizeLimit)
            {
                // We haven't passed ResizeLimit. Resize _first, copying over the previous items.
                Debug.Assert(_current == _first && _count == _first.Length);

                int nextCapacity = _count == 0 ? StartingCapacity : _count * 2;

                _current = new T[nextCapacity];
                Array.Copy(_first, 0, _current, 0, _count);
                _first = _current;
            }
            else
            {
                Debug.Assert(_count == ResizeLimit ^ _current != _first);

                int nextCapacity;
                if (_count == ResizeLimit)
                {
                    nextCapacity = ResizeLimit;
                }
                else
                {
                    _buffers.Add(_current);
                    nextCapacity = _current.Length * 2;
                }

                _current = new T[nextCapacity];
                _index = 0;
            }
        }
    }
}
