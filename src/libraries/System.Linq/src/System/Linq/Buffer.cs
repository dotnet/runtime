// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// A buffer into which the contents of an <see cref="IEnumerable{TElement}"/> can be stored.
    /// </summary>
    /// <typeparam name="TElement">The type of the buffer's elements.</typeparam>
    internal readonly struct Buffer<TElement>
    {
        /// <summary>
        /// The stored items.
        /// </summary>
        internal readonly TElement[] _items;
        
        /// <summary>
        /// The number of stored items.
        /// </summary>
        internal readonly int _count;

        /// <summary>
        /// Fully enumerates the provided enumerable and stores its items into an array.
        /// </summary>
        /// <param name="source">The enumerable to be store.</param>
        internal Buffer(IEnumerable<TElement> source)
        {
            if (source is IIListProvider<TElement> iterator)
            {
                TElement[] array = iterator.ToArray();
                _items = array;
                _count = array.Length;
            }
            else
            {
                _items = EnumerableHelpers.ToArray(source, out _count);
            }
        }
    }
}
