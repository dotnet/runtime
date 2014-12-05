// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ImmutableList.cs
//
//
// An immutable data structure that supports adding, removing, and enumerating elements.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Provides a simple, immutable list.</summary>
    /// <typeparam name="T">Specifies the type of the data stored in the list.</typeparam>
    [DebuggerDisplay("Count={Count}")]
    [DebuggerTypeProxy(typeof(EnumerableDebugView<>))]
    internal sealed class ImmutableList<T> : IEnumerable<T>
    {
        /// <summary>An empty list.</summary>
        private readonly static ImmutableList<T> _empty = new ImmutableList<T>();
        /// <summary>The immutable data in this list instance.</summary>
        private readonly T[] _array;

        /// <summary>Gets the empty list.</summary>
        public static ImmutableList<T> Empty { get { return _empty; } }

        /// <summary>Initializes the immutable list to be empty.</summary>
        private ImmutableList() : this(new T[0]) { }

        /// <summary>Initializes the immutable list with the specified elements.</summary>
        /// <param name="elements">The element array to use for this list's data.</param>
        private ImmutableList(T[] elements)
        {
            Contract.Requires(elements != null, "List requires an array to wrap.");
            _array = elements;
        }

        /// <summary>Creates a new immutable list from this list and the additional element.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns>The new list.</returns>
        public ImmutableList<T> Add(T item)
        {
            // Copy the elements from this list and the item
            // to a new list that's returned.
            var newArray = new T[_array.Length + 1];
            Array.Copy(_array, 0, newArray, 0, _array.Length);
            newArray[newArray.Length - 1] = item;
            return new ImmutableList<T>(newArray);
        }

        /// <summary>Creates a new immutable list from this list and without the specified element.</summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>The new list.</returns>
        public ImmutableList<T> Remove(T item)
        {
            // Get the index of the element.  If it's not in the list, just return this list.
            int index = Array.IndexOf(_array, item);
            if (index < 0) return this;

            // It's in the list, so if it's the only one, just return the empty list
            if (_array.Length == 1) return Empty;

            // Otherwise, copy the other elements to a new list that's returned.
            var newArray = new T[_array.Length - 1];
            Array.Copy(_array, 0, newArray, 0, index);
            Array.Copy(_array, index + 1, newArray, index, _array.Length - index - 1);
            return new ImmutableList<T>(newArray);
        }

        /// <summary>Gets the number of elements in this list.</summary>
        public int Count { get { return _array.Length; } }

        /// <summary>Gets whether the list contains the specified item.</summary>
        /// <param name="item">The item to lookup.</param>
        /// <returns>true if the list contains the item; otherwise, false.</returns>
        public bool Contains(T item) { return Array.IndexOf(_array, item) >= 0; }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
