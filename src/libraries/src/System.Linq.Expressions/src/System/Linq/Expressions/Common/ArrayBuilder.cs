// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Utility to create and populate arrays in a left-to-right manner.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    internal struct ArrayBuilder<T>
    {
        private readonly T[] _array;
        private int _count;

        /// <summary>
        /// Creates a new array of the specified length.
        /// </summary>
        /// <param name="length">The length of the array.</param>
        public ArrayBuilder(int length)
        {
            Debug.Assert(length > 0);

            _array = new T[length];
            _count = 0;
        }

        /// <summary>
        /// Adds an element to the next slot in the array and advances the cursor to the next slot.
        /// </summary>
        /// <param name="item">The element to add to the array.</param>
        public void Add(T item)
        {
            _array[_count++] = item;
        }

        /// <summary>
        /// Returns the array that was built.
        /// Note that this method does not create a copy of the array.
        /// </summary>
        public T[] ToArray()
        {
            Debug.Assert(_count == _array.Length);
            return _array;
        }

        /// <summary>
        /// Returns a read-only collection wrapper around the array that was built.
        /// Note that this method does not create a copy of the array.
        /// </summary>
        public ReadOnlyCollection<T> ToReadOnly()
        {
            Debug.Assert(_count == _array.Length);
            return new TrueReadOnlyCollection<T>(_array);
        }
    }
}
