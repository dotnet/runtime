// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Describes an ordered collection of elements.
    /// </summary>
    /// <typeparam name="T">The type of element in the collection.</typeparam>
    internal interface IOrderedCollection<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the element in the collection at a given index.
        /// </summary>
        T this[int index] { get; }
    }
}
