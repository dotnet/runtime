// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a reserved region within a <see cref="SparseArrayBuilder{T}"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal readonly struct Marker
    {
        /// <summary>
        /// Constructs a new marker.
        /// </summary>
        /// <param name="count">The number of items to reserve.</param>
        /// <param name="index">The index in the builder where this marker starts.</param>
        public Marker(int count, int index)
        {
            Debug.Assert(count >= 0);
            Debug.Assert(index >= 0);

            Count = count;
            Index = index;
        }

        /// <summary>
        /// The number of items to reserve.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The index in the builder where this marker starts.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets a string suitable for display in the debugger.
        /// </summary>
        private string DebuggerDisplay => $"{nameof(Index)}: {Index}, {nameof(Count)}: {Count}";
    }

    /// <summary>
    /// Helper type for building arrays where sizes of certain segments are known in advance.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal struct SparseArrayBuilder<T>
    {
        /// <summary>
        /// The underlying builder that stores items from non-reserved regions.
        /// </summary>
        /// <remarks>
        /// This field is a mutable struct; do not mark it readonly.
        /// </remarks>
        private LargeArrayBuilder<T> _builder;

        /// <summary>
        /// The list of reserved regions within this builder.
        /// </summary>
        /// <remarks>
        /// This field is a mutable struct; do not mark it readonly.
        /// </remarks>
        private ArrayBuilder<Marker> _markers;

        /// <summary>
        /// The total number of reserved slots within this builder.
        /// </summary>
        private int _reservedCount;

        /// <summary>
        /// Constructs a new builder.
        /// </summary>
        /// <param name="initialize">Pass <c>true</c>.</param>
        public SparseArrayBuilder(bool initialize)
            : this()
        {
            // Once C# gains parameterless struct constructors, please
            // remove this workaround.
            Debug.Assert(initialize);

            _builder = new LargeArrayBuilder<T>(initialize: true);
        }

        /// <summary>
        /// The total number of items in this builder, including reserved regions.
        /// </summary>
        public int Count => checked(_builder.Count + _reservedCount);
        
        /// <summary>
        /// The list of reserved regions in this builder.
        /// </summary>
        public ArrayBuilder<Marker> Markers => _markers;

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item) => _builder.Add(item);

        /// <summary>
        /// Adds a range of items to this builder.
        /// </summary>
        /// <param name="items">The sequence to add.</param>
        public void AddRange(IEnumerable<T> items) => _builder.AddRange(items);

        /// <summary>
        /// Copies the contents of this builder to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in <see cref="array"/> to start copying to.</param>
        /// <param name="count">The number of items to copy.</param>
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            Debug.Assert(array != null);
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0 && count <= Count);
            Debug.Assert(array.Length - arrayIndex >= count);

            int copied = 0;
            var position = CopyPosition.Start;

            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i];

                // During this iteration, copy until we satisfy `count` or reach the marker.
                int toCopy = Math.Min(marker.Index - copied, count);

                if (toCopy > 0)
                {
                    position = _builder.CopyTo(position, array, arrayIndex, toCopy);

                    arrayIndex += toCopy;
                    copied += toCopy;
                    count -= toCopy;
                }
                
                if (count == 0)
                {
                    return;
                }

                // We hit our marker. Advance until we satisfy `count` or fulfill `marker.Count`.
                int reservedCount = Math.Min(marker.Count, count);

                arrayIndex += reservedCount;
                copied += reservedCount;
                count -= reservedCount;
            }

            if (count > 0)
            {
                // Finish copying after the final marker.
                _builder.CopyTo(position, array, arrayIndex, count);
            }
        }

        /// <summary>
        /// Reserves a region starting from the current index.
        /// </summary>
        /// <param name="count">The number of items to reserve.</param>
        /// <remarks>
        /// This method will not make optimizations if <paramref name="count"/>
        /// is zero; the caller is responsible for doing so. The reason for this
        /// is that the number of markers needs to match up exactly with the number
        /// of times <see cref="Reserve"/> was called.
        /// </remarks>
        public void Reserve(int count)
        {
            Debug.Assert(count >= 0);

            _markers.Add(new Marker(count: count, index: Count));

            checked
            {
                _reservedCount += count;
            }
        }

        /// <summary>
        /// Reserves a region if the items' count can be predetermined; otherwise, adds the items to this builder.
        /// </summary>
        /// <param name="items">The items to reserve or add.</param>
        /// <returns><c>true</c> if the items were reserved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the items' count is predetermined to be 0, no reservation is made and the return value is <c>false</c>.
        /// The effect is the same as if the items were added, since adding an empty collection does nothing.
        /// </remarks>
        public bool ReserveOrAdd(IEnumerable<T> items)
        {
            int itemCount;
            if (EnumerableHelpers.TryGetCount(items, out itemCount))
            {
                if (itemCount > 0)
                {
                    Reserve(itemCount);
                    return true;
                }
            }
            else
            {
                AddRange(items);
            }
            return false;
        }

        /// <summary>
        /// Creates an array from the contents of this builder.
        /// </summary>
        /// <remarks>
        /// Regions created with <see cref="Reserve"/> will be default-initialized.
        /// </remarks>
        public T[] ToArray()
        {
            // If no regions were reserved, there are no 'gaps' we need to add to the array.
            // In that case, we can just call ToArray on the underlying builder.
            if (_markers.Count == 0)
            {
                Debug.Assert(_reservedCount == 0);
                return _builder.ToArray();
            }

            var array = new T[Count];
            CopyTo(array, 0, array.Length);
            return array;
        }
    }
}
