// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ImmutableArray.cs
//
//
// An immutable data structure that supports adding, removing, and enumerating elements.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks.Dataflow.Internal
{
    // NOTE: We use a very simple ImmutableArray implementation rather than taking a dependency
    // on the full System.Collections.Immutable.dll, which contains much more than we need
    // for such a simple purpose.

    /// <summary>Provides a simple, immutable array.</summary>
    /// <typeparam name="T">Specifies the type of the data stored in the array.</typeparam>
    [DebuggerDisplay("Count={Count}")]
    internal readonly struct ImmutableArray<T>
    {
        /// <summary>An empty array.</summary>
        private static readonly ImmutableArray<T> s_empty = new ImmutableArray<T>(new T[0]);
        /// <summary>The immutable data in this array instance.</summary>
        private readonly T[] _array;

        /// <summary>Gets the empty array.</summary>
        public static ImmutableArray<T> Empty { get { return s_empty; } }

        /// <summary>Initializes the immutable array with the specified elements.</summary>
        /// <param name="elements">The element array to use for this array's data.</param>
        private ImmutableArray(T[] elements)
        {
            Debug.Assert(elements != null, "Requires an array to wrap.");
            _array = elements;
        }

        /// <summary>Creates a new immutable array from this array and the additional element.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> Add(T item)
        {
            // Copy the elements from this array and the item
            // to a new array that's returned.
            var newArray = new T[_array.Length + 1];
            Array.Copy(_array, 0, newArray, 0, _array.Length);
            newArray[newArray.Length - 1] = item;
            return new ImmutableArray<T>(newArray);
        }

        /// <summary>Creates a new immutable array from this array and without the specified element.</summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> Remove(T item)
        {
            // Get the index of the element.  If it's not in the array, just return this array.
            int index = Array.IndexOf(_array, item);
            if (index < 0) return this;

            // It's in the array, so if it's the only one, just return the empty array
            if (_array.Length == 1) return Empty;

            // Otherwise, copy the other elements to a new array that's returned.
            var newArray = new T[_array.Length - 1];
            Array.Copy(_array, 0, newArray, 0, index);
            Array.Copy(_array, index + 1, newArray, index, _array.Length - index - 1);
            return new ImmutableArray<T>(newArray);
        }

        /// <summary>Gets the number of elements in this array.</summary>
        public int Count { get { return _array.Length; } }

        /// <summary>Gets whether the array contains the specified item.</summary>
        /// <param name="item">The item to lookup.</param>
        /// <returns>true if the array contains the item; otherwise, false.</returns>
        public bool Contains(T item) { return Array.IndexOf(_array, item) >= 0; }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }

        /// <summary>Returns the contents of the collection as an array.</summary>
        /// <returns>An array containing the contents of this collection.</returns>
        public T[] ToArray() { return _array.Length == 0 ? s_empty._array : (T[])_array.Clone(); }
    }
}
